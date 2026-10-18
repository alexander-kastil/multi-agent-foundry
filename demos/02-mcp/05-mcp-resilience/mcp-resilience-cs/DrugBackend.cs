namespace McpResilience;

public class TransientBackendException(string message) : Exception(message);

public class DrugBackend
{
    private static readonly Dictionary<string, (string Severity, string Note)> Interactions = new()
    {
        ["aspirin+warfarin"] = ("major", "Additive bleeding risk"),
        ["atorvastatin+clarithromycin"] = ("major", "CYP3A4 inhibition raises statin exposure"),
        ["lisinopril+metformin"] = ("none", "No clinically relevant interaction")
    };

    public bool Outage { get; set; }

    public async Task<object> LookupAsync(string drugA, string drugB, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);
        if (Outage)
        {
            throw new TransientBackendException("drugs-west returned 503");
        }

        var key = string.Join("+", new[] { drugA.ToLowerInvariant(), drugB.ToLowerInvariant() }.Order());
        return Interactions.TryGetValue(key, out var found)
            ? new { status = "ok", drugA, drugB, severity = found.Severity, note = found.Note }
            : new { status = "ok", drugA, drugB, severity = "unknown", note = "No record in the formulary, consult a pharmacist" };
    }
}
