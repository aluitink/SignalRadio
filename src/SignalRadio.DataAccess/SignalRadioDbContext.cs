using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SignalRadio.DataAccess;

public class SignalRadioDbContext : DbContext
{
    public SignalRadioDbContext(DbContextOptions<SignalRadioDbContext> options) : base(options)
    {
    }

    public DbSet<TalkGroup> TalkGroups { get; set; } = null!;
    public DbSet<Call> Calls { get; set; } = null!;
    public DbSet<Recording> Recordings { get; set; } = null!;
    public DbSet<StorageLocation> StorageLocations { get; set; } = null!;
    public DbSet<Transcription> Transcriptions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Call>(b =>
        {
            b.Property(e => e.RecordingTime).HasColumnName("RecordingTimeUtc");
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            b.HasOne(e => e.TalkGroup).WithMany(t => t.Calls).HasForeignKey(e => e.TalkGroupId);
            b.HasIndex(e => new { e.TalkGroupId, e.RecordingTime });
            b.Property(e => e.FrequencyHz).HasColumnType("float");
        });

        modelBuilder.Entity<Recording>(b =>
        {
            b.Property(e => e.ReceivedAt).HasColumnName("ReceivedAtUtc");
            b.HasOne(e => e.Call).WithMany(c => c.Recordings).HasForeignKey(e => e.CallId);
            b.HasOne(e => e.StorageLocation).WithMany(s => s.Recordings).HasForeignKey(e => e.StorageLocationId);
            // Index ReceivedAt (used for ordering). Mark descending to match common OrderByDescending queries.
            b.HasIndex(e => e.ReceivedAt).IsDescending();
        });

        modelBuilder.Entity<StorageLocation>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
        });

        modelBuilder.Entity<Transcription>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            b.Property(e => e.FullText).HasColumnType("nvarchar(max)");
            b.Property(e => e.AdditionalDataJson).HasColumnType("nvarchar(max)");
            b.HasOne(t => t.Recording).WithMany(r => r.Transcriptions).HasForeignKey(t => t.RecordingId);
            b.HasIndex(t => t.Service);

            // Index to efficiently find whether a Recording has any final Transcription.
            // This supports queries that check for existence of IsFinal records per Recording.
            b.HasIndex(t => new { t.RecordingId, t.IsFinal });
        });

        modelBuilder.Entity<TalkGroup>(b =>
        {
            b.HasIndex(t => t.Number);

            // Priority is used in ordering for transcription prioritization.
            b.HasIndex(t => t.Priority);
            // Useful lookup/indexes for admin & grouping
            b.HasIndex(t => t.AlphaTag);
            b.HasIndex(t => t.Tag);
            b.HasIndex(t => t.Category);
        });
    }
}
