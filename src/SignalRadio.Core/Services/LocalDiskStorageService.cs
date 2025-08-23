using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Models;
using System.Text.Json;
using System.IO;

namespace SignalRadio.Core.Services
{
    public class LocalDiskStorageService : IStorageService
    {
        private readonly LocalStorageOptions _options;
        private readonly ILogger<LocalDiskStorageService> _logger;

        public LocalDiskStorageService(IOptions<LocalStorageOptions> options, ILogger<LocalDiskStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // Ensure base path exists
            try
            {
                Directory.CreateDirectory(_options.BasePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create base path: {BasePath}", _options.BasePath);
            }
        }

        public async Task<StorageResult> UploadRecordingAsync(Stream fileStream, string fileName, string contentType, RecordingMetadata metadata)
        {
            try
            {
                var blobName = GenerateBlobName(metadata, fileName);

                var relativePath = blobName.Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_options.BasePath, relativePath);

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                fileStream.Position = 0;
                using (var outFile = File.Create(fullPath))
                {
                    await fileStream.CopyToAsync(outFile);
                }

                // Write metadata sidecar file
                var meta = new Dictionary<string, object>
                {
                    ["TalkgroupId"] = metadata.TalkgroupId,
                    ["SystemName"] = metadata.SystemName,
                    ["RecordingTime"] = metadata.RecordingTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["Frequency"] = metadata.Frequency,
                    ["OriginalFileName"] = fileName,
                    ["OriginalFormat"] = metadata.OriginalFormat,
                    ["OriginalSize"] = metadata.OriginalSize,
                    ["UploadedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var metaPath = fullPath + ".metadata.json";
                var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metaPath, json);

                var fileUri = new Uri(Path.GetFullPath(fullPath)).AbsoluteUri;
                _logger.LogInformation("Saved file to local storage: {FullPath} ({Size:N0} bytes)", fullPath, fileStream.Length);

                return new StorageResult
                {
                    IsSuccess = true,
                    BlobUri = fileUri,
                    BlobName = blobName,
                    UploadedBytes = fileStream.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save recording to local disk: {FileName}", fileName);
                return new StorageResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<Stream?> DownloadRecordingAsync(string blobName)
        {
            try
            {
                var relativePath = blobName.Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_options.BasePath, relativePath);

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found: {FullPath}", fullPath);
                    return Task.FromResult<Stream?>(null);
                }

                var stream = File.OpenRead(fullPath);
                return Task.FromResult<Stream?>(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open recording: {BlobName}", blobName);
                return Task.FromResult<Stream?>(null);
            }
        }

        public async Task<byte[]?> DownloadFileAsync(string blobName, CancellationToken cancellationToken = default)
        {
            try
            {
                var relativePath = blobName.Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_options.BasePath, relativePath);

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found: {FullPath}", fullPath);
                    return null;
                }

                return await File.ReadAllBytesAsync(fullPath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file: {BlobName}", blobName);
                return null;
            }
        }

        public Task<bool> DeleteRecordingAsync(string blobName)
        {
            try
            {
                var relativePath = blobName.Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_options.BasePath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    var metaPath = fullPath + ".metadata.json";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);

                    _logger.LogInformation("Deleted local file: {FullPath}", fullPath);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("File not found for deletion: {FullPath}", fullPath);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete recording: {BlobName}", blobName);
                return Task.FromResult(false);
            }
        }

        public async Task<IEnumerable<RecordingMetadata>> ListRecordingsAsync(string? systemName = null, string? talkgroupId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var results = new List<RecordingMetadata>();

                if (!Directory.Exists(_options.BasePath))
                    return results;

                var metaFiles = Directory.EnumerateFiles(_options.BasePath, "*.metadata.json", SearchOption.AllDirectories);
                foreach (var metaFile in metaFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(metaFile);
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                        if (dict == null) continue;

                        var audioPath = metaFile.Substring(0, metaFile.Length - ".metadata.json".Length);
                        var relative = Path.GetRelativePath(_options.BasePath, audioPath).Replace(Path.DirectorySeparatorChar, '/');

                        var rm = new RecordingMetadata
                        {
                            BlobName = relative,
                            BlobUri = new Uri(Path.GetFullPath(audioPath)).AbsoluteUri
                        };

                        if (dict.TryGetValue("TalkgroupId", out var tg) && tg != null)
                            rm.TalkgroupId = tg.ToString() ?? string.Empty;

                        if (dict.TryGetValue("SystemName", out var sn) && sn != null)
                            rm.SystemName = sn.ToString() ?? string.Empty;

                        if (dict.TryGetValue("RecordingTime", out var rt) && rt != null && DateTime.TryParse(rt.ToString(), out var parsedRt))
                            rm.RecordingTime = parsedRt;

                        if (dict.TryGetValue("Frequency", out var f) && f != null)
                            rm.Frequency = f.ToString() ?? string.Empty;

                        if (dict.TryGetValue("OriginalFileName", out var ofn) && ofn != null)
                            rm.FileName = ofn.ToString() ?? string.Empty;

                        if (dict.TryGetValue("OriginalFormat", out var ofmt) && ofmt != null)
                            rm.OriginalFormat = ofmt.ToString() ?? string.Empty;

                        if (dict.TryGetValue("OriginalSize", out var osz) && osz != null && long.TryParse(osz.ToString(), out var parsedSize))
                            rm.OriginalSize = parsedSize;

                        // Apply filters
                        if (!string.IsNullOrEmpty(systemName) && !string.Equals(systemName, rm.SystemName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrEmpty(talkgroupId) && !string.Equals(talkgroupId, rm.TalkgroupId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!IsWithinDateRange(rm, fromDate, toDate))
                            continue;

                        results.Add(rm);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse metadata file: {MetaFile}", metaFile);
                    }
                }

                return results.OrderByDescending(r => r.RecordingTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list local recordings");
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

            _logger.LogDebug("Generated local blob name: {BlobName} for file: {OriginalFileName}", blobName, originalFileName);

            return blobName;
        }

        private string SanitizePath(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "unknown";

            var invalidChars = new char[] { '\\', ':', '*', '?', '"', '<', '>', '|', '\t', '\r', '\n' };
            var sanitized = input;

            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '-');
            }

            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, "-+", "-");
            sanitized = sanitized.Trim('-', ' ');

            if (string.IsNullOrEmpty(sanitized))
                return "unknown";

            return sanitized;
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
}
