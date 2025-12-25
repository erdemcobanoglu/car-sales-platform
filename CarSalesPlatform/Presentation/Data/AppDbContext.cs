using Microsoft.EntityFrameworkCore;
using Presentation.Models; 

namespace Presentation.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Make> Makes => Set<Make>();
    public DbSet<VehicleModel> Models => Set<VehicleModel>();
    public DbSet<Trim> Trims => Set<Trim>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Make>()
            .HasIndex(x => x.Name).IsUnique();

        b.Entity<VehicleModel>()
            .HasIndex(x => new { x.MakeId, x.Name }).IsUnique();

        b.Entity<Trim>()
            .HasIndex(x => new { x.ModelId, x.Name }).IsUnique();

        b.Entity<Vehicle>()
            .Property(x => x.EngineLiters)
            .HasPrecision(3, 1);

        b.Entity<Vehicle>()
            .HasOne(x => x.Make)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.MakeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Vehicle>()
            .HasOne(x => x.Model)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Vehicle>()
            .HasOne(x => x.Trim)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.TrimId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

