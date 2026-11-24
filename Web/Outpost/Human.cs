using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.Outpost;

public sealed class Human(
    string name, IChatClient brain,
    string instructions, IEnumerable<AITool> tools)
    : OutpostAgent(name, brain, instructions.Trim(), tools);
