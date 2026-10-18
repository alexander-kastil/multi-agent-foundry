using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

record Order(string OrderId, double Total, int DiscountPercent);

record Step(string Name, long DurationMs, string Status, string? Output = null);

record Snapshot(
    string TraceId,
    string OrderId,
    string DeploymentName,
    string PromptVersion,
    string PromptContent,
    string PromptHash,
    Order MockOrder,
    List<Step> TraceTimeline,
    string ProductionOutput);

record Payment(string Status, double Amount = 0, string? Reason = null);

class TraceRecorder
{
    public string TraceId { get; } = Guid.NewGuid().ToString("N");
    public List<Step> Steps { get; } = [];
    public Order? ToolResponse { get; set; }

    public void Record(string name, long started, string status, string? output = null) =>
        Steps.Add(new Step(name, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds, status, output));
}

static partial class Pipeline
{
    public static readonly Dictionary<string, Order> Orders = new()
    {
        ["A100"] = new("A100", 120.0, 10),
        ["A101"] = new("A101", 80.0, 25),
        ["A102"] = new("A102", 45.5, 0),
        ["A103"] = new("A103", 310.0, 15),
    };

    [GeneratedRegex(@"^[0-9]+(\.[0-9]{1,2})?$")]
    public static partial Regex PricePattern();

    public static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static string RequestText(Order order) => JsonSerializer.Serialize(new { total = order.Total, discount_percent = order.DiscountPercent });

    public static Payment ProcessPayment(string priceText) =>
        PricePattern().IsMatch(priceText.Trim())
            ? new Payment("paid", double.Parse(priceText.Trim(), System.Globalization.CultureInfo.InvariantCulture))
            : new Payment("rejected", Reason: "non_numeric_amount");

    public static async Task<(string PriceText, Payment Payment)> RunCheckout(string orderId, string version, TraceRecorder recorder)
    {
        long started = Stopwatch.GetTimestamp();
        Order order = Orders[orderId];
        recorder.ToolResponse = order;
        recorder.Record("tool_order_lookup", started, "ok");

        started = Stopwatch.GetTimestamp();
        string priceText = await Foundry.AskPricingAgent(version, RequestText(order));
        recorder.Record("agent_pricing", started, "ok", priceText);

        started = Stopwatch.GetTimestamp();
        Payment payment = ProcessPayment(priceText);
        recorder.Record("agent_payment", started, payment.Status == "paid" ? "ok" : "error");
        return (priceText, payment);
    }

    public static Snapshot BuildSnapshot(TraceRecorder recorder, string orderId, string versionLabel)
    {
        string prompt = Foundry.PricingPrompts[versionLabel];
        return new Snapshot(
            recorder.TraceId,
            orderId,
            Foundry.Model,
            versionLabel,
            prompt,
            Sha256(prompt),
            recorder.ToolResponse! with { OrderId = "MOCK-" + recorder.ToolResponse.OrderId },
            recorder.Steps,
            recorder.Steps.First(step => step.Name == "agent_pricing").Output!);
    }
}
