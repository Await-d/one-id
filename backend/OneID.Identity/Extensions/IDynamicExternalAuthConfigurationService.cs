namespace OneID.Identity.Extensions;

public interface IDynamicExternalAuthConfigurationService
{
    Task ReloadConfigurationAsync();
}

public class DynamicExternalAuthConfigurationService : IDynamicExternalAuthConfigurationService
{
    public Task ReloadConfigurationAsync()
    {
        // 预留接口，用于热重载配置
        return Task.CompletedTask;
    }
}
