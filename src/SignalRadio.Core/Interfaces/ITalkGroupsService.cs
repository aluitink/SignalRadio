using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ITalkGroupsService
{
    Task<IReadOnlyList<TalkGroupDto>> GetAllAsync(CancellationToken ct = default);
    Task<TalkGroupDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TalkGroupDto?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<TalkGroupDto> CreateAsync(CreateTalkGroupRequest request, CancellationToken ct = default);
    Task<TalkGroupDto> UpdateAsync(int id, UpdateTalkGroupRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

