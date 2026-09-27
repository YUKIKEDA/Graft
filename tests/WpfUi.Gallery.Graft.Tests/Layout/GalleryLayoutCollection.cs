namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryLayout category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryLayoutCollection : ICollectionFixture<GalleryLayoutFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryLayout";
}
