using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OneID.AdminApi.Validation;
using OneID.Shared.Application.Clients;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClientsController(
    IClientQueryService clientQueryService,
    IClientCommandService clientCommandService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientSummary>>> GetAsync(CancellationToken cancellationToken)
    {
        var clients = await clientQueryService.ListAsync(cancellationToken);
        return Ok(clients);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientSummary>> PostAsync([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.Equals(request.ClientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            ModelState.AddModelError(nameof(request.ClientSecret), "Confidential 客户端需提供密钥。");
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await clientCommandService.CreateAsync(request, cancellationToken);
            return Created($"/api/clients/{result.ClientId}", result);
        }
        catch (ClientValidationException ex)
        {
            var problem = BuildValidationProblem(ex);
            return BadRequest(problem);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Client already exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpDelete("{clientId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string clientId, CancellationToken cancellationToken)
    {
        try
        {
            await clientCommandService.DeleteAsync(clientId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Client not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPut("{clientId}")]
    [ProducesResponseType(typeof(ClientSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientSummary>> PutAsync(string clientId, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.Equals(request.ClientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            ModelState.AddModelError(nameof(request.ClientSecret), "Confidential 客户端需提供密钥。");
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await clientCommandService.UpdateAsync(clientId, request, cancellationToken);
            return Ok(updated);
        }
        catch (ClientValidationException ex)
        {
            var problem = BuildValidationProblem(ex);
            return BadRequest(problem);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Client not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPut("{clientId}/scopes")]
    [ProducesResponseType(typeof(ClientSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientSummary>> UpdateScopesAsync(string clientId, [FromBody] UpdateClientScopesRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await clientCommandService.UpdateScopesAsync(clientId, request, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Client not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static ValidationProblemDetails BuildValidationProblem(ClientValidationException exception)
    {
        var errors = exception.Errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "客户端配置校验失败",
            Status = StatusCodes.Status400BadRequest
        };
    }
}
