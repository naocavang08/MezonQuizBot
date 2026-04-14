namespace WebApp.Application.AuditLog.Dtos;

public sealed class AuditLogQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Action { get; set; }
    public string? ResourceType { get; set; }
    public string? User { get; set; }
    public string? Status { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
