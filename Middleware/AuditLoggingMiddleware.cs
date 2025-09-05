using System.Text.Json;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var originalBodyStream = context.Response.Body;
        var requestBody = string.Empty;
        var responseBody = string.Empty;

        // Capture request body
        if (context.Request.Body.CanRead)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Capture response body
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        var startTime = DateTime.UtcNow;
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        try
        {
            await _next(context);
        }
        finally
        {
            memoryStream.Position = 0;
            responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);

            var duration = DateTime.UtcNow - startTime;

            // Log to stdout
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Method = context.Request.Method,
                Path = context.Request.Path,
                StatusCode = context.Response.StatusCode,
                Duration = duration.TotalMilliseconds,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RequestBody = requestBody,
                ResponseBody = responseBody
            };

            _logger.LogInformation("API Request: {@LogEntry}", logEntry);

            // Log to database if user is authenticated
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var userIdInt))
            {
                try
                {
                    var activityLog = new ActivityLog
                    {
                        UserId = userIdInt,
                        ActivityType = GetActivityType(context.Request.Method, context.Request.Path),
                        EntityType = GetEntityType(context.Request.Path),
                        Description = $"{context.Request.Method} {context.Request.Path}",
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.ActivityLogs.Add(activityLog);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save activity log to database");
                }
            }
        }
    }

    private static ActivityType GetActivityType(string method, string path)
    {
        return method.ToUpper() switch
        {
            "GET" => ActivityType.Create, // For audit purposes, treat GET as Create
            "POST" => ActivityType.Create,
            "PUT" => ActivityType.Update,
            "PATCH" => ActivityType.Update,
            "DELETE" => ActivityType.Delete,
            _ => ActivityType.Create
        };
    }

    private static string GetEntityType(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 1 ? segments[1].ToUpperInvariant() : "UNKNOWN";
    }
}
