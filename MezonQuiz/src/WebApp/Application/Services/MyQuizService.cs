using WebApp.Application.Interface;
using WebApp.Application.Dtos;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Domain.Entites;

namespace WebApp.Application.Services
{
    public class MyQuizService : IMyQuizService
    {
        private readonly AppDbContext _dbContext;

        public MyQuizService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<ListQuizDto>> GetAllQuizzesAsync()
        {
            var quizzes = await _dbContext.Quizzes
                .AsNoTracking()
                .Select(q => new ListQuizDto
                {
                    Id = q.Id,
                    Title = q.Title
                })
                .ToListAsync();
            return quizzes;
        }

        public async Task<IEnumerable<ListQuizDto>> GetMyQuizzesAsync(Guid userId)
        {
            var quizzes = await _dbContext.Quizzes
                .AsNoTracking()
                .Where(q => q.CreatorId == userId)
                .Select(q => new ListQuizDto
                {
                    Id = q.Id,
                    Title = q.Title
                })
                .ToListAsync();

            return quizzes;
        }

        public async Task<QuizDto?> GetQuizDetailsAsync(Guid quizId)
        {
            var quiz = await _dbContext.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz is null)
                return null;

            return MapQuizToDto(quiz);
        }

        public async Task<bool> CreateQuizAsync(QuizDto quizData)
        {
            if (!IsValidQuizData(quizData))
                return false;

            var now = DateTime.UtcNow;
            var mappedQuestions = MapQuestions(quizData.Questions);

            var quiz = new Quiz
            {
                Id = quizData.Id == Guid.Empty ? Guid.NewGuid() : quizData.Id,
                CreatorId = quizData.CreatorId,
                Title = quizData.Title,
                Description = quizData.Description,
                CategoryId = quizData.CategoryId,
                Questions = mappedQuestions,
                TotalPoints = mappedQuestions.Sum(q => q.Points),
                Settings = MapSettings(quizData.Settings),
                Visibility = quizData.Visibility,
                Status = quizData.Status,
                CreatedAt = quizData.CreatedAt == default ? now : quizData.CreatedAt,
                UpdatedAt = now
            };

            _dbContext.Quizzes.Add(quiz);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuizAsync(Guid quizId, QuizDto quizData)
        {
            if (!IsValidQuizData(quizData))
                return false;

            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null)
                return false;

            var mappedQuestions = MapQuestions(quizData.Questions);

            quiz.Title = quizData.Title;
            quiz.Description = quizData.Description;
            quiz.CategoryId = quizData.CategoryId;
            quiz.Questions = mappedQuestions;
            quiz.TotalPoints = mappedQuestions.Sum(q => q.Points);
            quiz.Settings = MapSettings(quizData.Settings);
            quiz.Visibility = quizData.Visibility;
            quiz.Status = quizData.Status;
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteQuizAsync(Guid quizId)
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null)
                return false;

            _dbContext.Quizzes.Remove(quiz);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddQuestionAsync(Guid quizId, QuizQuestion questionData)
        {
            if (questionData is null || !questionData.IsValid())
                return false;

            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null)
                return false;

