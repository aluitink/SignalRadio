namespace SignalRadio.Data.Entities;

public class NotableIncidentCall
{
    public long NotableIncidentId { get; set; }
    public NotableIncident? NotableIncident { get; set; }
    public long CallId { get; set; }
    public Call? Call { get; set; }
}
