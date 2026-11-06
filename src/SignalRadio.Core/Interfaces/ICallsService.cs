using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface ICallsService
{
    Task<CallDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CallDto> CreateAsync(CreateCallRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CallDto>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<CursorPagedResult<CallDto>> GetCursorAsync(int pageSize, DateTimeOffset? beforeTime = null, long? beforeId = null, int? talkGroupId = null, IEnumerable<int>? talkGroupIds = null, CancellationToken ct = default);
    Task<IReadOnlyList<CallVolumeDto>> GetVolumeAsync(DateTimeOffset? from, DateTimeOffset? to, int? talkGroupId, int? bucketMinutes, CancellationToken ct = default);
    Task<IReadOnlyList<CallVolumeByTalkGroupDto>> GetVolumeByTalkGroupAsync(DateTimeOffset? from, DateTimeOffset? to, int? bucketMinutes, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}
