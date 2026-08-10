namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: select a list/combo/tab item by index or name key.
/// </summary>
/// <param name="AutomationId">Host control automation id.</param>
/// <param name="Index">Zero-based index (xor <paramref name="Key"/>).</param>
/// <param name="Key">Item name key (xor <paramref name="Index"/>).</param>
public sealed record SelectOperation(string AutomationId, int? Index = null, string? Key = null)
    : ScenarioOperation(ScenarioActions.Select);
