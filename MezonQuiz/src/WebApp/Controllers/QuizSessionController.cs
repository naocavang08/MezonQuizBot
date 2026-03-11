using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizSessionController : ControllerBase
    {
        private readonly IQuizSessionService _sessionService;

        public QuizSessionController(IQuizSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSessions([FromQuery] QuizSessionQuery query)
        {
            var sessions = await _sessionService.GetSessionsAsync(query);
            return Ok(sessions);
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetSessionDetails(Guid sessionId)
        {
            var session = await _sessionService.GetSessionDetailsAsync(sessionId);
            if (session is null)
                return NotFound(new { Message = "Session not found." });

            return Ok(session);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateQuizSessionDto request)
        {
            var (result, session) = await _sessionService.CreateSessionAsync(request);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message, Session = session });
        }

        [HttpPost("{sessionId}/join")]
        public async Task<IActionResult> JoinSession(Guid sessionId, [FromBody] JoinQuizSessionDto request)
        {
            var result = await _sessionService.JoinSessionAsync(sessionId, request);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/start")]
        public async Task<IActionResult> StartSession(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.StartSessionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/pause")]
        public async Task<IActionResult> PauseSession(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.PauseSessionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/resume")]
        public async Task<IActionResult> ResumeSession(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.ResumeSessionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/finish")]
        public async Task<IActionResult> FinishSession(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.FinishSessionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/cancel")]
        public async Task<IActionResult> CancelSession(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.CancelSessionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.DeleteSessionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/next-question")]
        public async Task<IActionResult> NextQuestion(Guid sessionId, [FromQuery] Guid hostId)
        {
            var result = await _sessionService.NextQuestionAsync(sessionId, hostId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/answers")]
        public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] SubmitAnswerDto request)
        {
            var result = await _sessionService.SubmitAnswerAsync(sessionId, request);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("{sessionId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(Guid sessionId)
        {
            var leaderboard = await _sessionService.GetLeaderboardAsync(sessionId);
            return Ok(leaderboard);
        }
    }
}
