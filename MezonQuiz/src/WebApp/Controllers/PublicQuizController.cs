using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Dtos;
using WebApp.Application.Interface;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicQuizController : ControllerBase
    {
        private readonly IPublicQuizService _publicQuizService;
        public PublicQuizController(IPublicQuizService publicQuizService)
        {
            _publicQuizService = publicQuizService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPublicQuizzes([FromQuery] QuizListQuery query)
        {
            var quizzes = await _publicQuizService.GetAllPublicQuizzesAsync(query);
            return Ok(quizzes);
        }
    }
}
