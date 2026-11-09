using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class TranscriptSummariesService(SignalRadioDbContext db) : ITranscriptSummariesService
{
    public async Task<IReadOnlyList<TranscriptSummaryDto>> GetByTalkGroupAsync(int talkGroupId, DateTimeOffset? startTime, DateTimeOffset? endTime, CancellationToken ct = default)
    {
        var query = db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics).ThenInclude(t => t.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents).ThenInclude(n => n.NotableIncident)
            .Where(s => s.TalkGroupId == talkGroupId);

        if (startTime.HasValue) query = query.Where(s => s.StartTime >= startTime.Value);
        if (endTime.HasValue) query = query.Where(s => s.EndTime <= endTime.Value);

        var summaries = await query.OrderByDescending(s => s.StartTime).ToListAsync(ct);
        return summaries.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<TranscriptSummaryDto>> GetRecentAsync(int count = 10, CancellationToken ct = default)
    {
        var summaries = await db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics).ThenInclude(t => t.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents).ThenInclude(n => n.NotableIncident)
            .OrderByDescending(s => s.GeneratedAt)
            .Take(count)
            .ToListAsync(ct);
        return summaries.Select(ToDto).ToList();
    }

    public async Task<TranscriptSummaryDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var s = await db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics).ThenInclude(t => t.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents).ThenInclude(n => n.NotableIncident)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        return s == null ? null : ToDto(s);
    }

    public async Task<TranscriptSummaryDto?> GetLatestForTalkGroupAsync(int talkGroupId, CancellationToken ct = default)
    {
        var s = await db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Where(s => s.TalkGroupId == talkGroupId)
            .OrderByDescending(s => s.EndTime)
            .FirstOrDefaultAsync(ct);
        return s == null ? null : ToDto(s);
    }

    public async Task<IReadOnlyList<SummaryCallDto>> GetCallsAsync(long summaryId, CancellationToken ct = default)
    {
        var calls = await db.TranscriptSummaryCalls
            .Where(sc => sc.TranscriptSummaryId == summaryId)
            .Include(sc => sc.Call).ThenInclude(c => c!.TalkGroup)
            .OrderBy(sc => sc.Call!.RecordingTime)
            .Select(sc => new SummaryCallDto(
                sc.CallId,
                sc.Call!.TalkGroupId,
                sc.Call.TalkGroup!.Number,
                sc.Call.TalkGroup.Name,
                sc.Call.TalkGroup.Description,
                sc.Call.RecordingTime,
                sc.Call.FrequencyHz,
                sc.Call.DurationSeconds,
                sc.Call.Recordings.OrderByDescending(r => r.ReceivedAt).Select(r => r.Id).FirstOrDefault()))
            .ToListAsync(ct);
        return calls;
    }

    public async Task<TranscriptSummaryDto> CreateAsync(CreateTranscriptSummaryRequest request, CancellationToken ct = default)
    {
        var entity = new TranscriptSummary
        {
            TalkGroupId = request.TalkGroupId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TranscriptCount = request.TranscriptCount,
            TotalDurationSeconds = request.TotalDurationSeconds,
            Summary = request.Summary,
            Severity = request.Severity,
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var topicId in request.TopicIds)
        {
            entity.TranscriptSummaryTopics.Add(new TranscriptSummaryTopic { TopicId = topicId });
        }
        foreach (var incidentId in request.NotableIncidentIds)
        {
            entity.TranscriptSummaryNotableIncidents.Add(new TranscriptSummaryNotableIncident { NotableIncidentId = incidentId });
        }
        foreach (var callId in request.CallIds)
        {
            entity.TranscriptSummaryCalls.Add(new TranscriptSummaryCall { CallId = callId });
        }

        db.TranscriptSummaries.Add(entity);
        await db.SaveChangesAsync(ct);

        var dto = await GetByIdAsync(entity.Id, ct);
        return dto!;
    }

    private static TranscriptSummaryDto ToDto(TranscriptSummary s) => new(
        s.Id, s.TalkGroupId, s.TalkGroup!.Number, s.TalkGroup.Name, s.TalkGroup.Description,
        s.StartTime, s.EndTime, s.TranscriptCount, s.TotalDurationSeconds,
        s.Summary, s.Severity, s.GeneratedAt,
        s.TranscriptSummaryTopics.Select(t => new TopicDto(t.Topic!.Id, t.Topic.Name, t.Topic.Category)).ToList(),
        s.TranscriptSummaryNotableIncidents.Select(n => new NotableIncidentDto(n.NotableIncident!.Id, n.NotableIncident.Description, n.NotableIncident.ImportanceScore, n.NotableIncident.CreatedAt)).ToList(),
        s.TranscriptSummaryCalls.Select(c => c.CallId).ToList());
}
