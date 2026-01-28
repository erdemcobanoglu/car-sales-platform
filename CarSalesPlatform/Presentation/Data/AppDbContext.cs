using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Presentation.Models;

namespace Presentation.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Make> Makes => Set<Make>();
    public DbSet<VehicleModel> Models => Set<VehicleModel>();
    public DbSet<Trim> Trims => Set<Trim>();
    public DbSet<VehiclePhoto> VehiclePhotos => Set<VehiclePhoto>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

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

        // =========================
        // VEHICLE OWNER (Identity)
        // =========================
        b.Entity<Vehicle>()
            .Property(x => x.OwnerId)
            .IsRequired();

        b.Entity<Vehicle>()
            .HasIndex(x => x.OwnerId); // ✅ performans için

        b.Entity<Vehicle>()
            .HasOne(x => x.Owner)
            .WithMany() // ApplicationUser tarafında collection şart değil
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== VEHICLE =====
        b.Entity<Vehicle>()
            .Property(x => x.EngineLiters)
            .HasPrecision(3, 1);

        b.Entity<Vehicle>()
            .Property(x => x.Price)
            .HasPrecision(12, 0);

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

        b.Entity<VehiclePhoto>()
            .HasIndex(p => new { p.VehicleId, p.SortOrder })
            .IsUnique();

        b.Entity<VehiclePhoto>()
            .HasIndex(p => new { p.VehicleId, p.IsCover })
            .IsUnique()
            .HasFilter("[IsCover] = 1");

        // ===== USER PROFILE =====
        b.Entity<UserProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<UserProfile>()
            .Property(p => p.FullName)
            .HasMaxLength(120);

        b.Entity<UserProfile>()
            .Property(p => p.Phone)
            .HasMaxLength(30);

        b.Entity<UserProfile>()
            .Property(p => p.City)
            .HasMaxLength(200);
    }
}
