using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Infrastructure;

namespace OneID.AdminApi.Controllers;

/// <summary>
/// 用户批量导入控制器
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UserImportController : ControllerBase
{
    private readonly IUserImportService _importService;
    private readonly ILogger<UserImportController> _logger;

    public UserImportController(
        IUserImportService importService,
        ILogger<UserImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// 批量导入用户（CSV 文件）
    /// </summary>
    /// <param name="file">CSV 文件</param>
    /// <param name="defaultRole">默认角色（如果 CSV 中未指定）</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 限制 10MB
    public async Task<ActionResult<UserImportResult>> UploadCsv(
        IFormFile file,
        [FromForm] string? defaultRole,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Please upload a valid CSV file" });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only CSV files are supported" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importService.ImportUsersFromCsvAsync(
                stream,
                defaultRole,
                User.Identity?.Name,
                cancellationToken);

            _logger.LogInformation(
                "User import completed by {User}: {Success}/{Total} successful",
                User.Identity?.Name, result.SuccessCount, result.TotalRows);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user import by {User}", User.Identity?.Name);
            return StatusCode(500, new 
            { 
                message = "An error occurred during import",
                error = ex.Message 
            });
        }
    }

    /// <summary>
    /// 下载 CSV 示例文件
    /// </summary>
    [HttpGet("sample")]
    [AllowAnonymous]
    public IActionResult DownloadSample()
    {
        var sampleCsv = _importService.GenerateSampleCsv();
        var bytes = System.Text.Encoding.UTF8.GetBytes(sampleCsv);
        
        return File(
            bytes,
            "text/csv",
            "user-import-sample.csv");
    }

    /// <summary>
    /// 获取导入说明
    /// </summary>
    [HttpGet("instructions")]
    [AllowAnonymous]
    public ActionResult<ImportInstructions> GetInstructions()
    {
        return Ok(new ImportInstructions
        {
            RequiredColumns = new[] { "UserName", "Email", "Password" },
            OptionalColumns = new[] { "DisplayName", "Role" },
            DefaultRole = "User",
            PasswordRequirements = "Minimum 8 characters, at least one digit, one lowercase letter",
            MaxFileSize = "10 MB",
            SupportedFormats = new[] { "CSV" },
            Notes = new[]
            {
                "UserName and Email must be unique",
                "Password must meet security requirements",
                "Imported users will have EmailConfirmed set to true",
                "If Role is not specified, default role will be used",
                "The first row must be a header row"
            }
        });
    }
}

/// <summary>
/// 导入说明
/// </summary>
public class ImportInstructions
{
    public string[] RequiredColumns { get; set; } = Array.Empty<string>();
    public string[] OptionalColumns { get; set; } = Array.Empty<string>();
    public string DefaultRole { get; set; } = "User";
    public string PasswordRequirements { get; set; } = string.Empty;
    public string MaxFileSize { get; set; } = string.Empty;
    public string[] SupportedFormats { get; set; } = Array.Empty<string>();
    public string[] Notes { get; set; } = Array.Empty<string>();
}

