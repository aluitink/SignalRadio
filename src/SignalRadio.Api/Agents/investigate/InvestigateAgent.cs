using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace SignalRadio.Api.Agents;

/// <summary>
/// The investigation agent. A single conversational agent that both performs the initial
/// investigation and answers follow-up questions in the same chat.
///
/// It is grounded in an anchor call plus the surrounding radio traffic (provided as a
/// per-run system message) and uses tools to discover additional information (talkgroup
/// info, wider time windows, semantic search). It answers in plain natural language —
/// the initial turn produces a narrative summary of the event, and subsequent turns
/// answer the user's follow-up questions.
///
/// Its MAF <see cref="AgentSession"/> is serialized and persisted on the investigation
/// row after every turn, so the chat can be reloaded and continued across requests and
/// restarts.
/// </summary>
public class InvestigateAgent : IAgentDefinition
{
    public string Name => "investigate";

    public AIAgent BuildAgent(IChatClient chatClient, string instructions, Func<AIFunction[]> tools, string? extraSystemMessage = null)
    {
        // Prepend per-run context (anchor call + surrounding window) to the base instructions so
        // the system message is always at the beginning of the conversation. Providers such as
        // OpenAI reject requests where a system message is not the first message.
        var combinedInstructions = string.IsNullOrWhiteSpace(extraSystemMessage)
            ? instructions
            : $"{extraSystemMessage}\n\n{instructions}";

        var options = new ChatClientAgentOptions
        {
            Name = Name,
            ChatOptions = new ChatOptions
            {
                Instructions = combinedInstructions,
                // No ResponseFormat: the agent answers in natural language.
                Tools = tools().Select(t => (AITool)t).ToList(),
            },
        };
        return new ChatClientAgent(chatClient, options);
    }
}