            quiz.Questions.Add(questionData);
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuestionAsync(Guid quizId, int questionIndex, QuizQuestion questionData)
        {
            if (questionData is null || !questionData.IsValid())
                return false;

            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out _))
                return false;

            quiz.Questions[questionIndex] = questionData;
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteQuestionAsync(Guid quizId, int questionIndex)
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out _))
                return false;

            quiz.Questions.RemoveAt(questionIndex);
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddOptionAsync(Guid quizId, int questionIndex, QuizOption optionData)
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null || optionData is null || !TryGetQuestionByListIndex(quiz, questionIndex, out var question))
                return false;

            if (!IsValidOptionData(optionData))
                return false;

            question.Options.Add(optionData);
            if (!question.IsValid())
            {
                question.Options.RemoveAt(question.Options.Count - 1);
                return false;
            }

            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateOptionAsync(Guid quizId, int questionIndex, int optionIndex, QuizOption optionData)
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null || optionData is null || !TryGetQuestionByListIndex(quiz, questionIndex, out var question))
                return false;

            if (!TryGetOptionByListIndex(question, optionIndex, out _))
                return false;

            if (!IsValidOptionData(optionData))
                return false;

            var previousOption = question.Options[optionIndex];
            question.Options[optionIndex] = optionData;

            if (!question.IsValid())
            {
                question.Options[optionIndex] = previousOption;
                return false;
            }

            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteOptionAsync(Guid quizId, int questionIndex, int optionIndex)
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out var question))
                return false;

            if (!TryGetOptionByListIndex(question, optionIndex, out var optionToRemove))
                return false;

            question.Options.RemoveAt(optionIndex);

            if (!question.IsValid())
            {
                question.Options.Insert(optionIndex, optionToRemove);
                return false;
            }

            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuizSettingsAsync(Guid quizId, QuizSettings settingsData)
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null || settingsData is null || !settingsData.IsValid())
                return false;

            quiz.Settings = MapSettings(settingsData);
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        private static bool IsValidQuizData(QuizDto quizData)
        {
            if (quizData.CreatorId == Guid.Empty)
                return false;

            if (string.IsNullOrWhiteSpace(quizData.Title))
                return false;

            if (quizData.Settings is null || !quizData.Settings.IsValid())
                return false;

            if (quizData.Questions is null)
                return false;

            return quizData.Questions.All(q => q.IsValid());
        }

        private static QuizDto MapQuizToDto(Quiz quiz)
        {
            return new QuizDto
            {
                Id = quiz.Id,
                CreatorId = quiz.CreatorId,
                Title = quiz.Title,
                Description = quiz.Description,
                CategoryId = quiz.CategoryId,
                Questions = MapQuestions(quiz.Questions),
                TotalPoints = quiz.TotalPoints,
                Settings = MapSettings(quiz.Settings),
                Visibility = quiz.Visibility,
                Status = quiz.Status,
                CreatedAt = quiz.CreatedAt,
                UpdatedAt = quiz.UpdatedAt
            };
        }

        private static List<QuizQuestion> MapQuestions(IEnumerable<QuizQuestion> questions)
        {
            return questions.Select(question => new QuizQuestion
            {
                Id = question.Id,
                Index = question.Index,
                Content = question.Content,
                MediaUrl = question.MediaUrl,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                QuestionType = question.QuestionType,
                Options = question.Options.Select(option => new QuizOption
                {
                    Id = option.Id,
                    Index = option.Index,
                    Content = option.Content,
                    IsCorrect = option.IsCorrect
                }).ToList()
            }).ToList();
        }

        private static QuizSettings MapSettings(QuizSettings settings)
        {
            return new QuizSettings
            {
                ShuffleQuestions = settings.ShuffleQuestions,
                ShuffleOptions = settings.ShuffleOptions,
                ShowCorrectAnswer = settings.ShowCorrectAnswer,
                MaxAttempts = settings.MaxAttempts
            };
        }

        private async Task<Quiz?> GetQuizForUpdateAsync(Guid quizId)
        {
            return await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
        }

        private static void UpdateQuizMetadata(Quiz quiz)
        {
            quiz.TotalPoints = quiz.Questions.Sum(q => q.Points);
            quiz.UpdatedAt = DateTime.UtcNow;
        }

        private static bool TryGetQuestionByListIndex(Quiz quiz, int questionIndex, out QuizQuestion question)
        {
            question = null!;
            if (questionIndex < 0 || questionIndex >= quiz.Questions.Count)
                return false;

            question = quiz.Questions[questionIndex];
            return true;
        }

        private static bool TryGetOptionByListIndex(QuizQuestion question, int optionIndex, out QuizOption option)
        {
            option = null!;
            if (optionIndex < 0 || optionIndex >= question.Options.Count)
                return false;

            option = question.Options[optionIndex];
            return true;
        }

        private static bool IsValidOptionData(QuizOption optionData)
        {
            return !string.IsNullOrWhiteSpace(optionData.Content);
        }
    }
}