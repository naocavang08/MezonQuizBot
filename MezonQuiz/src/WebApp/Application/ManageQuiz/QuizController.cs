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
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaimValue, out var userId))
            {
                _logger.LogWarning("Unauthorized quiz list request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }
            var quizzes = await _quizService.GetAllQuizzes(userId, input);
            return Ok(quizzes);
        }

        [HttpGet("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Creator_View)]
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
            if (input is null)
            {
                return BadRequest(new { Message = "Quiz payload is required." });
            }

            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaimValue, out var userId))
            {
                _logger.LogWarning("Unauthorized quiz list request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var created = await _quizService.CreateQuiz(userId, input);
            if (!created)
                return BadRequest(new { Message = "Invalid quiz data or failed to create quiz." });

            return Ok(new { Message = "Quiz created successfully" });
        }

        [HttpPut("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] SaveQuizDto input)
        {
            if (input is null)
            {
                return BadRequest(new { Message = "Quiz payload is required." });
            }

            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaimValue, out var userId))
            {
                _logger.LogWarning("Unauthorized quiz update request: missing/invalid NameIdentifier claim.");
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            bool updated;
            try
            {
                updated = await _quizService.UpdateQuiz(userId, quizId, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating quiz {QuizId} by user {UserId}.", quizId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Unexpected server error while updating quiz." });
            }

            if (!updated)
                return BadRequest(new { Message = "Could not update quiz." });

            return Ok(new { Message = $"Quiz {quizId} updated successfully" });
        }

        [HttpDelete("{quizId}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Delete)]
        public async Task<IActionResult> DeleteQuiz(Guid quizId)
        {
            var quiz = await _quizService.GetQuiz(quizId);
            if (quiz == null)
                return NotFound(new { Message = "Quiz not found." });
            var deleted = await _quizService.DeleteQuiz(quiz);
            if (!deleted)
                return NotFound(new { Message = "Error occurred while deleting the quiz." });

            return Ok(new { Message = $"Quiz {quizId} deleted successfully" });
        }

        [HttpPost("{quizId}/questions")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] QuizQuestion questionData)
        {
            var added = await _quizService.AddQuestion(quizId, questionData);
            if (!added)
                return BadRequest(new { Message = "Could not add question to quiz." });

            return Ok(new { Message = "Question added successfully" });
        }

        [HttpPut("{quizId}/questions/{questionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuestion(Guid quizId, int questionIndex, [FromBody] QuizQuestion questionData)
        {
            var updated = await _quizService.UpdateQuestion(quizId, questionIndex, questionData);
            if (!updated)
                return BadRequest(new { Message = "Could not update question." });

            return Ok(new { Message = "Question updated successfully" });
        }

        [HttpDelete("{quizId}/questions/{questionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> DeleteQuestion(Guid quizId, int questionIndex)
        {
            var deleted = await _quizService.DeleteQuestion(quizId, questionIndex);
            if (!deleted)
                return NotFound(new { Message = "Could not delete question." });

            return Ok(new { Message = "Question deleted successfully" });
        }

        [HttpPost("{quizId}/questions/{questionIndex}/options")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> AddOption(Guid quizId, int questionIndex, [FromBody] QuizOption optionData)
        {
            var added = await _quizService.AddOption(quizId, questionIndex, optionData);
            if (!added)
                return BadRequest(new { Message = "Could not add option to question." });

            return Ok(new { Message = "Option added successfully" });
        }

        [HttpPut("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateOption(Guid quizId, int questionIndex, int optionIndex, [FromBody] QuizOption optionData)
        {
            var updated = await _quizService.UpdateOption(quizId, questionIndex, optionIndex, optionData);
            if (!updated)
                return BadRequest(new { Message = "Could not update option." });

            return Ok(new { Message = "Option updated successfully" });
        }

        [HttpDelete("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> DeleteOption(Guid quizId, int questionIndex, int optionIndex)
        {
            var deleted = await _quizService.DeleteOption(quizId, questionIndex, optionIndex);
            if (!deleted)
                return NotFound(new { Message = "Could not delete option." });

            return Ok(new { Message = "Option deleted successfully" });
        }

        [HttpPut("{quizId}/settings")]
        [PermissionAuthorize(PermissionNames.Quizzes.Update)]
        public async Task<IActionResult> UpdateQuizSettings(Guid quizId, [FromBody] QuizSettings settingsData)
        {
            var updated = await _quizService.UpdateQuizSettings(quizId, settingsData);
            if (!updated)
                return BadRequest(new { Message = "Could not update quiz settings." });

            return Ok(new { Message = "Quiz settings updated successfully" });
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
    }
}
