namespace SignalRadio.DataAccess;

public class StorageLocation
{
    public int Id { get; set; }
    public StorageKind Kind { get; set; }
    public string LocationUri { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    // stored in UTC
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
}
