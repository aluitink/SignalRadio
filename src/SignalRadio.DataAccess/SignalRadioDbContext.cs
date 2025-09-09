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
    public DbSet<TranscriptSummary> TranscriptSummaries { get; set; } = null!;
    public DbSet<Topic> Topics { get; set; } = null!;
    public DbSet<TranscriptSummaryTopic> TranscriptSummaryTopics { get; set; } = null!;
    public DbSet<NotableIncident> NotableIncidents { get; set; } = null!;
    public DbSet<NotableIncidentCall> NotableIncidentCalls { get; set; } = null!;
    public DbSet<TranscriptSummaryNotableIncident> TranscriptSummaryNotableIncidents { get; set; } = null!;

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

        modelBuilder.Entity<TranscriptSummary>(b =>
        {
            b.Property(e => e.GeneratedAt).HasColumnName("GeneratedAtUtc");
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            b.Property(e => e.StartTime).HasColumnName("StartTimeUtc");
            b.Property(e => e.EndTime).HasColumnName("EndTimeUtc");
            b.Property(e => e.Summary).HasColumnType("nvarchar(max)");
            
            b.HasOne(e => e.TalkGroup).WithMany().HasForeignKey(e => e.TalkGroupId);
            
            // Index for time range queries - most common query pattern
            b.HasIndex(e => new { e.TalkGroupId, e.StartTime, e.EndTime });
            // Index for finding existing summaries for cache lookup
            b.HasIndex(e => new { e.TalkGroupId, e.GeneratedAt });
        });

        // Topic entity configuration
        modelBuilder.Entity<Topic>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            b.Property(e => e.Name).HasMaxLength(200).IsRequired();
            b.Property(e => e.Category).HasMaxLength(100);
            
            // Index for topic searches
            b.HasIndex(e => e.Name);
            b.HasIndex(e => e.Category);
            
            // Ensure topic names are unique
            b.HasIndex(e => e.Name).IsUnique();
        });

        // TranscriptSummaryTopic linking table configuration
        modelBuilder.Entity<TranscriptSummaryTopic>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            
            b.HasOne(e => e.TranscriptSummary)
                .WithMany(s => s.TranscriptSummaryTopics)
                .HasForeignKey(e => e.TranscriptSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasOne(e => e.Topic)
                .WithMany(t => t.TranscriptSummaryTopics)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasIndex(e => e.TranscriptSummaryId);
            b.HasIndex(e => e.TopicId);
            
            // Unique constraint to prevent duplicate associations
            b.HasIndex(e => new { e.TranscriptSummaryId, e.TopicId }).IsUnique();
        });

        // NotableIncident entity configuration
        modelBuilder.Entity<NotableIncident>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            b.Property(e => e.Description).HasColumnType("nvarchar(max)").IsRequired();
            
            // Index for importance-based queries
            b.HasIndex(e => e.ImportanceScore);
        });

        // NotableIncidentCall linking table configuration
        modelBuilder.Entity<NotableIncidentCall>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            b.Property(e => e.CallNote).HasMaxLength(500);
            
            b.HasOne(e => e.NotableIncident)
                .WithMany(ni => ni.NotableIncidentCalls)
                .HasForeignKey(e => e.NotableIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasOne(e => e.Call)
                .WithMany()
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete calls when incident is deleted
                
            b.HasIndex(e => e.NotableIncidentId);
            b.HasIndex(e => e.CallId);
            
            // Unique constraint to prevent duplicate associations
            b.HasIndex(e => new { e.NotableIncidentId, e.CallId }).IsUnique();
        });

        // TranscriptSummaryNotableIncident linking table configuration
        modelBuilder.Entity<TranscriptSummaryNotableIncident>(b =>
        {
            b.Property(e => e.CreatedAt).HasColumnName("CreatedAtUtc");
            
            b.HasOne(e => e.TranscriptSummary)
                .WithMany(s => s.TranscriptSummaryNotableIncidents)
                .HasForeignKey(e => e.TranscriptSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasOne(e => e.NotableIncident)
                .WithMany(ni => ni.TranscriptSummaryNotableIncidents)
                .HasForeignKey(e => e.NotableIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasIndex(e => e.TranscriptSummaryId);
            b.HasIndex(e => e.NotableIncidentId);
            
            // Unique constraint to prevent duplicate associations
            b.HasIndex(e => new { e.TranscriptSummaryId, e.NotableIncidentId }).IsUnique();
        });
    }
}
