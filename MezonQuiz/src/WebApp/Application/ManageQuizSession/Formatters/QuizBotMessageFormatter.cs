using Mezon_sdk.Constants;
using Mezon_sdk.Models;
using Mezon_sdk.Structures;
using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Application.ManageQuizSession.Dtos;
using WebApp.Domain.Entites;
using System.Text.RegularExpressions;

namespace WebApp.Application.ManageQuizSession.Formatters;

public static class QuizBotMessageFormatter
{
    public static ChannelMessageContent BuildQuestionMessageContent(QuizSession session, Quiz quiz, QuizQuestion question)
    {
        var resolvedMediaUrl = ResolveMediaUrl(question.MediaUrl);
        var orderedOptions = (question.Options ?? new List<QuizOption>())
            .OrderBy(option => option.Index)
            .ToList();

        var hasZeroBasedIndex = orderedOptions.Any(option => option.Index == 0);
        var optionLines = orderedOptions
            .Select(option => $"{NormalizeOptionDisplayIndex(option.Index, hasZeroBasedIndex)} - {option.Content}")
            .ToList();

        var totalQuestionCount = quiz.Questions?.Count ?? 0;
        var questionTypeLabel = GetQuestionTypeLabel(question.QuestionType);
        var title = $"[{questionTypeLabel}] {quiz.Title} | Question {session.CurrentQuestion + 1}/{Math.Max(totalQuestionCount, 1)}";
        var optionsBlock = BuildOptionsPseudoCodeBlock(optionLines);
        var instruction = GetInstructionText(question.QuestionType);

        var descriptionSections = new List<string>
        {
            question.Content
        };

        if (!string.IsNullOrWhiteSpace(resolvedMediaUrl))
        {
            descriptionSections.Add($"Media: {resolvedMediaUrl}");
        }

        if (!string.IsNullOrWhiteSpace(optionsBlock))
        {
            descriptionSections.Add(optionsBlock);
        }

        descriptionSections.Add(instruction);

        var panelDescription = string.Join("\n\n", descriptionSections);

        return new ChannelMessageContent
        {
            Text = string.Empty,
            Embed =
            [
                new InteractiveMessageProps
                {
                    Color = GetPanelColor(question.QuestionType),
                    Title = title,
                    Description = panelDescription,
                    Image = BuildEmbedImage(resolvedMediaUrl)
                }
            ],
            Components = BuildOptionButtons(session, question.QuestionType, orderedOptions, hasZeroBasedIndex)
        };
    }

    public static ChannelMessageContent BuildAnsweredQuestionMessageContent(
        QuizSessionQuestionDto? question,
        int fallbackQuestionIndex)
    {
        if (question is null)
        {
            return new ChannelMessageContent
            {
                Text = string.Empty,
                Embed =
                [
                    new InteractiveMessageProps
                    {
                        Color = "#64748B",
                        Title = $"Question {fallbackQuestionIndex + 1}",
                        Description = "Câu hỏi đã được trả lời"
                    }
                ],
                Components = []
            };
        }

        var resolvedMediaUrl = ResolveMediaUrl(question.MediaUrl);
        var orderedOptions = (question.Options ?? [])
            .OrderBy(option => option.Index)
            .ToList();

        var hasZeroBasedIndex = orderedOptions.Any(option => option.Index == 0);
        var optionLines = orderedOptions
            .Select(option => $"{NormalizeOptionDisplayIndex(option.Index, hasZeroBasedIndex)} - {option.Content}")
            .ToList();

        var sections = new List<string>
        {
            question.Content
        };

        if (!string.IsNullOrWhiteSpace(resolvedMediaUrl))
        {
            sections.Add($"Media: {resolvedMediaUrl}");
        }

        var optionBlock = BuildOptionsPseudoCodeBlock(optionLines);
        if (!string.IsNullOrWhiteSpace(optionBlock))
        {
            sections.Add(optionBlock);
        }

        sections.Add("Câu hỏi đã được trả lời");

        return new ChannelMessageContent
        {
            Text = string.Empty,
            Embed =
            [
                new InteractiveMessageProps
                {
                    Color = "#64748B",
                    Title = $"Question {question.QuestionIndex + 1}",
                    Description = string.Join("\n\n", sections),
                    Image = BuildEmbedImage(resolvedMediaUrl)
                }
            ],
            Components = []
        };
    }

