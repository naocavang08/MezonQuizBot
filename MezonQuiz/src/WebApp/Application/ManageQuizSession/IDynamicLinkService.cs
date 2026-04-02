using WebApp.Application.ManageQuizSession.Dtos;

namespace WebApp.Application.ManageQuizSession
{
    public interface IDynamicLinkService
    {
        SessionLinksDto BuildSessionLinks(Guid sessionId);
    }
}
