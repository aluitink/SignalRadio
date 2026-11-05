using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

/// <summary>
/// Stub ASR that produces a short, deterministic transcript per call so the downstream
/// pipeline (summaries agent, embeddings, search) has real text to work with during simulation.
/// </summary>
public class StubAsrService : IAsrService
{
    private static readonly string[] Phrases =
    {
        "Unit 5, we have a traffic stop on Route 9 near the overpass, requesting backup.",
        "Dispatch, this is Engine 3, we are on scene at the intersection of Main and 5th, two vehicles involved, no injuries reported.",
        "All units, be advised the highway is closed in both directions at mile marker 42 due to an accident.",
        "Fire station 2, we have a structure fire at 1400 Oak Street, requesting additional engine companies.",
        "Unit 12, suspect is on foot, last seen heading north on Elm Street, description is a male, white male, wearing a red jacket.",
        "Dispatch, this is Medic 7, we have a patient on scene, male, 45 years old, conscious, complaining of chest pain.",
        "All units, code 3 to 2200 Pine Avenue, possible domestic disturbance, caller reports loud argument.",
        "Unit 8, we are holding at the scene, waiting for the detective to arrive, no further action at this time.",
        "Traffic advisory, heavy congestion on the bridge, expect delays of 20 to 30 minutes.",
        "Dispatch, this is Unit 3, we have located the missing person, they are safe and unharmed, no further assistance needed."
    };

    private readonly Random _rng = new();

    public Task<AsrResult> TranscribeAsync(Stream audioStream, string language, CancellationToken ct = default)
    {
        var text = Phrases[_rng.Next(Phrases.Length)];
        return Task.FromResult(new AsrResult(text, 0.92f, "stub"));
    }
}
