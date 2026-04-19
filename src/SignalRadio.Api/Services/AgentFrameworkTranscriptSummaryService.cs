using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalRadio.Core.Interfaces;
using SignalRadio.Core.Models;
using SignalRadio.DataAccess;
using SignalRadio.DataAccess.Extensions;
using SignalRadio.DataAccess.Services;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SignalRadio.Api.Services;

public class AgentFrameworkTranscriptSummaryService : ITranscriptSummaryService, IDisposable
{
    private readonly SemanticKernelOptions _options;
    private readonly ILogger<AgentFrameworkTranscriptSummaryService> _logger;
    private readonly ITranscriptionsService _transcriptionsService;
    private readonly ITalkGroupsService _talkGroupsService;
    private readonly ITranscriptSummariesService _summariesService;
    private readonly AzureOpenAIClient? _azureOpenAIClient;
    private readonly SemaphoreSlim _semaphore;
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _lastRequestTimes = new();
    private bool _disposed;

    private const string SUMMARY_PROMPT = """
        You are an expert radio communications analyst. You will be provided with transcripts from radio communications over a specific time period for a particular talk group.

        Each transcript entry includes:
        - Call ID (for referencing specific communications)
        - Exact call time (when the communication occurred)
        - Call duration (how long the communication lasted)
        - Radio frequency used
        - Transcription confidence level
        - The actual transcript text

        Your task is to analyze these radio communications in the context of emergency services, public safety, or operational communications and provide:

        1. A comprehensive summary of the major activities, incidents, and communications
        2. Key topics or themes that emerged during this time period
        3. Notable incidents, emergencies, or significant events mentioned (include the Call ID from the transcript headers for linking)
        4. Overall assessment of communication patterns and activity levels
        5. Analysis of timing patterns (peak activity times, clusters of communications, etc.)

        Pay attention to:
        - Temporal patterns in the communications (when activity peaks occurred)
        - Sequence of events and how they relate chronologically
        - Communication frequency and duration patterns
        - Any escalating situations or ongoing incidents
        - Call IDs from the transcript headers for referencing specific incidents
        - Transcription confidence levels - if a transcript has confidence below 70%, note it may be unreliable

        Please be concise but thorough. Focus on operational significance and avoid speculation about information not clearly stated in the transcripts.

        IMPORTANT: Respond ONLY with a valid JSON object. Do not include any explanatory text, markdown formatting, or code blocks. Return only the raw JSON object with the following structure:
        {
            "summary": "Overall summary of the communications and activities",
            "keyTopics": ["topic1", "topic2", "topic3"],
            "notableIncidentsWithCallIds": [
                {"description": "incident description", "callIds": [12345, 12346], "importanceScore": 3},
                {"description": "another incident", "callIds": [12347], "importanceScore": 5}
            ]
        }

        For notableIncidentsWithCallIds, include arrays of Call IDs from the transcript headers when incidents can be linked to specific calls. Multiple calls can be related to the same incident if they show progression, updates, or different perspectives of the same event. The callIds should be integers matching the "Call ID" fields from the transcript entries. The importanceScore should be an integer from 1 (routine/minor) to 5 (critical/emergency).

        Radio Communication Transcripts:
        {transcripts}

        Additional Context:
        - Talk Group: {talkGroupName}
        - Talk Group Description: {talkGroupDescription}
        - Talk Group Category: {talkGroupCategory}
        - Time Period: {timePeriod}
        - Total Calls: {callCount}
        - Total Duration: {totalDuration}
        - Average Call Duration: {averageDuration}
        - Timing Analysis: {timingAnalysis}
        """;

