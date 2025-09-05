using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;

namespace BookingAssetAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditController> _logger;
    
    public AuditController(ApplicationDbContext context, ILogger<AuditController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<AuditLogResponse>>> GetAuditLogs(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.ActivityLogs
            .Include(al => al.User)
            .AsQueryable();
            
        if (fromDate.HasValue)
            query = query.Where(al => al.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(al => al.CreatedAt <= toDate.Value);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(al => al.ActivityType.ToString() == action);
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.EntityType == entityType);
        if (userId.HasValue)
            query = query.Where(al => al.UserId == userId);
            
        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(al => al.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(al => new AuditLogResponse
            {
                Id = al.Id,
                Action = al.ActivityType.ToString(),
                EntityType = al.EntityType,
                EntityId = al.EntityId,
                Details = al.Description,
                UserName = al.User != null ? $"{al.User.FirstName} {al.User.LastName}" : "System",
                UserEmail = al.User != null ? al.User.Email : null,
                IpAddress = al.IpAddress,
                UserAgent = al.UserAgent,
                CreatedAt = al.CreatedAt
            })
            .ToListAsync();
            
        return Ok(new
        {
            Logs = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }
}

public class AuditLogResponse
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
