namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Shell category: launch, title bar, navigation, settings (shared process).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryShellCollection : ICollectionFixture<GalleryShellFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryShell";
}
