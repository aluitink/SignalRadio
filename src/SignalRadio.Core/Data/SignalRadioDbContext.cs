using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.Data;

public class SignalRadioDbContext : DbContext
{
    public SignalRadioDbContext(DbContextOptions<SignalRadioDbContext> options) : base(options)
    {
    }

    public DbSet<Call> Calls { get; set; }
    public DbSet<Recording> Recordings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Call entity
        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TalkgroupId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SystemName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Frequency).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RecordingTime).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Indexes for common queries
            entity.HasIndex(e => e.TalkgroupId);
            entity.HasIndex(e => e.SystemName);
            entity.HasIndex(e => e.RecordingTime);
            entity.HasIndex(e => new { e.TalkgroupId, e.SystemName, e.RecordingTime });
        });

        // Configure Recording entity
        modelBuilder.Entity<Recording>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Format).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BlobUri).HasMaxLength(500);
            entity.Property(e => e.BlobName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Foreign key relationship
            entity.HasOne(e => e.Call)
                .WithMany(e => e.Recordings)
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indexes
            entity.HasIndex(e => e.CallId);
            entity.HasIndex(e => e.Format);
            entity.HasIndex(e => e.IsUploaded);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Call call)
            {
                if (entry.State == EntityState.Added)
                {
                    call.CreatedAt = DateTime.UtcNow;
                }
                call.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Recording recording)
            {
                if (entry.State == EntityState.Added)
                {
                    recording.CreatedAt = DateTime.UtcNow;
                }
                recording.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
