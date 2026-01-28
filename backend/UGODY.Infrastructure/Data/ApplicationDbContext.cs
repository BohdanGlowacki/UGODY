using Microsoft.EntityFrameworkCore;
using UGODY.Core.Entities;

namespace UGODY.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PdfFile> PdfFiles { get; set; }
    public DbSet<OcrResult> OcrResults { get; set; }
    public DbSet<Configuration> Configurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PdfFile configuration
        modelBuilder.Entity<PdfFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FileContent).IsRequired();
            entity.Property(e => e.Hash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.LastModifiedDate).IsRequired();

            // Indexes for performance
            entity.HasIndex(e => e.Hash).IsUnique();
            entity.HasIndex(e => e.FileName);
        });

        // OcrResult configuration
        modelBuilder.Entity<OcrResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PdfFileId).IsRequired();
            entity.Property(e => e.ExtractedText).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProcessedDate).IsRequired();
            entity.Property(e => e.ProcessingStatus).IsRequired();

            // Foreign key relationship
            entity.HasOne(e => e.PdfFile)
                .WithMany(p => p.OcrResults)
                .HasForeignKey(e => e.PdfFileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration configuration
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.UpdatedDate).IsRequired();

            // Unique index on Key
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
