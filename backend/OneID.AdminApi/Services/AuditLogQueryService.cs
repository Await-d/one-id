using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OneID.Shared.Application.AuditLogs;
using OneID.Shared.Data;
using OneID.Shared.Domain;

namespace OneID.AdminApi.Services;

public sealed class AuditLogQueryService(AppDbContext dbContext) : IAuditLogQueryService
{
    public async Task<(IReadOnlyList<AuditLogSummary> Logs, int TotalCount)> QueryAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = ApplyFilters(dbContext.Set<AuditLog>(), query);

        var skip = Math.Max(0, query.Skip);
        var take = Math.Clamp(query.Take, 1, 100);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var logs = await queryable
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(a => new AuditLogSummary(
                a.Id,
                a.UserId,
                a.UserName,
                a.Action,
                a.Category,
                a.Details,
                a.IpAddress,
                a.Success,
                a.ErrorMessage,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return (logs, totalCount);
    }

    public async Task<IReadOnlyList<string>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<AuditLog>()
            .Select(a => a.Category)
            .Where(category => category != null)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogExportRow>> ExportAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = ApplyFilters(dbContext.Set<AuditLog>(), query);

        var items = await queryable
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(a => new AuditLogExportRow(
            a.CreatedAt,
            a.Category,
            a.Action,
            a.UserName,
            a.Success,
            a.IpAddress,
            a.Details,
            a.ErrorMessage,
            a.UserAgent)).ToList();
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> source, AuditLogQuery query)
    {
        var queryable = source;

        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(a => a.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(a => a.CreatedAt <= query.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(query.Category))
        {
            queryable = queryable.Where(a => a.Category == query.Category);
        }

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(a => a.UserId == query.UserId.Value);
        }

        if (query.Success.HasValue)
        {
            queryable = queryable.Where(a => a.Success == query.Success.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            queryable = queryable.Where(a =>
                (a.UserName != null && a.UserName.Contains(keyword)) ||
                a.Action.Contains(keyword) ||
                (a.Details != null && a.Details.Contains(keyword)) ||
                (a.ErrorMessage != null && a.ErrorMessage.Contains(keyword)));
        }

        return queryable;
    }
}
