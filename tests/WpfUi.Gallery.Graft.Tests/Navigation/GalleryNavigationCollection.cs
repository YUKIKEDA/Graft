namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryNavigation category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryNavigationCollection : ICollectionFixture<GalleryNavigationFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryNavigation";
}
