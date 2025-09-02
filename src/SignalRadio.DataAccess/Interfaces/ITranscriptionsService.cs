using System.Threading.Tasks;
using SignalRadio.Core.Models;

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
}
