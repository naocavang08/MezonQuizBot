using WebApp.Application.Dtos;

namespace WebApp.Application.Interface
{
    public interface IMyQuizService
    {
        Task<IEnumerable<ListQuizDto>> GetMyQuizzesAsync(Guid userId);
        Task<QuizDto?> GetQuizDetailsAsync(Guid quizId);
        Task<bool> CreateQuizAsync(QuizDto quizData);
        Task<bool> UpdateQuizAsync(Guid quizId, QuizDto quizData);
        Task<bool> DeleteQuizAsync(Guid quizId);
    }
}