using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SignalRadio.Api.Services
{
    public interface ILocalFileCacheService
    {
        string GetCachePath(string fileName);
        Task SaveFileAsync(string fileName, Stream fileStream);
        bool TryGetFile(string fileName, out string filePath);
        void Cleanup();
    }

    public class LocalFileCacheOptions
    {
        public string? CacheDirectory { get; set; }
        public int CacheDurationMinutes { get; set; } = 10;
    }
    public class LocalFileCacheService : ILocalFileCacheService
    {
        private readonly string _cacheDirectory;
        private readonly TimeSpan _cacheDuration;
        private readonly ILogger<LocalFileCacheService> _logger;

        public LocalFileCacheService(
            IOptions<LocalFileCacheOptions> options,
            ILogger<LocalFileCacheService> logger)
        {
            var opts = options.Value;
            _cacheDirectory = string.IsNullOrWhiteSpace(opts.CacheDirectory) ? "/tmp/signalradio-cache" : opts.CacheDirectory;
            _cacheDuration = opts.CacheDurationMinutes > 0 ? TimeSpan.FromMinutes(opts.CacheDurationMinutes) : TimeSpan.FromMinutes(10);
            _logger = logger;
            Directory.CreateDirectory(_cacheDirectory);
        }


        public string GetCachePath(string fileName)
        {
            return Path.Combine(_cacheDirectory, fileName);
        }

        public async Task SaveFileAsync(string fileName, Stream fileStream)
        {
            var path = GetCachePath(fileName);
            // Ensure parent directory exists in case fileName contains subdirectories
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

            using (var file = File.Create(path))
            {
                await fileStream.CopyToAsync(file);
            }
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            _logger.LogDebug("Cached file: {FileName}", fileName);
        }

        public bool TryGetFile(string fileName, out string filePath)
        {
            filePath = GetCachePath(fileName);
            if (File.Exists(filePath))
            {
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(filePath);
                if (age < _cacheDuration)
                {
                    _logger.LogDebug("Cache hit: {FileName}", fileName);
                    return true;
                }
                else
                {
                    _logger.LogDebug("Cache expired: {FileName}", fileName);
                    File.Delete(filePath);
                }
            }
            return false;
        }

        public void Cleanup()
        {
            foreach (var file in Directory.GetFiles(_cacheDirectory))
            {
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(file);
                if (age > _cacheDuration)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogDebug("Deleted expired cache file: {FilePath}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete cache file: {file}");
                    }
                }
            }
        }
    }
}
