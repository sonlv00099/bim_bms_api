using Microsoft.EntityFrameworkCore;
using BookingAssetAPI.Models;

namespace BookingAssetAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<PriceList> PriceLists { get; set; }
    public DbSet<PriceListItem> PriceListItems { get; set; }
    public DbSet<Lock> Locks { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Location).HasMaxLength(500);
        });

        // Building configuration
        modelBuilder.Entity<Building>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.Project)
                  .WithMany(e => e.Buildings)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Unit configuration
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.Property(e => e.UnitNumber).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.Area).HasPrecision(10, 2);
            entity.HasOne(e => e.Building)
                  .WithMany(e => e.Units)
                  .HasForeignKey(e => e.BuildingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PriceList configuration
        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.Project)
                  .WithMany(e => e.PriceLists)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Agency)
                  .WithMany()
                  .HasForeignKey(e => e.AgencyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PriceListItem configuration
        modelBuilder.Entity<PriceListItem>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(15, 2);
            entity.Property(e => e.Discount).HasPrecision(15, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.PriceList)
                  .WithMany(e => e.PriceListItems)
                  .HasForeignKey(e => e.PriceListId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Unit)
                  .WithMany(e => e.PriceListItems)
                  .HasForeignKey(e => e.UnitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Lock configuration
        modelBuilder.Entity<Lock>(entity =>
        {
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.Unit)
                  .WithMany(e => e.Locks)
                  .HasForeignKey(e => e.UnitId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Locks)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Reservation configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(15, 2);
            entity.Property(e => e.CustomerName).HasMaxLength(255);
            entity.Property(e => e.CustomerPhone).HasMaxLength(20);
            entity.Property(e => e.CustomerEmail).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.Unit)
                  .WithMany(e => e.Reservations)
                  .HasForeignKey(e => e.UnitId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Reservations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityLog configuration
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.OldValues).HasMaxLength(4000);
            entity.Property(e => e.NewValues).HasMaxLength(4000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.ActivityLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
