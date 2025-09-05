using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public enum UnitStatus
{
    Available,
    Locked,
    Reserved,
    Sold
}

public class Unit
{
    public int Id { get; set; }
    
    [Required]
    public string UnitNumber { get; set; } = string.Empty;
    
    public int Floor { get; set; }
    
    public decimal Area { get; set; }
    
    public int Bedrooms { get; set; }
    
    public int Bathrooms { get; set; }
    
    public string? Type { get; set; }
    
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    
    public int BuildingId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Building Building { get; set; } = null!;
    public virtual ICollection<Lock> Locks { get; set; } = new List<Lock>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public virtual ICollection<PriceListItem> PriceListItems { get; set; } = new List<PriceListItem>();
}
