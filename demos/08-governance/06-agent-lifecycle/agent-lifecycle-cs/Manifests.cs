using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class ManifestFile
{
    public Manifest AgentVersionManifest { get; set; } = new();
}

class Manifest
{
    public string AgentId { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "";
    public ModelSettings Model { get; set; } = new();
    public SystemPrompt SystemPrompt { get; set; } = new();
    public List<ToolRef> Tools { get; set; } = [];
    public Dictionary<string, double> EvaluationBaseline { get; set; } = [];
    public Approval Approval { get; set; } = new();
    public string EuAiActRiskClassification { get; set; } = "";
    public bool MaterialChangeAssessment { get; set; }
    public string TechnicalFileReference { get; set; } = "";
    public RetirementPlan? Retirement { get; set; }

    public byte[] PromptBytes() => File.ReadAllBytes(SystemPrompt.File);

    public string PromptText() => File.ReadAllText(SystemPrompt.File);

    public static Manifest Load(string path) => Yaml.Deserializer.Deserialize<ManifestFile>(File.ReadAllText(path)).AgentVersionManifest;
}

class ModelSettings
{
    public string DeploymentName { get; set; } = "";
    public double Temperature { get; set; }
}

class SystemPrompt
{
    public string Version { get; set; } = "";
    public string Hash { get; set; } = "";
    public string File { get; set; } = "";
}

class ToolRef
{
    public string Name { get; set; } = "";
}

class Approval
{
    public string Approver { get; set; } = "";
    public string ApprovalDate { get; set; } = "";
}

class RetirementPlan
{
    public string DeprecatedDate { get; set; } = "";
    public string DeletionDate { get; set; } = "";
    public string ReplacementAgent { get; set; } = "";
}

class TenantFile
{
    public List<Tenant> Tenants { get; set; } = [];
}

class Tenant
{
    public string Id { get; set; } = "";
    public int RolloutTier { get; set; }
    public string QuotaTier { get; set; } = "";
    public Pin? Pin { get; set; }
}

class Pin
{
    public string AgentVersion { get; set; } = "";
    public string Expires { get; set; } = "";
}

static class Yaml
{
    public static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static List<Tenant> LoadTenants(string path) => Deserializer.Deserialize<TenantFile>(File.ReadAllText(path)).Tenants;
}
