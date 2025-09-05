using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Services;

public class LockExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LockExpirationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public LockExpirationService(IServiceProvider serviceProvider, ILogger<LockExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndUnlockExpiredLocks();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in lock expiration service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait longer on error
            }
        }
    }

    private async Task CheckAndUnlockExpiredLocks()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredLocks = await context.Locks
            .Include(l => l.Unit)
            .Include(l => l.User)
            .Where(l => l.IsActive && l.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (!expiredLocks.Any())
        {
            _logger.LogDebug("No expired locks found");
            return;
        }

        _logger.LogInformation("Found {Count} expired locks to unlock", expiredLocks.Count);

        foreach (var lockItem in expiredLocks)
        {
            lockItem.IsActive = false;
            lockItem.UnlockedAt = DateTime.UtcNow;

            // Update unit status if no other active locks
            var hasOtherActiveLocks = await context.Locks
                .AnyAsync(l => l.UnitId == lockItem.UnitId && l.IsActive && l.Id != lockItem.Id);

            if (!hasOtherActiveLocks)
            {
                lockItem.Unit.Status = UnitStatus.Available;
            }

            // Log the activity
            var activityLog = new ActivityLog
            {
                UserId = lockItem.UserId,
                ActivityType = ActivityType.Unlock,
                EntityType = "Unit",
                EntityId = lockItem.UnitId,
                Description = $"Auto-unlocked unit {lockItem.Unit.UnitNumber} due to expiration",
                CreatedAt = DateTime.UtcNow
            };

            context.ActivityLogs.Add(activityLog);
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Successfully unlocked {Count} expired locks", expiredLocks.Count);
    }
}
