using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneID.Shared.Application.AuditLogs;

namespace OneID.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AuditLogsController(
    IAuditLogQueryService auditLogQueryService,
    ILogger<AuditLogsController> logger) : ControllerBase
{
    /// <summary>
    /// 查询审计日志
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? category,
        [FromQuery] Guid? userId,
        [FromQuery] bool? success,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var (query, normalizedPage, normalizedPageSize) = BuildQuery(startDate, endDate, category, userId, success, keyword, page, pageSize);
        var (logs, totalCount) = await auditLogQueryService.QueryAsync(query, cancellationToken);

        return Ok(new
        {
            logs,
            total = totalCount,
            page = normalizedPage,
            pageSize = normalizedPageSize
        });
    }

    /// <summary>
    /// 导出审计日志（CSV）
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? category,
        [FromQuery] Guid? userId,
        [FromQuery] bool? success,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken = default)
    {
        var (query, _, _) = BuildQuery(startDate, endDate, category, userId, success, keyword, page: 1, pageSize: 100);

        // 对于导出不限制数量，忽略 Skip/Take
        query = query with { Skip = 0, Take = int.MaxValue };

        var rows = await auditLogQueryService.ExportAsync(query, cancellationToken);
        var csv = BuildCsv(rows);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// 获取审计日志分类列表
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await auditLogQueryService.ListCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    private static (AuditLogQuery Query, int Page, int PageSize) BuildQuery(
        DateTime? startDate,
        DateTime? endDate,
        string? category,
        Guid? userId,
        bool? success,
        string? keyword,
        int page,
        int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;
        var take = normalizedPageSize;

        var query = new AuditLogQuery(startDate, endDate, category, userId, success, keyword, skip, take);
        return (query, normalizedPage, normalizedPageSize);
    }

    private static string BuildCsv(IReadOnlyList<AuditLogExportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("CreatedAt,Category,Action,UserName,Success,IpAddress,Details,ErrorMessage,UserAgent");

        foreach (var row in rows)
        {
            var fields = new[]
            {
                row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                row.Category,
                row.Action,
                row.UserName ?? string.Empty,
                row.Success ? "Success" : "Failure",
                row.IpAddress ?? string.Empty,
                row.Details ?? string.Empty,
                row.ErrorMessage ?? string.Empty,
                row.UserAgent ?? string.Empty
            };

            builder.AppendLine(string.Join(',', fields.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}
