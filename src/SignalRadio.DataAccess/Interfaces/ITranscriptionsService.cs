using System.Threading.Tasks;
using SignalRadio.Core.Models;
using SignalRadio.Core.AI.Models;

namespace SignalRadio.DataAccess.Services;

public interface ITranscriptionsService
{
    Task<PagedResult<Transcription>> GetAllAsync(int page, int pageSize);
    Task<Transcription?> GetByIdAsync(int id);
    Task<Transcription> CreateAsync(Transcription model);
    Task<bool> UpdateAsync(int id, Transcription model);
    Task<bool> DeleteAsync(int id);
    Task<PagedResult<Transcription>> SearchAsync(string q, int page, int pageSize);
    Task<PagedResult<Call>> SearchCallsAsync(string q, int page, int pageSize);
    
    // AI Summary methods
    Task<IEnumerable<Transcription>> GetTranscriptionsNeedingSummaryAsync(int limit = 10);
    Task<bool> UpdateTranscriptionSummaryAsync(int transcriptionId, SummaryResult? summaryResult, string? errorMessage = null);
    Task<IEnumerable<Transcription>> GetTranscriptionsWithSummariesAsync(int? recordingId = null, int limit = 50);
}
