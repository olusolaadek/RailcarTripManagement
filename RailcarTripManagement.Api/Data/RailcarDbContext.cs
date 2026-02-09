using Microsoft.EntityFrameworkCore;
using RailcarTripManagement.Api.Models;

namespace RailcarTripManagement.Api.Data;

public class RailcarDbContext : DbContext
{
    public RailcarDbContext(DbContextOptions<RailcarDbContext> options) : base(options)
    {
    }
    
    public DbSet<City> Cities { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<EquipmentEvent> EquipmentEvents { get; set; }
    public DbSet<EventCodeDefinition> EventCodeDefinitions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure City entity
        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId);
            entity.Property(e => e.CityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TimeZone).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.CityName);
        });
        
        // Configure Trip entity
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.TripId);
            entity.Property(e => e.EquipmentId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalTripHours).IsRequired();
            
            // Configure relationships
            entity.HasOne(t => t.OriginCity)
                .WithMany(c => c.OriginTrips)
                .HasForeignKey(t => t.OriginCityId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(t => t.DestinationCity)
                .WithMany(c => c.DestinationTrips)
                .HasForeignKey(t => t.DestinationCityId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for performance
            entity.HasIndex(e => e.EquipmentId);
            entity.HasIndex(e => e.StartUtc);
        });
        
        // Configure EquipmentEvent entity
        modelBuilder.Entity<EquipmentEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EquipmentId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EventCode).IsRequired().HasMaxLength(10);
            
            // Configure relationships
            entity.HasOne(e => e.City)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Events)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes for performance
            entity.HasIndex(e => e.EquipmentId);
            entity.HasIndex(e => e.EventTimeUtc);
            entity.HasIndex(e => e.TripId);
        });
        
        // Configure EventCodeDefinition entity
        modelBuilder.Entity<EventCodeDefinition>(entity =>
        {
            entity.HasKey(e => e.EventCode);
            entity.Property(e => e.EventDescription).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LongDescription).IsRequired().HasMaxLength(500);
        });
    }
}
