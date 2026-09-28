using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Type literal text one Unicode scalar at a time, waiting between scalars.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Text">Literal text (no chord DSL).</param>
/// <param name="DelayMs">Milliseconds to wait between scalars. Zero is allowed.</param>
public sealed record TypeHumanOperation(Selector Target, string Text, int DelayMs) : ScenarioOperation(ScenarioActions.TypeHuman);
