using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TripSplit.Server.Data;

public class TripDbContext(DbContextOptions<TripDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TripRecord> Trips => Set<TripRecord>();
    public DbSet<TripMembership> TripMemberships => Set<TripMembership>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TripRecord>(entity =>
        {
            entity.ToTable("Trip");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Json).IsRequired();
            entity.Property(t => t.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<TripMembership>(entity =>
        {
            entity.ToTable("TripMembership");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.TripId, m.UserId }).IsUnique();
        });

        modelBuilder.Entity<ShareLink>(entity =>
        {
            entity.ToTable("ShareLink");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Token).IsUnique();
        });
    }
}
