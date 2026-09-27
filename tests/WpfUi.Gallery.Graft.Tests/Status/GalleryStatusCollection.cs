namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryStatus category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryStatusCollection : ICollectionFixture<GalleryStatusFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryStatus";
}
