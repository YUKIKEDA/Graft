namespace Graft.Core.Scenario;

/// <summary>
/// Launch the target application.
/// </summary>
/// <param name="AppPath">Executable or csproj path.</param>
/// <param name="Configuration">MSBuild configuration when <paramref name="AppPath"/> is a csproj.</param>
/// <param name="Timeout">Optional launch + handshake budget.</param>
public sealed record LaunchOperation(
    string AppPath,
    string? Configuration = null,
    TimeSpan? Timeout = null
) : ScenarioOperation(ScenarioActions.Launch);
