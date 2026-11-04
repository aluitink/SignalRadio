using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Dtos;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Entities;

namespace SignalRadio.Data.Services;

public class TalkGroupsService(SignalRadioDbContext db) : ITalkGroupsService
{
    public async Task<IReadOnlyList<TalkGroupDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.TalkGroups
            .OrderBy(t => t.Number)
            .Select(t => new TalkGroupDto(t.Id, t.Number, t.Name, t.AlphaTag, t.Tag, t.Description, t.Category, t.Priority, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TalkGroupDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var tg = await db.TalkGroups.FindAsync([id], ct);
        return tg == null ? null : new TalkGroupDto(tg.Id, tg.Number, tg.Name, tg.AlphaTag, tg.Tag, tg.Description, tg.Category, tg.Priority, tg.CreatedAt);
    }

    public async Task<TalkGroupDto?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        var tg = await db.TalkGroups.FirstOrDefaultAsync(t => t.Number == number, ct);
        return tg == null ? null : new TalkGroupDto(tg.Id, tg.Number, tg.Name, tg.AlphaTag, tg.Tag, tg.Description, tg.Category, tg.Priority, tg.CreatedAt);
    }

    public async Task<TalkGroupDto> CreateAsync(CreateTalkGroupRequest request, CancellationToken ct = default)
    {
        var entity = new TalkGroup
        {
            Number = request.Number,
            Name = request.Name,
            AlphaTag = request.AlphaTag,
            Tag = request.Tag,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            CreatedAt = DateTime.UtcNow
        };
        db.TalkGroups.Add(entity);
        await db.SaveChangesAsync(ct);
        return new TalkGroupDto(entity.Id, entity.Number, entity.Name, entity.AlphaTag, entity.Tag, entity.Description, entity.Category, entity.Priority, entity.CreatedAt);
    }

    public async Task<TalkGroupDto> UpdateAsync(int id, UpdateTalkGroupRequest request, CancellationToken ct = default)
    {
        var entity = await db.TalkGroups.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"TalkGroup {id} not found.");
        if (request.Name != null) entity.Name = request.Name;
        if (request.AlphaTag != null) entity.AlphaTag = request.AlphaTag;
        if (request.Tag != null) entity.Tag = request.Tag;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Category != null) entity.Category = request.Category;
        if (request.Priority.HasValue) entity.Priority = request.Priority;
        await db.SaveChangesAsync(ct);
        return new TalkGroupDto(entity.Id, entity.Number, entity.Name, entity.AlphaTag, entity.Tag, entity.Description, entity.Category, entity.Priority, entity.CreatedAt);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.TalkGroups.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"TalkGroup {id} not found.");
        db.TalkGroups.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
