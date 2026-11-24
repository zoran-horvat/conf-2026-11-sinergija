using System.Collections.Concurrent;
using System.Diagnostics;

namespace MercuryOutpost.Web.AI;

/// <summary>
/// Records the OpenTelemetry spans produced by the chat client pipelines during one operation,
/// so the UI can show them. It listens to the same ActivitySource the pipelines write to, next to
/// the OpenTelemetry exporter, and keeps only the spans that belong to the operation's trace.
/// </summary>
public sealed class TelemetryCapture
{
    private static readonly ActivitySource Source = new(ChatClientRegistration.TelemetrySource);
    private static readonly ConcurrentDictionary<ActivityTraceId, TelemetryCapture> Active = new();

    static TelemetryCapture() =>
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == ChatClientRegistration.TelemetrySource,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (Active.TryGetValue(activity.TraceId, out var capture)) capture.spans.Enqueue(activity);
            },
        });

    private readonly Activity? root;
    private readonly ConcurrentQueue<Activity> spans = new();

    private TelemetryCapture(string operationName)
    {
        root = Source.StartActivity(operationName);
        if (root is not null) Active[root.TraceId] = this;
    }

    /// <summary>Starts a root span; every span created under it until <see cref="Stop"/> is recorded.</summary>
    public static TelemetryCapture Start(string operationName) => new(operationName);

    public IReadOnlyList<CapturedSpan> Stop()
    {
        if (root is null) return [];

        root.Stop();
        Active.TryRemove(root.TraceId, out _);

        return spans
            .OrderBy(span => span.StartTimeUtc)
            .Select(span => new CapturedSpan(
                span.DisplayName,
                span.Duration,
                span.Status,
                span.TagObjects
                    .Where(tag => tag.Value is not null)
                    .Select(tag => new KeyValuePair<string, string>(tag.Key, Format(tag.Value!)))
                    .ToList()))
            .ToList();
    }

    private static string Format(object value) =>
        value switch
        {
            string text => text,
            System.Collections.IEnumerable items => string.Join(", ", items.Cast<object>()),
            _ => value.ToString() ?? "",
        };
}

public sealed record CapturedSpan(
    string Name, TimeSpan Duration, ActivityStatusCode Status, IReadOnlyList<KeyValuePair<string, string>> Tags);