    public AgentFrameworkTranscriptSummaryService(
        IOptions<SemanticKernelOptions> options,
        ILogger<AgentFrameworkTranscriptSummaryService> logger,
        ITranscriptionsService transcriptionsService,
        ITalkGroupsService talkGroupsService,
        ITranscriptSummariesService summariesService)
    {
        _options = options.Value;
        _logger = logger;
        _transcriptionsService = transcriptionsService;
        _talkGroupsService = talkGroupsService;
        _summariesService = summariesService;

        var maxConcurrent = _options.MaxConcurrentRequests > 0 ? _options.MaxConcurrentRequests : 3;
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

        if (!string.IsNullOrEmpty(_options.AzureOpenAIEndpoint) && !string.IsNullOrEmpty(_options.AzureOpenAIKey))
        {
            _azureOpenAIClient = new AzureOpenAIClient(
                new Uri(_options.AzureOpenAIEndpoint),
                new AzureKeyCredential(_options.AzureOpenAIKey));
        }
        else if (!string.IsNullOrEmpty(_options.AzureOpenAIEndpoint))
        {
            // Fall back to DefaultAzureCredential when no API key is configured
            _azureOpenAIClient = new AzureOpenAIClient(
                new Uri(_options.AzureOpenAIEndpoint),
                new Azure.Identity.DefaultAzureCredential());
        }
    }

    public async Task<TranscriptSummaryResponse?> GenerateSummaryAsync(
        TranscriptSummaryRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Agent Framework transcript summary service is disabled");
            return null;
        }

