using Graft.Protocol;

namespace Graft.Core;

/// <summary>
/// Options for <see cref="Application.LaunchAsync"/>.
/// </summary>
public sealed class LaunchOptions
{
    /// <summary>
    /// MSBuild configuration used when <see cref="Configuration"/> is omitted.
    /// </summary>
    public const string DefaultConfiguration = GraftEnvironmentNames.TestConfiguration;

    /// <summary>
    /// Gets the default launch + handshake timeout (30 seconds).
    /// </summary>
    public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the path to an executable or <c>.csproj</c> to launch.
    /// </summary>
    /// <remarks>
    /// When a <c>.csproj</c> is supplied, Graft runs
    /// <c>dotnet run --project … -c</c> <see cref="Configuration"/>.
    /// </remarks>
    public required string AppPath { get; init; }

    /// <summary>
    /// Gets the named pipe name. When null, a unique name is generated.
    /// </summary>
    public string? PipeName { get; init; }

    /// <summary>
    /// Gets the connect token. When null, a unique token is generated.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Gets the overall process-start + connect + handshake budget.
    /// </summary>
    public TimeSpan Timeout { get; init; } = DefaultTimeout;

    /// <summary>
    /// Gets the MSBuild configuration used when <see cref="AppPath"/> is a csproj.
    /// </summary>
    public string Configuration { get; init; } = DefaultConfiguration;

    /// <summary>
    /// Gets optional operation timeline recording (PNG sequence + HTML viewer).
    /// </summary>
    /// <remarks>
    /// When set, <see cref="TimelineOptions.OutputDirectory"/> is required.
    /// Recording is Core-session only (no Scenario/MCP surface in v1).
    /// </remarks>
    public TimelineOptions? Timeline { get; init; }

    /// <summary>
    /// Gets extra environment variables merged into the child process
    /// (after Graft's own <c>GRAFT_*</c> values).
    /// </summary>
    /// <remarks>
    /// Optional extra process environment for the child app.
    /// Prefer app UI / settings for end-user configuration; use this when the host must inject process-level values.
    /// Null or empty keys are ignored. Values overwrite any inherited variable of the same name.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>
    /// Returns <paramref name="configuration"/> when it has a value, and <see cref="DefaultConfiguration"/> when it is empty.
    /// </summary>
    /// <param name="configuration">Caller-supplied configuration, or <see langword="null"/>.</param>
    /// <returns>A non-empty configuration name.</returns>
    public static string NormalizeConfiguration(string? configuration) =>
        string.IsNullOrWhiteSpace(configuration) ? DefaultConfiguration : configuration;
}
