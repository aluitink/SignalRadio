namespace SignalRadio.DataAccess;

/// <summary>
/// Linking table between NotableIncident and Call (many-to-many)
/// </summary>
public class NotableIncidentCall
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the notable incident
    /// </summary>
    public int NotableIncidentId { get; set; }
    public NotableIncident? NotableIncident { get; set; }

    /// <summary>
    /// Reference to the call involved in this incident
    /// </summary>
    public int CallId { get; set; }
    public Call? Call { get; set; }

    /// <summary>
    /// Optional note about this specific call's role in the incident
    /// </summary>
    public string? CallNote { get; set; }

    /// <summary>
    /// When this association was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