        try
        {
            // Check database cache first - extend cache duration and add flexible time range matching
            if (!request.ForceRefresh)
            {
                // First try exact match
                var existingSummary = await _summariesService.FindExistingSummaryAsync(
                    request.TalkGroupId, request.StartTime, request.EndTime);

                // If no exact match, try finding similar summaries within tolerance
                if (existingSummary == null)
                {
                    existingSummary = await _summariesService.FindSimilarSummaryAsync(
                        request.TalkGroupId, request.StartTime, request.EndTime, toleranceMinutes: 15);
                }

                if (existingSummary != null)
                {
                    var summaryAge = DateTimeOffset.UtcNow - existingSummary.GeneratedAt;
                    var maxCacheMinutes = _options.CacheDurationMinutes > 0 ? _options.CacheDurationMinutes : 60;

                    if (summaryAge.TotalMinutes < maxCacheMinutes)
                    {
                        _logger.LogInformation("Retrieved cached database summary for TalkGroup {TalkGroupId} (age: {Age} minutes, max: {MaxAge})",
                            request.TalkGroupId, summaryAge.TotalMinutes, maxCacheMinutes);
                        var cachedResponse = existingSummary.ToResponse();
                        cachedResponse.FromCache = true;
                        return cachedResponse;
                    }
                    else if (summaryAge.TotalMinutes < maxCacheMinutes * 2) // Serve stale data if not too old
                    {
                        _logger.LogInformation("Serving stale summary for TalkGroup {TalkGroupId} (age: {Age} minutes) - consider background refresh",
                            request.TalkGroupId, summaryAge.TotalMinutes);
                        var staleResponse = existingSummary.ToResponse();
                        staleResponse.FromCache = true;
                        return staleResponse;
                    }
                    else
                    {
                        _logger.LogInformation("Existing summary for TalkGroup {TalkGroupId} is {Age} minutes old, regenerating",
                            request.TalkGroupId, summaryAge.TotalMinutes);
                    }
                }
            }

            // Get talk group information
            var talkGroup = await _talkGroupsService.GetByIdAsync(request.TalkGroupId);
            if (talkGroup == null)
            {
                _logger.LogWarning("TalkGroup {TalkGroupId} not found", request.TalkGroupId);
                return null;
            }

            // Get transcripts for the time window
            var transcripts = await GetTranscriptsForTimeWindow(request.TalkGroupId, request.StartTime, request.EndTime);

            if (!transcripts.Any())
            {
                _logger.LogInformation("No transcripts found for TalkGroup {TalkGroupId} in time window {StartTime} to {EndTime}",
                    request.TalkGroupId, request.StartTime, request.EndTime);

                return new TranscriptSummaryResponse
                {
                    TalkGroupId = request.TalkGroupId,
                    TalkGroupName = GetTalkGroupDisplayName(talkGroup),
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TranscriptCount = 0,
                    TotalDurationSeconds = 0,
                    Summary = "No radio communications recorded during this time period.",
                    KeyTopics = new List<string>(),
                    NotableIncidentsWithCallIds = new List<SignalRadio.Core.Models.NotableIncident>(),
                    GeneratedAt = DateTimeOffset.UtcNow,
                    FromCache = false
                };
            }

            // Check if we have meaningful content to summarize (avoid AI calls for trivial content)
            var preliminaryTranscriptText = PrepareTranscriptText(transcripts);
            var minLength = _options.MinTranscriptLength > 0 ? _options.MinTranscriptLength : 50;
            if (string.IsNullOrWhiteSpace(preliminaryTranscriptText) || preliminaryTranscriptText.Length < minLength)
            {
                _logger.LogInformation("Insufficient transcript content for TalkGroup {TalkGroupId} (length: {Length})",
                    request.TalkGroupId, preliminaryTranscriptText?.Length ?? 0);

                var totalDurationForMinimal = transcripts.Where(t => t.Recording?.Call?.DurationSeconds > 0)
                                                        .Sum(t => t.Recording!.Call!.DurationSeconds);

                return new TranscriptSummaryResponse
                {
                    TalkGroupId = request.TalkGroupId,
                    TalkGroupName = GetTalkGroupDisplayName(talkGroup),
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TranscriptCount = transcripts.Count,
                    TotalDurationSeconds = totalDurationForMinimal,
                    Summary = transcripts.Count == 1 ?
                        "Single brief communication with minimal or no transcribed content." :
                        $"{transcripts.Count} brief communications with minimal or no transcribed content.",
                    KeyTopics = new List<string>(),
                    NotableIncidentsWithCallIds = new List<SignalRadio.Core.Models.NotableIncident>(),
                    GeneratedAt = DateTimeOffset.UtcNow,
                    FromCache = false
                };
            }

            // Prepare transcript data for AI analysis
            var transcriptText = PrepareTranscriptText(transcripts);
            var totalDuration = transcripts.Where(t => t.Recording?.Call?.DurationSeconds > 0)
                                        .Sum(t => t.Recording!.Call!.DurationSeconds);

            var callTimes = transcripts
                .Where(t => t.Recording?.Call != null)
                .Select(t => t.Recording!.Call!)
                .OrderBy(c => c.RecordingTime)
                .ToList();

            var timeSpan = callTimes.Any() ?
                callTimes.Last().RecordingTime - callTimes.First().RecordingTime :
                TimeSpan.Zero;

            // Rate limiting - prevent too many concurrent AI requests and duplicate requests
            var requestKey = $"{request.TalkGroupId}:{request.StartTime:yyyyMMddHHmm}:{request.EndTime:yyyyMMddHHmm}";

            if (_lastRequestTimes.TryGetValue(requestKey, out var lastRequest))
            {
                var timeSinceLastRequest = DateTimeOffset.UtcNow - lastRequest;
                var minInterval = _options.MinRequestIntervalMinutes > 0 ? _options.MinRequestIntervalMinutes : 2;
                if (timeSinceLastRequest.TotalMinutes < minInterval)
                {
                    _logger.LogWarning("Ignoring duplicate AI summary request for TalkGroup {TalkGroupId} (last request: {TimeSince} minutes ago)",
                        request.TalkGroupId, timeSinceLastRequest.TotalMinutes);
                    return null;
                }
            }

            // Acquire semaphore to limit concurrent requests
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                // Record this request timestamp and clean up old ones
                _lastRequestTimes[requestKey] = DateTimeOffset.UtcNow;

                var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);
                var keysToRemove = _lastRequestTimes.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                    _lastRequestTimes.TryRemove(key, out _);

