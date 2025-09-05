using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BuildingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingsController> _logger;

    public BuildingsController(ApplicationDbContext context, ILogger<BuildingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBuildings([FromQuery] int? projectId)
    {
        var query = _context.Buildings
            .Include(b => b.Project)
            .Where(b => b.IsActive);

        if (projectId.HasValue)
        {
            query = query.Where(b => b.ProjectId == projectId.Value);
        }

        var buildings = await query
            .Select(b => new
            {
                id = b.Id,
                name = b.Name,
                description = b.Description,
                floors = b.Floors,
                projectId = b.ProjectId,
                projectName = b.Project.Name,
                isActive = b.IsActive,
                createdAt = b.CreatedAt,
                updatedAt = b.UpdatedAt,
                unitCount = b.Units.Count
            })
            .ToListAsync();

        return Ok(buildings);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBuilding(int id)
    {
        var building = await _context.Buildings
            .Include(b => b.Project)
            .Include(b => b.Units)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (building == null)
        {
            return NotFound("Building not found");
        }

        var result = new
        {
            id = building.Id,
            name = building.Name,
            description = building.Description,
            floors = building.Floors,
            projectId = building.ProjectId,
            projectName = building.Project.Name,
            isActive = building.IsActive,
            createdAt = building.CreatedAt,
            updatedAt = building.UpdatedAt,
            units = building.Units.Select(u => new
            {
                id = u.Id,
                unitNumber = u.UnitNumber,
                floor = u.Floor,
                area = u.Area,
                bedrooms = u.Bedrooms,
                bathrooms = u.Bathrooms,
                type = u.Type,
                status = u.Status
            })
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateBuilding([FromBody] CreateBuildingRequest request)
    {
        // Verify project exists
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);

        if (project == null)
        {
            return BadRequest("Project not found");
        }

        var building = new Building
        {
            Name = request.Name,
            Description = request.Description,
            Floors = request.Floors,
            ProjectId = request.ProjectId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, new
        {
            id = building.Id,
            name = building.Name,
            description = building.Description,
            floors = building.Floors,
            projectId = building.ProjectId,
            isActive = building.IsActive,
            createdAt = building.CreatedAt,
            updatedAt = building.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateBuilding(int id, [FromBody] UpdateBuildingRequest request)
    {
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (building == null)
        {
            return NotFound("Building not found");
        }

        building.Name = request.Name;
        building.Description = request.Description;
        building.Floors = request.Floors;
        building.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = building.Id,
            name = building.Name,
            description = building.Description,
            floors = building.Floors,
            projectId = building.ProjectId,
            isActive = building.IsActive,
            createdAt = building.CreatedAt,
            updatedAt = building.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBuilding(int id)
    {
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (building == null)
        {
            return NotFound("Building not found");
        }

        building.IsActive = false;
        building.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Building deleted successfully" });
    }

    [HttpPost("{id}/generate-units")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GenerateUnits(int id, [FromBody] GenerateUnitsRequest request)
    {
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (building == null)
        {
            return NotFound("Building not found");
        }

        var units = new List<Unit>();
        var unitCounter = 1;

        for (int floor = 1; floor <= building.Floors; floor++)
        {
            for (int unit = 1; unit <= request.UnitsPerFloor; unit++)
            {
                units.Add(new Unit
                {
                    UnitNumber = $"{building.Name.Substring(0, 1)}{floor:D2}{unit:D2}",
                    Floor = floor,
                    Area = request.BaseArea + (floor * request.AreaIncrement) + (unit * 10),
                    Bedrooms = request.BaseBedrooms + (unit % 3),
                    Bathrooms = request.BaseBathrooms + (unit % 2),
                    Type = unit % 2 == 0 ? "Apartment" : "Studio",
                    Status = UnitStatus.Available,
                    BuildingId = building.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                unitCounter++;
            }
        }

        _context.Units.AddRange(units);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Generated {units.Count} units for building {building.Name}",
            unitCount = units.Count,
            units = units.Select(u => new
            {
                id = u.Id,
                unitNumber = u.UnitNumber,
                floor = u.Floor,
                area = u.Area,
                bedrooms = u.Bedrooms,
                bathrooms = u.Bathrooms,
                type = u.Type,
                status = u.Status
            })
        });
    }
}

public class CreateBuildingRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Floors { get; set; }
    public int ProjectId { get; set; }
}

public class UpdateBuildingRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Floors { get; set; }
}

public class GenerateUnitsRequest
{
    public int UnitsPerFloor { get; set; } = 4;
    public decimal BaseArea { get; set; } = 100;
    public decimal AreaIncrement { get; set; } = 10;
    public int BaseBedrooms { get; set; } = 2;
    public int BaseBathrooms { get; set; } = 1;
}
