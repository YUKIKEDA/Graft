namespace Graft.Core;

/// <summary>
/// Options for <see cref="Application.LaunchAsync"/>.
/// </summary>
public sealed class LaunchOptions
{
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
    public string Configuration { get; init; } = "GraftTest";

    /// <summary>
    /// Gets optional operation timeline recording (PNG sequence + HTML viewer).
    /// </summary>
    /// <remarks>
    /// When set, <see cref="TimelineOptions.OutputDirectory"/> is required.
    /// Recording is Core-session only (no Scenario/MCP surface in v1).
    /// </remarks>
    public TimelineOptions? Timeline { get; init; }
}
