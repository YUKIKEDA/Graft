namespace Graft.Core.Scenario;

/// <summary>
/// Compiled Scenario: versioned document ready for the Batch 4 runner.
/// </summary>
public sealed class ScenarioDocument
{
    /// <summary>
    /// Current supported document version (<c>v</c> in JSON).
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Gets the document version.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets an optional human-readable name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the compiled operation list (execution order).
    /// </summary>
    public required IReadOnlyList<ScenarioOperation> Operations { get; init; }
}
