namespace SignalRadio.Data.Entities;

public class TalkGroup
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AlphaTag { get; set; }
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Call> Calls { get; set; } = new List<Call>();
}
