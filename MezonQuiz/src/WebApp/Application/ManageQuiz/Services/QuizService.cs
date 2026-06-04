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
                    TotalPoints = q.TotalPoints,
                    QuestionCount = q.Questions.Count
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
                TotalPoints = quiz.TotalPoints,
                QuestionCount = quiz.Questions.Count
            };
        }

        public async Task<PagingDto<QuizDto>> GetAllQuizzes(Guid userId, QuizQuery input)
        {
            input ??= new QuizQuery();
            var page = input.Page < 1 ? 1 : input.Page;
            var pageSize = input.PageSize < 1 ? 10 : Math.Min(input.PageSize, 100);

            var normalizedTitle = input.Title?.Trim().ToLower();

            var quizzesQuery = _dbContext.Quizzes
                .AsNoTracking();

            if (input.OnlyMine)
            {
                quizzesQuery = quizzesQuery.Where(q => q.CreatorId == userId);
            }
            else
            {
                quizzesQuery = quizzesQuery.Where(q =>
                    q.CreatorId == userId ||
                    q.Status == QuizStatus.Published
                );
            }

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

        public async Task<QuizDto> CreateQuiz(Guid userId, SaveQuizDto input)
        {
            ArgumentNullException.ThrowIfNull(input);
            input.Validate();

            if (input.CategoryId.HasValue)
            {
                var categoryExists = await _dbContext.QuizCategories.AnyAsync(c => c.Id == input.CategoryId.Value);
                if (!categoryExists)
                {
                    throw new ArgumentException("Category does not exist.", nameof(input.CategoryId));
                }
            }

            var now = DateTime.UtcNow;
            var mappedQuestions = MapQuestions(input.Questions);

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                CreatorId = userId,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CategoryId = input.CategoryId,
                Questions = mappedQuestions,
                TotalPoints = mappedQuestions.Sum(q => q.Points),
                Settings = MapSettings(input.Settings),
                Visibility = input.Visibility,
                Status = input.Status,
                CreatedAt = now,
            };

            _dbContext.Quizzes.Add(quiz);
            await _dbContext.SaveChangesAsync();
            return new QuizDto
            {
                Id = quiz.Id,
                CreatorId = quiz.CreatorId,
                Title = quiz.Title,
                Description = quiz.Description,
                CategoryId = quiz.CategoryId,
                TotalPoints = quiz.TotalPoints,
                Visibility = quiz.Visibility,
                Status = quiz.Status,
                CreatedAt = quiz.CreatedAt,
                UpdatedAt = quiz.UpdatedAt
            };
        }

        public async Task<QuizDto> UpdateQuiz(Guid userId, Guid quizId, SaveQuizDto input)
        {
            ArgumentNullException.ThrowIfNull(input);
            input.Validate();

            if (input.CategoryId.HasValue)
            {
                var categoryExists = await _dbContext.QuizCategories.AnyAsync(c => c.Id == input.CategoryId.Value);
                if (!categoryExists)
                {
                    throw new ArgumentException("Category does not exist.", nameof(input.CategoryId));
                }
            }

            var quiz = await GetQuiz(quizId);

            if (quiz is null)
                throw new ArgumentException("Quiz not found.");

            if (quiz.CreatorId != userId)
                throw new UnauthorizedAccessException("You do not have permission to update this quiz.");

            var mappedQuestions = MapQuestions(input.Questions);

            quiz.Title = input.Title.Trim();
            quiz.Description = input.Description?.Trim();
            quiz.CategoryId = input.CategoryId;
            quiz.Questions = mappedQuestions;
            quiz.TotalPoints = mappedQuestions.Sum(q => q.Points);
            quiz.Settings = MapSettings(input.Settings);
            quiz.Visibility = input.Visibility;
            quiz.Status = input.Status;
            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();

            return new QuizDto
            {
                Id = quiz.Id,
                CreatorId = quiz.CreatorId,
                Title = quiz.Title,
                Description = quiz.Description,
                CategoryId = quiz.CategoryId,
                TotalPoints = quiz.TotalPoints,
                Visibility = quiz.Visibility,
                Status = quiz.Status,
                CreatedAt = quiz.CreatedAt,
                UpdatedAt = quiz.UpdatedAt
            };
        }

        public async Task DeleteQuiz(Quiz quiz)
        {
            _dbContext.Quizzes.Remove(quiz);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<QuizQuestion> AddQuestion(Guid quizId, QuizQuestion questionData)
        {
            ArgumentNullException.ThrowIfNull(questionData);
            questionData.Validate();

            var quiz = await GetQuiz(quizId);
            if (quiz is null)
                throw new ArgumentException("Quiz not found.");

            quiz.Questions.Add(questionData);
            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
            return questionData;
        }

        public async Task<QuizQuestion> UpdateQuestion(Guid quizId, int questionIndex, QuizQuestion questionData)
        {
            ArgumentNullException.ThrowIfNull(questionData);
            questionData.Validate();

            var quiz = await GetQuiz(quizId);
            if (quiz is null)
                throw new ArgumentException("Quiz not found.");

            if (!TryGetQuestionByListIndex(quiz, questionIndex, out _))
                throw new ArgumentException("Question not found.");

            quiz.Questions[questionIndex] = questionData;
            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
            return questionData;
        }

        public async Task DeleteQuestion(Guid quizId, int questionIndex)
        {
            var quiz = await GetQuiz(quizId);
            if (quiz is null)
                throw new ArgumentException("Quiz not found.");

            if (!TryGetQuestionByListIndex(quiz, questionIndex, out _))
                throw new ArgumentException("Question not found.");

            quiz.Questions.RemoveAt(questionIndex);
            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<QuizOption> AddOption(Guid quizId, int questionIndex, QuizOption optionData)
        {
            ArgumentNullException.ThrowIfNull(optionData);
            optionData.Validate();

            var quiz = await GetQuiz(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out var question))
                throw new ArgumentException("Quiz or question not found.");

            question.Options.Add(optionData);
            try
            {
                question.Validate();
            }
            catch (ArgumentException)
            {
                question.Options.RemoveAt(question.Options.Count - 1);
                throw;
            }

            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
            return optionData;
        }

        public async Task<QuizOption> UpdateOption(Guid quizId, int questionIndex, int optionIndex, QuizOption optionData)
        {
            ArgumentNullException.ThrowIfNull(optionData);
            optionData.Validate();

            var quiz = await GetQuiz(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out var question))
                throw new ArgumentException("Quiz or question not found.");

            if (!TryGetOptionByListIndex(question, optionIndex, out _))
                throw new ArgumentException("Option not found.");

            var previousOption = question.Options[optionIndex];
            question.Options[optionIndex] = optionData;

            try
            {
                question.Validate();
            }
            catch (ArgumentException)
            {
                question.Options[optionIndex] = previousOption;
                throw;
            }

            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
            return optionData;
        }

        public async Task DeleteOption(Guid quizId, int questionIndex, int optionIndex)
        {
            var quiz = await GetQuiz(quizId);
            if (quiz is null || !TryGetQuestionByListIndex(quiz, questionIndex, out var question))
                throw new ArgumentException("Quiz or question not found.");

            if (!TryGetOptionByListIndex(question, optionIndex, out var optionToRemove))
                throw new ArgumentException("Option not found.");

            question.Options.RemoveAt(optionIndex);

            try
            {
                question.Validate();
            }
            catch (ArgumentException)
            {
                question.Options.Insert(optionIndex, optionToRemove);
                throw;
            }

            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<QuizSettings> UpdateQuizSettings(Guid quizId, QuizSettings settingsData)
        {
            ArgumentNullException.ThrowIfNull(settingsData);
            settingsData.Validate();

            var quiz = await GetQuiz(quizId);
            if (quiz is null)
                throw new ArgumentException("Quiz not found.");

            quiz.Settings = MapSettings(settingsData);
            UpdateQuizMetadata(quiz);

            await _dbContext.SaveChangesAsync();
            return quiz.Settings;
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
                ShowCorrectAnswer = settings.ShowCorrectAnswer
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

    }
}
