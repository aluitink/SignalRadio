namespace SignalRadio.Core.Interfaces;

public interface IAsrService
{
    Task<AsrResult> TranscribeAsync(Stream audioStream, string language, CancellationToken ct = default);
}

public record AsrResult(string Text, double? Confidence, string Service);
