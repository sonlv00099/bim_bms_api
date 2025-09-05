using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BookingAssetAPI.Services;

namespace BookingAssetAPI.Services.BackgroundServices;

public class LockCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LockCleanupService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);
    
    public LockCleanupService(IServiceProvider serviceProvider, ILogger<LockCleanupService> logger)
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
                using var scope = _serviceProvider.CreateScope();
                var lockService = scope.ServiceProvider.GetRequiredService<ILockService>();
                
                await lockService.CleanupExpiredLocksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired locks");
            }
            
            await Task.Delay(_period, stoppingToken);
        }
    }
}
