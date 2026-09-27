using Graft.Core;
using Xunit;

namespace Graft.TestUtilities;

/// <summary>
/// xUnit collection fixture that launches one instrumented app and disposes it with the collection.
/// </summary>
/// <remarks>
/// Subclass and override <see cref="CreateLaunchOptions"/>. Declare the collection in the
/// <b>test assembly</b> (<c>CollectionDefinitionAttribute</c> is sealed and is not inherited):
/// <code>
/// public sealed class TodoAppFixture : GraftAppFixture
/// {
///     protected override LaunchOptions CreateLaunchOptions() =&gt; new()
///     {
///         AppPath = @"path\to\App.csproj",
///         Configuration = LaunchOptions.DefaultConfiguration,
///     };
/// }
///
/// [CollectionDefinition(Name, DisableParallelization = true)]
/// public sealed class TodoAppCollection : ICollectionFixture&lt;TodoAppFixture&gt;
/// {
///     public const string Name = "TodoApp";
/// }
/// </code>
/// <c>DisableParallelization = true</c> turns off parallelization for that test assembly.
/// It does not serialize other test projects. SendInput across assemblies still needs
/// <c>dotnet test -m:1</c>.
/// </remarks>
public abstract class GraftAppFixture : IAsyncLifetime
{
    private IAsyncDisposable? _owned;

    /// <summary>
    /// Gets the launched session. Available after the collection has started.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection has not started, or launch failed.</exception>
    public GraftSession Session =>
        _owned as GraftSession ?? throw new InvalidOperationException("GraftAppFixture.Session is available after the xUnit collection starts.");

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_owned is not null)
        {
            return;
        }

        var options = CreateLaunchOptions();
        ArgumentNullException.ThrowIfNull(options);
        _owned = await LaunchAsync(options, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_owned is null)
        {
            return;
        }

        await _owned.DisposeAsync().ConfigureAwait(false);
        _owned = null;
    }

    internal void AdoptForTests(IAsyncDisposable owned)
    {
        ArgumentNullException.ThrowIfNull(owned);
        _owned = owned;
    }

    internal bool OwnsSession => _owned is not null;

    /// <summary>
    /// Builds launch options for the app under test.
    /// </summary>
    /// <returns>Options passed to <see cref="Application.LaunchAsync"/>.</returns>
    protected abstract LaunchOptions CreateLaunchOptions();

    /// <summary>
    /// Starts the app. Tests override this to avoid a real process.
    /// </summary>
    /// <param name="options">Options from <see cref="CreateLaunchOptions"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session owned by this fixture.</returns>
    protected virtual Task<GraftSession> LaunchAsync(LaunchOptions options, CancellationToken cancellationToken) =>
        Application.LaunchAsync(options, cancellationToken);
}
