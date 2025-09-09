using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public class TranscriptSummariesService : ITranscriptSummariesService
{
    private readonly SignalRadioDbContext _db;

    public TranscriptSummariesService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<TranscriptSummary>> GetByTalkGroupAndTimeRangeAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident!)
                    .ThenInclude(ni => ni.NotableIncidentCalls)
                        .ThenInclude(nic => nic.Call)
            .Where(s => s.TalkGroupId == talkGroupId &&
                       s.StartTime <= endTime &&
                       s.EndTime >= startTime)
            .OrderByDescending(s => s.GeneratedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TranscriptSummary?> GetByIdAsync(int id)
    {
        return await _db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident!)
                    .ThenInclude(ni => ni.NotableIncidentCalls)
                        .ThenInclude(nic => nic.Call)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<TranscriptSummary> CreateAsync(TranscriptSummary summary)
    {
        summary.CreatedAt = DateTimeOffset.UtcNow;
        
        // Handle topics - ensure they exist or create them
        foreach (var topicLink in summary.TranscriptSummaryTopics)
        {
            topicLink.CreatedAt = DateTimeOffset.UtcNow;
            
            // If topic doesn't have an ID, we need to find or create it
            if (topicLink.TopicId == 0 && topicLink.Topic != null)
            {
                var existingTopic = await _db.Topics
                    .FirstOrDefaultAsync(t => t.Name == topicLink.Topic.Name);
                
                if (existingTopic != null)
                {
                    topicLink.TopicId = existingTopic.Id;
                    topicLink.Topic = null; // Don't create duplicate
                }
                else
                {
                    topicLink.Topic.CreatedAt = DateTimeOffset.UtcNow;
                }
            }
        }
        
        // Handle notable incidents
        foreach (var incidentLink in summary.TranscriptSummaryNotableIncidents)
        {
            incidentLink.CreatedAt = DateTimeOffset.UtcNow;
            
            if (incidentLink.NotableIncident != null)
            {
                incidentLink.NotableIncident.CreatedAt = DateTimeOffset.UtcNow;
                
                // Handle calls within incidents
                foreach (var callLink in incidentLink.NotableIncident.NotableIncidentCalls)
                {
                    callLink.CreatedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        _db.TranscriptSummaries.Add(summary);
        await _db.SaveChangesAsync();
        
        // Return the entity with generated IDs
        return await GetByIdAsync(summary.Id) ?? summary;
    }

    public async Task<TranscriptSummary> UpdateAsync(TranscriptSummary summary)
    {
        var existing = await _db.TranscriptSummaries
            .Include(s => s.TranscriptSummaryTopics)
            .Include(s => s.TranscriptSummaryNotableIncidents)
            .FirstOrDefaultAsync(s => s.Id == summary.Id);

        if (existing == null)
        {
            throw new ArgumentException($"TranscriptSummary with ID {summary.Id} not found");
        }

        // Update main properties
        existing.Summary = summary.Summary;
        existing.TranscriptCount = summary.TranscriptCount;
        existing.TotalDurationSeconds = summary.TotalDurationSeconds;
        existing.GeneratedAt = summary.GeneratedAt;

        // Remove existing topic and incident links
        _db.TranscriptSummaryTopics.RemoveRange(existing.TranscriptSummaryTopics);
        _db.TranscriptSummaryNotableIncidents.RemoveRange(existing.TranscriptSummaryNotableIncidents);

        // Add new topic links
        foreach (var topicLink in summary.TranscriptSummaryTopics)
        {
            topicLink.TranscriptSummaryId = existing.Id;
            topicLink.CreatedAt = DateTimeOffset.UtcNow;
            _db.TranscriptSummaryTopics.Add(topicLink);
        }

        // Add new incident links
        foreach (var incidentLink in summary.TranscriptSummaryNotableIncidents)
        {
            incidentLink.TranscriptSummaryId = existing.Id;
            incidentLink.CreatedAt = DateTimeOffset.UtcNow;
            _db.TranscriptSummaryNotableIncidents.Add(incidentLink);
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(existing.Id) ?? existing;
    }

    public async Task DeleteAsync(int id)
    {
        var summary = await _db.TranscriptSummaries.FindAsync(id);
        if (summary != null)
        {
            _db.TranscriptSummaries.Remove(summary);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<TranscriptSummary?> FindExistingSummaryAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident!)
                    .ThenInclude(ni => ni.NotableIncidentCalls)
                        .ThenInclude(nic => nic.Call)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TalkGroupId == talkGroupId &&
                                    s.StartTime == startTime &&
                                    s.EndTime == endTime);
    }

    public async Task<PagedResult<TranscriptSummary>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident)
            .AsNoTracking()
            .OrderByDescending(s => s.GeneratedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<TranscriptSummary>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<PagedResult<TranscriptSummary>> GetByTalkGroupAsync(int talkGroupId, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident)
            .Where(s => s.TalkGroupId == talkGroupId)
            .AsNoTracking()
            .OrderByDescending(s => s.GeneratedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<TranscriptSummary>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<IEnumerable<TranscriptSummaryTopic>> SearchTopicsAsync(string searchTerm)
    {
        return await _db.TranscriptSummaryTopics
            .Include(t => t.TranscriptSummary)
                .ThenInclude(s => s!.TalkGroup)
            .Include(t => t.Topic)
            .Where(t => t.Topic != null && t.Topic.Name.Contains(searchTerm))
            .AsNoTracking()
            .OrderByDescending(t => t.Relevance ?? 0)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotableIncidentCall>> GetNotableCallsByCallIdAsync(int callId)
    {
        return await _db.NotableIncidentCalls
            .Include(nic => nic.NotableIncident)
            .Include(nic => nic.Call)
            .Where(nic => nic.CallId == callId)
            .AsNoTracking()
            .OrderByDescending(nic => nic.NotableIncident!.ImportanceScore ?? 0)
            .ToListAsync();
    }
}
