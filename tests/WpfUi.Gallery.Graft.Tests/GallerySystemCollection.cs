namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GallerySystem category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GallerySystemCollection : ICollectionFixture<GallerySystemFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GallerySystem";
}
