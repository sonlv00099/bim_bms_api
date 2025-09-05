using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public class Lock
{
    public int Id { get; set; }
    
    public int UnitId { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? UnlockedAt { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Unit Unit { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
