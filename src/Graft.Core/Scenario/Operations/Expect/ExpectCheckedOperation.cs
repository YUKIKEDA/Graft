using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>checked</c> state.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Checked">Expected checked state.</param>
public sealed record ExpectCheckedOperation(Selector Target, bool Checked) : ScenarioOperation(ScenarioActions.ExpectChecked);
