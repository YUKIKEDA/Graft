using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Category-scoped Gallery process fixture (one session per collection).
/// </summary>
public class GalleryCategoryFixture : IAsyncLifetime
{
    private GraftSession? _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryCategoryFixture"/> class.
    /// </summary>
    /// <param name="categoryLeaf">Timeline / collection leaf name.</param>
    public GalleryCategoryFixture(string categoryLeaf)
    {
        CategoryLeaf = categoryLeaf;
    }

    /// <summary>
    /// Gets the category leaf used for Timeline path and diagnostics.
    /// </summary>
    public string CategoryLeaf { get; }

    /// <summary>
    /// Gets the Timeline output directory for this category process.
    /// </summary>
    public string TimelineDir { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the shared Graft session, or null when Gallery is unavailable.
    /// </summary>
    public GraftSession Session => _session ?? throw new InvalidOperationException($"Gallery session not started for category '{CategoryLeaf}'.");

    /// <summary>
    /// Gets a value indicating whether Launch succeeded (Gallery present and agent connected).
    /// </summary>
    public bool IsReady { get; private set; }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (!GalleryAppLocator.IsAvailable)
        {
            IsReady = false;
            return;
        }

        var leaf = $"{CategoryLeaf}-{Guid.NewGuid():N}";
        var (session, timelineDir) = await GalleryLaunch.LaunchAsync(leaf);
        _session = session;
        TimelineDir = timelineDir;
        IsReady = true;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
            if (IsReady && !string.IsNullOrWhiteSpace(TimelineDir))
            {
                GalleryLaunch.AssertTimelineWritten(TimelineDir);
            }
        }
    }
}
