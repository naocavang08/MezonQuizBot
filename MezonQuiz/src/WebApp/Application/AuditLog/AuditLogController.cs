using Microsoft.AspNetCore.Mvc;
using WebApp.Application.Auth.Authorization;
using WebApp.Application.AuditLog.Dtos;

namespace WebApp.Application.AuditLog;

[ApiController]
[Route("api/[controller]")]
public sealed class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [PermissionAuthorize(PermissionNames.AuditLogs.List)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditLogService.GetAuditLogsAsync(query);
        return Ok(result);
    }
}
