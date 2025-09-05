using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PriceListsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PriceListsController> _logger;

    public PriceListsController(ApplicationDbContext context, ILogger<PriceListsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPriceLists()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        var query = _context.PriceLists
            .Include(pl => pl.Agency)
            .Include(pl => pl.PriceListItems)
            .AsQueryable();

        // Filter based on user role
        if (user?.Role == UserRole.Agency)
        {
            query = query.Where(pl => pl.AgencyId == userId || pl.Type == PriceListType.Public);
        }

        var priceLists = await query
            .OrderBy(pl => pl.CreatedAt)
            .Select(pl => new
            {
                id = pl.Id,
                name = pl.Name,
                description = pl.Description,
                type = pl.Type,
                status = pl.Status,
                projectId = pl.ProjectId,
                agencyId = pl.AgencyId,
                agencyName = pl.Agency != null ? $"{pl.Agency.FirstName} {pl.Agency.LastName}" : null,
                createdAt = pl.CreatedAt,
                updatedAt = pl.UpdatedAt,
                publishedAt = pl.PublishedAt,
                closedAt = pl.ClosedAt,
                itemCount = pl.PriceListItems.Count
            })
            .ToListAsync();

        return Ok(priceLists);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPriceList(int id)
    {
        var priceList = await _context.PriceLists
            .Include(pl => pl.Agency)
            .Include(pl => pl.PriceListItems)
            .ThenInclude(pli => pli.Unit)
            .ThenInclude(u => u.Building)
            .FirstOrDefaultAsync(pl => pl.Id == id);

        if (priceList == null)
        {
            return NotFound("Price list not found");
        }

        var result = new
        {
            id = priceList.Id,
            name = priceList.Name,
            description = priceList.Description,
            type = priceList.Type,
            status = priceList.Status,
            projectId = priceList.ProjectId,
            agencyId = priceList.AgencyId,
            agencyName = priceList.Agency != null ? $"{priceList.Agency.FirstName} {priceList.Agency.LastName}" : null,
            createdAt = priceList.CreatedAt,
            updatedAt = priceList.UpdatedAt,
            publishedAt = priceList.PublishedAt,
            closedAt = priceList.ClosedAt,
            items = priceList.PriceListItems.Select(pli => new
            {
                id = pli.Id,
                unitId = pli.UnitId,
                unitNumber = pli.Unit.UnitNumber,
                buildingName = pli.Unit.Building.Name,
                price = pli.Price,
                discount = pli.Discount,
                notes = pli.Notes,
                createdAt = pli.CreatedAt,
                updatedAt = pli.UpdatedAt
            })
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreatePriceList([FromBody] CreatePriceListRequest request)
    {
        // Verify project exists
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);

        if (project == null)
        {
            return BadRequest("Project not found");
        }

        var priceList = new PriceList
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Status = PriceListStatus.Draft,
            ProjectId = request.ProjectId,
            AgencyId = request.AgencyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, new
        {
            id = priceList.Id,
            name = priceList.Name,
            description = priceList.Description,
            type = priceList.Type,
            status = priceList.Status,
            projectId = priceList.ProjectId,
            agencyId = priceList.AgencyId,
            createdAt = priceList.CreatedAt,
            updatedAt = priceList.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdatePriceList(int id, [FromBody] UpdatePriceListRequest request)
    {
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id);

        if (priceList == null)
        {
            return NotFound("Price list not found");
        }

        priceList.Name = request.Name;
        priceList.Description = request.Description;
        priceList.Type = request.Type;
        priceList.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = priceList.Id,
            name = priceList.Name,
            description = priceList.Description,
            type = priceList.Type,
            status = priceList.Status,
            projectId = priceList.ProjectId,
            agencyId = priceList.AgencyId,
            createdAt = priceList.CreatedAt,
            updatedAt = priceList.UpdatedAt
        });
    }

    [HttpPost("{id}/publish")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> PublishPriceList(int id)
    {
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id);

        if (priceList == null)
        {
            return NotFound("Price list not found");
        }

        priceList.Status = PriceListStatus.Published;
        priceList.PublishedAt = DateTime.UtcNow;
        priceList.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Price list published successfully" });
    }

    [HttpPost("{id}/close")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ClosePriceList(int id)
    {
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id);

        if (priceList == null)
        {
            return NotFound("Price list not found");
        }

        priceList.Status = PriceListStatus.Closed;
        priceList.ClosedAt = DateTime.UtcNow;
        priceList.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Price list closed successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePriceList(int id)
    {
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id);

        if (priceList == null)
        {
            return NotFound("Price list not found");
        }

        _context.PriceLists.Remove(priceList);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Price list deleted successfully" });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
    }
}

public class CreatePriceListRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PriceListType Type { get; set; }
    public int ProjectId { get; set; }
    public int? AgencyId { get; set; }
}

public class UpdatePriceListRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PriceListType Type { get; set; }
}
