using Graft.Core;
using Graft.TestUtilities;

namespace Graft.TestUtilities.Tests;

/// <summary>
/// Guards GraftAppFixture lifetime before a real process is launched.
/// </summary>
public sealed class GraftAppFixtureTests
{
    /// <summary>
    /// Session is unavailable until InitializeAsync stores a launched session.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Probe fixture constructed, InitializeAsync not called
    ///
    /// Steps:
    /// - Read Session
    ///
    /// Expected:
    /// - InvalidOperationException
    /// </remarks>
    [Fact]
    public void Session_BeforeInitialize_Throws()
    {
        var fixture = new ProbeFixture();

        var ex = Assert.Throws<InvalidOperationException>(() => fixture.Session);

        Assert.Contains("collection starts", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// InitializeAsync forwards CreateLaunchOptions into the launch seam.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Probe overrides LaunchAsync to throw ProbeLaunchException with the app path
    ///
    /// Steps:
    /// - InitializeAsync
    ///
    /// Expected:
    /// - ProbeLaunchException.AppPath equals the options path
    /// - Session still throws because no session was stored
    /// </remarks>
    [Fact]
    public async Task InitializeAsync_ForwardsLaunchOptions()
    {
        var fixture = new ProbeFixture();

        var ex = await Assert.ThrowsAsync<ProbeLaunchException>(() => fixture.InitializeAsync());

        Assert.Equal(ProbeFixture.AppPath, ex.AppPath);
        Assert.Throws<InvalidOperationException>(() => fixture.Session);
    }

    /// <summary>
    /// DisposeAsync before a successful launch does nothing.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Probe fixture never started
    ///
    /// Steps:
    /// - DisposeAsync
    ///
    /// Expected:
    /// - Completes without throwing
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_BeforeInitialize_Completes()
    {
        var fixture = new ProbeFixture();

        await fixture.DisposeAsync();
    }

    /// <summary>
    /// A null options instance is rejected before launch.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Probe returns null from CreateLaunchOptions
    ///
    /// Steps:
    /// - InitializeAsync
    ///
    /// Expected:
    /// - ArgumentNullException
    /// - LaunchAsync was not called
    /// </remarks>
    [Fact]
    public async Task InitializeAsync_NullOptions_Throws()
    {
        var fixture = new NullOptionsFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.InitializeAsync());

        Assert.False(fixture.LaunchCalled);
    }

    private sealed class ProbeFixture : GraftAppFixture
    {
        public const string AppPath = "probe.csproj";

        protected override LaunchOptions CreateLaunchOptions() => new() { AppPath = AppPath };

        protected override Task<GraftSession> LaunchAsync(LaunchOptions options, CancellationToken cancellationToken) =>
            throw new ProbeLaunchException(options.AppPath);
    }

    private sealed class NullOptionsFixture : GraftAppFixture
    {
        public bool LaunchCalled { get; private set; }

        protected override LaunchOptions CreateLaunchOptions() => null!;

        protected override Task<GraftSession> LaunchAsync(LaunchOptions options, CancellationToken cancellationToken)
        {
            LaunchCalled = true;
            throw new InvalidOperationException("launch should not run");
        }
    }

    private sealed class ProbeLaunchException : Exception
    {
        public ProbeLaunchException(string appPath)
            : base(appPath)
        {
            AppPath = appPath;
        }

        public string AppPath { get; }
    }
}