    public static ChannelMessageContent BuildAnswerFeedbackMessageContent(
        SessionOperationResult submitResult,
        int fallbackQuestionIndex,
        int fallbackSelectedDisplay)
    {
        if (!submitResult.Success)
        {
            return BuildFailureFeedbackMessageContent($"Cannot submit answer: {submitResult.Message}");
        }

        var isCorrect = submitResult.IsCorrect ?? false;
        var selectedDisplay = submitResult.SelectedOptionDisplay ?? fallbackSelectedDisplay;
        var totalScore = submitResult.TotalScore ?? 0;
        var correctAnswerLabel = FormatOptionList(submitResult.CorrectOptionDisplays);

        var title = isCorrect
            ? $"Correct!!!, you have {totalScore} points"
            : $"Incorrect!!!, The correct answer is {correctAnswerLabel}";

        return new ChannelMessageContent
        {
            Text = string.Empty,
            Embed =
            [
                new InteractiveMessageProps
                {
                    Color = isCorrect ? "#22C55E" : "#EF4444",
                    Title = title,
                }
            ]
        };
    }

    public static ChannelMessageContent BuildFailureFeedbackMessageContent(string message)
    {
        return new ChannelMessageContent
        {
            Text = string.Empty,
            Embed =
            [
                new InteractiveMessageProps
                {
                    Color = "#F59E0B",
                    Title = "Cannot submit answer",
                    Description = message
                }
            ]
        };
    }

    public static int NormalizeOptionDisplayIndex(int optionIndex, bool hasZeroBasedIndex)
    {
        if (hasZeroBasedIndex)
        {
            return optionIndex + 1;
        }

        return optionIndex;
    }

    private static string GetQuestionTypeLabel(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.MultipleChoice => "MULTI CHOICE",
            QuestionType.TrueFalse => "TRUE/FALSE",
            _ => "SINGLE CHOICE"
        };
    }

    private static string GetInstructionText(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.MultipleChoice => "(Chọn môt hoặc nhiểu đáp án đúng tương ứng phía bên dưới!)",
            QuestionType.TrueFalse => "(Chọn Đúng hoặc Sai bằng nút bên dưới!)",
            _ => "(Chọn đáp án đúng tương ứng phía bên dưới!)"
        };
    }

    private static string GetPanelColor(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.MultipleChoice => "#F59E0B",
            QuestionType.TrueFalse => "#06B6D4",
            _ => "#22C55E"
        };
    }

    private static InteractiveMessageMedia? BuildEmbedImage(string? mediaUrl)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new InteractiveMessageMedia
        {
            Url = mediaUrl
        };
    }

    private static string? ResolveMediaUrl(string? mediaValue)
    {
        if (string.IsNullOrWhiteSpace(mediaValue))
        {
            return null;
        }

        var trimmed = mediaValue.Trim();
        var markdownMatch = Regex.Match(trimmed, "!\\[[^\\]]*\\]\\((https?://[^\\s)]+)\\)", RegexOptions.IgnoreCase);
        if (markdownMatch.Success)
        {
            return markdownMatch.Groups[1].Value;
        }

        return trimmed;
    }

    private static string BuildOptionsPseudoCodeBlock(List<string> optionLines)
    {
        if (optionLines.Count == 0)
        {
            return string.Empty;
        }

        var optionsBody = string.Join("\n", optionLines);
        return $"```\n{optionsBody}\n```";
    }

    private static List<MessageActionRow> BuildOptionButtons(
        QuizSession session,
        QuestionType questionType,
        List<QuizOption> options,
        bool hasZeroBasedIndex)
    {
        if (options.Count == 0)
        {
            return [];
        }

        var buttonBuilder = new ButtonBuilder();
        foreach (var option in options)
        {
            var displayIndex = NormalizeOptionDisplayIndex(option.Index, hasZeroBasedIndex);
            var componentId = $"quiz:{session.Id}:q:{session.CurrentQuestion}:a:{displayIndex}";

            buttonBuilder.AddButton(
                componentId: componentId,
                label: displayIndex.ToString(),
                style: ResolveButtonStyle(questionType, displayIndex));
        }

        var components = buttonBuilder
            .Build()
            .Select(component => new MessageComponent
            {
                Type = component["type"],
                ComponentId = component["id"].ToString(),
                Component = component["component"] as Dictionary<string, object>
            })
            .ToList();

        return
        [
            new MessageActionRow
            {
                Components = components
            }
        ];
    }

    private static ButtonMessageStyle ResolveButtonStyle(QuestionType questionType, int displayIndex)
    {
        return questionType switch
        {
            QuestionType.MultipleChoice => ButtonMessageStyle.Secondary,
            QuestionType.TrueFalse => displayIndex == 1 ? ButtonMessageStyle.Success : ButtonMessageStyle.Danger,
            _ => ButtonMessageStyle.Primary
        };
    }

    private static string FormatOptionList(List<int> options)
    {
        if (options.Count == 0)
        {
            return "N/A";
        }

        return string.Join(", ", options.OrderBy(index => index));
    }
}
