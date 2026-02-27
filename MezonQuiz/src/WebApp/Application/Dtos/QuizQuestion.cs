namespace WebApp.Application.Dtos
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Content { get; set; } = null!;
        public string? MediaUrl { get; set; }
        public int TimeLimitSeconds { get; set; } = 30;
        public int Points { get; set; } = 10;
        public List<QuizOption> Options { get; set; } = new();
    }

    public class QuizOption
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;
    }
}