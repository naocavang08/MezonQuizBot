namespace WebApp.Application.ManageQuiz.Dtos
{
    public class QuizQuery
    {
        public Guid? Category { get; set; }
        public string? Title { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
