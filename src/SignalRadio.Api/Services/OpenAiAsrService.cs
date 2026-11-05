using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

/// <summary>
/// ASR client that calls an OpenAI-compatible <c>/v1/audio/transcriptions</c> endpoint
/// (OpenAI, or any compatible server such as whisper.cpp server, faster-whisper-server,
/// vLLM with a whisper model, etc.).
///
/// Configuration (bound from the "ASR" section):
///   - ASR:Endpoint  — base URL, e.g. http://localhost:9000/v1  (required)
///   - ASR:ApiKey    — bearer token (optional; some local servers ignore it)
///   - ASR:Model     — model name, e.g. "whisper-1" (optional, server default if omitted)
///   - ASR:Language  — ISO-639-1 hint, e.g. "en" (optional)
///
/// The audio stream is read into memory and uploaded as multipart/form-data with the
/// filename extension derived from the <c>language</c> argument (the pipeline passes the
/// container format, e.g. "wav").
/// </summary>
public class OpenAiAsrService : IAsrService
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string? _model;
    private readonly string? _language;
    private readonly ILogger<OpenAiAsrService> _logger;

    public OpenAiAsrService(HttpClient http, IConfiguration config, ILogger<OpenAiAsrService> logger)
    {
        _http = http;
        _logger = logger;
        _endpoint = config["ASR:Endpoint"]
            ?? throw new InvalidOperationException("ASR:Endpoint is required when ASR:Provider=openai.");
        _model = config["ASR:Model"];
        _language = config["ASR:Language"];

        var apiKey = config["ASR:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<AsrResult> TranscribeAsync(Stream audioStream, string language, CancellationToken ct = default)
    {
        // Buffer the audio so the multipart form can report its length.
        using var buffer = new MemoryStream();
        await audioStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var ext = string.IsNullOrWhiteSpace(language) ? "wav" : language.TrimStart('.');
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(buffer);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", $"audio.{ext}");

        if (!string.IsNullOrWhiteSpace(_model))
            form.Add(new StringContent(_model), "model");

        // Prefer the per-request language hint if the caller supplied one, else the configured default.
        var lang = !string.IsNullOrWhiteSpace(language) && language != "wav" ? language : _language;
        if (!string.IsNullOrWhiteSpace(lang))
            form.Add(new StringContent(lang), "language");

        var url = _endpoint.TrimEnd('/') + "/audio/transcriptions";
        _logger.LogDebug("Transcribing audio ({Bytes} bytes) via {Url} (model={Model}).",
            buffer.Length, url, _model ?? "<server default>");

        var response = await _http.PostAsync(url, form, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var text = root.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        double? confidence = null;
        // Some servers (e.g. whisper.cpp) return "confidence" or "avg_logprob"; OpenAI does not.
        if (root.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number)
            confidence = c.GetDouble();

        return new AsrResult(text, confidence, "openai");
    }
}
