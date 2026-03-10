using WebApp.Application.Dtos;

namespace WebApp.Application.Interface
{
    public interface IPublicQuizService
    {
        Task<PagedQuizListDto<PublicQuizDto>> GetAllPublicQuizzesAsync(QuizListQuery query);
        
    }
}
