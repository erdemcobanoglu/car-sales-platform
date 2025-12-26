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
    public DbSet<VehiclePhoto> VehiclePhotos => Set<VehiclePhoto>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ===== LOOKUPS =====
        b.Entity<Make>()
            .HasIndex(x => x.Name)
            .IsUnique();

        b.Entity<VehicleModel>()
            .HasIndex(x => new { x.MakeId, x.Name })
            .IsUnique();

        b.Entity<Trim>()
            .HasIndex(x => new { x.ModelId, x.Name })
            .IsUnique();

        // ===== VEHICLE =====
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

        // ===== VEHICLE PHOTO =====
        b.Entity<VehiclePhoto>()
            .Property(p => p.Url)
            .IsRequired()
            .HasMaxLength(500);

        b.Entity<VehiclePhoto>()
            .Property(p => p.IsCover)
            .HasDefaultValue(false);

        b.Entity<VehiclePhoto>()
            .HasOne(p => p.Vehicle)
            .WithMany(v => v.Photos)
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Aynı araçta aynı SortOrder olamaz
        b.Entity<VehiclePhoto>()
            .HasIndex(p => new { p.VehicleId, p.SortOrder })
            .IsUnique();

        // Aynı araçta sadece 1 tane cover olsun (SQL Server filtered unique index)
        b.Entity<VehiclePhoto>()
            .HasIndex(p => new { p.VehicleId, p.IsCover })
            .IsUnique()
            .HasFilter("[IsCover] = 1");
    }
}
