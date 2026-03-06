using WebApp.Application.Dtos;

namespace WebApp.Application.Interface
{
    public interface IMyQuizService
    {
        Task<IEnumerable<ListQuizDto>> GetAllQuizzesAsync();
        Task<IEnumerable<ListQuizDto>> GetMyQuizzesAsync(Guid userId);
        Task<QuizDto?> GetQuizDetailsAsync(Guid quizId);
        Task<bool> CreateQuizAsync(QuizDto quizData);
        Task<bool> UpdateQuizAsync(Guid quizId, QuizDto quizData);
        Task<bool> DeleteQuizAsync(Guid quizId);
        
        // Question operations
        Task<bool> AddQuestionAsync(Guid quizId, QuizQuestion questionData);
        Task<bool> UpdateQuestionAsync(Guid quizId, int questionIndex, QuizQuestion questionData);
        Task<bool> DeleteQuestionAsync(Guid quizId, int questionIndex);
        
        // Option operations
        Task<bool> AddOptionAsync(Guid quizId, int questionIndex, QuizOption optionData);
        Task<bool> UpdateOptionAsync(Guid quizId, int questionIndex, int optionIndex, QuizOption optionData);
        Task<bool> DeleteOptionAsync(Guid quizId, int questionIndex, int optionIndex);

        // Setting Options
        Task<bool> UpdateQuizSettingsAsync(Guid quizId, QuizSettings settingsData);
    }
}