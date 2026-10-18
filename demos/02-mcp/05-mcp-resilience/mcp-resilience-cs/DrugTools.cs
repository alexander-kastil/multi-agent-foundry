using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpResilience;

[McpServerToolType]
public class DrugTools(DrugBackend backend, ResilientLookup lookup)
{
    [McpServerTool(Name = "lookup_drug_interaction", ReadOnly = true)]
    [Description("Checks for interactions between two medications. Reports status unavailable with a fallback when the backend is down.")]
    public Task<object> LookupDrugInteraction(
        [Description("First medication")] string drugA,
        [Description("Second medication")] string drugB,
        CancellationToken cancellationToken) => lookup.LookupAsync(backend, drugA, drugB, cancellationToken);
}
