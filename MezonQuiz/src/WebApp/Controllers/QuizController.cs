using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _myQuizService;
        public QuizController(IQuizService myQuizService)
        {
            _myQuizService = myQuizService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuizzes([FromQuery] Guid? userId = null)
        {
            var quizzes = await _myQuizService.GetQuizzesAsync(userId);
            return Ok(quizzes);
        }

        [HttpGet("{quizId}")]
        public async Task<IActionResult> GetQuizDetails(Guid quizId)
        {
            var quiz = await _myQuizService.GetQuizDetailsAsync(quizId);
            if (quiz is null)
                return NotFound(new { Message = "Không tìm thấy quiz." });

            return Ok(quiz);
        }

        [HttpPost]
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

        [HttpPut("{quizId}")]
        public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] QuizDto quizData)
        {
            var updated = await _myQuizService.UpdateQuizAsync(quizId, quizData);
            if (!updated)
                return BadRequest(new { Message = "Không thể cập nhật quiz." });

            return Ok(new { Message = $"Quiz {quizId} updated successfully" });
        }

        [HttpDelete("{quizId}")]
        public async Task<IActionResult> DeleteQuiz(Guid quizId)
        {
            var deleted = await _myQuizService.DeleteQuizAsync(quizId);
            if (!deleted)
                return NotFound(new { Message = "Không tìm thấy quiz để xóa." });

            return Ok(new { Message = $"Quiz {quizId} deleted successfully" });
        }

        [HttpPost("{quizId}/questions")]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] QuizQuestion questionData)
        {
            var added = await _myQuizService.AddQuestionAsync(quizId, questionData);
            if (!added)
                return BadRequest(new { Message = "Could not add question to quiz." });

            return Ok(new { Message = "Question added successfully" });
        }

        [HttpPut("{quizId}/questions/{questionIndex}")]
        public async Task<IActionResult> UpdateQuestion(Guid quizId, int questionIndex, [FromBody] QuizQuestion questionData)
        {
            var updated = await _myQuizService.UpdateQuestionAsync(quizId, questionIndex, questionData);
            if (!updated)
                return BadRequest(new { Message = "Could not update question." });

            return Ok(new { Message = "Question updated successfully" });
        }

        [HttpDelete("{quizId}/questions/{questionIndex}")]
        public async Task<IActionResult> DeleteQuestion(Guid quizId, int questionIndex)
        {
            var deleted = await _myQuizService.DeleteQuestionAsync(quizId, questionIndex);
            if (!deleted)
                return NotFound(new { Message = "Could not delete question." });

            return Ok(new { Message = "Question deleted successfully" });
        }

        [HttpPost("{quizId}/questions/{questionIndex}/options")]
        public async Task<IActionResult> AddOption(Guid quizId, int questionIndex, [FromBody] QuizOption optionData)
        {
            var added = await _myQuizService.AddOptionAsync(quizId, questionIndex, optionData);
            if (!added)
                return BadRequest(new { Message = "Could not add option to question." });

            return Ok(new { Message = "Option added successfully" });
        }

        [HttpPut("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        public async Task<IActionResult> UpdateOption(Guid quizId, int questionIndex, int optionIndex, [FromBody] QuizOption optionData)
        {
            var updated = await _myQuizService.UpdateOptionAsync(quizId, questionIndex, optionIndex, optionData);
            if (!updated)
                return BadRequest(new { Message = "Could not update option." });

            return Ok(new { Message = "Option updated successfully" });
        }

        [HttpDelete("{quizId}/questions/{questionIndex}/options/{optionIndex}")]
        public async Task<IActionResult> DeleteOption(Guid quizId, int questionIndex, int optionIndex)
        {
            var deleted = await _myQuizService.DeleteOptionAsync(quizId, questionIndex, optionIndex);
            if (!deleted)
                return NotFound(new { Message = "Could not delete option." });

            return Ok(new { Message = "Option deleted successfully" });
        }

        [HttpPut("{quizId}/settings")]
        public async Task<IActionResult> UpdateQuizSettings(Guid quizId, [FromBody] QuizSettings settingsData)
        {
            var updated = await _myQuizService.UpdateQuizSettingsAsync(quizId, settingsData);
            if (!updated)
                return BadRequest(new { Message = "Could not update quiz settings." });

            return Ok(new { Message = "Quiz settings updated successfully" });
        }
    }
}