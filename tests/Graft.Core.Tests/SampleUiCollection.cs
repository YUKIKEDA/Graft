namespace Graft.Core.Tests;

/// <summary>
/// Serializes Core tests that launch SampleWpfApp (named pipe / UI contention).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SampleUiCollection : ICollectionFixture<object>
{
    public const string Name = "SampleUi";
}
