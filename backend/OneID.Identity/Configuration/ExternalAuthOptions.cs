namespace OneID.Identity.Configuration;

public class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";
    
    public GitHubOptions GitHub { get; set; } = new();
    public GoogleOptions Google { get; set; } = new();
    public GiteeOptions Gitee { get; set; } = new();
    public WeChatOptions WeChat { get; set; } = new();
}

public class GitHubOptions
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class GoogleOptions
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class GiteeOptions
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class WeChatOptions
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? AgentId { get; set; }
}
