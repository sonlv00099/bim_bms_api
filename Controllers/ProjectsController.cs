using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(ApplicationDbContext context, ILogger<ProjectsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _context.Projects
            .Where(p => p.IsActive)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                description = p.Description,
                location = p.Location,
                startDate = p.StartDate,
                endDate = p.EndDate,
                isActive = p.IsActive,
                createdAt = p.CreatedAt,
                updatedAt = p.UpdatedAt,
                buildingCount = p.Buildings.Count,
                priceListCount = p.PriceLists.Count
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Buildings)
            .Include(p => p.PriceLists)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (project == null)
        {
            return NotFound("Project not found");
        }

        var result = new
        {
            id = project.Id,
            name = project.Name,
            description = project.Description,
            location = project.Location,
            startDate = project.StartDate,
            endDate = project.EndDate,
            isActive = project.IsActive,
            createdAt = project.CreatedAt,
            updatedAt = project.UpdatedAt,
            buildings = project.Buildings.Select(b => new
            {
                id = b.Id,
                name = b.Name,
                description = b.Description,
                floors = b.Floors,
                unitCount = b.Units.Count
            }),
            priceLists = project.PriceLists.Select(pl => new
            {
                id = pl.Id,
                name = pl.Name,
                type = pl.Type,
                status = pl.Status
            })
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new
        {
            id = project.Id,
            name = project.Name,
            description = project.Description,
            location = project.Location,
            startDate = project.StartDate,
            endDate = project.EndDate,
            isActive = project.IsActive,
            createdAt = project.CreatedAt,
            updatedAt = project.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (project == null)
        {
            return NotFound("Project not found");
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.Location = request.Location;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = project.Id,
            name = project.Name,
            description = project.Description,
            location = project.Location,
            startDate = project.StartDate,
            endDate = project.EndDate,
            isActive = project.IsActive,
            createdAt = project.CreatedAt,
            updatedAt = project.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (project == null)
        {
            return NotFound("Project not found");
        }

        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Project deleted successfully" });
    }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
