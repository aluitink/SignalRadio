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

    public async Task<IReadOnlyList<CallVolumeDto>> GetVolumeAsync(DateTimeOffset? from, DateTimeOffset? to, int? talkGroupId, int? bucketMinutes, CancellationToken ct = default)
    {
        to ??= DateTimeOffset.UtcNow;
        from ??= to.Value.AddHours(-24);
        if (from >= to) return [];

        var bucket = ResolveBucket(bucketMinutes, to.Value - from.Value);

        var query = db.Calls.AsNoTracking()
            .Where(c => c.RecordingTime >= from && c.RecordingTime < to);

        if (talkGroupId is int tgId)
        {
            query = query.Where(c => c.TalkGroupId == tgId);
        }

        var raw = await query
            .Select(c => new { c.RecordingTime, c.DurationSeconds })
            .ToListAsync(ct);

        var rows = raw
            .GroupBy(c => c.RecordingTime.AddTicks(-c.RecordingTime.Ticks % bucket.Ticks))
            .Select(g => new CallVolumeDto(g.Key, g.Count(), g.Sum(c => c.DurationSeconds)))
            .OrderBy(v => v.BucketStart)
            .ToList();

        // Fill empty buckets so the client gets a contiguous series.
        var byBucket = rows.ToDictionary(v => v.BucketStart);
        var result = new List<CallVolumeDto>();
        for (var t = from.Value; t < to.Value; t += bucket)
        {
            var start = t.AddTicks(-t.Ticks % bucket.Ticks);
            result.Add(byBucket.TryGetValue(start, out var row) ? row : new CallVolumeDto(start, 0, 0));
        }
        return result;
    }

    public async Task<IReadOnlyList<CallVolumeByTalkGroupDto>> GetVolumeByTalkGroupAsync(DateTimeOffset? from, DateTimeOffset? to, int? bucketMinutes, CancellationToken ct = default)
    {
        to ??= DateTimeOffset.UtcNow;
        from ??= to.Value.AddHours(-24);
        if (from >= to) return [];

        var bucket = ResolveBucket(bucketMinutes, to.Value - from.Value);

        var raw = await db.Calls.AsNoTracking()
            .Where(c => c.RecordingTime >= from && c.RecordingTime < to)
            .Select(c => new { c.TalkGroupId, c.TalkGroup!.Number, c.TalkGroup.Name, c.RecordingTime, c.DurationSeconds })
            .ToListAsync(ct);

        var byBucket = raw
            .GroupBy(c => c.RecordingTime.AddTicks(-c.RecordingTime.Ticks % bucket.Ticks))
            .ToDictionary(
                g => g.Key,
                g => g
                    .GroupBy(c => c.TalkGroupId)
                    .Select(tg => new TalkGroupVolumeDto(
                        tg.Key,
                        tg.First().Number,
                        tg.First().Name,
                        tg.Count(),
                        tg.Sum(c => c.DurationSeconds)))
                    .ToList());

        var result = new List<CallVolumeByTalkGroupDto>();
        for (var t = from.Value; t < to.Value; t += bucket)
        {
            var start = t.AddTicks(-t.Ticks % bucket.Ticks);
            var tgs = byBucket.TryGetValue(start, out var list) ? list : [];
            result.Add(new CallVolumeByTalkGroupDto(start, tgs));
        }
        return result;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Calls.FindAsync([id], ct);
        if (entity == null) return false;
        db.Calls.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Resolves the bucket size: uses the requested size when given, otherwise
    /// picks a sensible default for the window (~48 buckets).
    /// </summary>
    private static TimeSpan ResolveBucket(int? bucketMinutes, TimeSpan span)
    {
        if (bucketMinutes is int m && m > 0)
        {
            return TimeSpan.FromMinutes(m);
        }
        var candidates = new[] { 1, 5, 15, 30, 60, 120, 240, 360, 720, 1440, 4320, 10080, 20160, 43200 };
        var ideal = span.TotalMinutes / 48;
        return TimeSpan.FromMinutes(candidates.Min(c => Math.Abs(c - ideal)));
    }
}
