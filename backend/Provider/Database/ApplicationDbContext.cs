using Microsoft.EntityFrameworkCore;
using RusalProject.Models.Entities;

namespace RusalProject.Provider.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<TitlePage> TitlePages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Unique index on email
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Email");
            
            entity.HasIndex(e => e.Role)
                  .HasDatabaseName("IX_Users_Role");

            // One-to-many relationships: User -> Profiles, User -> Documents, User -> TitlePages
            entity.HasMany(e => e.Profiles)
                  .WithOne(e => e.Creator)
                  .HasForeignKey(e => e.CreatorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Documents)
                  .WithOne(e => e.Creator)
                  .HasForeignKey(e => e.CreatorId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Default values
            entity.Property(e => e.Role)
                  .HasDefaultValue("User");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");
        });

        // Configure Profile entity
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_Profiles_CreatorId");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");
        });

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_Documents_CreatorId");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");
        });

        // Configure TitlePage entity
        modelBuilder.Entity<TitlePage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_TitlePages_CreatorId");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");
        });
    }

    // Override SaveChanges to automatically update UpdatedAt
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Profile profile)
            {
                profile.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Document document)
            {
                document.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is TitlePage titlePage)
            {
                titlePage.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

