using System.IO;
using SignalRadio.Core.Dtos;

namespace SignalRadio.Core.Interfaces;

public interface IRecordingsService
{
    Task<RecordingDto> UploadAsync(long callId, string fileName, Stream content, long sizeBytes, CancellationToken ct = default);
    Task<(RecordingDto Recording, CallDto Call)> CreateWithFileAsync(Stream content, string fileName, RecordingUploadRequest request, CancellationToken ct = default);
    Task<RecordingDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Stream?> GetFileAsync(long id, CancellationToken ct = default);
    Task<Stream?> GetFileByCallIdAsync(long callId, CancellationToken ct = default);
    Task<Stream?> GetFileByFileNameAsync(string fileName, CancellationToken ct = default);
    Task<IReadOnlyList<RecordingDto>> GetNeedingTranscriptionAsync(int limit, double minDurationSeconds, int lookbackHours, CancellationToken ct = default);
    Task MarkProcessedAsync(long id, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}
