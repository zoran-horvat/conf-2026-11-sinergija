using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.Outpost;

public sealed class Robot(
    string name, IChatClient brain,
    string instructions, IEnumerable<AITool> tools)
    : OutpostAgent(name, brain, ComposeInstructions(instructions), tools)
{
    private static string ComposeInstructions(string instructions)
    {
        var laws = string.Join('\n', RobotRules.LawsOfRobotics.Select((law, i) => $"{i + 1}. {law}"));
        return $"The Three Laws of Robotics:\n{laws}\n\n{instructions.Trim()}";
    }
}
