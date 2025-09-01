using Microsoft.AspNetCore.SignalR;
using SignalRadio.Api.Hubs;
using SignalRadio.Core.Services;
using SignalRadio.Api.Extensions;
using Microsoft.AspNetCore.Http;

namespace SignalRadio.Api.Services;

public class HubCallNotifier : ICallNotifier
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HubCallNotifier> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HubCallNotifier(IServiceProvider services, ILogger<HubCallNotifier> logger, IHttpContextAccessor httpContextAccessor)
    {
        _services = services;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task NotifyCallUpdatedAsync(int callId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _services.CreateScope();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TalkGroupHub>>();
            var callsService = scope.ServiceProvider.GetRequiredService<SignalRadio.DataAccess.Services.ICallsService>();

            var updatedCall = await callsService.GetByIdAsync(callId);
            if (updatedCall == null)
            {
                return;
            }

            // Get the API base URL from HTTP context if available
            var request = _httpContextAccessor.HttpContext?.Request;
            var apiBaseUrl = request != null ? $"{request.Scheme}://{request.Host}/api" : "/api";

            // Convert to DTO format - same as API returns
            var callDto = updatedCall.ToDto(apiBaseUrl);

            // Log that we're pushing this call to the global all-calls monitor group
            _logger.LogInformation("Pushing call {CallId} to all_calls_monitor (TalkGroup={TalkGroupId}, RecordingCount={RecordingCount})",
                updatedCall.Id, updatedCall.TalkGroupId, callDto.Recordings.Count);
            await hubContext.Clients.Group("all_calls_monitor").SendAsync("CallUpdated", callDto, cancellationToken);
            //await hubContext.Clients.Group($"talkgroup_{updatedCall.TalkGroupId}").SendAsync("CallUpdated", callDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify call update for call {CallId}", callId);
        }
    }
}
