using Graft.Core;
using Graft.TestSupport;

namespace SampleWpfApp.Tests;

/// <summary>
/// Launches SampleWpfApp with the sample-test configuration and a 60 second timeout.
/// </summary>
/// <remarks>
/// Each call starts a new process. GraftAppFixture keeps one session for an xUnit collection.
/// These tests change the window and expect the initial state, so a shared session would change
/// the assertions. SampleUiCollection only serializes the launches.
/// </remarks>
internal static class SampleWpfLaunch
{
    /// <summary>
    /// Gets the SampleWpfApp project path.
    /// </summary>
    public static string AppPath => SampleWpfAppLocator.ResolveProjectPath();

    /// <summary>
    /// Starts SampleWpfApp.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The launched session.</returns>
    public static Task<GraftSession> LaunchAsync(CancellationToken cancellationToken = default) =>
        Application.LaunchAsync(CreateOptions(), cancellationToken);

    /// <summary>
    /// Starts SampleWpfApp and records an operation timeline.
    /// </summary>
    /// <param name="timeline">Timeline output for this session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The launched session.</returns>
    public static Task<GraftSession> LaunchAsync(TimelineOptions timeline, CancellationToken cancellationToken = default) =>
        Application.LaunchAsync(CreateOptions(timeline), cancellationToken);

    private static LaunchOptions CreateOptions(TimelineOptions? timeline = null) =>
        new()
        {
            AppPath = AppPath,
            Configuration = LaunchOptions.DefaultConfiguration,
            Timeout = TimeSpan.FromSeconds(60),
            Timeline = timeline,
        };
}
