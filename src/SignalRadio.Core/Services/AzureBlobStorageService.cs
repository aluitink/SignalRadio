using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Models;
using System.Text.Json;

namespace SignalRadio.Core.Services;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(
        IOptions<AzureStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.ConnectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is not configured");
        }

        _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async Task<StorageResult> UploadRecordingAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        RecordingMetadata metadata)
    {
        try
        {
            // Ensure container exists
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobName = GenerateBlobName(metadata, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            // Create metadata for the blob
            var blobMetadata = new Dictionary<string, string>
            {
                ["TalkgroupId"] = SanitizeMetadataValue(metadata.TalkgroupId),
                ["SystemName"] = SanitizeMetadataValue(metadata.SystemName),
                ["RecordingTime"] = metadata.RecordingTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["Frequency"] = SanitizeMetadataValue(metadata.Frequency),
                ["OriginalFileName"] = SanitizeMetadataValue(fileName),
                ["OriginalFormat"] = SanitizeMetadataValue(metadata.OriginalFormat),
                ["OriginalSize"] = metadata.OriginalSize.ToString(),
                ["UploadedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            fileStream.Position = 0; // Reset stream position

            var uploadResponse = await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    Metadata = blobMetadata,
                    Conditions = null // Allow overwrite
                });

            _logger.LogInformation("Successfully uploaded blob: {BlobName} ({Size:N0} bytes)", 
                blobName, fileStream.Length);

            return new StorageResult
            {
                IsSuccess = true,
                BlobUri = blobClient.Uri.ToString(),
                BlobName = blobName,
                UploadedBytes = fileStream.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload recording to blob storage: {FileName}", fileName);
            return new StorageResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<Stream?> DownloadRecordingAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("Blob not found: {BlobName}", blobName);
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download recording: {BlobName}", blobName);
            return null;
        }
    }

    public async Task<bool> DeleteRecordingAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync();
            
            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob: {BlobName}", blobName);
            }
            else
            {
                _logger.LogWarning("Blob not found for deletion: {BlobName}", blobName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recording: {BlobName}", blobName);
            return false;
        }
    }

    public async Task<IEnumerable<RecordingMetadata>> ListRecordingsAsync(
        string? systemName = null,
        string? talkgroupId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var recordings = new List<RecordingMetadata>();
            var prefix = BuildSearchPrefix(systemName, talkgroupId, fromDate);

            await foreach (var blobItem in _containerClient.GetBlobsAsync(
                traits: BlobTraits.Metadata,
                prefix: prefix))
            {
                var metadata = ParseBlobMetadata(blobItem);
                if (metadata != null && IsWithinDateRange(metadata, fromDate, toDate))
                {
                    recordings.Add(metadata);
                }
            }

            return recordings.OrderByDescending(r => r.RecordingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list recordings");
            return Enumerable.Empty<RecordingMetadata>();
        }
    }

    public string GenerateBlobName(RecordingMetadata metadata, string originalFileName)
    {
        var pathPattern = _options.DefaultPathPattern
            .Replace("{SystemName}", SanitizePath(metadata.SystemName))
            .Replace("{TalkgroupId}", SanitizePath(metadata.TalkgroupId))
            .Replace("{Year}", metadata.RecordingTime.Year.ToString())
            .Replace("{Month}", metadata.RecordingTime.Month.ToString("D2"))
            .Replace("{Day}", metadata.RecordingTime.Day.ToString("D2"));

        var timestamp = metadata.RecordingTime.ToString("yyyyMMdd-HHmmss");
        var fileExtension = Path.GetExtension(originalFileName);
        var sanitizedFrequency = SanitizePath(metadata.Frequency);
        var fileName = $"{timestamp}-{sanitizedFrequency}Hz{fileExtension}";

        var blobName = $"{pathPattern}/{fileName}";
        
        _logger.LogDebug("Generated blob name: {BlobName} for file: {OriginalFileName}", blobName, originalFileName);
        
        return blobName;
    }

    private string SanitizePath(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unknown";

        // Replace invalid characters for blob storage paths
        // Azure blob names cannot contain: \ / : * ? " < > | and some others
        var invalidChars = new char[] { '\\', ':', '*', '?', '"', '<', '>', '|', '\t', '\r', '\n' };
        var sanitized = input;
        
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '-');
        }

        // Remove any control characters and non-printable characters
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "");
        
        // Replace multiple consecutive dashes with single dash
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"-+", "-");
        
        // Remove leading/trailing dashes and whitespace
        sanitized = sanitized.Trim('-', ' ');

        // Ensure it's not empty after sanitization
        if (string.IsNullOrEmpty(sanitized))
            return "unknown";

        return sanitized;
    }

    private string SanitizeMetadataValue(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unknown";

        // Azure Blob Storage metadata values must be ASCII and cannot contain certain characters
        // Remove or replace non-ASCII characters and control characters
        var sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"[^\x20-\x7E]", "");
        
        // Replace problematic characters that might cause header issues
        var invalidChars = new char[] { '\r', '\n', '\t', '"', '\'' };
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, ' ');
        }

        // Ensure the value is not too long (Azure has a 8KB limit for all metadata combined)
        if (sanitized.Length > 256)
        {
            sanitized = sanitized.Substring(0, 256);
        }

        return sanitized.Trim();
    }

    private string? BuildSearchPrefix(string? systemName, string? talkgroupId, DateTime? fromDate)
    {
        if (string.IsNullOrEmpty(systemName))
            return null;

        var prefix = SanitizePath(systemName);

        if (!string.IsNullOrEmpty(talkgroupId))
        {
            prefix += "/" + SanitizePath(talkgroupId);

            if (fromDate.HasValue)
            {
                prefix += "/" + fromDate.Value.Year.ToString();
            }
        }

        return prefix;
    }

    private RecordingMetadata? ParseBlobMetadata(BlobItem blobItem)
    {
        try
        {
            if (blobItem.Metadata == null)
                return null;

            var metadata = new RecordingMetadata
            {
                BlobName = blobItem.Name,
                BlobUri = _containerClient.GetBlobClient(blobItem.Name).Uri.ToString()
            };

            if (blobItem.Metadata.TryGetValue("TalkgroupId", out var talkgroupId))
                metadata.TalkgroupId = talkgroupId;

            if (blobItem.Metadata.TryGetValue("SystemName", out var systemName))
                metadata.SystemName = systemName;

            if (blobItem.Metadata.TryGetValue("RecordingTime", out var recordingTimeStr) &&
                DateTime.TryParse(recordingTimeStr, out var recordingTime))
                metadata.RecordingTime = recordingTime;

            if (blobItem.Metadata.TryGetValue("Frequency", out var frequency))
                metadata.Frequency = frequency;

            if (blobItem.Metadata.TryGetValue("OriginalFileName", out var originalFileName))
                metadata.FileName = originalFileName;

            if (blobItem.Metadata.TryGetValue("OriginalFormat", out var originalFormat))
                metadata.OriginalFormat = originalFormat;

            if (blobItem.Metadata.TryGetValue("OriginalSize", out var originalSizeStr) &&
                long.TryParse(originalSizeStr, out var originalSize))
                metadata.OriginalSize = originalSize;

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse blob metadata for: {BlobName}", blobItem.Name);
            return null;
        }
    }

    private bool IsWithinDateRange(RecordingMetadata metadata, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && metadata.RecordingTime < fromDate.Value)
            return false;

        if (toDate.HasValue && metadata.RecordingTime > toDate.Value)
            return false;

        return true;
    }
}
