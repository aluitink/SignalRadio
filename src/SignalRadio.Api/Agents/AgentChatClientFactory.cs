using System.ClientModel;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace SignalRadio.Api.Agents;

/// <summary>
/// A minimal <see cref="IChatClient"/> that echoes a canned summary back. Used as a fallback
/// when no real LLM endpoint is configured, so the full summaries pipeline (agent build, tool
/// calls, persistence, embedding, SignalR notification) still runs end-to-end in simulation.
/// </summary>
public sealed class StubChatClient : IChatClient, IDisposable
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var userText = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;

        var lines = userText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var count = lines.Length;
        var first = lines.FirstOrDefault() ?? "";
        var last = lines.LastOrDefault() ?? "";

        var summary = $"[stub summary] {count} transcript(s) processed. First: {Truncate(first, 120)} Last: {Truncate(last, 120)}";

        return Task.FromResult(new ChatResponse(
            [new ChatMessage(ChatRole.Assistant, summary)]));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public IChatClient WithOptions(ChatOptions? options) => this;

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}

/// <summary>
/// Builds the OpenAI-compatible <see cref="IChatClient"/> used by the AI agents.
///
/// Reuses the existing "AI" config section (Endpoint, ApiKey, Model) so a single configuration
/// drives both the chat agents and (optionally) the OpenAI-compatible embedding client.
/// </summary>
public static class AgentChatClientFactory
{
    public static IChatClient Create(string endpoint, string? apiKey, string? model)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("AI:Endpoint is required for AI agents.");

        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey ?? ""), options);
        return client.GetChatClient(model ?? "gpt-4o-mini").AsIChatClient();
    }

    /// <summary>
    /// Returns a real chat client when an endpoint is configured, otherwise a
    /// <see cref="StubChatClient"/> so the agent pipeline still runs in simulation.
    /// </summary>
    public static IChatClient CreateOrStub(string? endpoint, string? apiKey, string? model)
        => string.IsNullOrWhiteSpace(endpoint)
            ? new StubChatClient()
            : Create(endpoint, apiKey, model);
}
