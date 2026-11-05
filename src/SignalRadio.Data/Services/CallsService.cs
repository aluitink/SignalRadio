using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class CallsService(SignalRadioDbContext db) : ICallsService
{
    public async Task<PagedResult<CallDto>> GetPagedAsync(int talkGroupId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Calls
            .Where(c => c.TalkGroupId == talkGroupId)
            .OrderByDescending(c => c.RecordingTime);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CallDto(
                c.Id, c.TalkGroupId, c.TalkGroup!.Number, c.TalkGroup.Name,
                c.RecordingTime, c.FrequencyHz, c.DurationSeconds, c.CreatedAt,
                c.Recordings.Count,
                c.Recordings.OrderByDescending(r => r.ReceivedAt).Select(r => r.Id).FirstOrDefault()))
            .ToListAsync(ct);

        return new PagedResult<CallDto>(items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<CallDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var call = await db.Calls
            .Include(c => c.TalkGroup)
            .Include(c => c.Recordings)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (call == null) return null;
        var latestRecordingId = call.Recordings.OrderByDescending(r => r.ReceivedAt).Select(r => r.Id).FirstOrDefault();
        return new CallDto(call.Id, call.TalkGroupId, call.TalkGroup!.Number, call.TalkGroup.Name,
            call.RecordingTime, call.FrequencyHz, call.DurationSeconds, call.CreatedAt, call.Recordings.Count, latestRecordingId);
    }

    public async Task<CallDto> CreateAsync(CreateCallRequest request, CancellationToken ct = default)
    {
        var entity = new Call
        {
            TalkGroupId = request.TalkGroupId,
            RecordingTime = request.RecordingTime,
            FrequencyHz = request.FrequencyHz,
            DurationSeconds = request.DurationSeconds,
            CreatedAt = DateTime.UtcNow
        };
        db.Calls.Add(entity);
        await db.SaveChangesAsync(ct);
        var tg = await db.TalkGroups.FindAsync([request.TalkGroupId], ct);
        return new CallDto(entity.Id, entity.TalkGroupId, tg!.Number, tg.Name,
            entity.RecordingTime, entity.FrequencyHz, entity.DurationSeconds, entity.CreatedAt, 0);
    }

    public async Task<IReadOnlyList<CallDto>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        return await db.Calls
            .Include(c => c.TalkGroup)
            .OrderByDescending(c => c.RecordingTime)
            .Take(count)
            .Select(c => new CallDto(
                c.Id, c.TalkGroupId, c.TalkGroup!.Number, c.TalkGroup.Name,
                c.RecordingTime, c.FrequencyHz, c.DurationSeconds, c.CreatedAt,
                c.Recordings.Count,
                c.Recordings.OrderByDescending(r => r.ReceivedAt).Select(r => r.Id).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<CallDto>> GetRecentPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Calls.OrderByDescending(c => c.RecordingTime);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CallDto(
                c.Id, c.TalkGroupId, c.TalkGroup!.Number, c.TalkGroup.Name,
                c.RecordingTime, c.FrequencyHz, c.DurationSeconds, c.CreatedAt,
                c.Recordings.Count,
                c.Recordings.OrderByDescending(r => r.ReceivedAt).Select(r => r.Id).FirstOrDefault()))
            .ToListAsync(ct);

        return new PagedResult<CallDto>(items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Calls.FindAsync([id], ct);
        if (entity == null) return false;
        db.Calls.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
