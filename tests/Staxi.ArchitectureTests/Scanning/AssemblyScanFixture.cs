using Xunit;

namespace Staxi.ArchitectureTests.Scanning;

[CollectionDefinition(nameof(AssemblyScanFixture), DisableParallelization = true)]
public sealed class AssemblyScanFixture : ICollectionFixture<AssemblyCatalog>;
