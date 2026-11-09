using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Agents;

/// <summary>
/// The summaries agent. Produces a per-talkgroup, over-time summary of recent radio traffic.
///
/// It is NOT run per-call. The <c>SummariesAgentService</c> triggers it for a talkgroup only when
/// enough new transcripts have accumulated since the last summary (see <c>SummariesOptions</c>).
///
/// Tools give the agent access to context (previous summary, talkgroup metadata) so it can write
/// a summary that notes what is new versus ongoing. Tools are built per-run with scoped services
/// captured in a closure.
/// </summary>
public class SummariesAgent : IAgentDefinition
{
    public string Name => "summaries";

    public AIAgent BuildAgent(IChatClient chatClient, string instructions, Func<AIFunction[]> tools)
    {
        // Constrain the model to return a JSON object matching SummaryReport. The OpenAI
        // provider maps this to response_format=json_schema, so the output is guaranteed to be
        // parseable. The orchestrator (SummariesAgentService) reads the result via RunAsync<SummaryReport>.
        var options = new ChatClientAgentOptions
        {
            Name = Name,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<SummaryReport>(),
                Tools = tools().Select(t => (AITool)t).ToList(),
            },
        };
        return new ChatClientAgent(chatClient, options);
    }

    /// <summary>
    /// Build the agent's function tools, capturing the scoped services in a closure so the
    /// (static) tool methods can reach them.
    /// </summary>
    public static AIFunction[] CreateTools(IServiceProvider scoped)
    {
        var summaries = scoped.GetRequiredService<ITranscriptSummariesService>();
        var talkGroups = scoped.GetRequiredService<ITalkGroupsService>();

        async Task<string> GetPreviousSummary([Description("The talkgroup id.")] int talkGroupId)
        {
            var last = await summaries.GetLatestForTalkGroupAsync(talkGroupId);
            return last == null ? "No previous summary exists for this talkgroup." : last.Summary;
        }

        async Task<string> GetTalkGroupInfo([Description("The talkgroup id.")] int talkGroupId)
        {
            var tg = await talkGroups.GetByIdAsync(talkGroupId);
            return tg == null
                ? $"Talkgroup {talkGroupId} not found."
                : $"Number: {tg.Number}, Name: {tg.Name ?? "n/a"}, AlphaTag: {tg.AlphaTag ?? "n/a"}, Description: {tg.Description ?? "n/a"}";
        }

        return new AIFunction[]
        {
            AIFunctionFactory.Create(GetPreviousSummary),
            AIFunctionFactory.Create(GetTalkGroupInfo),
        };
    }
}
