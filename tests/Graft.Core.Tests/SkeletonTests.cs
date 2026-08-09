namespace Graft.Core.Tests;

public sealed class SkeletonTests
{
    /// <summary>
    /// Graft.Core exposes the public Application entry type.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Graft.Core is referenced by this test project
    ///
    /// Steps:
    /// - Resolve typeof(Application)
    ///
    /// Expected:
    /// - Type is non-null and in assembly Graft.Core
    /// </remarks>
    [Fact]
    public void Application_Type_IsPublicInGraftCore()
    {
        var type = typeof(Application);
        Assert.NotNull(type);
        Assert.Equal("Graft.Core", type.Assembly.GetName().Name);
    }
}
