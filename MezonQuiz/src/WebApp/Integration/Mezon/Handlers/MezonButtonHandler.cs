using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Application.ManageQuizSession;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Application.ManageQuizSession.Formatters;
using Rt = Mezon.Net.Internal.Realtime;
using WebApp.Integration.Mezon.Services;
using WebApp.Integration.Mezon.Utils;
using WebApp.Domain.Enums;
using WebApp.Application.ManageQuiz.Dtos;

namespace WebApp.Integration.Mezon.Handlers;

public class MezonButtonHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MezonMessageSender _messageSender;
    private readonly MezonBotState _state;
    private readonly ILogger<MezonButtonHandler> _logger;

    public MezonButtonHandler(
        IServiceScopeFactory scopeFactory,
        MezonMessageSender messageSender,
        MezonBotState state,
        ILogger<MezonButtonHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _messageSender = messageSender;
        _state = state;
        _logger = logger;
    }

    public async Task HandleButtonClickedAsync(Rt.MessageButtonClicked clickEvent)
    {
        var buttonId = MezonBotParser.ExtractButtonId(clickEvent);
        var isSubmitAction = MezonBotParser.TryParseQuizSubmitButtonId(buttonId, out var submitSessionId, out var submitQuestionIndex);
        var isOptionAction = MezonBotParser.TryParseQuizButtonId(buttonId, out var optionSessionId, out var optionQuestionIndex, out var selectedOption);
        if (!isSubmitAction && !isOptionAction)
        {
            _logger.LogWarning(
                "Ignored button click because button id format is invalid. ButtonId={ButtonId}, RawButtonId={RawButtonId}, ExtraData={ExtraData}, SenderId={SenderId}, UserId={UserId}",
                buttonId,
                clickEvent.ButtonId,
                clickEvent.ExtraData,
                clickEvent.SenderId,
                clickEvent.UserId);
            return;
        }

        var sessionId = isSubmitAction ? submitSessionId : optionSessionId;
        var questionIndex = isSubmitAction ? submitQuestionIndex : optionQuestionIndex;

        var mezonUserId = ResolveMezonUserId(clickEvent);
        if (string.IsNullOrWhiteSpace(mezonUserId))
        {
            _logger.LogWarning(
                "Ignoring quiz button click with invalid sender. ButtonId={ButtonId}, SenderId={SenderId}, UserId={UserId}",
                buttonId,
                clickEvent.SenderId,
                clickEvent.UserId);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quizSessionService = scope.ServiceProvider.GetRequiredService<IQuizSessionService>();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.MezonUserId == mezonUserId);

            if (user is null)
            {
                var fallbackUsername = $"mezon_{mezonUserId}";
                user = await dbContext.SessionParticipants
                    .Where(p => p.SessionId == sessionId)
                    .Select(p => p.User)
                    .FirstOrDefaultAsync(u => u.Username == fallbackUsername);

                if (user is not null && string.IsNullOrWhiteSpace(user.MezonUserId))
                {
                    user.MezonUserId = mezonUserId;
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Linked fallback local user {UserId} with Mezon user id {SenderId} during button click.",
                        user.Id,
                        mezonUserId);
                }
            }

            if (user is null)
            {
                _logger.LogWarning(
                    "Cannot map Mezon button click sender to local user. SenderId={SenderId}, ButtonId={ButtonId}",
                    mezonUserId,
                    buttonId);
                return;
            }

            var currentQuestionResult = await quizSessionService.GetCurrentQuestion(sessionId, user.Id);
            if (!currentQuestionResult.Result.Success || currentQuestionResult.Question is null)
            {
                await _messageSender.SendDmFeedbackAsync(
                    mezonUserId,
                    QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("Current question is unavailable."));
                return;
            }

            var currentQuestion = currentQuestionResult.Question;
            if (currentQuestion.QuestionIndex != questionIndex)
            {
                await _messageSender.SendDmFeedbackAsync(
                    mezonUserId,
                    QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("Question has changed. Please answer the latest question."));
                return;
            }

            if (currentQuestion.QuestionType == QuestionType.MultipleChoice)
            {
                if (isSubmitAction)
                {
                    if (_state.ShouldSkipDuplicateSubmission(sessionId, user.Id, questionIndex))
                    {
                        _logger.LogDebug(
                            "Skip duplicate multi-choice submission for session {SessionId}, user {UserId}, question {QuestionIndex}.",
                            sessionId,
                            user.Id,
                            questionIndex);
                        return;
                    }

                    var selectionKey = _state.BuildMultiChoiceSelectionKey(sessionId, questionIndex, mezonUserId);
                    if (!_state.TryGetPendingMultiChoiceSelections(selectionKey, out var pendingSelections)
                        || pendingSelections.Count == 0)
                    {
                        await _messageSender.SendDmFeedbackAsync(
                            mezonUserId,
                            QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("Please choose at least one option before submitting."));
                        return;
                    }

                    List<int> selectedIndexes;
                    lock (pendingSelections)
                    {
                        selectedIndexes = pendingSelections
                            .Distinct()
                            .OrderBy(index => index)
                            .ToList();
                    }

                    var multiChoiceSubmitResult = await quizSessionService.SubmitAnswer(sessionId, new SubmitAnswerDto
                    {
                        UserId = user.Id,
                        SelectedOption = selectedIndexes[0],
                        SelectedOptions = selectedIndexes,
                        SkipAutoDispatchNextQuestion = true
                    });

                    if (multiChoiceSubmitResult.Success)
                    {
                        _state.RemovePendingMultiChoiceSelections(selectionKey);
                    }

                    var multiChoiceFeedbackContent = QuizBotMessageFormatter.BuildAnswerFeedbackMessageContent(multiChoiceSubmitResult, questionIndex, selectedIndexes[0]);
                    await _messageSender.SendDmFeedbackAsync(mezonUserId, multiChoiceFeedbackContent);

                    var shouldLockMultiChoiceQuestionMessage = multiChoiceSubmitResult.Success
                        || multiChoiceSubmitResult.Message.Contains("already submitted", StringComparison.OrdinalIgnoreCase);

                    if (shouldLockMultiChoiceQuestionMessage)
                    {
                        await _messageSender.TryLockAnsweredQuestionMessageAsync(
                            clickEvent,
                            currentQuestion,
                            sessionId,
                            questionIndex,
                            mezonUserId);
                    }

                    if (multiChoiceSubmitResult.Success)
                    {
                        if (multiChoiceSubmitResult.ParticipantCompletedQuiz)
                        {
                            await _messageSender.SendParticipantFinishedQuizMessageAsync(mezonUserId, sessionId, user.Id, multiChoiceSubmitResult);
                        }
                        else
                        {
                            await quizSessionService.DispatchCurrentQuestionToParticipant(sessionId, user.Id);
                        }
                    }

                    return;
                }

                var toggledResolvedOption = await ResolveSubmittedOptionIndexAsync(
                    quizSessionService,
                    sessionId,
                    user.Id,
                    questionIndex,
                    selectedOption);

                var selectionKeyForToggle = _state.BuildMultiChoiceSelectionKey(sessionId, questionIndex, mezonUserId);
                var selectedSnapshot = _state.ToggleMultiChoiceSelection(selectionKeyForToggle, toggledResolvedOption);
                var selectedDisplays = selectedSnapshot
                    .Select(index => ResolveDisplayIndex(currentQuestion.Options, index))
                    .Where(index => index > 0)
                    .Distinct()
                    .OrderBy(index => index)
                    .ToList();

                await _messageSender.TryUpdateMultiChoiceQuestionMessageAsync(
                    clickEvent,
                    currentQuestion,
                    selectedDisplays,
                    mezonUserId);
                return;
            }

            if (_state.ShouldSkipDuplicateSubmission(sessionId, user.Id, questionIndex))
            {
                _logger.LogDebug(
                    "Skip duplicate single-choice submission for session {SessionId}, user {UserId}, question {QuestionIndex}.",
                    sessionId,
                    user.Id,
                    questionIndex);
                return;
            }

            var resolvedOption = await ResolveSubmittedOptionIndexAsync(
                quizSessionService,
                sessionId,
                user.Id,
                questionIndex,
                selectedOption);

            var submitResult = await quizSessionService.SubmitAnswer(sessionId, new SubmitAnswerDto
            {
                UserId = user.Id,
                SelectedOption = resolvedOption,
                SelectedOptions = [resolvedOption],
                SkipAutoDispatchNextQuestion = true
            });

            var feedbackContent = QuizBotMessageFormatter.BuildAnswerFeedbackMessageContent(submitResult, questionIndex, selectedOption);
            await _messageSender.SendDmFeedbackAsync(mezonUserId, feedbackContent);

            var shouldLockQuestionMessage = submitResult.Success
                || submitResult.Message.Contains("already submitted", StringComparison.OrdinalIgnoreCase);

            if (shouldLockQuestionMessage)
            {
                await _messageSender.TryLockAnsweredQuestionMessageAsync(
                    clickEvent,
                    currentQuestion,
                    sessionId,
                    questionIndex,
                    mezonUserId);
            }

            if (submitResult.Success)
            {
                if (submitResult.ParticipantCompletedQuiz)
                {
                    await _messageSender.SendParticipantFinishedQuizMessageAsync(mezonUserId, sessionId, user.Id, submitResult);
                }
                else
                {
                    await quizSessionService.DispatchCurrentQuestionToParticipant(sessionId, user.Id);
                }
            }

            _logger.LogInformation(
                "Processed quiz button click. SessionId={SessionId}, QuestionIndex={QuestionIndex}, SelectedOption={SelectedOption}, SenderId={SenderId}, Success={Success}, Message={Message}",
                sessionId,
                questionIndex,
                resolvedOption,
                mezonUserId,
                submitResult.Success,
                submitResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process quiz button click. ButtonId={ButtonId}, SenderId={SenderId}",
                buttonId,
                mezonUserId);

            await _messageSender.SendDmFeedbackAsync(
                mezonUserId,
                QuizBotMessageFormatter.BuildFailureFeedbackMessageContent("System is currently unavailable. Please try again."));
        }
    }

    private static string ResolveMezonUserId(Rt.MessageButtonClicked clickEvent)
    {
        if (clickEvent.UserId > 0)
        {
            return clickEvent.UserId.ToString();
        }

        return string.Empty;
    }

    private static async Task<int> ResolveSubmittedOptionIndexAsync(
        IQuizSessionService quizSessionService,
        Guid sessionId,
        Guid userId,
        int clickedQuestionIndex,
        int selectedOption)
    {
        var currentQuestion = await quizSessionService.GetCurrentQuestion(sessionId, userId);
        if (!currentQuestion.Result.Success || currentQuestion.Question is null)
        {
            return selectedOption;
        }

        if (currentQuestion.Question.QuestionIndex != clickedQuestionIndex)
        {
            return selectedOption;
        }

        var options = currentQuestion.Question.Options ?? [];
        if (selectedOption > 0 && selectedOption <= options.Count)
        {
            return options[selectedOption - 1].Index;
        }

        if (options.Any(option => option.Index == selectedOption))
        {
            return selectedOption;
        }

        return selectedOption;
    }

    private static int ResolveDisplayIndex(IReadOnlyList<QuizSessionQuestionOptionDto> options, int optionIndex)
    {
        for (var position = 0; position < options.Count; position++)
        {
            if (options[position].Index == optionIndex)
            {
                return position + 1;
            }
        }

        return -1;
    }
}
