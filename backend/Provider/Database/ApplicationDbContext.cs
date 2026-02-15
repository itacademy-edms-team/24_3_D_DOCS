using System.Linq;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RusalProject.Models.Entities;

namespace RusalProject.Provider.Database;

public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<SchemaLink> SchemaLinks { get; set; }
    public DbSet<DocumentLink> DocumentLinks { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<TitlePage> TitlePages { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AgentLog> AgentLogs { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<UserOllamaApiKey> UserOllamaApiKeys { get; set; }

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
            
            entity.HasMany<TitlePage>()
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

            entity.HasIndex(e => e.ProfileId)
                  .HasDatabaseName("IX_DocumentLinks_ProfileId");

            entity.HasIndex(e => e.TitlePageId)
                  .HasDatabaseName("IX_DocumentLinks_TitlePageId");

            entity.HasIndex(e => e.IsArchived)
                  .HasDatabaseName("IX_DocumentLinks_IsArchived");

            entity.HasIndex(e => e.DeletedAt)
                  .HasDatabaseName("IX_DocumentLinks_DeletedAt");

            entity.Property(e => e.Status)
                  .HasDefaultValue("draft");

            entity.Property(e => e.IsArchived)
                  .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");

            // Configure Metadata as text (changed from jsonb to avoid conversion issues)
            // The Metadata is stored as serialized JSON string
            entity.Property(e => e.Metadata)
                  .HasColumnType("text");

            // Foreign keys
            entity.HasOne(d => d.Profile)
                  .WithMany()
                  .HasForeignKey(d => d.ProfileId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.TitlePage)
                  .WithMany()
                  .HasForeignKey(d => d.TitlePageId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure DocumentVersion entity
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("IX_DocumentVersions_DocumentId");

            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_DocumentVersions_CreatedAt");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(v => v.Document)
                  .WithMany()
                  .HasForeignKey(v => v.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TitlePage entity
        modelBuilder.Entity<TitlePage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_TitlePages_CreatorId");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");
        });

        // Configure Attachment entity
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CreatorId)
                  .HasDatabaseName("IX_Attachments_CreatorId");

            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("IX_Attachments_DocumentId");

            entity.HasIndex(e => e.DeletedAt)
                  .HasDatabaseName("IX_Attachments_DeletedAt");

            entity.Property(e => e.FileName)
                  .HasMaxLength(260);

            entity.Property(e => e.ContentType)
                  .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.CreatorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Document)
                  .WithMany()
                  .HasForeignKey(a => a.DocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ChatSession entity
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("IX_ChatSessions_DocumentId");
            
            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("IX_ChatSessions_UserId");
            
            entity.HasIndex(e => e.IsArchived)
                  .HasDatabaseName("IX_ChatSessions_IsArchived");
            
            entity.HasIndex(e => e.DeletedAt)
                  .HasDatabaseName("IX_ChatSessions_DeletedAt");

            entity.Property(e => e.IsArchived)
                  .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(c => c.Document)
                  .WithMany()
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.ChatSessionId)
                  .HasDatabaseName("IX_ChatMessages_ChatSessionId");

            entity.HasIndex(e => new { e.ChatSessionId, e.ClientMessageId })
                  .HasDatabaseName("IX_ChatMessages_ChatSessionId_ClientMessageId")
                  .HasFilter("\"client_message_id\" IS NOT NULL")
                  .IsUnique();

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(m => m.ChatSession)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(m => m.ChatSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserOllamaApiKey entity
        modelBuilder.Entity<UserOllamaApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.UserId)
                  .IsUnique()
                  .HasDatabaseName("IX_UserOllamaApiKeys_UserId");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AgentLog entity
        modelBuilder.Entity<AgentLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("IX_AgentLogs_DocumentId");
            
            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("IX_AgentLogs_UserId");
            
            entity.HasIndex(e => e.ChatSessionId)
                  .HasDatabaseName("IX_AgentLogs_ChatSessionId");
            
            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_AgentLogs_Timestamp");
            
            entity.HasIndex(e => e.LogType)
                  .HasDatabaseName("IX_AgentLogs_LogType");

            entity.Property(e => e.Timestamp)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(a => a.Document)
                  .WithMany()
                  .HasForeignKey(a => a.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.ChatSession)
                  .WithMany()
                  .HasForeignKey(a => a.ChatSessionId)
                  .OnDelete(DeleteBehavior.SetNull);
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
            else if (entry.Entity is TitlePage titlePage)
            {
                titlePage.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ChatSession chatSession)
            {
                chatSession.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is UserOllamaApiKey userOllamaApiKey)
            {
                userOllamaApiKey.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

