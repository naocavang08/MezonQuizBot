namespace WebApp.Domain.Enums
{
    public class Status
    {
        public enum QuizVisibility
        {
            Private,
            Public,
            Unlisted
        }

        public enum QuizStatus
        {
            Draft,
            Published,
            Archived
        }

        public enum SessionStatus
        {
            Waiting,
            Active,
            Paused,
            Finished,
            Cancelled
        }
    }
}
