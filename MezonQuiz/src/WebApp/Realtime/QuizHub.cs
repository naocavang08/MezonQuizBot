using Microsoft.AspNetCore.SignalR;

namespace WebApp.Realtime
{
    public class QuizHub : Hub
    {
        public async Task JoinSessionGroup(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task LeaveSessionGroup(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}
