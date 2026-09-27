namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryText category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryTextCollection : ICollectionFixture<GalleryTextFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryText";
}
