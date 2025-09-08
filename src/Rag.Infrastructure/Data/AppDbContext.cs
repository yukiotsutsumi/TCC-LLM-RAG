using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Entities;

namespace Rag.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Source).HasColumnName("source");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasMany(x => x.Chunks).WithOne(c => c.Document!).HasForeignKey(c => c.DocumentId);
        });

        modelBuilder.Entity<Chunk>(e =>
        {
            e.ToTable("chunks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DocumentId).HasColumnName("document_id");
            e.Property(x => x.ChunkIndex).HasColumnName("chunk_index");
            e.Property(x => x.Content).HasColumnName("content");

            e.Property(x => x.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(768)");

            e.Property(x => x.MetadataJson)
                .HasColumnName("metadata_json")
                .HasColumnType("jsonb");

            e.Property(x => x.UmapX).HasColumnName("umap_x");
            e.Property(x => x.UmapY).HasColumnName("umap_y");
        });
    }
}