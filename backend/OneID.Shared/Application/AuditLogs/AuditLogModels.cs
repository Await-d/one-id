namespace OneID.Shared.Application.AuditLogs;

public sealed record AuditLogSummary(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string Category,
    string? Details,
    string? IpAddress,
    bool Success,
    string? ErrorMessage,
    DateTime CreatedAt);

public sealed record AuditLogQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Category = null,
    Guid? UserId = null,
    bool? Success = null,
    string? Keyword = null,
    int Skip = 0,
    int Take = 50);

public sealed record AuditLogExportRow(
    DateTime CreatedAt,
    string Category,
    string Action,
    string? UserName,
    bool Success,
    string? IpAddress,
    string? Details,
    string? ErrorMessage,
    string? UserAgent);
