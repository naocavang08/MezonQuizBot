using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Application.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Application.ManageQuiz
{
    public class UploadQuestionMediaRequest
    {
        public IFormFile? File { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly ILogger<QuizController> _logger;

        public QuizController(
            IQuizService quizService,
            ILogger<QuizController> logger)
        {
            _quizService = quizService;
            _logger = logger;
        }

        [HttpGet("available-quiz")]
        public async Task<IActionResult> GetAllAvailableQuizzes([FromQuery] QuizQuery input)
        {
            Guid? viewerId = null;
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaimValue, out var parsedViewerId))
            {
                viewerId = parsedViewerId;
            }

            var quizzes = await _quizService.GetAllAvailableQuizzes(viewerId, input);
            return Ok(quizzes);
        }

        [HttpGet("available-quiz/{quizId}")]
        public async Task<IActionResult> GetAvailableQuiz(Guid quizId)
        {
            var quiz = await _quizService.GetAvailableQuiz(quizId);
            if (quiz == null)
                return NotFound(new { Message = "Quiz not found." });
            return Ok(quiz);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionNames.Quizzes.Creator_List, PermissionNames.Quizzes.Admin_List)]
        public async Task<IActionResult> GetAllQuizzes([FromQuery] QuizQuery input)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized quiz list request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }
            var quizzes = await _quizService.GetAllQuizzes(userId, input);
            return Ok(quizzes);
        }

        [HttpGet("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Creator_View, PermissionNames.Quizzes.Admin_View)]
        public async Task<IActionResult> GetQuiz(Guid quizId)
        {
            var quiz = await _quizService.GetQuiz(quizId);
            if (quiz == null)
                return NotFound(new { Message = "Quiz not found." });

            return Ok(quiz);
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Quizzes.Create)]
        public async Task<IActionResult> CreateQuiz([FromBody] SaveQuizDto input)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized quiz list request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var created = await _quizService.CreateQuiz(userId, input);
                return CreatedAtAction(nameof(GetQuiz), new { quizId = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] SaveQuizDto input)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized quiz update request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var updated = await _quizService.UpdateQuiz(userId, quizId, input);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden quiz update request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating quiz {QuizId} by user {UserId}.", quizId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Unexpected server error while updating quiz." });
            }
        }

        [HttpDelete("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Delete)]
        public async Task<IActionResult> DeleteQuiz(Guid quizId)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized quiz delete request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                await _quizService.DeleteQuiz(userId, quizId);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden quiz delete request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpPost("{quizId}/questions")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] QuizQuestion questionData)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized add question request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var added = await _quizService.AddQuestion(userId, quizId, questionData);
                return Ok(added);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden add question request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpPut("{quizId}/questions/{questionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuestion(Guid quizId, int questionIndex, [FromBody] QuizQuestion questionData)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized update question request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var updated = await _quizService.UpdateQuestion(userId, quizId, questionIndex, questionData);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden update question request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpDelete("{quizId}/questions/{questionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> DeleteQuestion(Guid quizId, int questionIndex)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized delete question request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                await _quizService.DeleteQuestion(userId, quizId, questionIndex);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden delete question request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpPost("{quizId}/questions/{questionIndex}/options")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> AddOption(Guid quizId, int questionIndex, [FromBody] QuizOption optionData)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized add option request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var added = await _quizService.AddOption(userId, quizId, questionIndex, optionData);
                return Ok(added);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden add option request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpPut("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateOption(Guid quizId, int questionIndex, int optionIndex, [FromBody] QuizOption optionData)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized update option request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var updated = await _quizService.UpdateOption(userId, quizId, questionIndex, optionIndex, optionData);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden update option request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpDelete("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> DeleteOption(Guid quizId, int questionIndex, int optionIndex)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized delete option request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                await _quizService.DeleteOption(userId, quizId, questionIndex, optionIndex);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden delete option request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpPut("{quizId}/settings")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuizSettings(Guid quizId, [FromBody] QuizSettings settingsData)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized update quiz settings request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            try
            {
                var updated = await _quizService.UpdateQuizSettings(userId, quizId, settingsData);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden update quiz settings request for quiz {QuizId} by user {UserId}.", quizId, userId);
                return Forbid();
            }
        }

        [HttpPost("upload-media")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UploadQuestionMedia([FromForm] UploadQuestionMediaRequest request)
        {
            var result = await _quizService.UploadQuestionMedia(request.File, Request);
            if (!result.Success)
            {
                return BadRequest(new { Message = result.Message });
            }

            return Ok(new { Url = result.Url, Markdown = result.Markdown });
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaimValue, out userId);
        }
    }
}
