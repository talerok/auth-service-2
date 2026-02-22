using Xunit;

namespace Auth.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration tests";
}