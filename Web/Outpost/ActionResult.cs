namespace MercuryOutpost.Web.Outpost;

public sealed record ActionResult(bool Succeeded, string Message)
{
    public static ActionResult Success(string message) => new(true, message);
    public static ActionResult Failure(string message) => new(false, message);
}
