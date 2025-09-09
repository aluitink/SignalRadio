using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;
using SignalRadio.DataAccess.Extensions;

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

    public async Task<SearchResultPage> SearchAsync(string searchTerm, IEnumerable<string>? contentTypes = null, int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new SearchResultPage { Results = Enumerable.Empty<SearchResult>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }

        var typesToSearch = contentTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase) 
                           ?? new HashSet<string> { "Summary", "Incident", "Topic" };

        var results = new List<SearchResult>();

        // Search summaries if requested
        if (typesToSearch.Contains("Summary"))
        {
            var summaryResults = await _db.TranscriptSummaries
                .AsQueryable()
                .WhereFreeTextContains(searchTerm)
                .Include(ts => ts.TalkGroup)
                .Select(ts => new SearchResult
                {
                    Type = "Summary",
                    Id = ts.Id,
                    Title = $"Summary for {ts.TalkGroup!.AlphaTag ?? ts.TalkGroup.Tag} ({ts.StartTime:yyyy-MM-dd HH:mm} - {ts.EndTime:HH:mm})",
                    Content = ts.Summary.Length > 200 ? ts.Summary.Substring(0, 200) + "..." : ts.Summary,
                    CreatedAt = ts.CreatedAt,
                    Relevance = 1.0
                })
                .ToListAsync();

            results.AddRange(summaryResults);
        }

        // Search incidents if requested
        if (typesToSearch.Contains("Incident"))
        {
            var incidentResults = await _db.NotableIncidents
                .AsQueryable()
                .WhereFreeTextContains(searchTerm)
                .Select(ni => new SearchResult
                {
                    Type = "Incident",
                    Id = ni.Id,
                    Title = $"Notable Incident (Score: {ni.ImportanceScore:F1})",
                    Content = ni.Description.Length > 200 ? ni.Description.Substring(0, 200) + "..." : ni.Description,
                    CreatedAt = ni.CreatedAt,
                    Relevance = ni.ImportanceScore ?? 0.5
                })
                .ToListAsync();

            results.AddRange(incidentResults);
        }

        // Search topics if requested
        if (typesToSearch.Contains("Topic"))
        {
            var topicResults = await _db.Topics
                .AsQueryable()
                .WhereFreeTextContains(searchTerm)
                .Select(t => new SearchResult
                {
                    Type = "Topic",
                    Id = t.Id,
                    Title = t.Name,
                    Content = t.Category ?? "No category",
                    CreatedAt = t.CreatedAt,
                    Relevance = 0.8
                })
                .ToListAsync();

            results.AddRange(topicResults);
        }

        // Sort and paginate
        var sortedResults = results
            .OrderByDescending(r => r.Relevance)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

        var totalCount = sortedResults.Count;
        var pagedResults = sortedResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new SearchResultPage
        {
            Results = pagedResults,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<TranscriptSummary>> SearchSummariesAsync(string searchTerm, int? talkGroupId = null, 
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<TranscriptSummary>();
        }

        IQueryable<TranscriptSummary> query = _db.TranscriptSummaries
            .AsQueryable()
            .WhereFreeTextContains(searchTerm);

        // Apply filters before includes to optimize query
        if (talkGroupId.HasValue)
        {
            query = query.Where(ts => ts.TalkGroupId == talkGroupId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(ts => ts.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(ts => ts.EndTime <= endDate.Value);
        }

        // Add includes after filtering
        query = query
            .Include(ts => ts.TalkGroup)
            .Include(ts => ts.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(ts => ts.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident);

        return await query
            .OrderByDescending(ts => ts.CreatedAt)
            .Take(maxResults)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<NotableIncident>> SearchIncidentsAsync(string searchTerm, double? minImportanceScore = null, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<NotableIncident>();
        }

        var query = _db.NotableIncidents
            .AsQueryable()
            .WhereFreeTextContains(searchTerm);

        if (minImportanceScore.HasValue)
        {
            query = query.Where(ni => ni.ImportanceScore >= minImportanceScore.Value);
        }

        return await query
            .OrderByDescending(ni => ni.ImportanceScore ?? 0)
            .ThenByDescending(ni => ni.CreatedAt)
            .Take(maxResults)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Topic>> SearchTopicsAsync(string searchTerm, string? category = null, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<Topic>();
        }

        var query = _db.Topics
            .AsQueryable()
            .WhereFreeTextContains(searchTerm);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category == category);
        }

        return await query
            .OrderBy(t => t.Name)
            .Take(maxResults)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TranscriptSummary?> FindSimilarSummaryAsync(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime, int toleranceMinutes = 10)
    {
        // Look for summaries that cover similar time ranges within tolerance
        var toleranceSpan = TimeSpan.FromMinutes(toleranceMinutes);
        var startTolerance = startTime.Add(-toleranceSpan);
        var endTolerance = endTime.Add(toleranceSpan);
        
        return await _db.TranscriptSummaries
            .Include(s => s.TalkGroup)
            .Include(s => s.TranscriptSummaryTopics)
                .ThenInclude(st => st.Topic)
            .Include(s => s.TranscriptSummaryNotableIncidents)
                .ThenInclude(sni => sni.NotableIncident!)
                    .ThenInclude(ni => ni.NotableIncidentCalls)
                        .ThenInclude(nic => nic.Call)
            .Where(s => s.TalkGroupId == talkGroupId &&
                       s.StartTime >= startTolerance && s.StartTime <= endTolerance &&
                       s.EndTime >= startTolerance && s.EndTime <= endTolerance)
            .OrderByDescending(s => s.GeneratedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
}
