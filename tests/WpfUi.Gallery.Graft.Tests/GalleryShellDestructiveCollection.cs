namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Destructive TitleBar Close tests (own process; must not share Shell session).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryShellDestructiveCollection : ICollectionFixture<object>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryShellDestructive";
}
