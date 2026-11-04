namespace SignalRadio.Core.Interfaces;

public interface IStorageService
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default);
    Task<Stream?> OpenAsync(string fileName, CancellationToken ct = default);
    Task DeleteAsync(string fileName, CancellationToken ct = default);
    string GetUrl(string fileName);
}
