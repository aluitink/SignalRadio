using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface IInvestigationsService
{
    /// <summary>Create a new investigation (queued for background processing).</summary>
    Task<InvestigationDto> CreateAsync(CreateInvestigationRequest request, CancellationToken ct = default);

    /// <summary>Get a single investigation by ID.</summary>
    Task<InvestigationDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>List investigations, optionally filtered by status or call.</summary>
    Task<IReadOnlyList<InvestigationDto>> ListAsync(
        string? status = null,
        long? callId = null,
        bool includeViewed = true,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>Get unviewed completed investigations (for the ticker).</summary>
    Task<IReadOnlyList<InvestigationDto>> GetUnviewedCompletedAsync(int limit = 10, CancellationToken ct = default);

    /// <summary>Mark an investigation as viewed (dismisses it from the ticker).</summary>
    Task MarkViewedAsync(long id, CancellationToken ct = default);

    /// <summary>Delete an investigation and its follow-ups.</summary>
    Task DeleteAsync(long id, CancellationToken ct = default);
}
