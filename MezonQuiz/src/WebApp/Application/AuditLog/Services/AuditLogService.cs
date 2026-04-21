using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebApp.Application.AuditLog.Dtos;
using WebApp.Data;

namespace WebApp.Application.AuditLog.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _dbContext;

    public AuditLogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedAuditLogResultDto> GetAuditLogsAsync(AuditLogQueryDto query)
    {
        var safePage = Math.Max(1, query.Page);
        var safePageSize = Math.Clamp(query.PageSize, 5, 100);
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim();

        var logs = BuildBaseQuery();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            logs = logs.Where(log => log.Action != null && log.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(query.ResourceType))
        {
            var resourceType = query.ResourceType.Trim();
            logs = logs.Where(log =>
                log.ResourceType != null &&
                log.ResourceType.Contains(resourceType));
        }

        if (!string.IsNullOrWhiteSpace(query.User))
        {
            var user = query.User.Trim();
            logs = logs.Where(log =>
                log.User != null &&
                (
                    (log.User.DisplayName != null && log.User.DisplayName.Contains(user)) ||
                    (log.User.Username != null && log.User.Username.Contains(user))
                )
            );
        }

        if (query.FromDate.HasValue)
        {
            var fromDate = ToUtcDateStart(query.FromDate.Value);
            logs = logs.Where(log => log.CreatedAt >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            var toExclusive = ToUtcDateStart(query.ToDate.Value).AddDays(1);
            logs = logs.Where(log => log.CreatedAt < toExclusive);
        }

        if (!string.IsNullOrEmpty(normalizedStatus))
        {
            // Details is mapped via jsonb value-converter; status filter is applied in memory
            // to avoid provider translation errors while preserving correct pagination totals.
            var allItems = await logs
                .OrderByDescending(log => log.CreatedAt)
                .Select(MapAuditLogItem())
                .ToListAsync();

            var filteredItems = allItems
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item.Details?.Status) &&
                    item.Details!.Status!.Contains(normalizedStatus, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var filteredTotalCount = filteredItems.Count;
            var pagedItems = filteredItems
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToList();

            return new PagedAuditLogResultDto
            {
                Items = pagedItems,
                TotalCount = filteredTotalCount,
                Page = safePage,
                PageSize = safePageSize,
                TotalPages = filteredTotalCount > 0 ? (int)Math.Ceiling(filteredTotalCount / (double)safePageSize) : 0,
            };
        }

        var totalCount = await logs.CountAsync();
        var items = await logs
            .OrderByDescending(log => log.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(MapAuditLogItem())
            .ToListAsync();

        return new PagedAuditLogResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = safePage,
            PageSize = safePageSize,
            TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)safePageSize) : 0,
        };
    }

    private IQueryable<Domain.Entites.AuditLog> BuildBaseQuery()
    {
        return _dbContext.AuditLogs
            .Include(log => log.User)
            .AsNoTracking();
    }

    private static Expression<Func<Domain.Entites.AuditLog, AuditLogItemDto>> MapAuditLogItem()
    {
        return log => new AuditLogItemDto
        {
            Id = log.Id,
            Action = log.Action,
            UserDisplayName = log.User != null ? log.User.DisplayName : "System",
            ResourceType = log.ResourceType,
            IpAddress = log.IpAddress,
            CreatedAt = log.CreatedAt,
            Details = log.Details,
        };
    }

    private static DateTime ToUtcDateStart(DateTime input)
    {
        var dateOnly = input.Date;

        return input.Kind switch
        {
            DateTimeKind.Utc => dateOnly,
            DateTimeKind.Local => dateOnly.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc),
        };
    }
}
