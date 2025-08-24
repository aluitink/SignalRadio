using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

/// <summary>
/// Service for communicating with the Whisper ASR webservice
/// </summary>
public class WhisperAsrService : IAsrService
{
    private readonly HttpClient _httpClient;
    private readonly AsrOptions _options;
    private readonly ILogger<WhisperAsrService> _logger;

    public WhisperAsrService(HttpClient httpClient, IOptions<AsrOptions> options, ILogger<WhisperAsrService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData, string fileName, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("ASR service is disabled");
        }

        try
        {
            _logger.LogInformation("Starting transcription for file: {FileName} ({Size} bytes)", fileName, audioData.Length);

            using var content = new MultipartFormDataContent();
            using var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "audio_file", fileName);

            // Build query parameters
            var queryParams = new List<string>
            {
                $"output={_options.OutputFormat}",
                $"word_timestamps={_options.WordTimestamps.ToString().ToLower()}",
                $"vad_filter={_options.VadFilter.ToString().ToLower()}"
            };

            var requestUrl = $"{_options.TranscriptionEndpoint}?{string.Join("&", queryParams)}";

            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("ASR Response: {Response}", jsonResponse);

            var whisperResponse = JsonSerializer.Deserialize<WhisperResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

                if (whisperResponse == null)
            {
                throw new InvalidOperationException("Failed to parse ASR response");
            }

            var result = new TranscriptionResult
            {
                Text = whisperResponse.Text?.Trim() ?? string.Empty,
                Language = whisperResponse.Language ?? "unknown"
            };

            // Convert segments if available
            if (whisperResponse.Segments != null)
            {
                result.Segments = whisperResponse.Segments.Select(s => new TranscriptionSegment
                {
                    Id = s.Id,
                    Start = s.Start,
                    End = s.End,
                    Text = s.Text?.Trim() ?? string.Empty,
                    AvgLogprob = s.AvgLogprob,
                    CompressionRatio = s.CompressionRatio,
                    NoSpeechProb = s.NoSpeechProb,
                    // Compute a confidence value in range 0.0 - 1.0 when possible.
                    // Prefer converting avg_logprob -> probability via exp(avg_logprob).
                    // If that's not available, fall back to 1 - no_speech_prob when present.
                    Confidence = s.AvgLogprob.HasValue
                        ? Math.Clamp(Math.Exp(s.AvgLogprob.Value), 0.0, 1.0)
                        : (s.NoSpeechProb.HasValue ? Math.Clamp(1.0 - s.NoSpeechProb.Value, 0.0, 1.0) : (double?)null)
                }).ToList();
            }

            _logger.LogInformation("Transcription completed for {FileName}. Length: {Length} chars, Language: {Language}", 
                fileName, result.Text.Length, result.Language);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during transcription of {FileName}", fileName);
            throw new InvalidOperationException($"ASR service communication error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Transcription timeout for {FileName}", fileName);
            throw new TimeoutException($"ASR service timeout after {_options.TimeoutSeconds} seconds", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transcription of {FileName}", fileName);
            throw;
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        return await TranscribeAsync(memoryStream.ToArray(), fileName, cancellationToken);
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (!_options.Enabled)
        {
            return false;
        }

        try
        {
            var status = await GetHealthStatusAsync();
            return status == AsrHealthStatus.Healthy;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AsrHealthStatus> GetHealthStatusAsync()
    {
        if (!_options.Enabled)
        {
            return AsrHealthStatus.Unknown;
        }

        try
        {
            var response = await _httpClient.GetAsync(_options.WhisperServiceUrl, HttpCompletionOption.ResponseHeadersRead);
            
            if (response.IsSuccessStatusCode)
            {
                return AsrHealthStatus.Healthy;
            }
            else if ((int)response.StatusCode >= 500)
            {
                return AsrHealthStatus.Unhealthy;
            }
            else
            {
                return AsrHealthStatus.Degraded;
            }
        }
        catch (HttpRequestException)
        {
            return AsrHealthStatus.Unhealthy;
        }
        catch (TaskCanceledException)
        {
            return AsrHealthStatus.Degraded;
        }
        catch
        {
            return AsrHealthStatus.Unknown;
        }
    }

    /// <summary>
    /// Internal model for Whisper API response
    /// </summary>
    private class WhisperResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("segments")]
        public List<WhisperSegment>? Segments { get; set; }
    }

    /// <summary>
    /// Internal model for Whisper segment response
    /// </summary>
    private class WhisperSegment
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("avg_logprob")]
        public double? AvgLogprob { get; set; }

        [JsonPropertyName("compression_ratio")]
        public double? CompressionRatio { get; set; }

        [JsonPropertyName("no_speech_prob")]
        public double? NoSpeechProb { get; set; }
    }
}
