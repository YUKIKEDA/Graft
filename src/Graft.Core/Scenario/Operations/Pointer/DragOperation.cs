using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: drag from one element to another (SendInput).
/// </summary>
/// <param name="Target">Source automation id.</param>
/// <param name="To">Target automation id.</param>
public sealed record DragOperation(Selector Target, Selector To) : ScenarioOperation(ScenarioActions.Drag);
