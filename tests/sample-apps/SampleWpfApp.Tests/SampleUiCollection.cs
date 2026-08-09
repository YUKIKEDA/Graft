namespace SampleWpfApp.Tests;

/// <summary>
/// Serializes Sample E2E tests that launch SampleWpfApp (named pipe / UI contention).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SampleUiCollection : ICollectionFixture<object>
{
    public const string Name = "SampleUi";
}
