using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SignalRadio.Core.Interfaces;
using SignalRadio.Core.Models;
using SignalRadio.DataAccess.Services;
using SignalRadio.DataAccess;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SignalRadio.Api.Services;

public class SemanticKernelTranscriptSummaryService : ITranscriptSummaryService
{
    private readonly SemanticKernelOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SemanticKernelTranscriptSummaryService> _logger;
    private readonly ITranscriptionsService _transcriptionsService;
    private readonly ITalkGroupsService _talkGroupsService;
    private readonly Kernel _kernel;
    private readonly KernelFunction _summaryFunction;

    private const string CACHE_KEY_PREFIX = "transcript_summary_";
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

        Please be concise but thorough. Focus on operational significance and avoid speculation about information not clearly stated in the transcripts.

        IMPORTANT: Respond ONLY with a valid JSON object. Do not include any explanatory text, markdown formatting, or code blocks. Return only the raw JSON object with the following structure:
        {
            "summary": "Overall summary of the communications and activities",
            "keyTopics": ["topic1", "topic2", "topic3"],
            "notableIncidentsWithCallIds": [
                {"description": "incident description", "callIds": [12345, 12346]},
                {"description": "another incident", "callIds": [12347]}
            ]
        }

        For notableIncidentsWithCallIds, include arrays of Call IDs from the transcript headers when incidents can be linked to specific calls. Multiple calls can be related to the same incident if they show progression, updates, or different perspectives of the same event. The callIds should be integers matching the "Call ID" fields from the transcript entries.

        Radio Communication Transcripts:
        {{$transcripts}}

