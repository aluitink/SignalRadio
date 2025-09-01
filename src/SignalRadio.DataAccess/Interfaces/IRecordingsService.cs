using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Services;

public interface IRecordingsService
{
    Task<PagedResult<Recording>> GetAllAsync(int page, int pageSize);
    Task<Recording?> GetByIdAsync(int id);
    Task<Recording> CreateAsync(Recording model);
    /// <summary>
    /// Create a Recording from an uploaded file stream and optional metadata DTOs.
    /// The service is responsible for storing the file (via IStorageService) and persisting the Recording and related entities.
    /// </summary>
    Task<Recording> CreateWithFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        SignalRadio.Core.Models.RecordingUploadRequest? uploadRequest = null,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, Recording model);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Recording>> GetRecordingsNeedingTranscriptionAsync(int limit = 10);
}
