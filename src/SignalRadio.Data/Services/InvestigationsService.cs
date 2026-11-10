using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class InvestigationsService(SignalRadioDbContext db) : IInvestigationsService
{
    public async Task<InvestigationDto> CreateAsync(CreateInvestigationRequest request, CancellationToken ct = default)
    {
        var call = await db.Calls
            .Include(c => c.TalkGroup)
            .FirstOrDefaultAsync(c => c.Id == request.CallId, ct)
            ?? throw new InvalidOperationException($"Call {request.CallId} not found.");

        var investigation = new Investigation
        {
            CallId = request.CallId,
            TalkGroupId = request.TalkGroupId,
            WindowMinutes = Math.Clamp(request.WindowMinutes, 5, 120),
            Prompt = request.Prompt,
            ParentInvestigationId = request.ParentInvestigationId,
            Status = InvestigationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Call = call,
        };

        // Persist the user's prompt as the chat's first message so it shows up in the thread
        // immediately (and in history if the turn fails).
        investigation.Messages.Add(new InvestigationChatMessage
        {
            Index = 1,
            Role = "user",
            Text = request.Prompt,
            CreatedAt = DateTime.UtcNow,
        });

        db.Investigations.Add(investigation);
        await db.SaveChangesAsync(ct);

        return ToDto(investigation);
    }

    public async Task<InvestigationDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var inv = await db.Investigations
            .Include(i => i.Call)
            .ThenInclude(c => c!.TalkGroup)
            .Include(i => i.Messages)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return inv is null ? null : ToDto(inv);
    }

    public async Task<IReadOnlyList<InvestigationDto>> ListAsync(
        string? status = null,
        long? callId = null,
        bool includeViewed = true,
        int limit = 50,
        CancellationToken ct = default)
    {
        var query = db.Investigations
            .Include(i => i.Call)
            .ThenInclude(c => c!.TalkGroup)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<InvestigationStatus>(status, ignoreCase: true, out var parsed))
                query = query.Where(i => i.Status == parsed);
        }

        if (callId is not null)
            query = query.Where(i => i.CallId == callId);

        if (!includeViewed)
            query = query.Where(i => !i.IsViewed);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return items.Select(i => ToDto(i)).ToList();
    }

    public async Task<IReadOnlyList<InvestigationDto>> GetUnviewedCompletedAsync(int limit = 10, CancellationToken ct = default)
    {
        var items = await db.Investigations
            .Include(i => i.Call)
            .ThenInclude(c => c!.TalkGroup)
            .Where(i => i.Status == InvestigationStatus.Completed && !i.IsViewed)
            .OrderByDescending(i => i.CompletedAt)
            .Take(limit)
            .ToListAsync(ct);

        return items.Select(i => ToDto(i)).ToList();
    }

    public async Task MarkViewedAsync(long id, CancellationToken ct = default)
    {
        var inv = await db.Investigations.FindAsync(new object[] { id }, ct);
        if (inv is null) return;
        inv.IsViewed = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var inv = await db.Investigations
            .Include(i => i.FollowUps)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inv is null) return;

        // Delete follow-ups first (they reference this as parent).
        db.Investigations.RemoveRange(inv.FollowUps);
        db.Investigations.Remove(inv);
        await db.SaveChangesAsync(ct);
    }

    private static InvestigationDto ToDto(Investigation i) => new(
        i.Id,
        i.CallId,
        i.Call?.TalkGroup?.Number ?? "",
        i.Call?.TalkGroup?.Name,
        i.Call?.RecordingTime ?? DateTimeOffset.MinValue,
        i.WindowMinutes,
        i.Prompt,
        i.ParentInvestigationId,
        i.Status.ToString(),
        i.CreatedAt,
        i.StartedAt,
        i.CompletedAt,
        i.Error,
        i.IsViewed,
        i.Messages
            .OrderBy(m => m.Index)
            .Select(m => new InvestigationChatMessageDto(
                m.Id,
                m.Index,
                m.Role,
                m.Text,
                m.ToolName,
                m.ToolArgs,
                m.ToolResult,
                m.ToolCallId,
                m.Error,
                m.CreatedAt))
            .ToList());
}
