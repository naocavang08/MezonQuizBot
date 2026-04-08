using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Auth.Authorization;
using WebApp.Application.ManageQuizSession.Dtos;

namespace WebApp.Application.ManageQuizSession
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizSessionController : ControllerBase
    {
        private readonly IQuizSessionService _sessionService;
        private readonly ILogger<QuizSessionController> _logger;

        public QuizSessionController(IQuizSessionService sessionService, ILogger<QuizSessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpGet]
        [PermissionAuthorize(PermissionNames.Sessions.List)]
        public async Task<IActionResult> GetAllSessions(Guid? quizId)
        {
            if (quizId == null) {
                return BadRequest(new { Message = "Quiz ID is required." });
            }
            var sessions = await _sessionService.GetAllSessions(quizId);
            return Ok(sessions);
        }

        [HttpGet("{sessionId}")]
        [PermissionAuthorize(PermissionNames.Sessions.View)]
        public async Task<IActionResult> GetSession(Guid sessionId)
        {
            var session = await _sessionService.GetSession(sessionId);
            if (session is null)
                return NotFound(new { Message = "Session not found." });

            return Ok(session);
        }

        [HttpPost]
        [PermissionAuthorize(PermissionNames.Sessions.Create)]
        public async Task<IActionResult> CreateSession([FromBody] CreateQuizSessionDto request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var (result, session) = await _sessionService.CreateSession(request, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message, Session = session });
        }

        [HttpPost("{sessionId}/clear")]
        public async Task<IActionResult> ClearParticipant(Guid sessionId, [FromBody] ClearParticipantDto request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.ClearParticipant(sessionId, currentUserId, request);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/start")]
        [PermissionAuthorize(PermissionNames.Sessions.Start)]
        public async Task<IActionResult> StartSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.StartSession(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/pause")]
        public async Task<IActionResult> PauseSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.PauseSession(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/resume")]
        public async Task<IActionResult> ResumeSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.ResumeSession(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/finish")]
        [PermissionAuthorize(PermissionNames.Sessions.End)]
        public async Task<IActionResult> FinishSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.FinishSession(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/cancel")]
        public async Task<IActionResult> CancelSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.CancelSession(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpDelete("{sessionId}")]
        [PermissionAuthorize(PermissionNames.Sessions.Delete)]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.DeleteSession(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/next-question")]
        public async Task<IActionResult> NextQuestion(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.NextQuestion(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("{sessionId}/current-question")]
        public async Task<IActionResult> GetCurrentQuestion(Guid sessionId)
        {
            var (result, question) = await _sessionService.GetCurrentQuestion(sessionId);
            if (!result.Success)
            {
                return BadRequest(new { Message = result.Message });
            }

            return Ok(question);
        }

        [HttpPost("{sessionId}/answers")]
        public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] SubmitAnswerDto request)
        {
            var result = await _sessionService.SubmitAnswer(sessionId, request);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("{sessionId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(Guid sessionId)
        {
            var leaderboard = await _sessionService.GetLeaderboard(sessionId);
            return Ok(leaderboard);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdClaimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var parsed = Guid.TryParse(userIdClaimValue, out userId);
            if (!parsed)
            {
                _logger.LogWarning("Unauthorized quiz session request: missing/invalid NameIdentifier claim.");
            }

            return parsed;
        }
    }
}
