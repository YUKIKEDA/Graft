namespace Graft.Core.Scenario;

/// <summary>
/// Invokes an element that may open a window (modal-safe BeginInvoke path).
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="WaitForNewWindow">
/// When true (default), waits for a new WPF window and switches to it.
/// When false, only queues BeginInvoke (OpenFile seam).
/// </param>
public sealed record InvokeOpeningWindowOperation(string AutomationId, bool WaitForNewWindow = true)
    : ScenarioOperation(ScenarioActions.InvokeOpeningWindow);
