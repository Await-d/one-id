namespace OneID.Shared.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Persistence";

    public DatabaseProvider Provider { get; init; } = DatabaseProvider.Postgres;

    public string? ConnectionString { get; init; }

    public string ConnectionName { get; init; } = "Default";

    public bool EnableSensitiveLogging { get; init; }
}

public enum DatabaseProvider
{
    Postgres,
    SqlServer,
    MySql,
    Sqlite
}
