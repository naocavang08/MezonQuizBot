using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using WebApp.Data;
using WebApp.Domain.Entites;
using WebApp.Application.ManageQuiz.Dtos;
using static WebApp.Domain.Enums.Status;

namespace WebApp.Application.ManageQuiz.Services
{
    public class QuizService : IQuizService
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public QuizService(AppDbContext dbContext, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public async Task<PagingDto<AvailableQuizDto>> GetAllAvailableQuizzes(Guid? userId, QuizQuery input)
        {
            input ??= new QuizQuery();
            var page = input.Page < 1 ? 1 : input.Page;
            var pageSize = input.PageSize < 1 ? 10 : Math.Min(input.PageSize, 100);

            var normalizedTitle = input.Title?.Trim();
            var hasTitleSearch = !string.IsNullOrWhiteSpace(normalizedTitle);
            var normalizedTitleLower = normalizedTitle?.ToLower();

            var quizzesQuery = _dbContext.Quizzes
                .AsNoTracking()
                .Where(q =>
                    (userId.HasValue && q.CreatorId == userId.Value) ||
                    (q.Status == QuizStatus.Published && (
                        q.Visibility == QuizVisibility.Public ||
                        (hasTitleSearch && q.Visibility == QuizVisibility.Unlisted && q.Title.ToLower() == normalizedTitleLower)
                    ))
                );

            if (input.Category.HasValue)
            {
                quizzesQuery = quizzesQuery.Where(q => q.CategoryId == input.Category.Value);
            }
            if (!string.IsNullOrWhiteSpace(input.Title))
            {
                quizzesQuery = quizzesQuery.Where(q => q.Title.Contains(normalizedTitle!));
            }
            var totalCount = await quizzesQuery.CountAsync();
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var quizzes = await quizzesQuery
                .OrderByDescending(q => q.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new AvailableQuizDto
                {
                    Id = q.Id,
                    CreatorId = q.CreatorId,
                    Title = q.Title,
                    Description = q.Description,
                    CategoryId = q.CategoryId,
                    TotalPoints = q.TotalPoints
                })
                .ToListAsync();
            return new PagingDto<AvailableQuizDto>
            {
                Items = quizzes,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<AvailableQuizDto?> GetAvailableQuiz(Guid quizId)
        {
            var quiz = await _dbContext.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quizId && q.Visibility == QuizVisibility.Public);
            
            if (quiz == null)
                return null;
            return new AvailableQuizDto
            {
                Id = quiz.Id,
                CreatorId = quiz.CreatorId,
                Title = quiz.Title,
                Description = quiz.Description,
                CategoryId = quiz.CategoryId,
                TotalPoints = quiz.TotalPoints
            };
        }

        public async Task<PagingDto<QuizDto>> GetAllQuizzes(Guid userId, QuizQuery input)
        {
            input ??= new QuizQuery();
            var page = input.Page < 1 ? 1 : input.Page;
            var pageSize = input.PageSize < 1 ? 10 : Math.Min(input.PageSize, 100);

            var normalizedTitle = input.Title?.Trim().ToLower();

            var quizzesQuery = _dbContext.Quizzes
                .AsNoTracking()
                .Where(q =>
                    q.CreatorId == userId ||
                    (q.Status == QuizStatus.Published)
                );

            if (input.Category.HasValue)
            {
                quizzesQuery = quizzesQuery.Where(q => q.CategoryId == input.Category.Value);
            }
            if (!string.IsNullOrWhiteSpace(input.Title))
            {
                quizzesQuery = quizzesQuery.Where(q => q.Title.ToLower().Contains(normalizedTitle!));
            }

            var totalCount = await quizzesQuery.CountAsync();
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var quizzes = await quizzesQuery
                .OrderByDescending(q => q.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuizDto
                {
                    Id = q.Id,
                    CreatorId = q.CreatorId,
                    Title = q.Title,
                    Description = q.Description,
                    CategoryId = q.CategoryId,
                    TotalPoints = q.TotalPoints,
                    Visibility = q.Visibility,
                    Status = q.Status,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt
                })
                .ToListAsync();

            return new PagingDto<QuizDto>
            {
                Items = quizzes,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<Quiz?> GetQuiz(Guid quizId)
        {
            var quiz = await _dbContext.Quizzes
                .FirstOrDefaultAsync(q => q.Id == quizId);
            
            if (quiz == null)
                return null;
            return quiz;
        }

        public async Task<bool> CreateQuiz(Guid userId, SaveQuizDto input)
        {
            if (!IsValidinput(input))
                return false;

            var now = DateTime.UtcNow;
            var mappedQuestions = MapQuestions(input.Questions);

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                CreatorId = userId,
                Title = input.Title,
                Description = input.Description,
                CategoryId = input.CategoryId,
                Questions = mappedQuestions,
                TotalPoints = mappedQuestions.Sum(q => q.Points),
                Settings = MapSettings(input.Settings),
                Visibility = input.Visibility,
                Status = input.Status,
                CreatedAt = now,
            };

            _dbContext.Quizzes.Add(quiz);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuiz(Guid userId, Guid quizId, SaveQuizDto input)
        {
            if (!IsValidinput(input))
                return false;

            var quiz = await GetQuiz(quizId);

            if (quiz is null)
                return false;

            if (quiz.CreatorId != userId)
                return false;

            var mappedQuestions = MapQuestions(input.Questions);

            quiz.Title = input.Title;
            quiz.Description = input.Description;
            quiz.CategoryId = input.CategoryId;
            quiz.Questions = mappedQuestions;
            quiz.TotalPoints = mappedQuestions.Sum(q => q.Points);
            quiz.Settings = MapSettings(input.Settings);
            quiz.Visibility = input.Visibility;
            quiz.Status = input.Status;
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteQuiz(Quiz quiz)
        {
            _dbContext.Quizzes.Remove(quiz);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddQuestion(Guid quizId, QuizQuestion questionData)
        {
            if (questionData is null || !questionData.IsValid())
                return false;

            var quiz = await GetQuiz(quizId);
            if (quiz is null)
                return false;

            quiz.Questions.Add(questionData);
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateQuestion(Guid quizId, int questionIndex, QuizQuestion questionData)
        {
            if (questionData is null || !questionData.IsValid())
                return false;

            var quiz = await GetQuiz(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out _))
                return false;

            quiz.Questions[questionIndex] = questionData;
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteQuestion(Guid quizId, int questionIndex)
        {
            var quiz = await GetQuiz(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out _))
                return false;

            quiz.Questions.RemoveAt(questionIndex);
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddOption(Guid quizId, int questionIndex, QuizOption optionData)
        {
            var quiz = await GetQuiz(quizId);
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

        public async Task<bool> UpdateOption(Guid quizId, int questionIndex, int optionIndex, QuizOption optionData)
        {
            var quiz = await GetQuiz(quizId);
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

        public async Task<bool> DeleteOption(Guid quizId, int questionIndex, int optionIndex)
        {
            var quiz = await GetQuiz(quizId);
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

        public async Task<bool> UpdateQuizSettings(Guid quizId, QuizSettings settingsData)
        {
            var quiz = await GetQuiz(quizId);
            if (quiz is null || settingsData is null || !settingsData.IsValid())
                return false;

            quiz.Settings = MapSettings(settingsData);
            UpdateQuizMetadata(quiz);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<(bool Success, string Message, string? Url, string? Markdown)> UploadQuestionMedia(IFormFile? file, HttpRequest request)
        {
            if (file is null || file.Length == 0)
            {
                return (false, "File is required.", null, null);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) ||
                !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Only image files are allowed (.jpg, .jpeg, .png, .webp, .gif, .svg).", null, null);
            }

            var webRootPath = _webHostEnvironment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            }

            var relativeFolder = Path.Combine("uploads", "quiz-media");
            var targetFolder = Path.Combine(webRootPath, relativeFolder);
            Directory.CreateDirectory(targetFolder);

            var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var savePath = Path.Combine(targetFolder, safeFileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            var mediaPath = $"/{relativeFolder.Replace('\\', '/')}/{safeFileName}";
            var configuredBaseUrl = _configuration["Domain:BaseUrl"]?.TrimEnd('/');
            var host = request.Host.HasValue ? request.Host.Value : string.Empty;
            var requestBaseUrl = string.IsNullOrWhiteSpace(host)
                ? string.Empty
                : $"{request.Scheme}://{host}{request.PathBase}".TrimEnd('/');
            var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? configuredBaseUrl
                : requestBaseUrl;
            var absoluteUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? mediaPath
                : $"{baseUrl}{mediaPath}";

            var markdown = $"![quiz-media]({absoluteUrl})";
            return (true, "Upload successful.", absoluteUrl, markdown);
        }

        private static bool IsValidinput(SaveQuizDto? input)
        {
            if (input is null)
                return false;

            if (string.IsNullOrWhiteSpace(input.Title))
                return false;

            if (input.Settings is null || !input.Settings.IsValid())
                return false;

            if (input.Questions is null)
                return false;

            return input.Questions.All(q => q.IsValid());
        }

        private static List<QuizQuestion> MapQuestions(IEnumerable<QuizQuestion>? questions)
        {
            if (questions is null)
            {
                return new List<QuizQuestion>();
            }

            return questions.Select(question => new QuizQuestion
            {
                Id = question.Id,
                Index = question.Index,
                Content = question.Content,
                MediaUrl = question.MediaUrl,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                QuestionType = question.QuestionType,
                Options = (question.Options ?? new List<QuizOption>()).Select(option => new QuizOption
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

        private static void UpdateQuizMetadata(Quiz quiz)
        {
            quiz.Questions ??= new List<QuizQuestion>();
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
