namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryDesign category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryDesignCollection : ICollectionFixture<GalleryDesignFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryDesign";
}
