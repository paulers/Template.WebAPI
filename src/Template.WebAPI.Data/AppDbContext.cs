using Template.WebAPI.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Template.WebAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Home> Homes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Professional> Professionals { get; set; }
    public DbSet<ProfessionalType> ProfessionalTypes { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Remap the names to get rid of the AspNet thing in front...
        ConfigureUsersTable(builder);
        ConfigureHomeTable(builder);
        ConfigureRoomTable(builder);
        ConfigureProTypesTable(builder);
        ConfigureProsTable(builder);
        ConfigureInventoryTable(builder);
    }

    private void ConfigureHomeTable(ModelBuilder builder)
    {
        var b = builder.Entity<Home>();
        
        b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        b.Property(x => x.StreetAddress).HasMaxLength(255);
        b.Property(x => x.AddressLine2).HasMaxLength(255);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Country).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.StateProvince).HasMaxLength(100);
        b.Property(x => x.UserId).IsRequired();
        b.HasIndex(x => x.UserId);
        
        builder.Entity<Home>().ToTable("Homes");
    }

    private void ConfigureRoomTable(ModelBuilder builder)
    {
        var b = builder.Entity<Room>();
        b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        b.Property(x => x.HomeId).IsRequired();
        b.HasIndex(e => e.HomeId);
        
        builder.Entity<Room>().ToTable("Rooms");
    }

    private void ConfigureProTypesTable(ModelBuilder builder)
    {
        var b = builder.Entity<ProfessionalType>();
        b.Property(x => x.Name).IsRequired().HasMaxLength(255);

        // Keep these IDs stable because migrations use them to track the reference data.
        b.HasData(
            new ProfessionalType { Id = new Guid("b3767919-1a56-4a8c-b083-8943a53841f3"), Name = "Plumber" },
            new ProfessionalType { Id = new Guid("ce091f19-d24d-45bb-b1a0-0c557d5695df"), Name = "Electrician" },
            new ProfessionalType { Id = new Guid("20628bc2-8c45-4407-a964-c12d7f99f750"), Name = "HVAC technician" },
            new ProfessionalType { Id = new Guid("78bc6516-4e77-412d-b1c8-37ed556c9ddc"), Name = "Roofer" },
            new ProfessionalType { Id = new Guid("0e831d38-267d-461e-9dff-00718ff75e82"), Name = "General contractor" });
        
        builder.Entity<ProfessionalType>().ToTable("ProfessionalTypes");
    }

    private void ConfigureProsTable(ModelBuilder builder)
    {
        var b = builder.Entity<Professional>();
        b.Property(x => x.BusinessName).HasMaxLength(255);
        b.Property(x => x.BusinessPhoneNumber).HasMaxLength(50);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Country).HasMaxLength(100);
        b.Property(x => x.EmailAddress).HasMaxLength(100);
        b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        b.Property(x => x.PersonalPhoneNumber).HasMaxLength(50);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.ProfessionalTypeId).IsRequired();
        b.Property(x => x.StateProvince).HasMaxLength(100);
        b.Property(x => x.StreetAddress).HasMaxLength(255);
        b.Property(x => x.WebsiteUrl).HasMaxLength(255);
        b.Property(x => x.UserId).IsRequired();
        b.HasIndex(x => x.UserId);

        b.HasOne(x => x.ProfessionalType)
            .WithMany()
            .HasForeignKey(x => x.ProfessionalTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<Professional>().ToTable("Professionals");
    }

    private void ConfigureInventoryTable(ModelBuilder builder)
    {
        var b = builder.Entity<InventoryItem>();
        b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.SerialNumber).HasMaxLength(50);
        
        b.Property(x => x.UserId).IsRequired();
        b.HasIndex(x => x.UserId);
        
        b.Property(x => x.HomeId).IsRequired();
        b.HasIndex(x => x.HomeId);

        b.HasOne(x => x.Room)
            .WithMany(x => x.InventoryItems)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Entity<InventoryItem>().ToTable("InventoryItems");
    }

    private void ConfigureUsersTable(ModelBuilder builder)
    {
        var u = builder.Entity<AppUser>();
        u.HasMany(x => x.Homes)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }
}