        Additional Context:
        - Talk Group: {{$talkGroupName}}
        - Time Period: {{$timePeriod}}
        - Total Calls: {{$callCount}}
        - Total Duration: {{$totalDuration}}
        - Average Call Duration: {{$averageDuration}}
        - Timing Analysis: {{$timingAnalysis}}
        """;

    public SemanticKernelTranscriptSummaryService(
        IOptions<SemanticKernelOptions> options,
        IMemoryCache cache,
        ILogger<SemanticKernelTranscriptSummaryService> logger,
        ITranscriptionsService transcriptionsService,
        ITalkGroupsService talkGroupsService)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _transcriptionsService = transcriptionsService;
        _talkGroupsService = talkGroupsService;

        // Initialize Semantic Kernel
        var kernelBuilder = Kernel.CreateBuilder();
        
        if (!string.IsNullOrEmpty(_options.AzureOpenAIEndpoint) && !string.IsNullOrEmpty(_options.AzureOpenAIKey))
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: _options.ChatDeploymentName,
                endpoint: _options.AzureOpenAIEndpoint,
                apiKey: _options.AzureOpenAIKey);
        }

        _kernel = kernelBuilder.Build();
        _summaryFunction = _kernel.CreateFunctionFromPrompt(SUMMARY_PROMPT);
    }

    public async Task<TranscriptSummaryResponse?> GenerateSummaryAsync(TranscriptSummaryRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Semantic Kernel service is disabled");
            return null;
        }

        try
        {
            // Check cache first
            var cacheKey = GenerateCacheKey(request.TalkGroupId, request.StartTime, request.EndTime);
            if (!request.ForceRefresh && _cache.TryGetValue(cacheKey, out TranscriptSummaryResponse? cachedSummary))
            {
                _logger.LogInformation("Retrieved cached summary for TalkGroup {TalkGroupId}", request.TalkGroupId);
                cachedSummary!.FromCache = true;
                return cachedSummary;
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
                    NotableIncidents = new List<string>(),
                    NotableIncidentsWithCallIds = new List<NotableIncident>(),
                    GeneratedAt = DateTimeOffset.UtcNow,
                    FromCache = false
                };
            }

            // Prepare transcript data for AI analysis
            var transcriptText = PrepareTranscriptText(transcripts);
            var totalDuration = transcripts.Where(t => t.Recording?.Call?.DurationSeconds > 0)
                                        .Sum(t => t.Recording!.Call!.DurationSeconds);

            // Collect timing insights for the LLM
            var callTimes = transcripts
                .Where(t => t.Recording?.Call != null)
                .Select(t => t.Recording!.Call!)
                .OrderBy(c => c.RecordingTime)
                .ToList();

            var timeSpan = callTimes.Any() ? 
                callTimes.Last().RecordingTime - callTimes.First().RecordingTime : 
                TimeSpan.Zero;

            // Generate AI summary with enhanced timing context
            var aiResponse = await GenerateAISummary(transcriptText, talkGroup, transcripts.Count, totalDuration, callTimes, timeSpan, cancellationToken);
            
            if (aiResponse == null)
            {
                _logger.LogError("Failed to generate AI summary for TalkGroup {TalkGroupId}", request.TalkGroupId);
                return null;
            }

            var response = new TranscriptSummaryResponse
            {
                TalkGroupId = request.TalkGroupId,
                TalkGroupName = GetTalkGroupDisplayName(talkGroup),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                TranscriptCount = transcripts.Count,
                TotalDurationSeconds = totalDuration,
                Summary = aiResponse.Summary,
                KeyTopics = aiResponse.KeyTopics,
                NotableIncidents = aiResponse.NotableIncidents,
                NotableIncidentsWithCallIds = aiResponse.NotableIncidentsWithCallIds,
                GeneratedAt = DateTimeOffset.UtcNow,
                FromCache = false
            };

            // Cache the result
            var cacheExpiry = TimeSpan.FromMinutes(_options.CacheDurationMinutes);
            _cache.Set(cacheKey, response, cacheExpiry);

            _logger.LogInformation("Generated and cached summary for TalkGroup {TalkGroupId} with {TranscriptCount} transcripts", 
                request.TalkGroupId, transcripts.Count);

            return response;
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
            _logger.LogError(ex, "Error checking Semantic Kernel service availability");
            return Task.FromResult(false);
        }
    }

    public async Task ClearCacheAsync(int? talkGroupId = null)
    {
        try
        {
            if (talkGroupId.HasValue)
            {
                // Clear cache for specific talkgroup - this is simplified for now
                // In a production environment, you might want to track cache keys more explicitly
                _logger.LogInformation("Cache clearing for specific TalkGroup {TalkGroupId} requested", talkGroupId);
            }
            else
            {
                // Clear all cached summaries - this is simplified for now
                _logger.LogInformation("Full cache clearing requested");
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
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
        
        foreach (var transcript in transcripts.OrderBy(t => t.Recording?.Call?.RecordingTime ?? t.CreatedAt))
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

    private async Task<AISummaryResponse?> GenerateAISummary(string transcriptText, TalkGroup talkGroup, 
        int callCount, double totalDuration, List<Call> callTimes, TimeSpan timeSpan, CancellationToken cancellationToken)
    {
        try
        {
            var talkGroupName = GetTalkGroupDisplayName(talkGroup);
            var timePeriod = $"{callCount} calls with total duration {TimeSpan.FromSeconds(totalDuration):hh\\:mm\\:ss}";
            var averageDuration = callCount > 0 ? TimeSpan.FromSeconds(totalDuration / callCount).ToString(@"mm\:ss") : "N/A";
            var timeSpanFormatted = timeSpan.TotalHours > 0 ? 
                timeSpan.ToString(@"hh\:mm\:ss") : 
                timeSpan.ToString(@"mm\:ss");
            
            // Create a summary of call timing patterns
            var timingAnalysis = "";
            if (callTimes.Any())
            {
                var firstCall = callTimes.First().RecordingTime;
                var lastCall = callTimes.Last().RecordingTime;
                timingAnalysis = $"First call: {firstCall:yyyy-MM-dd HH:mm:ss}, Last call: {lastCall:yyyy-MM-dd HH:mm:ss}, Time span: {timeSpanFormatted}";
            }
            
            var result = await _summaryFunction.InvokeAsync(_kernel, new()
            {
                ["transcripts"] = transcriptText,
                ["talkGroupName"] = talkGroupName,
                ["timePeriod"] = timePeriod,
                ["callCount"] = callCount.ToString(),
                ["totalDuration"] = TimeSpan.FromSeconds(totalDuration).ToString(@"hh\:mm\:ss"),
                ["averageDuration"] = averageDuration,
                ["timingAnalysis"] = timingAnalysis
            }, cancellationToken);

            // Log detailed token usage information from the response
            LogTokenUsage(result, talkGroup.Id);

            var responseText = result.GetValue<string>();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Received empty response from AI service");
                return null;
            }

            // Try to parse JSON response
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
                
                // Fallback: clean the response and treat it as the summary
                return new AISummaryResponse
                {
                    Summary = responseText,
                    KeyTopics = new List<string>(),
                    NotableIncidents = new List<string>(),
                    NotableIncidentsWithCallIds = new List<NotableIncident>()
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

    private string GenerateCacheKey(int talkGroupId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var startKey = startTime.ToString("yyyy-MM-dd-HH-mm");
        var endKey = endTime.ToString("yyyy-MM-dd-HH-mm");
        return $"{CACHE_KEY_PREFIX}{talkGroupId}_{startKey}_{endKey}";
    }

    private void LogTokenUsage(FunctionResult result, int talkGroupId)
    {
        try
        {
            // Try to get usage information from the result metadata
            if (result.Metadata?.TryGetValue("Usage", out var usageObj) == true && usageObj != null)
            {
                // Check if it's an Azure OpenAI usage object
                var usageType = usageObj.GetType();
                if (usageType.Name.Contains("Usage") || usageType.Name.Contains("CompletionUsage"))
                {
                    // Use reflection to extract token counts
                    var promptTokens = GetPropertyValue(usageObj, "PromptTokens") ?? 
                                     GetPropertyValue(usageObj, "InputTokens") ?? 0;
                    var completionTokens = GetPropertyValue(usageObj, "CompletionTokens") ?? 
                                         GetPropertyValue(usageObj, "OutputTokens") ?? 0;
                    var totalTokens = GetPropertyValue(usageObj, "TotalTokens") ?? 
                                    (int)promptTokens + (int)completionTokens;

                    _logger.LogInformation("AI Summary Token Usage - TalkGroup: {TalkGroupId}, " +
                        "Prompt tokens: {PromptTokens}, Completion tokens: {CompletionTokens}, Total tokens: {TotalTokens}",
                        talkGroupId, promptTokens, completionTokens, totalTokens);
                }
                else
                {
                    // Fallback: log the usage object as JSON if possible
                    try
                    {
                        var usageJson = JsonSerializer.Serialize(usageObj, new JsonSerializerOptions { WriteIndented = false });
                        _logger.LogInformation("AI Summary Token Usage - TalkGroup: {TalkGroupId}, Usage: {Usage}",
                            talkGroupId, usageJson);
                    }
                    catch
                    {
                        _logger.LogInformation("AI Summary Token Usage - TalkGroup: {TalkGroupId}, Usage: {Usage}",
                            talkGroupId, usageObj.ToString());
                    }
                }
            }
            else if (result.Metadata != null)
            {
                // Log all available metadata keys for debugging
                var metadataKeys = string.Join(", ", result.Metadata.Keys);
                _logger.LogDebug("AI Summary Metadata - TalkGroup: {TalkGroupId}, Available keys: {MetadataKeys}",
                    talkGroupId, metadataKeys);

                // Check for other possible usage-related keys
                foreach (var kvp in result.Metadata)
                {
                    if (kvp.Key.Contains("token", StringComparison.OrdinalIgnoreCase) || 
                        kvp.Key.Contains("usage", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("AI Summary Usage Info - TalkGroup: {TalkGroupId}, {Key}: {Value}",
                            talkGroupId, kvp.Key, kvp.Value);
                    }
                }
            }
            else
            {
                _logger.LogDebug("AI Summary - TalkGroup: {TalkGroupId}, No metadata available for token usage",
                    talkGroupId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error logging token usage for TalkGroup {TalkGroupId}", talkGroupId);
        }
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        try
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private class AISummaryResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyTopics { get; set; } = new();
        public List<string> NotableIncidents { get; set; } = new();
        public List<NotableIncident> NotableIncidentsWithCallIds { get; set; } = new();
    }
}
