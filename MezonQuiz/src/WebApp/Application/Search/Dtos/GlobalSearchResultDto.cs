namespace WebApp.Application.Search.Dtos;

public class GlobalSearchResultDto
{
    public List<SearchItemDto> Quizzes { get; set; } = new();
    public List<SearchItemDto> Categories { get; set; } = new();
}

public class SearchItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
}
