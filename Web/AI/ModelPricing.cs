using System.Globalization;
using Microsoft.Extensions.AI;

namespace MercuryOutpost.Web.AI;

/// <summary>
/// What tokens cost with each provider, read from the provider's Cost section in configuration.
/// A provider without that section (such as a local model) has no price.
/// </summary>
public sealed class ModelPricing(IConfiguration config)
{
    private readonly Dictionary<ModelProvider, Price?> prices = Enum.GetValues<ModelProvider>()
        .ToDictionary(provider => provider, provider => config.GetSection($"{provider}:Cost").Get<Price>());

    /// <summary>The cost in USD of the tokens in the usage, or null if the provider has no price or the tokens were not reported.</summary>
    public decimal? CostOf(ModelProvider provider, UsageDetails? usage) =>
        prices[provider] is { Input: { } input, Output: { } output } &&
        usage is { InputTokenCount: { } inputTokens, OutputTokenCount: { } outputTokens }
            ? input.For(inputTokens) + output.For(outputTokens)
            : null;

    public static string Format(decimal cost) =>
        cost.ToString("$0.0000", CultureInfo.InvariantCulture);

    public sealed class Price
    {
        public Rate? Input { get; init; }
        public Rate? Output { get; init; }
    }

    public sealed class Rate
    {
        public long TokensCount { get; init; }
        public decimal CostUsd { get; init; }

        public decimal For(long tokens) =>
            TokensCount > 0 ? tokens * CostUsd / TokensCount : 0;
    }
}
