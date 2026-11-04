using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

public class StubAsrService : IAsrService
{
    public Task<AsrResult> TranscribeAsync(Stream audioStream, string language, CancellationToken ct = default)
    {
        return Task.FromResult(new AsrResult("", null, "stub"));
    }
}
