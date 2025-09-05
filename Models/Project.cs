using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public class Project
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public string Location { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Building> Buildings { get; set; } = new List<Building>();
    public virtual ICollection<PriceList> PriceLists { get; set; } = new List<PriceList>();
}
