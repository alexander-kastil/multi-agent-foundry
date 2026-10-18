using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

class SpanCollector(int latencyThresholdMs = 5000) : BaseExporter<Activity>
{
    readonly List<Activity> spans = [];

    public override ExportResult Export(in Batch<Activity> batch)
    {
        lock (spans)
            foreach (Activity activity in batch) spans.Add(activity);
        return ExportResult.Success;
    }

    List<Activity> Relevant()
    {
        HashSet<ActivityTraceId> traces = spans.Where(s => s.DisplayName == "customer_request").Select(s => s.TraceId).ToHashSet();
        return spans.Where(s => traces.Contains(s.TraceId)).ToList();
    }

    static double DurationMs(Activity span) => span.Duration.TotalMilliseconds;

    public List<Activity> Flagged() =>
        Relevant().Where(s => s.Status == ActivityStatusCode.Error || DurationMs(s) > latencyThresholdMs).ToList();

    public HashSet<string> TraceIds() => Relevant().Select(s => s.TraceId.ToString()).ToHashSet();

    public string Waterfall()
    {
        List<string> lines = [];
        List<Activity> relevant = Relevant();
        HashSet<string> ids = relevant.Select(s => s.SpanId.ToString()).ToHashSet();
        ILookup<string?, Activity> children = relevant.OrderBy(s => s.StartTimeUtc)
            .ToLookup(s => s.ParentSpanId != default && ids.Contains(s.ParentSpanId.ToString()) ? s.ParentSpanId.ToString() : null);

        void Walk(string? parentId, int level)
        {
            foreach (Activity span in children[parentId])
            {
                string status = span.Status == ActivityStatusCode.Error ? "ERROR" : "UNSET";
                lines.Add($"{span.TraceId.ToString()[..8]}  {new string(' ', level * 2)}{span.DisplayName}  {DurationMs(span):F0} ms  {status}");
                Walk(span.SpanId.ToString(), level + 1);
            }
        }

        Walk(null, 0);
        return string.Join("\n", lines);
    }
}

static class Telemetry
{
    public static readonly string[] Sources = ["agent-framework", "orchestrator", "downstream-agents", "System.Net.Http"];

    public static TracerProvider Configure(SpanCollector collector, string? connectionString)
    {
        TracerProviderBuilder builder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "tracing-demo"))
            .AddSource(Sources)
            .SetSampler(new AlwaysOnSampler())
            .AddProcessor(new SimpleActivityExportProcessor(collector));
        if (connectionString is not null) builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString);
        return builder.Build()!;
    }
}
