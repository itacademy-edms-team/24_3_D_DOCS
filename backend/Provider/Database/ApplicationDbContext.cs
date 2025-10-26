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
    public DbSet<SchemaLink> SchemaLinks { get; set; }
    public DbSet<DocumentLink> DocumentLinks { get; set; }

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

            // One-to-many relationships: User -> SchemaLinks, User -> DocumentLinks
            entity.HasMany(e => e.SchemaLinks)
                  .WithOne(e => e.Creator)
                  .HasForeignKey(e => e.CreatorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DocumentLinks)
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

        // Configure SchemaLink entity
        modelBuilder.Entity<SchemaLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_SchemaLinks_CreatorId");

            entity.HasIndex(e => e.IsPublic)
                  .HasDatabaseName("IX_SchemaLinks_IsPublic");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");
        });

        // Configure DocumentLink entity
        modelBuilder.Entity<DocumentLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_DocumentLinks_CreatorId");

            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_DocumentLinks_Status");

            entity.Property(e => e.Status)
                  .HasDefaultValue("draft");

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
            else if (entry.Entity is SchemaLink schemaLink)
            {
                schemaLink.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is DocumentLink documentLink)
            {
                documentLink.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

