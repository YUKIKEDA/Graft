namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryCollections category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryCollectionsCollection : ICollectionFixture<GalleryCollectionsFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryCollections";
}
