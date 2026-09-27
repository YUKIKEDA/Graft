namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// GalleryBasicInput category: shared Gallery process (Timeline Always).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GalleryBasicInputCollection : ICollectionFixture<GalleryBasicInputFixture>
{
    /// <summary>
    /// Collection name for xUnit.
    /// </summary>
    public const string Name = "GalleryBasicInput";
}
