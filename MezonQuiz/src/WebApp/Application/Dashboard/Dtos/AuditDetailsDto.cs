namespace WebApp.Application.Dashboard.Dtos;

public sealed class AuditDetailsDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public int? ParticipantCount { get; set; }
}
