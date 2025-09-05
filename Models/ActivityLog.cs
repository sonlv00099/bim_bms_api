using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public enum ActivityType
{
    Login,
    Logout,
    Create,
    Update,
    Delete,
    Lock,
    Unlock,
    Reserve,
    Confirm,
    Cancel,
    Publish,
    Close
}

public class ActivityLog
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public ActivityType ActivityType { get; set; }
    
    public string EntityType { get; set; } = string.Empty;
    
    public int? EntityId { get; set; }
    
    public string? Description { get; set; }
    
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
