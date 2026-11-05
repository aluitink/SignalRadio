using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace SignalRadio.Api.Agents;

/// <summary>
/// Discovers and builds AI agents. Each agent lives in its own folder under the configured
/// prompt path (default "Agents") and consists of:
///
///   - a <c>PROMPT.md</c> file holding the system prompt (editable without recompiling), and
///   - an <see cref="IAgentDefinition"/> that supplies the agent's tools and builds the AIAgent.
///
/// Adding a new agent = adding a folder + registering its <see cref="IAgentDefinition"/>.
/// </summary>
public class AgentRegistry(ILogger<AgentRegistry> logger, IConfiguration configuration)
{
    private readonly string _promptRoot = configuration["Agents:PromptPath"] ?? "Agents";
    private readonly Dictionary<string, IAgentDefinition> _agents = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAgentDefinition agent)
    {
        _agents[agent.Name] = agent;
        logger.LogDebug("Registered agent '{Name}'.", agent.Name);
    }

    public IReadOnlyCollection<string> AgentNames => _agents.Keys;

    /// <summary>Read the agent's PROMPT.md system prompt from disk.</summary>
    public string LoadPrompt(IAgentDefinition agent)
    {
        var candidates = new[]
        {
            Path.Combine(_promptRoot, agent.Name, "PROMPT.md"),
            Path.Combine(AppContext.BaseDirectory, _promptRoot, agent.Name, "PROMPT.md"),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                logger.LogDebug("Loaded prompt for '{Name}' from {Path} ({Len} chars).", agent.Name, path, text.Length);
                return text;
            }
        }
        throw new FileNotFoundException(
            $"PROMPT.md not found for agent '{agent.Name}'. Tried: {string.Join(", ", candidates)}");
    }

    /// <summary>Build a ready-to-run AIAgent for the named agent using the supplied chat client.</summary>
    public AIAgent Build(string name, IChatClient chatClient, Func<AIFunction[]> tools)
    {
        if (!_agents.TryGetValue(name, out var agent))
            throw new KeyNotFoundException($"Unknown agent '{name}'. Known: {string.Join(", ", _agents.Keys)}");

        var instructions = LoadPrompt(agent);
        return agent.BuildAgent(chatClient, instructions, tools);
    }
}
