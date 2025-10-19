using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Application.ExternalAuth;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ExternalAuthProvidersController(
    IExternalAuthProviderQueryService queryService,
    IExternalAuthProviderCommandService commandService,
    ILogger<ExternalAuthProvidersController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有外部认证提供者
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExternalAuthProviderSummary>>> GetAll(CancellationToken cancellationToken)
    {
        var providers = await queryService.ListAsync(cancellationToken);
        return Ok(providers);
    }

    /// <summary>
    /// 获取已启用的外部认证提供者
    /// </summary>
    [HttpGet("enabled")]
    [AllowAnonymous] // 允许匿名访问，供登录页使用
    public async Task<ActionResult<IReadOnlyList<ExternalAuthProviderSummary>>> GetEnabled(CancellationToken cancellationToken)
    {
        var providers = await queryService.GetEnabledAsync(cancellationToken);
        return Ok(providers);
    }

    /// <summary>
    /// 根据ID获取外部认证提供者
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExternalAuthProviderSummary>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var provider = await queryService.GetByIdAsync(id, cancellationToken);
        if (provider == null)
        {
            return NotFound(new { error = "Provider not found" });
        }

        return Ok(provider);
    }

    /// <summary>
    /// 根据名称获取外部认证提供者
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<ExternalAuthProviderSummary>> GetByName(string name, CancellationToken cancellationToken)
    {
        var provider = await queryService.GetByNameAsync(name, cancellationToken);
        if (provider == null)
        {
            return NotFound(new { error = "Provider not found" });
        }

        return Ok(provider);
    }

    /// <summary>
    /// 创建新的外部认证提供者
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExternalAuthProviderSummary>> Create(
        [FromBody] CreateExternalAuthProviderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await commandService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = provider.Id }, provider);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to create external auth provider");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新外部认证提供者
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExternalAuthProviderSummary>> Update(
        Guid id,
        [FromBody] UpdateExternalAuthProviderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await commandService.UpdateAsync(id, request, cancellationToken);
            return Ok(provider);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to update external auth provider {Id}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除外部认证提供者
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await commandService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to delete external auth provider {Id}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 启用/禁用外部认证提供者
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    public async Task<ActionResult<ExternalAuthProviderSummary>> ToggleEnabled(
        Guid id,
        [FromBody] ToggleEnabledRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await commandService.ToggleEnabledAsync(id, request.Enabled, cancellationToken);
            return Ok(provider);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to toggle external auth provider {Id}", id);
            return NotFound(new { error = ex.Message });
        }
    }
}

public record ToggleEnabledRequest(bool Enabled);
