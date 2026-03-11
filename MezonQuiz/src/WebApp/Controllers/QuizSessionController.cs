using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;
using WebApp.Authorization;

namespace WebApp.Controllers
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
        public async Task<IActionResult> GetSessions([FromQuery] QuizSessionQuery query)
        {
            var sessions = await _sessionService.GetSessionsAsync(query);
            return Ok(sessions);
        }

        [HttpGet("{sessionId}")]
        [PermissionAuthorize(PermissionNames.Sessions.View)]
        public async Task<IActionResult> GetSessionDetails(Guid sessionId)
        {
            var session = await _sessionService.GetSessionDetailsAsync(sessionId);
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

            var (result, session) = await _sessionService.CreateSessionAsync(request, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message, Session = session });
        }

        [HttpPost("{sessionId}/join")]
        [PermissionAuthorize(PermissionNames.Sessions.View)]
        public async Task<IActionResult> JoinSession(Guid sessionId, [FromBody] JoinQuizSessionDto request)
        {
            var result = await _sessionService.JoinSessionAsync(sessionId, request);
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

            var result = await _sessionService.StartSessionAsync(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/pause")]
        [PermissionAuthorize(PermissionNames.Sessions.Start)]
        public async Task<IActionResult> PauseSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.PauseSessionAsync(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/resume")]
        [PermissionAuthorize(PermissionNames.Sessions.Start)]
        public async Task<IActionResult> ResumeSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.ResumeSessionAsync(sessionId, currentUserId);
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

            var result = await _sessionService.FinishSessionAsync(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/cancel")]
        [PermissionAuthorize(PermissionNames.Sessions.End)]
        public async Task<IActionResult> CancelSession(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.CancelSessionAsync(sessionId, currentUserId);
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

            var result = await _sessionService.DeleteSessionAsync(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/next-question")]
        [PermissionAuthorize(PermissionNames.Sessions.Start)]
        public async Task<IActionResult> NextQuestion(Guid sessionId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User identity is invalid or missing." });
            }

            var result = await _sessionService.NextQuestionAsync(sessionId, currentUserId);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{sessionId}/answers")]
        [PermissionAuthorize(PermissionNames.Sessions.View)]
        public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] SubmitAnswerDto request)
        {
            var result = await _sessionService.SubmitAnswerAsync(sessionId, request);
            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("{sessionId}/leaderboard")]
        [PermissionAuthorize(PermissionNames.Sessions.View)]
        public async Task<IActionResult> GetLeaderboard(Guid sessionId)
        {
            var leaderboard = await _sessionService.GetLeaderboardAsync(sessionId);
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
