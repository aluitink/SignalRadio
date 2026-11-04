using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

public class StubAiClient : IAiClient
{
    public bool IsEnabled => false;
    public bool AreEmbeddingsEnabled => false;

    public Task<string> SummarizeAsync(string text, CancellationToken ct = default) =>
        Task.FromResult(text.Length > 200 ? text[..200] + "..." : text);

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default) =>
        Task.FromResult(Array.Empty<float>());
}
