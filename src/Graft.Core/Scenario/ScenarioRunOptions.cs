namespace Graft.Core.Scenario;

/// <summary>
/// Runtime options for <see cref="ScenarioRunner"/>.
/// </summary>
public sealed class ScenarioRunOptions
{
    /// <summary>
    /// Gets an absolute app path that overrides the Scenario <c>launch.appPath</c>.
    /// </summary>
    /// <remarks>
    /// Useful in tests that resolve the target project at runtime.
    /// </remarks>
    public string? AppPath { get; init; }

    /// <summary>
    /// Gets a directory used to resolve a relative Scenario <c>launch.appPath</c>
    /// when <see cref="AppPath"/> is not set.
    /// </summary>
    public string? WorkingDirectory { get; init; }
}
