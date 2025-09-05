using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public class PriceListItem
{
    public int Id { get; set; }
    
    public int PriceListId { get; set; }
    
    public int UnitId { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    public decimal? Discount { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual PriceList PriceList { get; set; } = null!;
    public virtual Unit Unit { get; set; } = null!;
}
