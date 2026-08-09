namespace Graft.Core.Scenario;

/// <summary>
/// One compiled Scenario operation (internal model shared with Fluent / future MCP).
/// </summary>
/// <param name="Action">Action id (see <see cref="ScenarioActions"/>).</param>
public abstract record ScenarioOperation(string Action);
