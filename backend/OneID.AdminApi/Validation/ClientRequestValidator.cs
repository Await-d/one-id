using System.Collections.Generic;
using OneID.AdminApi.Configuration;
using OneID.Shared.Application.Clients;

namespace OneID.AdminApi.Validation;

internal static class ClientRequestValidator
{
    public static void Validate(CreateClientRequest request, ClientValidationOptions options)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        ValidateRedirect("RedirectUri", request.RedirectUri, options, errors);

        if (!string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri))
        {
            ValidateRedirect("PostLogoutRedirectUri", request.PostLogoutRedirectUri!, options, errors);
        }

        if (errors.Count > 0)
        {
            throw ClientValidationException.From(errors);
        }
    }

    public static void Validate(UpdateClientRequest request, ClientValidationOptions options)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        ValidateRedirect("RedirectUri", request.RedirectUri, options, errors);

        if (!string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri))
        {
            ValidateRedirect("PostLogoutRedirectUri", request.PostLogoutRedirectUri!, options, errors);
        }

        if (errors.Count > 0)
        {
            throw ClientValidationException.From(errors);
        }
    }

    private static void ValidateRedirect(
        string field,
        string value,
        ClientValidationOptions options,
        IDictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(field, "Redirect URI 不能为空。", errors);
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            AddError(field, "Redirect URI 必须是合法的绝对地址。", errors);
            return;
        }

        if (!options.AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            AddError(field, $"不支持的 Scheme: {uri.Scheme}。仅允许: {string.Join(',', options.AllowedSchemes)}。", errors);
        }

        if (string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
        {
            if (!options.AllowHttpOnLoopback || !IsLoopback(uri))
            {
                AddError(field, "仅允许 localhost/127.0.0.1 使用 http Scheme。", errors);
            }
        }

        if (options.AllowedHosts.Length > 0 && !options.AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            AddError(field, $"主机 {uri.Host} 未包含在允许列表中。", errors);
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            AddError(field, "Redirect URI 不允许包含 Fragment (#)。", errors);
        }
    }

    private static bool IsLoopback(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return true;
        }

        // 特殊判断 Docker/局域网常见形式
        return string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddError(string field, string message, IDictionary<string, List<string>> errors)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            errors[field] = list;
        }

        if (!list.Contains(message, StringComparer.OrdinalIgnoreCase))
        {
            list.Add(message);
        }
    }
}
