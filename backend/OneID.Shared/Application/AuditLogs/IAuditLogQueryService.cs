namespace OneID.Shared.Application.AuditLogs;

public interface IAuditLogQueryService
{
    Task<(IReadOnlyList<AuditLogSummary> Logs, int TotalCount)> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogExportRow>> ExportAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
