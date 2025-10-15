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
    public DbSet<Schema> Schemas { get; set; }

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

            // One-to-many relationship: User -> Schemas
            entity.HasMany(e => e.Schemas)
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

        // Configure Schema entity
        modelBuilder.Entity<Schema>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_Schemas_CreatorId");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

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
            else if (entry.Entity is Schema schema)
            {
                schema.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

