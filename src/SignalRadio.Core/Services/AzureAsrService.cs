using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Models;

namespace SignalRadio.Core.Services;

/// <summary>
/// ASR service implementation using Azure Cognitive Services Speech SDK
/// </summary>
public class AzureAsrService : IAsrService
{
    private readonly AsrOptions _options;
    private readonly ILogger<AzureAsrService>? _logger;
    private readonly SpeechConfig? _speechConfig;

    public AzureAsrService(IOptions<AsrOptions> options, ILogger<AzureAsrService>? logger = null)
    {
        _options = options?.Value ?? new AsrOptions();
        _logger = logger;

    // Read credentials from AsrOptions (AsrSettings) only.
    var key = _options.AzureSpeechKey;
    var region = _options.AzureSpeechRegion;

        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(region))
        {
            try
            {
                _speechConfig = SpeechConfig.FromSubscription(key, region);
                // Optional defaults
                _speechConfig.SpeechRecognitionLanguage = "en-US";
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create SpeechConfig");
                _speechConfig = null;
            }
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData, string fileName, CancellationToken cancellationToken = default)
    {
        await using var ms = new MemoryStream(audioData);
        return await TranscribeAsync(ms, fileName, cancellationToken);
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (_speechConfig == null)
            throw new InvalidOperationException("Azure Speech SDK not configured. Set AsrSettings:AzureSpeechKey and AsrSettings:AzureSpeechRegion in configuration.");

        // Create audio config from stream
        using var audioInput = AudioConfig.FromStreamInput(new BinaryAudioStreamReader(audioStream));
        using var recognizer = new SpeechRecognizer(_speechConfig, audioInput);

        var result = new TranscriptionResult();

        var sb = new StringBuilder();

        // Use continuous recognition but stop after first session completes
        var done = new TaskCompletionSource<bool>();

        recognizer.Recognizing += (s, e) =>
        {
            // Partial results can be appended or ignored
            _logger?.LogDebug("Recognizing: {Text}", e.Result.Text);
        };

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                sb.AppendLine(e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                _logger?.LogDebug("NoMatch: {Details}", e.Result.ToString());
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            _logger?.LogWarning("Recognition canceled: {Reason} - {Text}", e.Reason, e.ErrorDetails);
            done.TrySetResult(true);
        };

        recognizer.SessionStopped += (s, e) =>
        {
            done.TrySetResult(true);
        };

        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

        // Wait for session to complete or cancellation
        using (cancellationToken.Register(() => done.TrySetCanceled()))
        {
            try
            {
                await done.Task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                _logger?.LogInformation("Transcription canceled");
            }
        }

        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        result.Text = sb.ToString().Trim();
        // Azure SDK does not provide per-segment timing/confidence easily via simple recognizer,
        // so we leave segments empty. Language set from config or empty.
        result.Language = _speechConfig.SpeechRecognitionLanguage ?? string.Empty;

        return result;
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_speechConfig != null);
    }

    public Task<AsrHealthStatus> GetHealthStatusAsync()
    {
        if (_speechConfig == null) return Task.FromResult(AsrHealthStatus.Unhealthy);
        // We can't call Azure health without making a request; assume healthy if configured
        return Task.FromResult(AsrHealthStatus.Healthy);
    }
}

// Adapter class to feed Stream into Speech SDK PushAudioInputStream
internal class BinaryAudioStreamReader : PullAudioInputStreamCallback
{
    private readonly Stream _source;

    public BinaryAudioStreamReader(Stream source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (!_source.CanRead) throw new ArgumentException("Stream must be readable", nameof(source));
    }

    public override int Read(byte[] dataBuffer, uint size)
    {
        try
        {
            // size is uint; read up to that many bytes
            var toRead = (int)size;
            var buffer = new byte[toRead];
            var read = _source.Read(buffer, 0, toRead);
            if (read == 0) return 0; // EOS
            Array.Copy(buffer, 0, dataBuffer, 0, read);
            return read;
        }
        catch
        {
            return 0;
        }
    }

    public override void Close()
    {
        // do not dispose underlying stream here; caller owns it
    }
}
