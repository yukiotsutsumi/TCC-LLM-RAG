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

        // Índice HNSW para otimizar buscas KNN
        modelBuilder.Entity<Chunk>()
            .HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);

        // Configuração para KnnRow (usado em consultas SQL raw)
        modelBuilder.Entity<KnnRow>(e =>
        {
            e.HasNoKey();
            e.Property(x => x.Embedding).HasColumnType("vector(1024)");
        });

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
    }
}