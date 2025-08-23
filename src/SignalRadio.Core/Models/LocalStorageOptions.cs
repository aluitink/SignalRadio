namespace SignalRadio.Core.Models;

public class LocalStorageOptions
{
    public string BasePath { get; set; } = "/data/recordings";
    public string DefaultPathPattern { get; set; } = "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}";
}
