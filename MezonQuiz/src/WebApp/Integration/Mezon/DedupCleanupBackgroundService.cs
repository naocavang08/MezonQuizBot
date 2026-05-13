using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Integration.Mezon
{
    public class DedupCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DedupCleanupBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

        public DedupCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DedupCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dedup Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredRecordsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Dedup Cleanup.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Dedup Cleanup Background Service is stopping.");
        }

        private async Task CleanupExpiredRecordsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            // Tìm và xoá các bản ghi đã quá hạn
            var expiredRecords = await dbContext.DedupRecords
                .Where(r => r.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            if (expiredRecords.Count > 0)
            {
                dbContext.DedupRecords.RemoveRange(expiredRecords);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired dedup records.", expiredRecords.Count);
            }
        }
    }
}
