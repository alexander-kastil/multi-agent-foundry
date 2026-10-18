using Microsoft.Agents.AI;

record SpokeOutcome(string Spoke, string Status, string? Result);

class CircuitState
{
    public int Failures;
    public DateTime? OpenedAt;
}

static class Resilience
{
    const int FailureThreshold = 3;
    static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan SpokeTimeout = TimeSpan.FromSeconds(90);
    const int MaxConcurrentSpokes = 2;

    static readonly Dictionary<string, AIAgent> SpokeAgents = Spokes.MakeSpokes();
    static readonly SemaphoreSlim Semaphore = new(MaxConcurrentSpokes);
    public static readonly Dictionary<string, CircuitState> Circuits = [];

    static bool CircuitOpen(string spokeName) =>
        Circuits.TryGetValue(spokeName, out CircuitState? state) && state.OpenedAt is { } opened && DateTime.UtcNow - opened < Cooldown;

    static void RecordOutcome(string spokeName, bool success)
    {
        lock (Circuits)
        {
            CircuitState state = Circuits.TryGetValue(spokeName, out CircuitState? existing) ? existing : Circuits[spokeName] = new CircuitState();
            if (success)
            {
                state.Failures = 0;
                state.OpenedAt = null;
                return;
            }
            state.Failures++;
            if (state.Failures >= FailureThreshold)
                state.OpenedAt = DateTime.UtcNow;
        }
    }

    public static async Task<SpokeOutcome> InvokeSpoke(string spokeName, string request, TimeSpan? timeout = null)
    {
        if (CircuitOpen(spokeName))
            return new SpokeOutcome(spokeName, "circuit_open", null);
        await Semaphore.WaitAsync();
        try
        {
            using CancellationTokenSource cts = new(timeout ?? SpokeTimeout);
            AgentResponse result = await SpokeAgents[spokeName].RunAsync(request, cancellationToken: cts.Token);
            RecordOutcome(spokeName, true);
            return new SpokeOutcome(spokeName, "success", result.Text);
        }
        catch (OperationCanceledException)
        {
            RecordOutcome(spokeName, false);
            return new SpokeOutcome(spokeName, "timeout", null);
        }
        catch (Exception ex)
        {
            RecordOutcome(spokeName, false);
            return new SpokeOutcome(spokeName, "error", ex.Message);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