                var maxConcurrent = _options.MaxConcurrentRequests > 0 ? _options.MaxConcurrentRequests : 3;
                _logger.LogInformation("Starting AI summary generation for TalkGroup {TalkGroupId} (concurrent requests: {Active}/{Max})",
                    request.TalkGroupId, maxConcurrent - _semaphore.CurrentCount, maxConcurrent);

                var aiResponse = await GenerateAISummaryAsync(transcriptText, talkGroup, transcripts.Count, totalDuration, callTimes, timeSpan, cancellationToken);

                if (aiResponse == null)
                {
                    _logger.LogError("Failed to generate AI summary for TalkGroup {TalkGroupId}", request.TalkGroupId);
                    return null;
                }

                // Save to database
                var summaryEntity = new TranscriptSummaryResponse
                {
                    TalkGroupId = request.TalkGroupId,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TranscriptCount = transcripts.Count,
                    TotalDurationSeconds = totalDuration,
                    Summary = aiResponse.Summary,
                    KeyTopics = aiResponse.KeyTopics,
                    NotableIncidentsWithCallIds = aiResponse.NotableIncidentsWithCallIds,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    FromCache = false
                }.ToEntity();

                var savedSummary = await _summariesService.CreateAsync(summaryEntity);

                var response = savedSummary.ToResponse();
                response.FromCache = false;

                _logger.LogInformation("Generated and saved summary to database for TalkGroup {TalkGroupId} with {TranscriptCount} transcripts",
                    request.TalkGroupId, transcripts.Count);

