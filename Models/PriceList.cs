using System.ComponentModel.DataAnnotations;

namespace BookingAssetAPI.Models;

public enum PriceListType
{
    Public,
    Private
}

public enum PriceListStatus
{
    Draft,
    Published,
    Closed
}

public class PriceList
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public PriceListType Type { get; set; }
    
    public PriceListStatus Status { get; set; } = PriceListStatus.Draft;
    
    public int ProjectId { get; set; }
    
    public int? AgencyId { get; set; } // For private price lists
    
    public DateTime? PublishedAt { get; set; }
    
    public DateTime? ClosedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual User? Agency { get; set; }
    public virtual ICollection<PriceListItem> PriceListItems { get; set; } = new List<PriceListItem>();
}
