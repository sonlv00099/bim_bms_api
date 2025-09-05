using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public class Building
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int Floors { get; set; }
    
    public int ProjectId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
