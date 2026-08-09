namespace Graft.McpServer.Tests;

/// <summary>
/// Serializes MCP tests that launch SampleWpfApp (named pipe / UI contention).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class McpUiCollection : ICollectionFixture<object>
{
    public const string Name = "McpUi";
}
