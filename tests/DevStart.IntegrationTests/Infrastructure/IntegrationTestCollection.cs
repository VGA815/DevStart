namespace DevStart.IntegrationTests.Infrastructure
{
    /// <summary>
    /// All integration tests share a single <see cref="IntegrationTestWebAppFactory"/> (one container, one
    /// host) and therefore run sequentially. Each test resets the database first, so they don't interfere.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>
    {
        public const string Name = "Integration";
    }
}
