namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryMedia category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryMediaCollection : ICollectionFixture<GalleryMediaFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryMedia";
}
