namespace Graft.Core.Scenario;

/// <summary>
/// Scroll an element or list item into view.
/// </summary>
/// <param name="AutomationId">Target element or list automation id.</param>
/// <param name="Index">Optional list item index.</param>
public sealed record ScrollIntoViewOperation(string AutomationId, int? Index) : ScenarioOperation(ScenarioActions.ScrollIntoView);
