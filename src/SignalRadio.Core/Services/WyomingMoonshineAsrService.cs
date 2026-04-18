using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

/// <summary>
/// ASR service implementation using the Wyoming protocol against a Moonshine ONNX server
/// (e.g. ghcr.io/cronus42/wyoming-moonshine).
///
/// Wyoming is a JSON-newline-framed TCP protocol. The STT flow is:
///   1. Send  "transcribe"  event
///   2. Send  "audio-start" event  (16 kHz / 16-bit / mono)
///   3. Send  "audio-chunk" events with raw PCM payload
///   4. Send  "audio-stop"  event
///   5. Receive "transcript" event (or "error")
/// </summary>
public class WyomingMoonshineAsrService : IAsrService
{
    private readonly AsrOptions _options;
    private readonly ILogger<WyomingMoonshineAsrService> _logger;

    private const int TargetSampleRate = 16000;
    private const int AudioChunkBytes = 4096; // bytes per TCP chunk

    // Moonshine limits: audio must be between 0.1s and 64s per call (exclusive)
    // At 16 kHz / 16-bit / mono: 2 bytes per sample
    private const int BytesPerSample = 2;
    private const int MinPcmBytes = (int)(0.1 * TargetSampleRate * BytesPerSample) + 1; // > 0.1s
    private const int MaxPcmBytes = (int)(64.0 * TargetSampleRate * BytesPerSample) - 1; // < 64s

    public WyomingMoonshineAsrService(
        IOptions<AsrOptions> options,
        ILogger<WyomingMoonshineAsrService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // ── Public IAsrService ────────────────────────────────────────────────

    public async Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData, string fileName, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("ASR service is disabled");

        _logger.LogInformation("Starting Moonshine transcription for {FileName} ({Bytes} bytes)",
            fileName, audioData.Length);

        var pcm = ConvertToPcm16k(audioData, fileName);

        // Guard: audio too short — Moonshine requires > 0.1s
        if (pcm.Length < MinPcmBytes)
        {
            _logger.LogWarning("Audio for {FileName} is too short ({Bytes} bytes, < 0.1s); skipping transcription", fileName, pcm.Length);
            return new TranscriptionResult { Text = string.Empty, Language = "en" };
        }

        // If audio exceeds 64s, split into chunks and concatenate results
        if (pcm.Length > MaxPcmBytes)
        {
            _logger.LogInformation("Audio for {FileName} exceeds 64s ({Bytes} bytes); splitting into chunks", fileName, pcm.Length);
            var parts = new List<string>();
            for (int chunkStart = 0; chunkStart < pcm.Length; chunkStart += MaxPcmBytes)
            {
                var chunkLen = Math.Min(MaxPcmBytes, pcm.Length - chunkStart);
                if (chunkLen < MinPcmBytes)
                {
                    _logger.LogDebug("Skipping final PCM chunk for {FileName} — too short ({Bytes} bytes)", fileName, chunkLen);
                    break;
                }
                var chunk = pcm.AsSpan(chunkStart, chunkLen).ToArray();
                var chunkResult = await TranscribePcmAsync(chunk, fileName, cancellationToken);
                if (!string.IsNullOrWhiteSpace(chunkResult.Text))
                    parts.Add(chunkResult.Text);
            }
            var combined = string.Join(" ", parts).Trim();
            _logger.LogInformation("Moonshine transcription completed for {FileName} ({Chunks} chunks): {Length} chars",
                fileName, parts.Count, combined.Length);
            return new TranscriptionResult { Text = combined, Language = "en" };
        }

        var result = await TranscribePcmAsync(pcm, fileName, cancellationToken);

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            var durationSec = pcm.Length / (double)(TargetSampleRate * BytesPerSample);
            var rms = ComputePcmRms(pcm);
            var dbFs = rms > 0 ? 20.0 * Math.Log10(rms) : double.NegativeInfinity;
            _logger.LogWarning(
                "Moonshine returned empty transcript for {FileName} — duration: {Duration:F2}s, RMS: {Rms:F6} ({DbFs:F1} dBFS). " +
                "Likely cause: {Cause}",
                fileName, durationSec, rms, dbFs,
                dbFs < -50 ? "silence / squelch tail" : dbFs < -30 ? "low-level noise / tone" : "speech not recognised by model");
        }
        else
        {
            _logger.LogInformation("Moonshine transcription completed for {FileName}: {Length} chars",
                fileName, result.Text.Length);
        }

