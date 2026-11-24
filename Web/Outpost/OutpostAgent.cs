using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.Outpost;

public abstract class OutpostAgent
{
    private readonly ChatClientAgent agent;
    private AgentSession? session;
    private readonly SemaphoreSlim oneOrderAtATime = new(1, 1);

    public string Name { get; }
    public string Instructions { get; }

    protected OutpostAgent(
        string name, IChatClient brain, string instructions,
        IEnumerable<AITool> tools)
    {
        Name = name;
        Instructions = instructions;
        string? description = null;
        agent = new(brain, instructions, name, description, [.. tools]);
    }

    public async Task<AgentResponse> RunAsync(string order, CancellationToken cancellationToken = default)
    {
        await oneOrderAtATime.WaitAsync(cancellationToken);
        try
        {
            session ??= await agent.CreateSessionAsync(cancellationToken);
            return await agent.RunAsync(order, session, cancellationToken: cancellationToken);
        }
        finally
        {
            oneOrderAtATime.Release();
        }
    }

    public Task<AgentResponse> RunAsync(CancellationToken cancellationToken = default) =>
        RunAsync("Continue.", cancellationToken);

    public void Forget() => session = null;
}
