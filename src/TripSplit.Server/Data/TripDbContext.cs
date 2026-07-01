using Microsoft.EntityFrameworkCore;

namespace TripSplit.Server.Data;

public class TripDbContext(DbContextOptions<TripDbContext> options) : DbContext(options)
{
    public DbSet<TripRecord> Trips => Set<TripRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TripRecord>(entity =>
        {
            entity.ToTable("Trip");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Json).IsRequired();
        });
    }
}
