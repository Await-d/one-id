using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ScopesController(
    IOpenIddictScopeManager scopeManager) : ControllerBase
{
    /// <summary>
    /// 获取所有Scope
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var scopes = new List<object>();
        
        await foreach (var scope in scopeManager.ListAsync(cancellationToken: cancellationToken))
        {
            var name = await scopeManager.GetNameAsync(scope, cancellationToken);
            var displayName = await scopeManager.GetDisplayNameAsync(scope, cancellationToken);
            var description = await scopeManager.GetDescriptionAsync(scope, cancellationToken);
            var resources = await scopeManager.GetResourcesAsync(scope, cancellationToken);
            
            scopes.Add(new
            {
                Name = name,
                DisplayName = displayName,
                Description = description,
                Resources = resources.ToList()
            });
        }
        
        return Ok(scopes);
    }
    
    /// <summary>
    /// 根据名称获取Scope
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetByName(string name, CancellationToken cancellationToken)
    {
        var scope = await scopeManager.FindByNameAsync(name, cancellationToken);
        if (scope == null)
        {
            return NotFound(new { Message = $"Scope '{name}' not found" });
        }
        
        var displayName = await scopeManager.GetDisplayNameAsync(scope, cancellationToken);
        var description = await scopeManager.GetDescriptionAsync(scope, cancellationToken);
        var resources = await scopeManager.GetResourcesAsync(scope, cancellationToken);
        
        return Ok(new
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Resources = resources.ToList()
        });
    }
    
    /// <summary>
    /// 创建新Scope
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScopeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { Message = "Scope name is required" });
        }
        
        var existing = await scopeManager.FindByNameAsync(request.Name, cancellationToken);
        if (existing != null)
        {
            return BadRequest(new { Message = $"Scope '{request.Name}' already exists" });
        }
        
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = request.Name,
            DisplayName = request.DisplayName ?? request.Name,
            Description = request.Description
        };
        
        if (request.Resources != null)
        {
            foreach (var resource in request.Resources)
            {
                descriptor.Resources.Add(resource);
            }
        }
        
        await scopeManager.CreateAsync(descriptor, cancellationToken);
        
        return CreatedAtAction(nameof(GetByName), new { name = request.Name }, new
        {
            Name = request.Name,
            DisplayName = descriptor.DisplayName,
            Description = descriptor.Description,
            Resources = descriptor.Resources.ToList()
        });
    }
    
    /// <summary>
    /// 更新Scope
    /// </summary>
    [HttpPut("{name}")]
    public async Task<IActionResult> Update(string name, [FromBody] UpdateScopeRequest request, CancellationToken cancellationToken)
    {
        var scope = await scopeManager.FindByNameAsync(name, cancellationToken);
        if (scope == null)
        {
            return NotFound(new { Message = $"Scope '{name}' not found" });
        }
        
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name, // Name cannot be changed
            DisplayName = request.DisplayName ?? name,
            Description = request.Description
        };
        
        if (request.Resources != null)
        {
            foreach (var resource in request.Resources)
            {
                descriptor.Resources.Add(resource);
            }
        }
        
        await scopeManager.UpdateAsync(scope, descriptor, cancellationToken);
        
        return Ok(new
        {
            Name = name,
            DisplayName = descriptor.DisplayName,
            Description = descriptor.Description,
            Resources = descriptor.Resources.ToList()
        });
    }
    
    /// <summary>
    /// 删除Scope
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken)
    {
        var scope = await scopeManager.FindByNameAsync(name, cancellationToken);
        if (scope == null)
        {
            return NotFound(new { Message = $"Scope '{name}' not found" });
        }
        
        // 检查是否为内置scope，不允许删除
        var builtInScopes = new[] { "openid", "profile", "email", "offline_access", "admin_api" };
        if (builtInScopes.Contains(name))
        {
            return BadRequest(new { Message = $"Cannot delete built-in scope '{name}'" });
        }
        
        await scopeManager.DeleteAsync(scope, cancellationToken);
        
        return NoContent();
    }
}

public record CreateScopeRequest(
    string Name,
    string? DisplayName,
    string? Description,
    List<string>? Resources
);

public record UpdateScopeRequest(
    string? DisplayName,
    string? Description,
    List<string>? Resources
);

