using System.Net.Http.Headers;
using System.Text.Json;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

/// <summary>
/// ASR client for the openai-whisper-asr-webservice (onerahmet/whisper-asr-webservice),
/// which exposes <c>POST /asr</c> (multipart, field <c>audio_file</c>) and returns
/// <c>{"language": "...", "segments": [{"text": "...", "avg_logprob": ...}, ...]}</c>.
///
/// Configuration (bound from the "ASR" section):
///   - ASR:Endpoint  — base URL of the webservice, e.g. http://host.docker.internal:9000 (required)
///   - ASR:ApiKey    — bearer token (optional; the webservice does not use one)
///   - ASR:Language  — ISO-639-1 hint, e.g. "en" (optional)
///   - ASR:VadFilter — enable VAD filtering, "true"/"false" (optional, default true)
///
/// The <c>language</c> argument passed by the pipeline is the audio container format
/// (e.g. "wav"), not a language code — it is used only for the upload filename.
/// </summary>
public class WhisperAsrService : IAsrService
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string? _language;
    private readonly bool _vadFilter;
    private readonly ILogger<WhisperAsrService> _logger;

    public WhisperAsrService(HttpClient http, IConfiguration config, ILogger<WhisperAsrService> logger)
    {
        _http = http;
        _logger = logger;
        _endpoint = (config["ASR:Endpoint"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(_endpoint))
            throw new InvalidOperationException("ASR:Endpoint is required when ASR:Provider=whisper.");
        _language = config["ASR:Language"];
        _vadFilter = config.GetValue("ASR:VadFilter", true);

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
        form.Add(fileContent, "audio_file", $"audio.{ext}");

        var query = $"output=json&vad_filter={_vadFilter.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(_language))
            query += $"&language={Uri.EscapeDataString(_language)}";

        var url = $"{_endpoint}/asr?{query}";
        _logger.LogDebug("Transcribing audio ({Bytes} bytes) via {Url}.", buffer.Length, url);

        var response = await _http.PostAsync(url, form, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Concatenate segment texts; average the per-segment avg_logprob into a 0..1 confidence.
        var sb = new System.Text.StringBuilder();
        double logprobSum = 0;
        int logprobCount = 0;

        if (root.TryGetProperty("segments", out var segments) && segments.ValueKind == JsonValueKind.Array)
        {
            foreach (var seg in segments.EnumerateArray())
            {
                if (seg.TryGetProperty("text", out var t))
                    sb.Append(t.GetString());
                if (seg.TryGetProperty("avg_logprob", out var lp) && lp.ValueKind == JsonValueKind.Number)
                {
                    logprobSum += lp.GetDouble();
                    logprobCount++;
                }
            }
        }
        else if (root.TryGetProperty("text", out var t))
        {
            sb.Append(t.GetString());
        }

        double? confidence = logprobCount > 0
            ? Math.Clamp(Math.Exp(logprobSum / logprobCount), 0.0, 1.0)
            : null;

        return new AsrResult(sb.ToString().Trim(), confidence, "whisper");
    }
}
