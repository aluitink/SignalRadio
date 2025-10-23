using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

public interface IStorageService
{
    Task<StorageResult> UploadRecordingAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        RecordingMetadata metadata);

    Task<Stream?> DownloadRecordingAsync(string blobName);

    Task<byte[]?> DownloadFileAsync(string blobName, CancellationToken cancellationToken = default);

    Task<bool> DeleteRecordingAsync(string blobName);

    Task<IEnumerable<RecordingMetadata>> ListRecordingsAsync(
        string? systemName = null,
        string? talkgroupId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    string GenerateBlobName(RecordingMetadata metadata, string originalFileName);
}
