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

            var quiz = await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
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
            quiz.UpdatedAt = DateTime.UtcNow;

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteQuizAsync(Guid quizId)
        {
            var quiz = await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz is null)
                return false;

            _dbContext.Quizzes.Remove(quiz);
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
    }
}