using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.AI;

/// <summary>
/// The IChatClient the rest of the application depends on.
/// Each call is forwarded to the pipeline of the provider selected in the UI at that moment.
/// </summary>
public sealed class SelectedChatClient(ModelSelection selection, IServiceProvider services) : IChatClient
{
    private IChatClient Current => services.GetRequiredKeyedService<IChatClient>(selection.Current);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.GetResponseAsync(messages, options, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : Current.GetService(serviceType, serviceKey);

    public void Dispose() { }
}
