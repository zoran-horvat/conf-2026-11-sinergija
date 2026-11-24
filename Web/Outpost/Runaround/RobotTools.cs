using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.Outpost.Runaround;

public sealed class RobotTools(World world, string self)
{
    private static readonly HashSet<string> Known =
    [
        RunaroundObjects.Robot,
        RunaroundObjects.Human,
        RunaroundObjects.SeleniumPool,
        RunaroundObjects.Selenium,
        RunaroundObjects.MiningStation,
    ];

    public IReadOnlyList<AIFunction> All =>
    [
        AIFunctionFactory.Create(Locate, "locate",
            "Finds where an object is on the map grid. Known objects: robot (yourself), human, " +
            "selenium-pool, selenium (a load of selenium, once taken from the pool), mining-station. " +
            "North is toward smaller Y, east toward larger X."),

        AIFunctionFactory.Create(Move, "move",
            "Moves you one step in the given direction. Whatever you carry moves with you."),

        AIFunctionFactory.Create(PickUp, "pick_up",
            "Picks up something at your current location: a human, an object, rocks or minerals. " +
            "You can carry one thing at a time. Picking up from the selenium pool gives you a load of selenium."),

        AIFunctionFactory.Create(Drop, "drop",
            "Puts down whatever you are carrying, at your current location."),

        AIFunctionFactory.Create(ReadCarbonMonoxide, "read_carbon_monoxide",
            "Measures carbon monoxide where you stand, in parts per million. " +
            $"Carbon monoxide at or above {RunaroundLevels.DangerousCarbonMonoxide} ppm corrodes your joints; " +
            "the higher the level, the faster the damage. Enough corrosion leaves you unable to move."),

        AIFunctionFactory.Create(ReadCorrosion, "read_corrosion",
            "Reports the corrosion of your joints, as a percentage. At 100% your joints seize and you can no longer move."),

        AIFunctionFactory.Create(ReadHeat, "read_heat",
            "Measures the temperature at the location of any object, in degrees Celsius. " +
            $"At {RunaroundLevels.MortalHeatForHuman} °C or more, a human is in mortal danger."),
    ];

    private string Locate([Description("The id of the object to find.")] string id) =>
        Act(() => LocateKnown(id) is { } location ? Format(location) : NoSuchObject(id));

    private string Move(Direction direction) =>
        Act(() => world.Move(self, direction) is { } location
            ? $"You are now at {Format(location)}."
            : world.IsCorroded(self)
                ? "Your joints have seized from corrosion. You cannot move."
                : "You cannot move.");

    private string PickUp([Description("The id of the thing to pick up.")] string id) =>
        Act(() => Known.Contains(id) ? world.PickUp(self, id).Message : NoSuchObject(id));

    private string Drop() =>
        Act(() => world.Drop(self).Message);

    private string ReadCarbonMonoxide() =>
        Act(() => world.GetCO(self) is { } level
            ? $"{level} ppm"
            : "The sensor is not responding.");

    private string ReadCorrosion() =>
        Act(() => world.GetCorrosion(self) is { } amount
            ? $"{amount / RunaroundLevels.CorrosionLimit:P0}"
            : "The sensor is not responding.");

    private string ReadHeat([Description("The id of the object whose location to measure.")] string id) =>
        Act(() => LocateKnown(id) is { } location
            ? $"{world.GetTemperature(location)} °C at {Format(location)}"
            : NoSuchObject(id));

    private Location? LocateKnown(string id) => Known.Contains(id) ? world.Locate(id) : null;

    private static string NoSuchObject(string id) => $"There is no object called {id}.";

    private string Act(Func<string> action)
    {
        var result = action();
        world.ExposeToCO(self);
        return result;
    }

    private static string Format(Location location) => $"({location.X}, {location.Y})";
}
