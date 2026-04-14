namespace WebApp.Application.AuditLog.Dtos;

public sealed class AuditLogItemDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public string? ResourceType { get; set; }
    public string? IpAddress { get; set; }
    public AuditDetailsDto? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
