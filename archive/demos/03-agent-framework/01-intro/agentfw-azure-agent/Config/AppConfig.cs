namespace sk_ai_agent;

public class AppConfig
{
    public LoggingConfig? Logging { get; set; }
    public string Model { get; set; }
    public string ProjectConnectionString { get; set; }
    public string AgentName { get; set; }
    public ModelParamsConfig? ModelParams { get; set; }
    public GraphCfg? GraphCfg { get; set; }
    public string? AllowedHosts { get; set; }

    public AppConfig()
    {
        Logging = new LoggingConfig();
        Model = string.Empty;
        ProjectConnectionString = string.Empty;
        AgentName = string.Empty;
        ModelParams = new ModelParamsConfig();
        GraphCfg = new GraphCfg();
        AllowedHosts = string.Empty;
    }
}

public class LoggingConfig
{
    public LogLevelConfig? LogLevel { get; set; }
    public LoggingConfig()
    {
        LogLevel = new LogLevelConfig();
    }
}

public class LogLevelConfig
{
    public string? Default { get; set; }
    public string? Microsoft { get; set; }
    public string? MicrosoftHostingLifetime { get; set; }
    public LogLevelConfig()
    {
        Default = string.Empty;
        Microsoft = string.Empty;
        MicrosoftHostingLifetime = string.Empty;
    }
}

public class ModelParamsConfig
{
    public int MaxCompletionTokens { get; set; }
    public int MaxPromptTokens { get; set; }
    public double Temperature { get; set; }
    public double TopP { get; set; }
    public ModelParamsConfig()
    {
        MaxCompletionTokens = 0;
        MaxPromptTokens = 0;
        Temperature = 0.0;
        TopP = 0.0;
    }
}
