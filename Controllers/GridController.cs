using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;
using System.Security.Claims;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GridController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GridController> _logger;

    public GridController(ApplicationDbContext context, ILogger<GridController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetGrid([FromQuery] int? projectId, [FromQuery] int? buildingId)
    {
        var query = _context.Units
            .Include(u => u.Building)
            .Include(u => u.Building.Project)
            .Include(u => u.Locks.Where(l => l.IsActive))
            .Include(u => u.Reservations.Where(r => r.Status == ReservationStatus.Confirmed))
            .Include(u => u.PriceListItems)
            .ThenInclude(pli => pli.PriceList)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(u => u.Building.ProjectId == projectId.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(u => u.BuildingId == buildingId.Value);
        }

        var units = await query
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
                building = new
                {
                    id = u.Building.Id,
                    name = u.Building.Name,
                    project = new
                    {
                        id = u.Building.Project.Id,
                        name = u.Building.Project.Name
                    }
                },
                currentLock = u.Locks.FirstOrDefault(),
                currentReservation = u.Reservations.FirstOrDefault(),
                price = u.PriceListItems.FirstOrDefault() != null ? u.PriceListItems.First().Price : 0,
                discount = u.PriceListItems.FirstOrDefault() != null ? u.PriceListItems.First().Discount : 0
            })
            .ToListAsync();

        return Ok(units);
    }

    [HttpPost("lock")]
    public async Task<IActionResult> LockUnit([FromBody] LockUnitRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var lockTtlMinutes = int.Parse(Environment.GetEnvironmentVariable("LOCK_TTL_MIN") ?? "30");

        var unit = await _context.Units
            .Include(u => u.Locks.Where(l => l.IsActive))
            .FirstOrDefaultAsync(u => u.Id == request.UnitId);

        if (unit == null)
        {
            return NotFound("Unit not found");
        }

        if (unit.Status != UnitStatus.Available)
        {
            return BadRequest("Unit is not available for locking");
        }

        // Check if user has reached lock limit
        var userLockCount = await _context.Locks
            .CountAsync(l => l.UserId == userId && l.IsActive);

        var lockLimit = int.Parse(Environment.GetEnvironmentVariable("LOCK_LIMIT") ?? "3");
        if (userLockCount >= lockLimit)
        {
            return BadRequest($"You have reached the maximum limit of {lockLimit} active locks");
        }

        var lockItem = new Lock
        {
            UnitId = request.UnitId,
            UserId = userId,
            LockedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(lockTtlMinutes),
            IsActive = true,
            Notes = request.Notes
        };

        unit.Status = UnitStatus.Locked;
        _context.Locks.Add(lockItem);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Unit locked successfully",
            lockId = lockItem.Id,
            expiresAt = lockItem.ExpiresAt
        });
    }

    [HttpPost("unlock")]
    public async Task<IActionResult> UnlockUnit([FromBody] UnlockUnitRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var lockItem = await _context.Locks
            .Include(l => l.Unit)
            .FirstOrDefaultAsync(l => l.Id == request.LockId && l.IsActive);

        if (lockItem == null)
        {
            return NotFound("Lock not found");
        }

        // Only the user who created the lock or admin can unlock it
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (lockItem.UserId != userId && userRole != "Admin")
        {
            return Forbid();
        }

        lockItem.IsActive = false;
        lockItem.UnlockedAt = DateTime.UtcNow;

        // Check if there are other active locks on this unit
        var hasOtherActiveLocks = await _context.Locks
            .AnyAsync(l => l.UnitId == lockItem.UnitId && l.IsActive && l.Id != lockItem.Id);

        if (!hasOtherActiveLocks)
        {
            lockItem.Unit.Status = UnitStatus.Available;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Unit unlocked successfully" });
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveUnit([FromBody] ReserveUnitRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var unit = await _context.Units
            .Include(u => u.PriceListItems)
            .FirstOrDefaultAsync(u => u.Id == request.UnitId);

        if (unit == null)
        {
            return NotFound("Unit not found");
        }

        if (unit.Status != UnitStatus.Available && unit.Status != UnitStatus.Locked)
        {
            return BadRequest("Unit is not available for reservation");
        }

        var price = unit.PriceListItems.FirstOrDefault()?.Price ?? 0;

        var reservation = new Reservation
        {
            UnitId = request.UnitId,
            UserId = userId,
            Status = ReservationStatus.Pending,
            ReservedAt = DateTime.UtcNow,
            Price = price,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            Notes = request.Notes
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Unit reserved successfully",
            reservationId = reservation.Id
        });
    }

    [HttpPost("confirm-reservation")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ConfirmReservation([FromBody] ConfirmReservationRequest request)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Unit)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId);

        if (reservation == null)
        {
            return NotFound("Reservation not found");
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return BadRequest("Reservation is not in pending status");
        }

        reservation.Status = ReservationStatus.Confirmed;
        reservation.ConfirmedAt = DateTime.UtcNow;
        reservation.Unit.Status = UnitStatus.Reserved;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservation confirmed successfully" });
    }
}

public class LockUnitRequest
{
    public int UnitId { get; set; }
    public string? Notes { get; set; }
}

public class UnlockUnitRequest
{
    public int LockId { get; set; }
}

public class ReserveUnitRequest
{
    public int UnitId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Notes { get; set; }
}

public class ConfirmReservationRequest
{
    public int ReservationId { get; set; }
}
