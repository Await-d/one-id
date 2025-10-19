using Xunit;

namespace OneID.AdminApi.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<AdminApiFactory>
{
    public const string Name = "AdminApi integration";
}
