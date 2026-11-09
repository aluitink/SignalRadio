using Microsoft.EntityFrameworkCore;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data;

public class SignalRadioDbContext : DbContext
{
    public SignalRadioDbContext(DbContextOptions<SignalRadioDbContext> options) : base(options) { }

    public DbSet<TalkGroup> TalkGroups => Set<TalkGroup>();
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<Recording> Recordings => Set<Recording>();
    public DbSet<Transcription> Transcriptions => Set<Transcription>();
    public DbSet<TranscriptSummary> TranscriptSummaries => Set<TranscriptSummary>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<NotableIncident> NotableIncidents => Set<NotableIncident>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<TranscriptSummaryTopic> TranscriptSummaryTopics => Set<TranscriptSummaryTopic>();
    public DbSet<TranscriptSummaryNotableIncident> TranscriptSummaryNotableIncidents => Set<TranscriptSummaryNotableIncident>();
    public DbSet<TranscriptSummaryCall> TranscriptSummaryCalls => Set<TranscriptSummaryCall>();
    public DbSet<NotableIncidentCall> NotableIncidentCalls => Set<NotableIncidentCall>();
    public DbSet<Investigation> Investigations => Set<Investigation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TalkGroup
        modelBuilder.Entity<TalkGroup>(entity =>
        {
            entity.HasIndex(e => e.Number).IsUnique();
            entity.HasIndex(e => e.AlphaTag);
            entity.HasIndex(e => e.Tag);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Priority);
        });

        // Call
        modelBuilder.Entity<Call>(entity =>
        {
            // Natural key: a call is uniquely identified by talk group + time + frequency.
            // Prevents duplicate Call rows on re-upload of the same trunk-recorder event.
            entity.HasIndex(e => new { e.TalkGroupId, e.RecordingTime, e.FrequencyHz }).IsUnique();
            entity.HasOne(e => e.TalkGroup)
                .WithMany(t => t.Calls)
                .HasForeignKey(e => e.TalkGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Recording
        modelBuilder.Entity<Recording>(entity =>
        {
            entity.HasIndex(e => e.ReceivedAt);
            entity.HasIndex(e => new { e.CallId, e.ReceivedAt });
            entity.HasOne(e => e.Call)
                .WithMany(c => c.Recordings)
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.StorageLocation)
                .WithMany(s => s.Recordings)
                .HasForeignKey(e => e.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Transcription
        modelBuilder.Entity<Transcription>(entity =>
        {
            entity.HasIndex(e => new { e.RecordingId, e.IsFinal });
            entity.HasIndex(e => e.CreatedAt);
            // Embedding column is managed by raw SQL (pgvector), not EF Core
            entity.Ignore(e => e.Embedding);
            entity.HasOne(e => e.Recording)
                .WithMany(r => r.Transcriptions)
                .HasForeignKey(e => e.RecordingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TranscriptSummary
        modelBuilder.Entity<TranscriptSummary>(entity =>
        {
            entity.HasIndex(e => new { e.TalkGroupId, e.StartTime, e.EndTime });
            entity.HasIndex(e => new { e.TalkGroupId, e.GeneratedAt });
            entity.Ignore(e => e.Embedding);
            entity.HasOne(e => e.TalkGroup)
                .WithMany()
                .HasForeignKey(e => e.TalkGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Topic
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // NotableIncident
        modelBuilder.Entity<NotableIncident>(entity =>
        {
            entity.HasIndex(e => e.ImportanceScore);
            entity.HasIndex(e => e.TalkGroupId);
            entity.HasOne(e => e.TalkGroup)
                .WithMany()
                .HasForeignKey(e => e.TalkGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TranscriptSummaryTopic (join)
        modelBuilder.Entity<TranscriptSummaryTopic>(entity =>
        {
            entity.HasKey(e => new { e.TranscriptSummaryId, e.TopicId });
            entity.HasOne(e => e.TranscriptSummary)
                .WithMany(t => t.TranscriptSummaryTopics)
                .HasForeignKey(e => e.TranscriptSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Topic)
                .WithMany(t => t.TranscriptSummaryTopics)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TranscriptSummaryNotableIncident (join)
        modelBuilder.Entity<TranscriptSummaryNotableIncident>(entity =>
        {
            entity.HasKey(e => new { e.TranscriptSummaryId, e.NotableIncidentId });
            entity.HasOne(e => e.TranscriptSummary)
                .WithMany(t => t.TranscriptSummaryNotableIncidents)
                .HasForeignKey(e => e.TranscriptSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.NotableIncident)
                .WithMany(n => n.TranscriptSummaryNotableIncidents)
                .HasForeignKey(e => e.NotableIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TranscriptSummaryCall (join)
        modelBuilder.Entity<TranscriptSummaryCall>(entity =>
        {
            entity.HasKey(e => new { e.TranscriptSummaryId, e.CallId });
            // Support lookups of summaries by call (composite key leads with TranscriptSummaryId).
            entity.HasIndex(e => e.CallId);
            entity.HasOne(e => e.TranscriptSummary)
                .WithMany(t => t.TranscriptSummaryCalls)
                .HasForeignKey(e => e.TranscriptSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Call)
                .WithMany()
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NotableIncidentCall (join)
        modelBuilder.Entity<NotableIncidentCall>(entity =>
        {
            entity.HasKey(e => new { e.NotableIncidentId, e.CallId });
            // Support lookups of incidents by call (composite key leads with NotableIncidentId).
            entity.HasIndex(e => e.CallId);
            entity.HasOne(e => e.NotableIncident)
                .WithMany(n => n.NotableIncidentCalls)
                .HasForeignKey(e => e.NotableIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Call)
                .WithMany()
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Investigation
        modelBuilder.Entity<Investigation>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.CallId, e.CreatedAt });
            // SessionJson holds the serialized MAF AgentSession (chat history) — can be large.
            entity.Property(e => e.SessionJson).HasColumnType("text");
            entity.HasOne(e => e.Call)
                .WithMany()
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ParentInvestigation)
                .WithMany(p => p.FollowUps)
                .HasForeignKey(e => e.ParentInvestigationId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
