namespace SignalRadio.Core.Models;

public class StorageResult
{
    public bool IsSuccess { get; set; }
    public string BlobUri { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public long UploadedBytes { get; set; }
}
