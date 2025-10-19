using System;

namespace OneID.AdminApi.Configuration;

public sealed class ClientValidationOptions
{
    /// <summary>
    /// 允许的 URI Scheme，默认包含 https 与 http（限本地）。
    /// </summary>
    public string[] AllowedSchemes { get; set; } = new[] { "https", "http" };

    /// <summary>
    /// 是否允许 http scheme 用于 localhost/127.0.0.1/::1。
    /// </summary>
    public bool AllowHttpOnLoopback { get; set; } = true;

    /// <summary>
    /// 限定可用主机名清单，为空表示不限制。
    /// </summary>
    public string[] AllowedHosts { get; set; } = Array.Empty<string>();
}
