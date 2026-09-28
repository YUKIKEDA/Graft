using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Type literal text into an element by automation id.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Text">Literal text (no chord DSL).</param>
public sealed record SendKeysOperation(Selector Target, string Text) : ScenarioOperation(ScenarioActions.SendKeys);
