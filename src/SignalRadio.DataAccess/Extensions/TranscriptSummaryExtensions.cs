using SignalRadio.Core.Models;

namespace SignalRadio.DataAccess.Extensions;

public static class TranscriptSummaryExtensions
{
    /// <summary>
    /// Convert database TranscriptSummary to API response model
    /// </summary>
    public static TranscriptSummaryResponse ToResponse(this TranscriptSummary summary)
    {
        return new TranscriptSummaryResponse
        {
            TalkGroupId = summary.TalkGroupId,
            TalkGroupName = summary.TalkGroup?.Name ?? summary.TalkGroup?.AlphaTag ?? $"TalkGroup {summary.TalkGroupId}",
            StartTime = summary.StartTime,
            EndTime = summary.EndTime,
            TranscriptCount = summary.TranscriptCount,
            TotalDurationSeconds = summary.TotalDurationSeconds,
            Summary = summary.Summary,
            KeyTopics = summary.TranscriptSummaryTopics.Select(st => st.Topic?.Name ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList(),
            NotableIncidents = summary.TranscriptSummaryNotableIncidents.Select(sni => sni.NotableIncident?.Description ?? "").Where(desc => !string.IsNullOrEmpty(desc)).ToList(),
            NotableIncidentsWithCallIds = summary.TranscriptSummaryNotableIncidents
                .Where(sni => sni.NotableIncident != null)
                .Select(sni => new SignalRadio.Core.Models.NotableIncident
                {
                    Description = sni.NotableIncident!.Description,
                    CallIds = sni.NotableIncident.NotableIncidentCalls.Select(nic => nic.CallId).ToList()
                }).ToList(),
            GeneratedAt = summary.GeneratedAt,
            FromCache = true // Will be set to false by the service if newly generated
        };
    }

    /// <summary>
    /// Convert API request to database TranscriptSummary entity
    /// </summary>
    public static TranscriptSummary ToEntity(this TranscriptSummaryRequest request)
    {
        return new TranscriptSummary
        {
            TalkGroupId = request.TalkGroupId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedAt = DateTimeOffset.UtcNow,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Create TranscriptSummary entity from response data (for saving AI results)
    /// </summary>
    public static TranscriptSummary ToEntity(this TranscriptSummaryResponse response)
    {
        var entity = new TranscriptSummary
        {
            TalkGroupId = response.TalkGroupId,
            StartTime = response.StartTime,
            EndTime = response.EndTime,
            TranscriptCount = response.TranscriptCount,
            TotalDurationSeconds = response.TotalDurationSeconds,
            Summary = response.Summary,
            GeneratedAt = response.GeneratedAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Create topic links
        foreach (var topicName in response.KeyTopics)
        {
            entity.TranscriptSummaryTopics.Add(new TranscriptSummaryTopic
            {
                Topic = new Topic { Name = topicName, CreatedAt = DateTimeOffset.UtcNow },
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Create notable incident links
        foreach (var incident in response.NotableIncidentsWithCallIds)
        {
            var notableIncident = new SignalRadio.DataAccess.NotableIncident
            {
                Description = incident.Description,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Create call links for the incident
            foreach (var callId in incident.CallIds)
            {
                notableIncident.NotableIncidentCalls.Add(new NotableIncidentCall
                {
                    CallId = callId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            entity.TranscriptSummaryNotableIncidents.Add(new TranscriptSummaryNotableIncident
            {
                NotableIncident = notableIncident,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return entity;
    }
}
