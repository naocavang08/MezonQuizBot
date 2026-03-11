using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Authorization;
using WebApp.Data;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _myQuizService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<QuizController> _logger;

        public QuizController(IQuizService myQuizService, AppDbContext dbContext, ILogger<QuizController> logger)
        {
            _myQuizService = myQuizService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        [PermissionAuthorize(PermissionNames.Quizzes.List)]
        public async Task<IActionResult> GetQuizzes([FromQuery] QuizListQuery query)
        {
            var quizzes = await _myQuizService.GetQuizzesAsync(query);
            return Ok(quizzes);
        }

        [HttpGet("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.View)]
        public async Task<IActionResult> GetQuizDetails(Guid quizId)
        {
            var quiz = await _myQuizService.GetQuizDetailsAsync(quizId);
            if (quiz is null)
                return NotFound(new { Message = "Không tìm thấy quiz." });

            return Ok(quiz);
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Quizzes.Create)]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizDto quizData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            quizData.CreatorId = currentUserId;
            var created = await _myQuizService.CreateQuizAsync(quizData);
            if (!created)
                return BadRequest(new { Message = "Dữ liệu quiz không hợp lệ hoặc tạo thất bại." });

            return Ok(new { Message = "Quiz created successfully" });
        }

        [HttpPut("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] QuizDto quizData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var updated = await _myQuizService.UpdateQuizAsync(quizId, quizData);
            if (!updated)
                return BadRequest(new { Message = "Không thể cập nhật quiz." });

            return Ok(new { Message = $"Quiz {quizId} updated successfully" });
        }

        [HttpDelete("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Delete)]
        public async Task<IActionResult> DeleteQuiz(Guid quizId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var deleted = await _myQuizService.DeleteQuizAsync(quizId);
            if (!deleted)
                return NotFound(new { Message = "Không tìm thấy quiz để xóa." });

            return Ok(new { Message = $"Quiz {quizId} deleted successfully" });
        }

        [HttpPost("{quizId}/questions")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] QuizQuestion questionData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var added = await _myQuizService.AddQuestionAsync(quizId, questionData);
            if (!added)
                return BadRequest(new { Message = "Could not add question to quiz." });

            return Ok(new { Message = "Question added successfully" });
        }

        [HttpPut("{quizId}/questions/{questionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuestion(Guid quizId, int questionIndex, [FromBody] QuizQuestion questionData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var updated = await _myQuizService.UpdateQuestionAsync(quizId, questionIndex, questionData);
            if (!updated)
                return BadRequest(new { Message = "Could not update question." });

            return Ok(new { Message = "Question updated successfully" });
        }

        [HttpDelete("{quizId}/questions/{questionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> DeleteQuestion(Guid quizId, int questionIndex)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var deleted = await _myQuizService.DeleteQuestionAsync(quizId, questionIndex);
            if (!deleted)
                return NotFound(new { Message = "Could not delete question." });

            return Ok(new { Message = "Question deleted successfully" });
        }

        [HttpPost("{quizId}/questions/{questionIndex}/options")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> AddOption(Guid quizId, int questionIndex, [FromBody] QuizOption optionData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var added = await _myQuizService.AddOptionAsync(quizId, questionIndex, optionData);
            if (!added)
                return BadRequest(new { Message = "Could not add option to question." });

            return Ok(new { Message = "Option added successfully" });
        }

        [HttpPut("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateOption(Guid quizId, int questionIndex, int optionIndex, [FromBody] QuizOption optionData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var updated = await _myQuizService.UpdateOptionAsync(quizId, questionIndex, optionIndex, optionData);
            if (!updated)
                return BadRequest(new { Message = "Could not update option." });

            return Ok(new { Message = "Option updated successfully" });
        }

        [HttpDelete("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> DeleteOption(Guid quizId, int questionIndex, int optionIndex)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var deleted = await _myQuizService.DeleteOptionAsync(quizId, questionIndex, optionIndex);
            if (!deleted)
                return NotFound(new { Message = "Could not delete option." });

            return Ok(new { Message = "Option deleted successfully" });
        }

        [HttpPut("{quizId}/settings")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuizSettings(Guid quizId, [FromBody] QuizSettings settingsData)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var authorizeResult = await EnsureCanMutateQuizAsync(quizId, currentUserId);
            if (authorizeResult is not null)
            {
                return authorizeResult;
            }

            var updated = await _myQuizService.UpdateQuizSettingsAsync(quizId, settingsData);
            if (!updated)
                return BadRequest(new { Message = "Could not update quiz settings." });

            return Ok(new { Message = "Quiz settings updated successfully" });
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var parsed = Guid.TryParse(userIdClaimValue, out userId);
            if (!parsed)
            {
                _logger.LogWarning("Unauthorized quiz request: missing/invalid NameIdentifier claim.");
            }

            return parsed;
        }

        private async Task<IActionResult?> EnsureCanMutateQuizAsync(Guid quizId, Guid userId)
        {
            var creatorId = await _dbContext.Quizzes
                .AsNoTracking()
                .Where(q => q.Id == quizId)
                .Select(q => (Guid?)q.CreatorId)
                .FirstOrDefaultAsync();

            if (!creatorId.HasValue)
            {
                return NotFound(new { Message = "Không tìm thấy quiz." });
            }

            if (creatorId.Value == userId)
            {
                return null;
            }

            var hasSystemRole = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => r.IsSystem)
                .AnyAsync(isSystem => isSystem);

            if (!hasSystemRole)
            {
                _logger.LogWarning(
                    "Forbidden quiz mutation: UserId={UserId}, QuizId={QuizId}, OwnerId={OwnerId}.",
                    userId,
                    quizId,
                    creatorId.Value);
                return Forbid();
            }

            return null;
        }
    }
}