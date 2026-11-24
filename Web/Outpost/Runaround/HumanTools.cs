using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.Outpost.Runaround;

public sealed class HumanTools(World world, string self)
{
    public IReadOnlyList<AIFunction> All =>
    [
        AIFunctionFactory.Create(Move, "move"),
    ];

    [Description("Moves you one step in the given direction. North is toward smaller Y, east toward larger X.")]
    private string Move(Direction direction) =>
        world.Move(self, direction) is { } location
            ? $"You are now at ({location.X}, {location.Y})."
            : "You cannot move.";
}
