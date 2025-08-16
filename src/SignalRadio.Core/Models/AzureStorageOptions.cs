namespace SignalRadio.Core.Models;

public class AzureStorageOptions
{
    public const string Section = "AzureStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "recordings";
    public string DefaultPathPattern { get; set; } = "{SystemName}/{TalkgroupId}/{Year}/{Month}/{Day}";
}
