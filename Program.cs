using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using BookingAssetAPI.Data;
using BookingAssetAPI.Models;
using BookingAssetAPI.Services;
using BookingAssetAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog (stdout JSON)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatter: new Serilog.Formatting.Json.JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("MYSQL_CONN");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configure JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-key-with-at-least-32-characters";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "BookingAssetAPI",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"] ?? "BookingAssetAPI",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
    options.AddPolicy("AgencyOrStaffOrAdmin", policy => policy.RequireRole("Agency", "Staff", "Admin"));
});

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHostedService<LockExpirationService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Booking Asset API", Version = "v1" });
    
    // Configure JWT authentication in Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger (enabled in Development by default; set ASPNETCORE_ENVIRONMENT=Development in Docker)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Commented out for development
app.UseCors("AllowAll");

// Add audit logging middleware
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-migrate database if enabled
if (Environment.GetEnvironmentVariable("AUTO_MIGRATE") == "true")
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    
    // Seed data if database is empty
    if (!context.Users.Any())
    {
        await SeedData(context);
    }
}

app.Run();

// Seed data method
static async Task SeedData(ApplicationDbContext context)
{
    // Create default users
    var users = new List<User>
    {
        new()
        {
            Email = "admin@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true
        },
        new()
        {
            Email = "staff@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
            FirstName = "Staff",
            LastName = "User",
            Role = UserRole.Staff,
            IsActive = true
        },
        new()
        {
            Email = "agency@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agency@123"),
            FirstName = "Agency",
            LastName = "User",
            Role = UserRole.Agency,
            IsActive = true
        }
    };

    context.Users.AddRange(users);
    await context.SaveChangesAsync();

    // Create sample project
    var project = new Project
    {
        Name = "Sample Project",
        Description = "A sample BIM project for demonstration",
        Location = "Sample Location",
        StartDate = DateTime.UtcNow,
        IsActive = true
    };

    context.Projects.Add(project);
    await context.SaveChangesAsync();

    // Create sample building
    var building = new Building
    {
        Name = "Building A",
        Description = "Sample building with multiple units",
        Floors = 5,
        ProjectId = project.Id,
        IsActive = true
    };

    context.Buildings.Add(building);
    await context.SaveChangesAsync();

    // Create sample units
    var units = new List<Unit>();
    for (int floor = 1; floor <= 5; floor++)
    {
        for (int unit = 1; unit <= 4; unit++)
        {
            units.Add(new Unit
            {
                UnitNumber = $"A{floor:D2}{unit:D2}",
                Floor = floor,
                Area = 100 + (floor * 10) + unit,
                Bedrooms = 2 + (unit % 3),
                Bathrooms = 1 + (unit % 2),
                Type = unit % 2 == 0 ? "Apartment" : "Studio",
                Status = UnitStatus.Available,
                BuildingId = building.Id,
                IsActive = true
            });
        }
    }

    context.Units.AddRange(units);
    await context.SaveChangesAsync();

    // Create sample price list
    var priceList = new PriceList
    {
        Name = "Public Price List",
        Description = "Default public price list",
        Type = PriceListType.Public,
        Status = PriceListStatus.Published,
        ProjectId = project.Id,
        PublishedAt = DateTime.UtcNow
    };

    context.PriceLists.Add(priceList);
    await context.SaveChangesAsync();

    // Create price list items
    var priceListItems = units.Select((unit, index) => new PriceListItem
    {
        PriceListId = priceList.Id,
        UnitId = unit.Id,
        Price = 50000 + (index * 5000),
        Discount = index % 3 == 0 ? 5000 : null,
        Notes = index % 3 == 0 ? "Special discount available" : null
    }).ToList();

    context.PriceListItems.AddRange(priceListItems);
    await context.SaveChangesAsync();
}
