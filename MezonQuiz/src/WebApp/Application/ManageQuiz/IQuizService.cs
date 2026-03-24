using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Domain.Entites;

namespace WebApp.Application.ManageQuiz
{
    public interface IQuizService
    {
        // Player operations
        Task<PagingDto<AvailableQuizDto>> GetAllAvailableQuizzes(Guid? userId, QuizQuery input);
        Task<AvailableQuizDto?> GetAvailableQuiz(Guid quizId);

        // Creator operations
        Task<PagingDto<QuizDto>> GetAllQuizzes(Guid userId, QuizQuery input);
        Task<Quiz?> GetQuiz(Guid quizId);

        // Quiz operations
        Task<bool> CreateQuiz(Guid userId, SaveQuizDto input);
        Task<bool> UpdateQuiz(Guid userId, Guid quizId, SaveQuizDto input);
        Task<bool> DeleteQuiz(Quiz quiz);
        
        // Question operations
        Task<bool> AddQuestion(Guid quizId, QuizQuestion questionData);
        Task<bool> UpdateQuestion(Guid quizId, int questionIndex, QuizQuestion questionData);
        Task<bool> DeleteQuestion(Guid quizId, int questionIndex);
        
        // Option operations
        Task<bool> AddOption(Guid quizId, int questionIndex, QuizOption optionData);
        Task<bool> UpdateOption(Guid quizId, int questionIndex, int optionIndex, QuizOption optionData);
        Task<bool> DeleteOption(Guid quizId, int questionIndex, int optionIndex);

        // Setting Options
        Task<bool> UpdateQuizSettings(Guid quizId, QuizSettings settingsData);
    }
}