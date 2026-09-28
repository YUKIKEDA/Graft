using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>name</c>.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Name">Expected name.</param>
public sealed record ExpectNameOperation(Selector Target, string Name) : ScenarioOperation(ScenarioActions.ExpectName);
