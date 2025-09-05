using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    
    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var originalBodyStream = context.Response.Body;
        
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;
        
        var startTime = DateTime.UtcNow;
        
        try
        {
            await _next(context);
            
            // Log the activity
            await LogActivity(context, dbContext, startTime);
        }
        finally
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }
    
    private async Task LogActivity(HttpContext context, ApplicationDbContext dbContext, DateTime startTime)
    {
        try
        {
            var userId = GetUserIdFromContext(context);
            var activityType = GetActivityType(context);
            var entityType = GetEntityType(context);
            var entityId = GetEntityId(context);
            
            if (activityType != ActivityType.Login && userId.HasValue)
            {
                var activityLog = new ActivityLog
                {
                    UserId = userId.Value,
                    ActivityType = activityType,
                    EntityType = entityType ?? "",
                    EntityId = entityId,
                    Description = $"{context.Request.Method} {context.Request.Path}",
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    CreatedAt = startTime
                };
                
                dbContext.ActivityLogs.Add(activityLog);
                await dbContext.SaveChangesAsync();
                
                // Also log to stdout as JSON
                _logger.LogInformation("Activity: {@Activity}", new
                {
                    UserId = userId,
                    ActivityType = activityType.ToString(),
                    EntityType = entityType,
                    EntityId = entityId,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    StatusCode = context.Response.StatusCode,
                    Duration = DateTime.UtcNow - startTime
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity");
        }
    }
    
    private int? GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
    }
    
    private ActivityType GetActivityType(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value?.ToLower();
        
        if (path?.Contains("/auth/login") == true) return ActivityType.Login;
        if (path?.Contains("/auth/logout") == true) return ActivityType.Logout;
        if (path?.Contains("/users") == true)
        {
            if (method == "POST") return ActivityType.Create;
            if (method == "PUT") return ActivityType.Update;
            if (path.Contains("/lock")) return ActivityType.Lock;
            if (path.Contains("/unlock")) return ActivityType.Unlock;
            if (path.Contains("/reset-password")) return ActivityType.Update;
        }
        if (path?.Contains("/projects") == true)
        {
            if (method == "POST") return ActivityType.Create;
            if (method == "PUT") return ActivityType.Update;
            if (method == "DELETE") return ActivityType.Delete;
        }
        if (path?.Contains("/buildings") == true)
        {
            if (method == "POST") return ActivityType.Create;
            if (method == "PUT") return ActivityType.Update;
            if (method == "DELETE") return ActivityType.Delete;
        }
        if (path?.Contains("/units") == true)
        {
            if (method == "POST") return ActivityType.Create;
            if (method == "PUT") return ActivityType.Update;
            if (method == "DELETE") return ActivityType.Delete;
            if (path.Contains("/bulk-generate")) return ActivityType.Create;
        }
        if (path?.Contains("/grid") == true)
        {
            if (path.Contains("/lock")) return ActivityType.Lock;
            if (path.Contains("/unlock")) return ActivityType.Unlock;
            if (path.Contains("/reserve")) return ActivityType.Reserve;
        }
        if (path?.Contains("/pricelists") == true)
        {
            if (method == "POST") return ActivityType.Create;
            if (path.Contains("/publish")) return ActivityType.Publish;
            if (path.Contains("/close")) return ActivityType.Close;
        }
        
        return ActivityType.Update;
    }
    
    private string? GetEntityType(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        
        if (path?.Contains("/users") == true) return "User";
        if (path?.Contains("/projects") == true) return "Project";
        if (path?.Contains("/buildings") == true) return "Building";
        if (path?.Contains("/units") == true) return "Unit";
        if (path?.Contains("/pricelists") == true) return "PriceList";
        
        return null;
    }
    
    private int? GetEntityId(HttpContext context)
    {
        var path = context.Request.Path.Value;
        var segments = path?.Split('/');
        
        if (segments?.Length >= 3)
        {
            if (int.TryParse(segments[2], out var id))
                return id;
        }
        
        return null;
    }
}
