namespace SignalRadio.Core.Interfaces;

public interface IAiClient
{
    Task<string> SummarizeAsync(string text, CancellationToken ct = default);
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    bool IsEnabled { get; }
    bool AreEmbeddingsEnabled { get; }
}
