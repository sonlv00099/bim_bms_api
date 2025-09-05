using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitsController> _logger;

    public UnitsController(ApplicationDbContext context, ILogger<UnitsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUnits([FromQuery] int? buildingId, [FromQuery] int? projectId)
    {
        var query = _context.Units
            .Include(u => u.Building)
            .ThenInclude(b => b.Project)
            .Include(u => u.Locks.Where(l => l.IsActive))
            .ThenInclude(l => l.User)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(u => u.BuildingId == buildingId.Value);
        }
        else if (projectId.HasValue)
        {
            query = query.Where(u => u.Building.ProjectId == projectId.Value);
        }

        var units = await query
            .OrderBy(u => u.UnitNumber)
            .Select(u => new
            {
                id = u.Id,
                unitNumber = u.UnitNumber,
                floor = u.Floor,
                area = u.Area,
                bedrooms = u.Bedrooms,
                bathrooms = u.Bathrooms,
                type = u.Type,
                status = u.Status,
                buildingId = u.BuildingId,
                buildingName = u.Building.Name,
                projectName = u.Building.Project.Name,
                isActive = u.IsActive,
                createdAt = u.CreatedAt,
                updatedAt = u.UpdatedAt,
                lockExpiresAt = u.Locks.FirstOrDefault() != null ? (DateTime?)u.Locks.First().ExpiresAt : null,
                lockedBy = u.Locks.FirstOrDefault() != null 
                    ? $"{u.Locks.First().User.FirstName} {u.Locks.First().User.LastName}"
                    : null
            })
            .ToListAsync();

        return Ok(units);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUnit(int id)
    {
        var unit = await _context.Units
            .Include(u => u.Building)
            .ThenInclude(b => b.Project)
            .Include(u => u.Locks.Where(l => l.IsActive))
            .ThenInclude(l => l.User)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (unit == null)
        {
            return NotFound("Unit not found");
        }

        var result = new
        {
            id = unit.Id,
            unitNumber = unit.UnitNumber,
            floor = unit.Floor,
            area = unit.Area,
            bedrooms = unit.Bedrooms,
            bathrooms = unit.Bathrooms,
            type = unit.Type,
            status = unit.Status,
            buildingId = unit.BuildingId,
            buildingName = unit.Building.Name,
            projectName = unit.Building.Project.Name,
            isActive = unit.IsActive,
            createdAt = unit.CreatedAt,
            updatedAt = unit.UpdatedAt,
            lockExpiresAt = unit.Locks.FirstOrDefault() != null ? (DateTime?)unit.Locks.First().ExpiresAt : null,
            lockedBy = unit.Locks.FirstOrDefault() != null 
                ? $"{unit.Locks.First().User.FirstName} {unit.Locks.First().User.LastName}"
                : null
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest request)
    {
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == request.BuildingId && b.IsActive);

        if (building == null)
        {
            return BadRequest("Building not found");
        }

        var unit = new Unit
        {
            UnitNumber = request.UnitNumber,
            Floor = request.Floor,
            Area = request.Area,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            Type = request.Type,
            Status = UnitStatus.Available,
            BuildingId = request.BuildingId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, new
        {
            id = unit.Id,
            unitNumber = unit.UnitNumber,
            floor = unit.Floor,
            area = unit.Area,
            bedrooms = unit.Bedrooms,
            bathrooms = unit.Bathrooms,
            type = unit.Type,
            status = unit.Status,
            buildingId = unit.BuildingId,
            buildingName = building.Name,
            projectName = building.Project.Name,
            isActive = unit.IsActive,
            createdAt = unit.CreatedAt,
            updatedAt = unit.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] UpdateUnitRequest request)
    {
        var unit = await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (unit == null)
        {
            return NotFound("Unit not found");
        }

        unit.UnitNumber = request.UnitNumber;
        unit.Floor = request.Floor;
        unit.Area = request.Area;
        unit.Bedrooms = request.Bedrooms;
        unit.Bathrooms = request.Bathrooms;
        unit.Type = request.Type;
        unit.Status = request.Status;
        unit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = unit.Id,
            unitNumber = unit.UnitNumber,
            floor = unit.Floor,
            area = unit.Area,
            bedrooms = unit.Bedrooms,
            bathrooms = unit.Bathrooms,
            type = unit.Type,
            status = unit.Status,
            buildingId = unit.BuildingId,
            isActive = unit.IsActive,
            createdAt = unit.CreatedAt,
            updatedAt = unit.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var unit = await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (unit == null)
        {
            return NotFound("Unit not found");
        }

        unit.IsActive = false;
        unit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Unit deleted successfully" });
    }
}

public class CreateUnitRequest
{
    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public decimal Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string? Type { get; set; }
    public int BuildingId { get; set; }
}

public class UpdateUnitRequest
{
    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public decimal Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string? Type { get; set; }
    public UnitStatus Status { get; set; }
}
