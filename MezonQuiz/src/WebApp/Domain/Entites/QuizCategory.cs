namespace WebApp.Domain.Entites
{
    public class QuizCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Slug { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
