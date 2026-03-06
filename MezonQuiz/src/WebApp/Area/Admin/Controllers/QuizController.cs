using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Interface;

namespace WebApp.Area.Admin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IMyQuizService _myQuizService;
        public QuizController(IMyQuizService myQuizService)
        {
            _myQuizService = myQuizService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuizzes()
        {
            var quizzes = await _myQuizService.GetAllQuizzesAsync();
            return Ok(quizzes);
        }
    }
}
