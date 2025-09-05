using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Staff")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailabilityReport([FromQuery] int? projectId)
    {
        var query = _context.Units
            .Include(u => u.Building)
            .ThenInclude(b => b.Project)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(u => u.Building.ProjectId == projectId.Value);
        }

        var units = await query.ToListAsync();

        var availabilitySummary = new
        {
            totalUnits = units.Count,
            availableUnits = units.Count(u => u.Status == UnitStatus.Available),
            lockedUnits = units.Count(u => u.Status == UnitStatus.Locked),
            reservedUnits = units.Count(u => u.Status == UnitStatus.Reserved),
            soldUnits = units.Count(u => u.Status == UnitStatus.Sold),
            availabilityRate = units.Count > 0 ? (double)units.Count(u => u.Status == UnitStatus.Available) / units.Count * 100 : 0,
            byProject = units.GroupBy(u => u.Building.Project.Name).Select(g => new
            {
                projectName = g.Key,
                totalUnits = g.Count(),
                availableUnits = g.Count(u => u.Status == UnitStatus.Available),
                lockedUnits = g.Count(u => u.Status == UnitStatus.Locked),
                reservedUnits = g.Count(u => u.Status == UnitStatus.Reserved),
                soldUnits = g.Count(u => u.Status == UnitStatus.Sold)
            }),
            byBuilding = units.GroupBy(u => u.Building.Name).Select(g => new
            {
                buildingName = g.Key,
                projectName = g.First().Building.Project.Name,
                totalUnits = g.Count(),
                availableUnits = g.Count(u => u.Status == UnitStatus.Available),
                lockedUnits = g.Count(u => u.Status == UnitStatus.Locked),
                reservedUnits = g.Count(u => u.Status == UnitStatus.Reserved),
                soldUnits = g.Count(u => u.Status == UnitStatus.Sold)
            }),
            byType = units.GroupBy(u => u.Type).Select(g => new
            {
                unitType = g.Key ?? "Unknown",
                totalUnits = g.Count(),
                availableUnits = g.Count(u => u.Status == UnitStatus.Available),
                lockedUnits = g.Count(u => u.Status == UnitStatus.Locked),
                reservedUnits = g.Count(u => u.Status == UnitStatus.Reserved),
                soldUnits = g.Count(u => u.Status == UnitStatus.Sold)
            })
        };

        return Ok(availabilitySummary);
    }

    [HttpGet("depreciation")]
    public async Task<IActionResult> GetDepreciationReport([FromQuery] int? projectId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = _context.PriceListItems
            .Include(pli => pli.Unit)
            .ThenInclude(u => u.Building)
            .ThenInclude(b => b.Project)
            .Include(pli => pli.PriceList)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(pli => pli.Unit.Building.ProjectId == projectId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(pli => pli.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(pli => pli.CreatedAt <= toDate.Value);
        }

        var priceItems = await query.ToListAsync();

        var depreciationSummary = new
        {
            totalPriceItems = priceItems.Count,
            totalValue = priceItems.Sum(pli => pli.Price),
            averagePrice = priceItems.Any() ? priceItems.Average(pli => pli.Price) : 0,
            totalDiscount = priceItems.Sum(pli => pli.Discount ?? 0),
            byProject = priceItems.GroupBy(pli => pli.Unit.Building.Project.Name).Select(g => new
            {
                projectName = g.Key,
                totalItems = g.Count(),
                totalValue = g.Sum(pli => pli.Price),
                averagePrice = g.Average(pli => pli.Price),
                totalDiscount = g.Sum(pli => pli.Discount ?? 0)
            }),
            byBuilding = priceItems.GroupBy(pli => pli.Unit.Building.Name).Select(g => new
            {
                buildingName = g.Key,
                projectName = g.First().Unit.Building.Project.Name,
                totalItems = g.Count(),
                totalValue = g.Sum(pli => pli.Price),
                averagePrice = g.Average(pli => pli.Price),
                totalDiscount = g.Sum(pli => pli.Discount ?? 0)
            }),
            byUnitType = priceItems.GroupBy(pli => pli.Unit.Type).Select(g => new
            {
                unitType = g.Key ?? "Unknown",
                totalItems = g.Count(),
                totalValue = g.Sum(pli => pli.Price),
                averagePrice = g.Average(pli => pli.Price),
                totalDiscount = g.Sum(pli => pli.Discount ?? 0)
            }),
            priceRange = new
            {
                minPrice = priceItems.Any() ? priceItems.Min(pli => pli.Price) : 0,
                maxPrice = priceItems.Any() ? priceItems.Max(pli => pli.Price) : 0,
                priceDistribution = new[]
                {
                    new { range = "0-100k", count = priceItems.Count(pli => pli.Price <= 100000) },
                    new { range = "100k-200k", count = priceItems.Count(pli => pli.Price > 100000 && pli.Price <= 200000) },
                    new { range = "200k-300k", count = priceItems.Count(pli => pli.Price > 200000 && pli.Price <= 300000) },
                    new { range = "300k-400k", count = priceItems.Count(pli => pli.Price > 300000 && pli.Price <= 400000) },
                    new { range = "400k+", count = priceItems.Count(pli => pli.Price > 400000) }
                }
            }
        };

        return Ok(depreciationSummary);
    }

    [HttpGet("activity")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetActivityReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? activityType)
    {
        var query = _context.ActivityLogs
            .Include(al => al.User)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(al => al.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(al => al.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(activityType))
        {
            if (Enum.TryParse<ActivityType>(activityType, out var parsedType))
            {
                query = query.Where(al => al.ActivityType == parsedType);
            }
        }

        var activities = await query
            .OrderByDescending(al => al.CreatedAt)
            .Take(1000)
            .ToListAsync();

        var activitySummary = new
        {
            totalActivities = activities.Count,
            byUser = activities.GroupBy(al => al.User.Email).Select(g => new
            {
                userEmail = g.Key,
                userName = $"{g.First().User.FirstName} {g.First().User.LastName}",
                activityCount = g.Count(),
                lastActivity = g.Max(al => al.CreatedAt)
            }),
            byActivityType = activities.GroupBy(al => al.ActivityType).Select(g => new
            {
                activityType = g.Key.ToString(),
                count = g.Count()
            }),
            byEntityType = activities.GroupBy(al => al.EntityType).Select(g => new
            {
                entityType = g.Key,
                count = g.Count()
            }),
            recentActivities = activities.Take(50).Select(al => new
            {
                id = al.Id,
                userId = al.UserId,
                userEmail = al.User.Email,
                userName = $"{al.User.FirstName} {al.User.LastName}",
                activityType = al.ActivityType.ToString(),
                entityType = al.EntityType,
                entityId = al.EntityId,
                description = al.Description,
                ipAddress = al.IpAddress,
                createdAt = al.CreatedAt
            })
        };

        return Ok(activitySummary);
    }

    [HttpGet("locks")]
    public async Task<IActionResult> GetLocksReport([FromQuery] int? projectId)
    {
        var query = _context.Locks
            .Include(l => l.Unit)
            .ThenInclude(u => u.Building)
            .ThenInclude(b => b.Project)
            .Include(l => l.User)
            .Where(l => l.IsActive);

        if (projectId.HasValue)
        {
            query = query.Where(l => l.Unit.Building.ProjectId == projectId.Value);
        }

        var locks = await query.ToListAsync();

        var locksSummary = new
        {
            totalActiveLocks = locks.Count,
            expiringSoon = locks.Count(l => l.ExpiresAt <= DateTime.UtcNow.AddMinutes(30)),
            byUser = locks.GroupBy(l => l.User.Email).Select(g => new
            {
                userEmail = g.Key,
                userName = $"{g.First().User.FirstName} {g.First().User.LastName}",
                lockCount = g.Count(),
                averageLockDuration = g.Average(l => (l.ExpiresAt - l.LockedAt).TotalMinutes)
            }),
            byProject = locks.GroupBy(l => l.Unit.Building.Project.Name).Select(g => new
            {
                projectName = g.Key,
                lockCount = g.Count()
            }),
            byBuilding = locks.GroupBy(l => l.Unit.Building.Name).Select(g => new
            {
                buildingName = g.Key,
                projectName = g.First().Unit.Building.Project.Name,
                lockCount = g.Count()
            }),
            lockDetails = locks.Select(l => new
            {
                id = l.Id,
                unitNumber = l.Unit.UnitNumber,
                buildingName = l.Unit.Building.Name,
                projectName = l.Unit.Building.Project.Name,
                userName = $"{l.User.FirstName} {l.User.LastName}",
                userEmail = l.User.Email,
                lockedAt = l.LockedAt,
                expiresAt = l.ExpiresAt,
                timeRemaining = (l.ExpiresAt - DateTime.UtcNow).TotalMinutes
            })
        };

        return Ok(locksSummary);
    }
}
