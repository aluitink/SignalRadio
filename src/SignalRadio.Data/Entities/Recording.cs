namespace SignalRadio.Data.Entities;

public class Recording
{
    public long Id { get; set; }
    public long CallId { get; set; }
    public Call? Call { get; set; }
    public int StorageLocationId { get; set; }
    public StorageLocation? StorageLocation { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public bool IsProcessed { get; set; }

    public ICollection<Transcription> Transcriptions { get; set; } = new List<Transcription>();
}
