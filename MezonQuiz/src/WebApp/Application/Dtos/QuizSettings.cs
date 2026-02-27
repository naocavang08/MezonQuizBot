namespace WebApp.Application.Dtos
{
    public class QuizSettings
    {
        public int TimeLimitSeconds { get; set; } = 30;
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShuffleOptions { get; set; } = false;
        public bool ShowCorrectAnswer { get; set; } = true;
        public int MaxAttempts { get; set; } = 1;
    }
}