using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

public class OpenAiCompatibleClient : IAiClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _embeddingModel;
    private readonly bool _embeddingsEnabled;

    public bool IsEnabled => true;
    public bool AreEmbeddingsEnabled => _embeddingsEnabled;

    public OpenAiCompatibleClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["AI:Model"] ?? "gpt-4o-mini";
        _embeddingModel = config["AI:EmbeddingModel"] ?? "text-embedding-3-small";
        _embeddingsEnabled = config.GetValue<bool>("AI:EmbeddingsEnabled");
        var apiKey = config["AI:ApiKey"] ?? "";
        if (!string.IsNullOrEmpty(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken ct = default)
    {
        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant that summarizes radio communications." },
                new { role = "user", content = $"Summarize the following radio communication transcript:\n\n{text}" }
            },
            max_tokens = 500
        };

        var response = await _http.PostAsync("/chat/completions",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!_embeddingsEnabled) return Array.Empty<float>();

        var payload = new
        {
            model = _embeddingModel,
            input = text
        };

        var response = await _http.PostAsync("/embeddings",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var embedding = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
        return embedding.EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }
}
