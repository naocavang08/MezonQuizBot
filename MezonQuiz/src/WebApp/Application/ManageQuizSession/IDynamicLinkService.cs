namespace WebApp.Application.ManageQuizSession
{
    public interface IDynamicLinkService
    {
        (string DeepLink, string QrCodeUrl) BuildSessionLinks(Guid sessionId, Guid quizId, Guid hostId);
    }
}