                return response;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for TalkGroup {TalkGroupId}", request.TalkGroupId);
            return null;
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            var isAvailable = _options.Enabled &&
                   !string.IsNullOrEmpty(_options.AzureOpenAIEndpoint) &&
                   !string.IsNullOrEmpty(_options.AzureOpenAIKey) &&
                   !string.IsNullOrEmpty(_options.ChatDeploymentName);
            return Task.FromResult(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Agent Framework service availability");
            return Task.FromResult(false);
        }
    }

    public async Task ClearCacheAsync(int? talkGroupId = null)
    {
        try
        {
            if (talkGroupId.HasValue)
            {
                var summaries = await _summariesService.GetByTalkGroupAsync(talkGroupId.Value, 1, 1000);
                foreach (var summary in summaries.Items)
                {
                    await _summariesService.DeleteAsync(summary.Id);
                }
                _logger.LogInformation("Cleared {Count} cached summaries for TalkGroup {TalkGroupId}",
                    summaries.Items.Count, talkGroupId);
            }
            else
            {
                var allSummaries = await _summariesService.GetAllAsync(1, 10000);
                foreach (var summary in allSummaries.Items)
                {
                    await _summariesService.DeleteAsync(summary.Id);
                }
                _logger.LogInformation("Cleared {Count} cached summaries", allSummaries.Items.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for TalkGroup {TalkGroupId}", talkGroupId);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }

    private async Task<List<Transcription>> GetTranscriptsForTimeWindow(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        try
        {
            return await _transcriptionsService.GetByTalkGroupAndTimeRangeAsync(talkGroupId, startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transcripts for TalkGroup {TalkGroupId}", talkGroupId);
            return new List<Transcription>();
        }
    }

    private string PrepareTranscriptText(List<Transcription> transcripts)
    {
        var sb = new StringBuilder();

        foreach (var transcript in transcripts)
        {
            var call = transcript.Recording?.Call;
            var callId = call?.Id ?? 0;
            var callTime = call?.RecordingTime.ToString("yyyy-MM-dd HH:mm:ss") ?? transcript.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            var duration = call?.DurationSeconds > 0 ? $"{call.DurationSeconds}s" : "Unknown";
            var frequency = call?.FrequencyHz > 0 ? $"{call.FrequencyHz:F0} Hz" : "Unknown";
            var confidence = transcript.Confidence?.ToString("P1") ?? "N/A";

            sb.AppendLine($"[{callTime}] Call ID: {callId}, Duration: {duration}, Frequency: {frequency}, Confidence: {confidence}");
            sb.AppendLine(transcript.FullText);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<AISummaryResponse?> GenerateAISummaryAsync(
        string transcriptText, TalkGroup talkGroup,
        int callCount, double totalDuration, List<Call> callTimes, TimeSpan timeSpan,
        CancellationToken cancellationToken)
    {
        if (_azureOpenAIClient == null)
        {
            _logger.LogError("Azure OpenAI client is not configured — check AzureOpenAIEndpoint and AzureOpenAIKey");
            return null;
        }

        try
        {
            var talkGroupName = GetTalkGroupDisplayName(talkGroup);
            var timePeriod = $"{callCount} calls with total duration {TimeSpan.FromSeconds(totalDuration):hh\\:mm\\:ss}";
            var averageDuration = callCount > 0 ? TimeSpan.FromSeconds(totalDuration / callCount).ToString(@"mm\:ss") : "N/A";
            var timeSpanFormatted = timeSpan.TotalHours > 0 ?
                timeSpan.ToString(@"hh\:mm\:ss") :
                timeSpan.ToString(@"mm\:ss");

            var timingAnalysis = "";
            if (callTimes.Any())
            {
                var firstCall = callTimes.First().RecordingTime;
                var lastCall = callTimes.Last().RecordingTime;
                timingAnalysis = $"First call: {firstCall:yyyy-MM-dd HH:mm:ss}, Last call: {lastCall:yyyy-MM-dd HH:mm:ss}, Time span: {timeSpanFormatted}";
            }

            var prompt = SUMMARY_PROMPT
                .Replace("{transcripts}", transcriptText)
                .Replace("{talkGroupName}", talkGroupName)
                .Replace("{talkGroupDescription}", !string.IsNullOrWhiteSpace(talkGroup.Description) ? talkGroup.Description : "No description available")
                .Replace("{talkGroupCategory}", !string.IsNullOrWhiteSpace(talkGroup.Tag) ? talkGroup.Tag : (!string.IsNullOrWhiteSpace(talkGroup.Category) ? talkGroup.Category : "Unknown"))
                .Replace("{timePeriod}", timePeriod)
                .Replace("{callCount}", callCount.ToString())
                .Replace("{totalDuration}", TimeSpan.FromSeconds(totalDuration).ToString(@"hh\:mm\:ss"))
                .Replace("{averageDuration}", averageDuration)
                .Replace("{timingAnalysis}", timingAnalysis);

            var chatClient = _azureOpenAIClient.GetChatClient(_options.ChatDeploymentName);

            var completionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _options.MaxTokens > 0 ? _options.MaxTokens : 1500,
                Temperature = (float)(_options.Temperature > 0 ? _options.Temperature : 0.3)
            };

            var result = await chatClient.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                completionOptions,
                cancellationToken);

            var responseText = result.Value.Content[0].Text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Received empty response from AI service");
                return null;
            }

            try
            {
                var aiResponse = JsonSerializer.Deserialize<AISummaryResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return aiResponse;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed to parse AI response as JSON, using fallback parsing. Response: {Response}", responseText);

                return new AISummaryResponse
                {
                    Summary = responseText,
                    KeyTopics = new List<string>(),
                    NotableIncidentsWithCallIds = new List<SignalRadio.Core.Models.NotableIncident>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary");
            return null;
        }
    }

    private string GetTalkGroupDisplayName(TalkGroup talkGroup)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(talkGroup.AlphaTag))
            parts.Add(talkGroup.AlphaTag);
        else if (!string.IsNullOrWhiteSpace(talkGroup.Name))
            parts.Add(talkGroup.Name);

        if (parts.Count == 0)
            parts.Add($"TalkGroup {talkGroup.Number}");
        else
            parts.Add($"({talkGroup.Number})");

        return string.Join(" ", parts);
    }

    private class AISummaryResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyTopics { get; set; } = new();
        public List<SignalRadio.Core.Models.NotableIncident> NotableIncidentsWithCallIds { get; set; } = new();
    }
}
