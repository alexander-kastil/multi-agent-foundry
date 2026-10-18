namespace sk_ai_agent;

public class GraphCfg
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string CacheLocation { get; set; }
    public Endpoints Endpoints { get; set; }
    public string ReturnUrl { get; set; }
    public string MailSender { get; set; }

    public GraphCfg()
    {
        TenantId = string.Empty;
        ClientId = string.Empty;
        ClientSecret = string.Empty;
        CacheLocation = string.Empty;
        Endpoints = new Endpoints();
        ReturnUrl = string.Empty;
        MailSender = string.Empty;
    }
}

public class Endpoints
{
    public string GraphApiUri { get; set; }

    public Endpoints()
    {
        GraphApiUri = string.Empty;
    }
}

