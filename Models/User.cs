using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public enum UserRole
{
    Admin,
    Staff,
    Agency
}

public class User
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    
    public UserRole Role { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsLocked { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public virtual ICollection<Lock> Locks { get; set; } = new List<Lock>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
