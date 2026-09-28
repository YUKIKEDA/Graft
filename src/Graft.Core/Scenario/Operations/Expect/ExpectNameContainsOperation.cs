using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree name contains a substring.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Substring">Expected ordinal substring.</param>
public sealed record ExpectNameContainsOperation(Selector Target, string Substring) : ScenarioOperation(ScenarioActions.ExpectNameContains);
