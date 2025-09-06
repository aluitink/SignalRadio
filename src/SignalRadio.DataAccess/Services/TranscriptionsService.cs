using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Models;
using SignalRadio.Core.AI.Models;

namespace SignalRadio.DataAccess.Services;

public class TranscriptionsService : ITranscriptionsService
{
    private readonly SignalRadioDbContext _db;

    public TranscriptionsService(SignalRadioDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Transcription>> GetAllAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.Transcriptions.Include(t => t.Recording).AsNoTracking().OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Transcription>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<Transcription?> GetByIdAsync(int id)
    {
        return await _db.Transcriptions.Include(t => t.Recording).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transcription> CreateAsync(Transcription model)
    {
        _db.Transcriptions.Add(model);
        await _db.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateAsync(int id, Transcription model)
    {
        if (id != model.Id) return false;
        var exists = await _db.Transcriptions.AnyAsync(t => t.Id == id);
        if (!exists) return false;
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Transcriptions.FindAsync(id);
        if (item == null) return false;
        _db.Transcriptions.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<Transcription>> SearchAsync(string q, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var param = new SqlParameter("@p0", q);

        var query = _db.Transcriptions
            .FromSqlRaw("SELECT * FROM [Transcriptions] WHERE CONTAINS([FullText], @p0)", param)
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
                    .ThenInclude(c => c!.TalkGroup)
            .AsNoTracking();

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Transcription>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<PagedResult<Call>> SearchCallsAsync(string q, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var param = new SqlParameter("@p0", q);

        // First, get transcriptions that match the search
        var transcriptionQuery = _db.Transcriptions
            .FromSqlRaw("SELECT * FROM [Transcriptions] WHERE CONTAINS([FullText], @p0)", param)
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
                    .ThenInclude(c => c!.TalkGroup)
            .AsNoTracking();

        // Get unique call IDs from the transcription search results
        var callIds = await transcriptionQuery
            .Where(t => t.Recording != null && t.Recording.Call != null)
            .Select(t => t.Recording!.CallId)
            .Distinct()
            .ToListAsync();

        // Now get the calls with their full data
        var callQuery = _db.Calls
            .Where(c => callIds.Contains(c.Id))
            .Include(c => c.TalkGroup)
            .Include(c => c.Recordings)
                .ThenInclude(r => r.Transcriptions)
            .AsNoTracking()
            .OrderByDescending(c => c.RecordingTime);

        var total = await callQuery.CountAsync();
        var calls = await callQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Call>
        {
            Items = calls,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
    
    // AI Summary methods implementation
    public async Task<IEnumerable<Transcription>> GetTranscriptionsNeedingSummaryAsync(int limit = 10)
    {
        return await _db.Transcriptions
            .Where(t => t.IsFinal && 
                       !t.HasSummary && 
                       !string.IsNullOrWhiteSpace(t.FullText) &&
                       t.SummaryAttempts < 3) // Limit retry attempts
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
                    .ThenInclude(c => c!.TalkGroup)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
    
    public async Task<bool> UpdateTranscriptionSummaryAsync(int transcriptionId, SummaryResult? summaryResult, string? errorMessage = null)
    {
        var transcription = await _db.Transcriptions.FindAsync(transcriptionId);
        if (transcription == null)
            return false;

        if (summaryResult != null && summaryResult.IsSuccessful)
        {
            transcription.HasSummary = true;
            transcription.SummaryText = summaryResult.Summary;
            transcription.SummaryModel = summaryResult.Model;
            transcription.SummaryConfidence = summaryResult.Confidence;
            transcription.SummaryGeneratedAt = DateTimeOffset.UtcNow;
            transcription.SummaryProcessingTimeMs = summaryResult.ProcessingTimeMs;
            transcription.LastSummaryError = null;
        }
        else
        {
            transcription.LastSummaryError = errorMessage ?? summaryResult?.ErrorMessage ?? "Unknown error";
            transcription.SummaryAttempts++;
        }

        _db.Entry(transcription).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }
    
    public async Task<IEnumerable<Transcription>> GetTranscriptionsWithSummariesAsync(int? recordingId = null, int limit = 50)
    {
        var query = _db.Transcriptions
            .Where(t => t.HasSummary)
            .Include(t => t.Recording)
                .ThenInclude(r => r!.Call)
                    .ThenInclude(c => c!.TalkGroup)
            .AsNoTracking();

        if (recordingId.HasValue)
        {
            query = query.Where(t => t.RecordingId == recordingId.Value);
        }

        return await query
            .OrderByDescending(t => t.SummaryGeneratedAt)
            .Take(limit)
            .ToListAsync();
    }
}
