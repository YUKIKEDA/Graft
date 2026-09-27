namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryWindows category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryWindowsCollection : ICollectionFixture<GalleryWindowsFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryWindows";
}
