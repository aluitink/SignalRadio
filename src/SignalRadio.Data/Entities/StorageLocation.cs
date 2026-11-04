namespace SignalRadio.Data.Entities;

public enum StorageKind
{
    Local = 0,
    Azure = 1
}

public class StorageLocation
{
    public int Id { get; set; }
    public StorageKind Kind { get; set; }
    public string LocationUri { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
}
