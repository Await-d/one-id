namespace OneID.Shared.Infrastructure;

/// <summary>
/// 租户上下文 - 存储当前请求的租户信息
/// </summary>
public class TenantContext
{
    private static readonly AsyncLocal<Guid?> _currentTenantId = new();

    public static Guid? CurrentTenantId
    {
        get => _currentTenantId.Value;
        set => _currentTenantId.Value = value;
    }

    public static bool HasTenant => _currentTenantId.Value.HasValue;
}

public interface ITenantContextAccessor
{
    Guid? GetCurrentTenantId();
    void SetCurrentTenantId(Guid? tenantId);
}

public class TenantContextAccessor : ITenantContextAccessor
{
    public Guid? GetCurrentTenantId()
    {
        return TenantContext.CurrentTenantId;
    }

    public void SetCurrentTenantId(Guid? tenantId)
    {
        TenantContext.CurrentTenantId = tenantId;
    }
}

