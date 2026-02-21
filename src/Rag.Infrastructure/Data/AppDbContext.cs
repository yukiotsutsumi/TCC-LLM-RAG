using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Entities;
using Rag.Core.Domain.Models;

namespace Rag.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User, Role, Guid>(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("Documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id);
            e.Property(x => x.Title);
            e.Property(x => x.Source);
            e.Property(x => x.CreatedAt);
            e.HasMany(x => x.Chunks).WithOne(c => c.Document!).HasForeignKey(c => c.DocumentId);
        });

        modelBuilder.Entity<Chunk>(e =>
        {
            e.ToTable("Chunks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id);
            e.Property(x => x.DocumentId);
            e.Property(x => x.ChunkIndex);
            e.Property(x => x.Content);

            e.Property(x => x.Embedding)
                .HasColumnType("vector(1024)")
                .IsRequired();

            e.Property(x => x.MetadataJson).HasColumnType("jsonb");

            e.Property(x => x.UmapX);
            e.Property(x => x.UmapY);
        });

        modelBuilder.Entity<Chunk>()
            .HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);

        modelBuilder.Entity<KnnRow>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
            e.Property(x => x.Id);
            e.Property(x => x.DocumentId);
            e.Property(x => x.ChunkIndex);
            e.Property(x => x.Content);
            e.Property(x => x.Embedding).HasColumnType("vector(1024)");
            e.Property(x => x.MetadataJson);
            e.Property(x => x.UmapX);
            e.Property(x => x.UmapY);
            e.Property(x => x.Title);
            e.Property(x => x.Source);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.FamilyId);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RevokedToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Jti).IsUnique();
            e.HasIndex(x => x.ExpiresAt);
        });

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}