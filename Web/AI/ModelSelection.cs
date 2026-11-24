namespace MercuryOutpost.Web.AI;

/// <summary>
/// The model provider currently selected in the UI.
/// Registered as a singleton, so the choice survives page reloads and applies to every page.
/// </summary>
public sealed class ModelSelection
{
    private ModelProvider current = ModelProvider.Anthropic;

    public event Action? Changed;

    public ModelProvider Current
    {
        get => current;
        set
        {
            if (current == value) return;
            current = value;
            Changed?.Invoke();
        }
    }
}