        return result;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await audioStream.CopyToAsync(ms, cancellationToken);
        return await TranscribeAsync(ms.ToArray(), fileName, cancellationToken);
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (!_options.Enabled) return false;
        return await GetHealthStatusAsync() == AsrHealthStatus.Healthy;
    }

    public async Task<AsrHealthStatus> GetHealthStatusAsync()
    {
        if (!_options.Enabled) return AsrHealthStatus.Unknown;

        try
        {
            var uri = ParseTcpUri(_options.MoonshineServiceUrl);
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(uri.host, uri.port);
            return AsrHealthStatus.Healthy;
        }
        catch
        {
            return AsrHealthStatus.Unhealthy;
        }
    }

    // ── Wyoming protocol helpers ──────────────────────────────────────────

    /// <summary>
    /// Sends a pre-validated PCM buffer (must be within Moonshine's 0.1–64s window) and returns the transcript.
    /// </summary>
    private async Task<TranscriptionResult> TranscribePcmAsync(byte[] pcm, string fileName, CancellationToken cancellationToken)
    {
        var uri = ParseTcpUri(_options.MoonshineServiceUrl);
        var host = uri.host;
        var port = uri.port;

        using var tcp = new TcpClient();
        tcp.NoDelay = true;

        try
        {
            await tcp.ConnectAsync(host, port, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot connect to Wyoming Moonshine at {_options.MoonshineServiceUrl}: {ex.Message}", ex);
        }

        await using var stream = tcp.GetStream();

        // 1. Transcribe request
        await WriteJson(stream, new
        {
            type = "transcribe",
            data = new { name = _options.MoonshineModel, language = _options.MoonshineLanguage }
        }, cancellationToken);

        // 2. Audio-start
        await WriteJson(stream, new
        {
            type = "audio-start",
            data = new { rate = TargetSampleRate, width = 2, channels = 1 }
        }, cancellationToken);

        // 3. Audio-chunks
        for (int offset = 0; offset < pcm.Length; offset += AudioChunkBytes)
        {
            var size = Math.Min(AudioChunkBytes, pcm.Length - offset);
            await WriteAudioChunk(stream, pcm, offset, size, cancellationToken);
        }

        // 4. Audio-stop
        await WriteJson(stream, new { type = "audio-stop", data = new { } }, cancellationToken);

        // 5. Read transcript
        return await ReadTranscript(stream, cancellationToken);
    }

    private static async Task WriteJson(
        NetworkStream stream, object message, CancellationToken ct)
    {
        // Wyoming wire format: {"type":"...","data_length":N}\n<N bytes of data JSON>
        // Extract "type" from the serialized object, put everything else into data.
        // For simplicity we embed data inline in the header (servers accept both forms).
        var json = JsonSerializer.Serialize(message) + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes, ct);
    }

    private static async Task WriteAudioChunk(
        NetworkStream stream, byte[] pcm, int offset, int length, CancellationToken ct)
    {
        // Header: audio-chunk JSON with payload_length, then raw bytes
        var header = JsonSerializer.Serialize(new
        {
            type = "audio-chunk",
            data = new { rate = TargetSampleRate, width = 2, channels = 1 },
            payload_length = length
        }) + "\n";

        var headerBytes = Encoding.UTF8.GetBytes(header);
        await stream.WriteAsync(headerBytes, ct);
        await stream.WriteAsync(pcm.AsMemory(offset, length), ct);
    }

    private static async Task<TranscriptionResult> ReadTranscript(
        NetworkStream stream, CancellationToken ct)
    {
        // Wyoming wire format per event.py:
        //   {"type":"transcript","data_length":N,"version":"..."}\n
        //   <N bytes of UTF-8 JSON: {"text":"...","language":"en"}>
        // Older/inline form also accepted: {"type":"transcript","data":{"text":"..."}}\n
        var lineBuffer = new List<byte>(256);

        while (!ct.IsCancellationRequested)
        {
            var b = stream.ReadByte();
            if (b == -1) break; // EOF

            if (b == '\n')
            {
                var line = Encoding.UTF8.GetString(lineBuffer.ToArray());
                lineBuffer.Clear();

                if (string.IsNullOrWhiteSpace(line)) continue;

                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp)) continue;
                var type = typeProp.GetString();

                // Read separate data blob (Wyoming standard wire format)
                JsonElement dataElement = default;
                bool hasData = false;

                if (root.TryGetProperty("data_length", out var dlProp))
                {
                    var dataLen = dlProp.GetInt32();
                    if (dataLen > 0)
                    {
                        var dataBytes = new byte[dataLen];
                        await stream.ReadExactlyAsync(dataBytes, ct);
                        // Clone so the element is independent of the JsonDocument lifetime
                        using var dataDoc = JsonDocument.Parse(dataBytes);
                        dataElement = dataDoc.RootElement.Clone();
                        hasData = true;
                    }
                }
                else if (root.TryGetProperty("data", out var inlineData))
                {
                    dataElement = inlineData.Clone();
                    hasData = true;
                }

                // Skip any binary payload bytes
                if (root.TryGetProperty("payload_length", out var plProp))
                {
                    var payloadLen = plProp.GetInt32();
                    if (payloadLen > 0)
                    {
                        var skip = new byte[payloadLen];
                        await stream.ReadExactlyAsync(skip, ct);
                    }
                }

                if (type == "transcript")
                {
                    var text = string.Empty;
                    if (hasData && dataElement.TryGetProperty("text", out var textProp))
                        text = textProp.GetString() ?? string.Empty;

                    return new TranscriptionResult { Text = text.Trim(), Language = "en" };
                }

                if (type == "error")
                {
                    var msg = hasData && dataElement.TryGetProperty("text", out var t)
                              ? t.GetString() : "unknown error";
                    throw new InvalidOperationException($"Wyoming server error: {msg}");
                }
            }
            else
            {
                lineBuffer.Add((byte)b);
            }
        }

        throw new InvalidOperationException("Wyoming connection closed before transcript was received");
    }

    // ── Audio conversion ──────────────────────────────────────────────────

    /// <summary>
    /// Parse a WAV file and return 16 kHz mono 16-bit signed little-endian PCM bytes.
    /// Handles 8/16/24/32-bit PCM and IEEE_FLOAT WAV inputs.
    /// Falls back to treating raw data as-is if the header cannot be parsed.
    /// </summary>
    private byte[] ConvertToPcm16k(byte[] audioData, string fileName)
    {
        try
        {
            if (!TryParseWavHeader(audioData, out var fmt))
            {
                _logger.LogWarning("Could not parse WAV header for {FileName}; sending raw bytes", fileName);
                return audioData;
            }

            // Extract raw PCM samples as normalised doubles
            var samples = ExtractSamples(audioData, fmt);

            // Down-mix to mono
            if (fmt.Channels > 1)
                samples = MixToMono(samples, fmt.Channels);

            // Resample to 16 kHz
            if (fmt.SampleRate != TargetSampleRate)
                samples = ResampleLinear(samples, fmt.SampleRate, TargetSampleRate);

            // Convert to 16-bit signed PCM bytes
            return SamplesToBytes(samples);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio conversion failed for {FileName}; sending raw bytes as fallback", fileName);
            return audioData;
        }
    }

    private record WavFormat(int AudioFormat, int Channels, int SampleRate, int BitsPerSample, int DataOffset, int DataLength);

    private static bool TryParseWavHeader(byte[] data, out WavFormat fmt)
    {
        fmt = null!;
        if (data.Length < 44) return false;
        if (Encoding.ASCII.GetString(data, 0, 4) != "RIFF") return false;
        if (Encoding.ASCII.GetString(data, 8, 4) != "WAVE") return false;

        int pos = 12;
        int audioFormat = 0, channels = 0, sampleRate = 0, bitsPerSample = 0;
        int dataOffset = 0, dataLength = 0;

        while (pos + 8 <= data.Length)
        {
            var chunkId = Encoding.ASCII.GetString(data, pos, 4);
            var chunkSize = BitConverter.ToInt32(data, pos + 4);
            pos += 8;

            if (chunkId == "fmt ")
            {
                audioFormat = BitConverter.ToInt16(data, pos);
                channels = BitConverter.ToInt16(data, pos + 2);
                sampleRate = BitConverter.ToInt32(data, pos + 4);
                bitsPerSample = BitConverter.ToInt16(data, pos + 14);
            }
            else if (chunkId == "data")
            {
                dataOffset = pos;
                dataLength = Math.Min(chunkSize, data.Length - pos);
                break;
            }

            pos += chunkSize;
            if ((chunkSize & 1) != 0) pos++; // RIFF pad byte
        }

        if (dataOffset == 0 || channels == 0 || sampleRate == 0) return false;

        fmt = new WavFormat(audioFormat, channels, sampleRate, bitsPerSample, dataOffset, dataLength);
        return true;
    }

    /// <summary>
    /// Extract all samples as normalised doubles in [-1.0, 1.0].
    /// Samples are interleaved by channel.
    /// Supports PCM 8/16/24/32-bit and IEEE_FLOAT 32-bit.
    /// </summary>
    private static double[] ExtractSamples(byte[] data, WavFormat fmt)
    {
        var bytesPerSample = fmt.BitsPerSample / 8;
        var sampleCount = fmt.DataLength / bytesPerSample;
        var samples = new double[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            var offset = fmt.DataOffset + i * bytesPerSample;
            samples[i] = fmt.AudioFormat switch
            {
                3 when fmt.BitsPerSample == 32 => // IEEE_FLOAT
                    Math.Clamp(BitConverter.ToSingle(data, offset), -1f, 1f),
                1 when fmt.BitsPerSample == 8 => // unsigned 8-bit
                    (data[offset] - 128) / 128.0,
                1 when fmt.BitsPerSample == 16 =>
                    BitConverter.ToInt16(data, offset) / 32768.0,
                1 when fmt.BitsPerSample == 24 =>
                    ((data[offset] | (data[offset + 1] << 8) | ((sbyte)data[offset + 2] << 16))) / 8388608.0,
                1 when fmt.BitsPerSample == 32 =>
                    BitConverter.ToInt32(data, offset) / 2147483648.0,
                _ => BitConverter.ToInt16(data, offset) / 32768.0
            };
        }

        return samples;
    }

    private static double[] MixToMono(double[] interleaved, int channels)
    {
        var mono = new double[interleaved.Length / channels];
        for (int i = 0; i < mono.Length; i++)
        {
            double sum = 0;
            for (int c = 0; c < channels; c++)
                sum += interleaved[i * channels + c];
            mono[i] = sum / channels;
        }
        return mono;
    }

    private static double[] ResampleLinear(double[] input, int inputRate, int outputRate)
    {
        if (inputRate == outputRate) return input;

        var outputLength = (int)((long)input.Length * outputRate / inputRate);
        var output = new double[outputLength];
        var ratio = (double)inputRate / outputRate;

        for (int i = 0; i < outputLength; i++)
        {
            var srcPos = i * ratio;
            var srcIdx = (int)srcPos;
            var frac   = srcPos - srcIdx;
            var a = srcIdx < input.Length ? input[srcIdx] : 0.0;
            var b = srcIdx + 1 < input.Length ? input[srcIdx + 1] : 0.0;
            output[i] = a + frac * (b - a);
        }

        return output;
    }

    private static byte[] SamplesToBytes(double[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            var s = (short)Math.Clamp(samples[i] * 32767.0, short.MinValue, short.MaxValue);
            bytes[i * 2] = (byte)(s & 0xFF);
            bytes[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return bytes;
    }

    /// <summary>
    /// Computes the root-mean-square amplitude of a 16-bit LE PCM buffer, normalised to [0, 1].
    /// </summary>
    private static double ComputePcmRms(byte[] pcm)
    {
        if (pcm.Length < 2) return 0.0;
        double sum = 0.0;
        var sampleCount = pcm.Length / 2;
        for (int i = 0; i < sampleCount; i++)
        {
            var s = BitConverter.ToInt16(pcm, i * 2) / 32768.0;
            sum += s * s;
        }
        return Math.Sqrt(sum / sampleCount);
    }

    private static (string host, int port) ParseTcpUri(string rawUri)
    {
        // Accept "tcp://host:port" or plain "host:port"
        if (!rawUri.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            rawUri = "tcp://" + rawUri;

        var uri = new Uri(rawUri);
        return (uri.Host, uri.Port > 0 ? uri.Port : 10300);
    }
}
