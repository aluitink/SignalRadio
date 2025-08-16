using System.ComponentModel.DataAnnotations;

namespace SignalRadio.Core.Models;

public class RecordingMetadata
{
    public string TalkgroupId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public DateTime RecordingTime { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFormat { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public string? BlobUri { get; set; }
    public string? BlobName { get; set; }
}

public class RecordingUploadRequest
{
    [Required]
    public string TalkgroupId { get; set; } = string.Empty;

    [Required]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    public string SystemName { get; set; } = string.Empty;
}

public class StorageResult
{
    public bool IsSuccess { get; set; }
    public string BlobUri { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public long UploadedBytes { get; set; }
}

public class AzureStorageOptions
{
    public const string Section = "AzureStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "recordings";
    public string DefaultPathPattern { get; set; } = "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}";
}
