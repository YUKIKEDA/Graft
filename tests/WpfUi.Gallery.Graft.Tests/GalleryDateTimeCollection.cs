namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryDateTime category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryDateTimeCollection : ICollectionFixture<GalleryDateTimeFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryDateTime";
}
