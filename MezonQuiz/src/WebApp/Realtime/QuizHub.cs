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

        public async Task JoinQuizGroup(string quizId)
        {
            if (string.IsNullOrWhiteSpace(quizId))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"quiz_{quizId}");
        }

        public async Task LeaveQuizGroup(string quizId)
        {
            if (string.IsNullOrWhiteSpace(quizId))
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"quiz_{quizId}");
        }
    }
}
