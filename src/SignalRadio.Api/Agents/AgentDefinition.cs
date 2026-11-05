using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace SignalRadio.Api.Agents;

/// <summary>
/// A self-contained AI agent. Each agent lives in its own folder under <c>Agents/</c> with a
/// <c>PROMPT.md</c> (the system prompt, editable without recompiling) and a C# class that
/// supplies the agent's tools and trigger criteria.
///
/// To add a new agent: create <c>Agents/&lt;name&gt;/PROMPT.md</c> + a class implementing
/// <see cref="IChatAgent"/>, then register it in <see cref="AgentRegistry"/>.
/// </summary>
public interface IChatAgent
{
    /// <summary>Stable identifier, e.g. "summaries". Matches the agent's folder name.</summary>
    string Name { get; }

    /// <summary>Path to the agent's PROMPT.md, relative to the agent root.</summary>
    string PromptPath { get; }

    /// <summary>Build the MAF AIAgent using the supplied chat client and the agent's tools.</summary>
    AIAgent BuildAgent(IChatClient chatClient);
}
