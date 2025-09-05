using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public class Reservation
{
    public int Id { get; set; }
    
    public int UnitId { get; set; }
    
    public int UserId { get; set; }
    
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ConfirmedAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public decimal Price { get; set; }
    
    public string? CustomerName { get; set; }
    
    public string? CustomerPhone { get; set; }
    
    public string? CustomerEmail { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Unit Unit { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
