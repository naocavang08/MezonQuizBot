namespace WebApp.Application.AuditLog.Dtos;

public sealed class PagedAuditLogResultDto
{
    public List<AuditLogItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
