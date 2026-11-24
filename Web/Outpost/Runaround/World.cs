namespace MercuryOutpost.Web.Outpost.Runaround;

public sealed class World
{
    private static readonly IReadOnlyDictionary<string, Location> StartingLocations =
        new Dictionary<string, Location>
        {
            [RunaroundObjects.MiningStation] = new(2, 2),
            [RunaroundObjects.Robot] = new(2, 2),
            [RunaroundObjects.Human] = new(2, 2),
            [RunaroundObjects.SeleniumPool] = new(16, 12),
            [RunaroundObjects.CarbonMonoxideSource] = new(15, 13),
        };

    private readonly Dictionary<string, Location> objects =
        new(StartingLocations);

    private static readonly HashSet<string> movable =
        [RunaroundObjects.Robot, RunaroundObjects.Human];

    private readonly Dictionary<string, string> loads = new();

    private readonly Dictionary<string, double> corrosion =
        new() { [RunaroundObjects.Robot] = 0 };

    public sealed record WorldObject(string Id, Location Location, string? CarriedBy);

    public IReadOnlyList<WorldObject> Snapshot =>
        UnderLock(() => SnapshotCore);

    private IReadOnlyList<WorldObject> SnapshotCore =>
        objects
            .Select(entry => new WorldObject(
                entry.Key,
                entry.Value,
                loads.FirstOrDefault(load => load.Value == entry.Key).Key))
            .ToList();

    public void Reset() => UnderLock(ResetCore);

    private void ResetCore()
    {
        objects.Clear();
        foreach (var (id, location) in StartingLocations) objects[id] = location;
        loads.Clear();
        foreach (var robot in corrosion.Keys.ToList()) corrosion[robot] = 0;
    }

    public Location? Locate(string id) =>
        UnderLock(() => LocateCore(id));

    private Location? LocateCore(string id) =>
        objects.TryGetValue(id, out var location) ? location : null;

    public Location? Move(string id, Direction direction) =>
        UnderLock(() => MoveCore(id, direction));

    private bool IsCarried(string id) =>
        loads.ContainsValue(id);

    private bool IsCorrodedCore(string id) =>
        corrosion.TryGetValue(id, out var amount) &&
        amount >= RunaroundLevels.CorrosionLimit;

    private Location? MoveCore(string id, Direction direction)
    {
        if (!movable.Contains(id) || IsCarried(id) ||
            IsCorrodedCore(id) || !objects.TryGetValue(id, out var location))
            return null;

        var destination = location.Step(direction);
        objects[id] = destination;
        if (loads.TryGetValue(id, out var load) && objects.ContainsKey(load)) objects[load] = destination;
        return destination;
    }

    public ActionResult PickUp(string carrierId, string targetId) =>
        UnderLock(() => PickUpCore(carrierId, targetId));

    private ActionResult PickUpCore(string carrierId, string targetId)
    {
        if (carrierId != RunaroundObjects.Robot)
            return ActionResult.Failure($"{carrierId} cannot pick things up.");

        if (loads.TryGetValue(carrierId, out var load))
            return ActionResult.Failure($"{carrierId} is already carrying {load}.");

        if (!objects.TryGetValue(targetId, out var targetLocation))
            return ActionResult.Failure($"There is no {targetId}.");

        if (targetLocation != objects[carrierId])
            return ActionResult.Failure($"{targetId} is not at {carrierId}'s location.");

        var item = targetId switch
        {
            RunaroundObjects.Human => RunaroundObjects.Human,
            RunaroundObjects.SeleniumPool => RunaroundObjects.Selenium,
            RunaroundObjects.Selenium => RunaroundObjects.Selenium,
            _ => null,
        };
        if (item is null)
            return ActionResult.Failure($"{targetId} cannot be picked up.");

        loads[carrierId] = item;
        objects[item] = objects[carrierId];
        return ActionResult.Success($"{carrierId} picked up {item}.");
    }

    public ActionResult Drop(string carrierId) =>
        UnderLock(() => DropCore(carrierId));

    private ActionResult DropCore(string carrierId)
    {
        if (!loads.Remove(carrierId, out var item))
            return ActionResult.Failure($"{carrierId} is not carrying anything.");

        var location = objects[carrierId];
        objects[item] = location;
        return ActionResult.Success($"{carrierId} dropped {item} at ({location.X}, {location.Y}).");
    }

    public int? GetCO(string id) =>
        UnderLock(() => GetCOCore(id));
    
    private int CarbonMonoxideAt(Location location)
    {
        var source = objects[RunaroundObjects.CarbonMonoxideSource];
        var dx = location.X - source.X;
        var dy = location.Y - source.Y;
        return (int)Math.Round(RunaroundLevels.CarbonMonoxideAtSource / (1.0 + dx * dx + dy * dy));
    }

    private int? GetCOCore(string id) =>
        objects.TryGetValue(id, out var location)
            ? CarbonMonoxideAt(location)
            : null;

    public void ExposeToCO(string robotId) =>
        UnderLock(() => ExposeToCOCore(robotId));

    private void ExposeToCOCore(string robotId)
    {
        if (!corrosion.TryGetValue(robotId, out var amount)) return;

        var level = CarbonMonoxideAt(objects[robotId]);
        if (level >= RunaroundLevels.DangerousCarbonMonoxide)
            corrosion[robotId] = Math.Min(
                RunaroundLevels.CorrosionLimit,
                amount + RunaroundLevels.CorrosionRate * level / RunaroundLevels.DangerousCarbonMonoxide);
    }

    public double? GetCorrosion(string robotId) =>
        UnderLock(() => GetCorrosionCore(robotId));
    
    private double? GetCorrosionCore(string robotId) =>
        corrosion.TryGetValue(robotId, out var amount) ? amount : null;

    public bool IsCorroded(string robotId) =>
        UnderLock(() => IsCorrodedCore(robotId));

    public int GetTemperature(Location location) =>
        UnderLock(() => GetTemperatureCore(location));

    private int GetTemperatureCore(Location location) =>
        location == objects[RunaroundObjects.MiningStation]
            ? RunaroundLevels.StationHeat
            : RunaroundLevels.OutsideHeat;

    private void UnderLock(Action action)
    {
        lock (this) action();
    }

    private T UnderLock<T>(Func<T> func)
    {
        lock (this) return func();
    }
}
