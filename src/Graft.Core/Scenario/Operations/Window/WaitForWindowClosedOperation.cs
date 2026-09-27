namespace Graft.Core.Scenario;

/// <summary>
/// Wait until a window matching title and/or automation id is closed.
/// </summary>
/// <param name="Title">Optional exact window title.</param>
/// <param name="AutomationId">Optional exact window automation id.</param>
public sealed record WaitForWindowClosedOperation(string? Title, string? AutomationId) : ScenarioOperation(ScenarioActions.WaitForWindowClosed);
