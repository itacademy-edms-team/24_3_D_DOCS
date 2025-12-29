using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<TitlePage> TitlePages { get; set; }
    public DbSet<DocumentBlock> DocumentBlocks { get; set; }
    public DbSet<BlockEmbedding> BlockEmbeddings { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Attachment> Attachments { get; set; }

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

            // Configure Metadata as jsonb
            // Convert string to JsonDocument for proper jsonb storage in PostgreSQL
            // Npgsql requires JsonDocument for jsonb type, not plain string
            entity.Property(e => e.Metadata)
                  .HasColumnType("jsonb")
                  .HasConversion(
                      v => v == null ? null : JsonDocument.Parse(v, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }),
                      v => v == null ? null : v.RootElement.GetRawText());

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

        // Configure DocumentBlock entity
        modelBuilder.Entity<DocumentBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("IX_DocumentBlocks_DocumentId");

            entity.HasIndex(e => e.ContentHash)
                  .HasDatabaseName("IX_DocumentBlocks_ContentHash");

            entity.HasIndex(e => e.DeletedAt)
                  .HasDatabaseName("IX_DocumentBlocks_DeletedAt");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(d => d.Document)
                  .WithMany()
                  .HasForeignKey(d => d.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // Configure BlockEmbedding entity
        modelBuilder.Entity<BlockEmbedding>(entity =>
        {
            entity.HasKey(e => e.BlockId);
            
            entity.HasIndex(e => e.Model)
                  .HasDatabaseName("IX_BlockEmbeddings_Model");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            // Configure vector type - use PostgreSQL array type as workaround
            // Vector operations will be done via raw SQL queries
            entity.Property(e => e.Embedding)
                  .HasColumnType("real[]"); // Use PostgreSQL real array instead of vector for now

            entity.HasOne(e => e.Block)
                  .WithOne(b => b.Embedding)
                  .HasForeignKey<BlockEmbedding>(e => e.BlockId)
                  .OnDelete(DeleteBehavior.Cascade);
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

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(m => m.ChatSession)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(m => m.ChatSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
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
            else if (entry.Entity is DocumentBlock documentBlock)
            {
                documentBlock.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ChatSession chatSession)
            {
                chatSession.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

