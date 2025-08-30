namespace SignalRadio.DataAccess;

public class TalkGroup
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }
    public int? Priority { get; set; }

    public ICollection<Call> Calls { get; set; } = new List<Call>();
}
