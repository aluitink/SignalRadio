using Microsoft.EntityFrameworkCore;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;
using SignalRadio.Data.Entities;

namespace SignalRadio.Api.Services;

public class LocalStorageService : IStorageService
{
    private readonly SignalRadioDbContext _db;
    private readonly string _baseUrl;
    private readonly string _configFallbackPath;

    public LocalStorageService(SignalRadioDbContext db, IConfiguration config)
    {
        _db = db;
        _baseUrl = config["Storage:BaseUrl"] ?? "http://localhost:5000/api/recordings/file";
        // Config value is the fallback; the StorageLocation row is the source of truth.
        _configFallbackPath = config["Storage:Path"] ?? "/data/recordings";
    }

    private async Task<string> ResolveBasePathAsync(CancellationToken ct)
    {
        var location = await _db.StorageLocations
            .FirstOrDefaultAsync(s => s.Kind == StorageKind.Local, ct);
        var path = location?.LocationUri;
        if (string.IsNullOrWhiteSpace(path)) path = _configFallbackPath;
        Directory.CreateDirectory(path);
        return path;
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var basePath = await ResolveBasePathAsync(ct);
        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(basePath, safeFileName);
        using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, ct);
        return safeFileName;
    }

    public async Task<Stream?> OpenAsync(string fileName, CancellationToken ct = default)
    {
        var basePath = await ResolveBasePathAsync(ct);
        var fullPath = Path.Combine(basePath, Path.GetFileName(fileName));
        if (!File.Exists(fullPath)) return null;
        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        var basePath = await ResolveBasePathAsync(ct);
        var fullPath = Path.Combine(basePath, Path.GetFileName(fileName));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public string GetUrl(string fileName) => $"{_baseUrl}/{Path.GetFileName(fileName)}";
}
