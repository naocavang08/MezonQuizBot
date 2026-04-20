namespace WebApp.Application.ManageQuiz.Dtos
{
    public class QuizQuery
    {
        public bool OnlyMine { get; set; } = false;
        public Guid? Category { get; set; }
        public string? Title { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
