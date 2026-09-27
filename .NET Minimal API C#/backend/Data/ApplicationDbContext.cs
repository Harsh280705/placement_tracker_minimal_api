using Microsoft.EntityFrameworkCore;
using PlacementTracker.MinimalApi.Models;

namespace PlacementTracker.MinimalApi.Data;

// EF Core bridge: Minimal API endpoints -> DbContext -> PostgreSQL.
//
// IMPORTANT: this context maps to the "minimal_api_applications" table,
// which is DIFFERENT from the controller-based app's "Applications" table.
// The legacy table is never touched by this implementation.
public class ApplicationDbContext : DbContext
{
    public const string TableName = "minimal_api_applications";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Application> Applications => Set<Models.Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Application>(entity =>
        {
            entity.ToTable(TableName);
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Company).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Role).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Status).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Notes).HasMaxLength(1000);
            // Dates carry no timezone in this app (plain calendar dates + UTC stamps),
            // so store them without timezone. This also accepts Unspecified-Kind
            // DateTimes, which Npgsql rejects for "timestamp with time zone".
            entity.Property(a => a.AppliedOn).HasColumnType("timestamp without time zone");
            entity.Property(a => a.CreatedAt).HasColumnType("timestamp without time zone");
        });
    }
}
