using Anthropic;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace MercuryOutpost.Web.AI;

public static class ChatClientRegistration
{
    public const string TelemetrySource = "MercuryOutpost";

    public const int MaximumRoundTrips = 40;

    public const int MaximumOutputTokens = 16_000;

    public static IServiceCollection AddChatClients(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ModelSelection>();
        services.AddSingleton<ModelPricing>();

        services.AddKeyedChatClient(ModelProvider.Anthropic, _ => CreateAnthropic(config.GetSection("Anthropic")))
            .UseFunctionInvocation(configure: invoker => invoker.MaximumIterationsPerRequest = MaximumRoundTrips)
            .UseOpenTelemetry(sourceName: TelemetrySource);

        services.AddKeyedChatClient(ModelProvider.Ollama, _ => CreateOllama(config.GetSection("Ollama")))
            .UseFunctionInvocation(configure: invoker => invoker.MaximumIterationsPerRequest = MaximumRoundTrips)
            .UseOpenTelemetry(sourceName: TelemetrySource);

        services.AddSingleton<IChatClient, SelectedChatClient>();

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(TelemetrySource).AddOtlpExporter())
            .WithMetrics(metrics => metrics.AddMeter(TelemetrySource).AddOtlpExporter());

        return services;
    }

    private static IChatClient CreateAnthropic(IConfigurationSection section)
    {
        var apiKey = section["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("sk-ant-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The Anthropic API key is not configured. Set it with: dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\"");
        }

        return new AnthropicClient { ApiKey = apiKey }.AsIChatClient(Required(section, "Model"), defaultMaxOutputTokens: MaximumOutputTokens);
    }

    private static IChatClient CreateOllama(IConfigurationSection section) =>
        new OllamaApiClient(new Uri(Required(section, "Endpoint")), Required(section, "Model"));

    private static string Required(IConfigurationSection section, string key) =>
        section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configuration value {section.Key}:{key} is missing.");
}
