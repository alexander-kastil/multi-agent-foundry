using System.Diagnostics;

record Charge(string Status, string Gateway, double Amount = 0);

class Gateway(string name, bool healthy = true)
{
    public string Name { get; } = name;
    public bool Healthy { get; set; } = healthy;

    public bool Health() => Healthy;

    public bool TestTransaction() => Healthy;

    public Charge Charge(double amount) => Healthy ? new Charge("paid", Name, amount) : new Charge("error", Name);
}

class CircuitBreaker(int window = 50, double failureThreshold = 0.4, double probeIntervalSeconds = 30)
{
    readonly Queue<bool> results = new();
    long openedAt;

    public string State { get; private set; } = "closed";

    public Charge Call(Func<Charge> primary, Func<Charge> fallback)
    {
        if (State == "open" && Stopwatch.GetElapsedTime(openedAt).TotalSeconds >= probeIntervalSeconds) State = "half_open";
        if (State == "open") return fallback();
        Charge result = primary();
        bool success = result.Status != "error";
        if (State == "half_open")
        {
            State = success ? "closed" : "open";
            openedAt = Stopwatch.GetTimestamp();
            results.Clear();
            return success ? result : fallback();
        }
        results.Enqueue(success);
        if (results.Count > window) results.Dequeue();
        int failures = results.Count(item => !item);
        if (results.Count >= 10 && (double)failures / results.Count > failureThreshold)
        {
            State = "open";
            openedAt = Stopwatch.GetTimestamp();
        }
        return success ? result : fallback();
    }
}

class GatewayRunbook(Gateway primary, Gateway backup, Dictionary<string, string> config, int maxRunsPerHour = 3, double deadlineSeconds = 300)
{
    readonly List<long> runTimes = [];

    public string Run()
    {
        long started = Stopwatch.GetTimestamp();
        if (runTimes.Count(moment => Stopwatch.GetElapsedTime(moment).TotalSeconds < 3600) >= maxRunsPerHour)
            return "{status: escalation_required, reason: rate_limited}";
        runTimes.Add(started);
        if (config["payment_gateway"] == backup.Name)
            return "{status: no_action_needed, reason: already_on_backup}";
        if (Enumerable.Range(0, 3).Count(_ => !primary.Health()) < 3)
            return "{status: no_action_needed, reason: primary_healthy}";
        if (!backup.Health())
            return "{status: escalation_required, reason: both_gateways_unhealthy}";
        config["payment_gateway"] = backup.Name;
        if (Stopwatch.GetElapsedTime(started).TotalSeconds > deadlineSeconds)
            return "{status: escalation_required, reason: deadline_exceeded}";
        return backup.TestTransaction()
            ? "{status: failover_successful, action: notify_on_call}"
            : "{status: failover_verification_failed, action: page_on_call_urgent}";
    }
}

static class Severity
{
    public static string Classify(double checkoutErrorRate, double agentsIncorrectRate = 0, double affectedTraffic = 1)
    {
        if (checkoutErrorRate > 0.05) return "P1";
        if (checkoutErrorRate > 0.01 || agentsIncorrectRate > 0.10) return "P2";
        if (affectedTraffic < 0.05 || agentsIncorrectRate > 0) return "P3";
        return "P4";
    }
}
