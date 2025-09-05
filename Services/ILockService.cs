namespace BookingAssetAPI.Services;

public interface ILockService
{
    Task<bool> LockUnitAsync(int unitId, int userId, string? notes = null);
    Task<bool> UnlockUnitAsync(int lockId, int userId);
    Task<bool> IsUnitLockedAsync(int unitId);
    Task<DateTime?> GetLockExpirationAsync(int unitId);
    Task CleanupExpiredLocksAsync();
}
