using WebApp.Application.ManageQuiz.Dtos;
using WebApp.Domain.Entites;
using Microsoft.AspNetCore.Http;

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
        Task<QuizDto> CreateQuiz(Guid userId, SaveQuizDto input);
        Task<QuizDto> UpdateQuiz(Guid userId, Guid quizId, SaveQuizDto input);
        Task DeleteQuiz(Quiz quiz);
        
        // Question operations
        Task<QuizQuestion> AddQuestion(Guid quizId, QuizQuestion questionData);
        Task<QuizQuestion> UpdateQuestion(Guid quizId, int questionIndex, QuizQuestion questionData);
        Task DeleteQuestion(Guid quizId, int questionIndex);
        
        // Option operations
        Task<QuizOption> AddOption(Guid quizId, int questionIndex, QuizOption optionData);
        Task<QuizOption> UpdateOption(Guid quizId, int questionIndex, int optionIndex, QuizOption optionData);
        Task DeleteOption(Guid quizId, int questionIndex, int optionIndex);
        
        // Setting Options
        Task<QuizSettings> UpdateQuizSettings(Guid quizId, QuizSettings settingsData);

        // Media operations
        Task<(bool Success, string Message, string? Url, string? Markdown)> UploadQuestionMedia(IFormFile? file, HttpRequest request);
    }
}