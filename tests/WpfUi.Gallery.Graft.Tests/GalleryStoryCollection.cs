namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryStory category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryStoryCollection : ICollectionFixture<GalleryStoryFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryStory";
}
