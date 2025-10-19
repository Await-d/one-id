using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Domain;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 邮件模板控制器
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<EmailTemplatesController> _logger;

    public EmailTemplatesController(
        IEmailTemplateService templateService,
        ILogger<EmailTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmailTemplate>>> GetAllTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        return Ok(templates);
    }

    [HttpGet("language/{language}")]
    public async Task<ActionResult<IReadOnlyList<EmailTemplate>>> GetTemplatesByLanguage(string language)
    {
        var templates = await _templateService.GetTemplatesByLanguageAsync(language);
        return Ok(templates);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmailTemplate>> GetTemplate(Guid id)
    {
        var template = await _templateService.GetTemplateAsync(id);
        if (template == null)
        {
            return NotFound();
        }
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<EmailTemplate>> CreateTemplate([FromBody] EmailTemplate template)
    {
        try
        {
            template.LastModifiedBy = User.Identity?.Name;
            var created = await _templateService.CreateTemplateAsync(template);
            return CreatedAtAction(nameof(GetTemplate), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmailTemplate>> UpdateTemplate(Guid id, [FromBody] EmailTemplate template)
    {
        if (id != template.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        try
        {
            template.LastModifiedBy = User.Identity?.Name;
            var updated = await _templateService.UpdateTemplateAsync(template);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            await _templateService.DeleteTemplateAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<EmailTemplate>> DuplicateTemplate(Guid id, [FromBody] DuplicateTemplateRequest request)
    {
        try
        {
            var duplicated = await _templateService.DuplicateTemplateAsync(id, request.NewLanguage);
            return Ok(duplicated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("extract-variables")]
    public ActionResult<List<string>> ExtractVariables([FromBody] ExtractVariablesRequest request)
    {
        var variables = _templateService.ExtractVariables(request.Template);
        return Ok(variables);
    }

    [HttpPost("ensure-defaults")]
    public async Task<ActionResult> EnsureDefaultTemplates()
    {
        await _templateService.EnsureDefaultTemplatesAsync();
        return Ok(new { message = "Default templates ensured" });
    }
}

public record DuplicateTemplateRequest(string NewLanguage);
public record ExtractVariablesRequest(string Template);

