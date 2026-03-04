using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;

namespace WebApp.Area.User.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyQuizController : ControllerBase
    {
        private readonly IMyQuizService _myQuizService;
        public MyQuizController(IMyQuizService myQuizService)
        {
            _myQuizService = myQuizService;
        }

        [HttpGet("quizzes")]
        public async Task<IActionResult> GetMyQuizzes([FromQuery] Guid? userId = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var claimUserId))
            {
                if (userId is null || userId == Guid.Empty)
                    return Unauthorized(new { Message = "Không xác định được userId." });

                claimUserId = userId.Value;
            }

            var quizzes = await _myQuizService.GetMyQuizzesAsync(claimUserId);
            return Ok(quizzes);
        }

        [HttpGet("quizzes/{quizId}")]
        public async Task<IActionResult> GetQuizDetails(Guid quizId)
        {
            var quiz = await _myQuizService.GetQuizDetailsAsync(quizId);
            if (quiz is null)
                return NotFound(new { Message = "Không tìm thấy quiz." });

            return Ok(quiz);
        }

        [HttpPost("quizzes")]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizDto quizData, [FromQuery] Guid? userId = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var claimUserId))
            {
                if (userId is null || userId == Guid.Empty)
                    return Unauthorized(new { Message = "Không xác định được userId." });

                claimUserId = userId.Value;
            }

            quizData.CreatorId = claimUserId;
            var created = await _myQuizService.CreateQuizAsync(quizData);
            if (!created)
                return BadRequest(new { Message = "Dữ liệu quiz không hợp lệ hoặc tạo thất bại." });

            return Ok(new { Message = "Quiz created successfully" });
        }

        [HttpPut("quizzes/{quizId}")]
        public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] QuizDto quizData)
        {
            var updated = await _myQuizService.UpdateQuizAsync(quizId, quizData);
            if (!updated)
                return BadRequest(new { Message = "Không thể cập nhật quiz." });

            return Ok(new { Message = $"Quiz {quizId} updated successfully" });
        }

        [HttpDelete("quizzes/{quizId}")]
        public async Task<IActionResult> DeleteQuiz(Guid quizId)
        {
            var deleted = await _myQuizService.DeleteQuizAsync(quizId);
            if (!deleted)
                return NotFound(new { Message = "Không tìm thấy quiz để xóa." });

            return Ok(new { Message = $"Quiz {quizId} deleted successfully" });
        }
    }
}