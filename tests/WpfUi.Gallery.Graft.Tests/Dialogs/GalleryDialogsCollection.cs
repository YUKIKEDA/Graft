namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryDialogs category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryDialogsCollection : ICollectionFixture<GalleryDialogsFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryDialogs";
}
