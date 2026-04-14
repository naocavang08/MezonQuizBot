using WebApp.Application.AuditLog.Dtos;

namespace WebApp.Application.AuditLog;

public interface IAuditLogService
{
    Task<PagedAuditLogResultDto> GetAuditLogsAsync(AuditLogQueryDto query);
}
