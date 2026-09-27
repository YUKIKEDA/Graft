using Graft.Instrumentation.Input;

namespace Graft.Instrumentation.Tests;

public sealed class InputInjectorTests
{
    /// <summary>
    /// TypeText with empty string is a no-op (does not throw).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - None
    ///
    /// Steps:
    /// - Call InputInjector.TypeText("")
    ///
    /// Expected:
    /// - Completes without exception
    /// </remarks>
    [Fact]
    public void TypeText_Empty_DoesNotThrow()
    {
        InputInjector.TypeText(string.Empty);
    }

    /// <summary>
    /// TypeText rejects null.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - None
    ///
    /// Steps:
    /// - Call InputInjector.TypeText(null!)
    ///
    /// Expected:
    /// - ArgumentNullException
    /// </remarks>
    [Fact]
    public void TypeText_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => InputInjector.TypeText(null!));
    }

    /// <summary>
    /// Absolute SendInput coordinates are normalized against the whole virtual desktop (#85).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Virtual desktop layouts: single 1920x1080 primary; secondary monitor to the left (origin -1920,0, 3840x1080);
    ///   secondary monitor above (origin 0,-1080, 1920x2160)
    ///
    /// Steps:
    /// - NormalizeToVirtualDesktop for points at the corners and on the secondary monitor
    ///
    /// Expected:
    /// - The virtual desktop's top-left maps to (0,0) and bottom-right to (65535,65535)
    /// - A point on a negative-origin monitor maps inside the range instead of clamping to the primary
    /// </remarks>
    [Theory]
    [InlineData(0, 0, 0, 0, 1920, 1080, 0, 0)]
    [InlineData(1919, 1079, 0, 0, 1920, 1080, 65535, 65535)]
    [InlineData(-1920, 0, -1920, 0, 3840, 1080, 0, 0)]
    [InlineData(1919, 1079, -1920, 0, 3840, 1080, 65535, 65535)]
    [InlineData(-960, 540, -1920, 0, 3840, 1080, 16388, 32798)]
    [InlineData(0, 0, -1920, 0, 3840, 1080, 32776, 0)]
    [InlineData(960, -540, 0, -1080, 1920, 2160, 32785, 16391)]
    public void NormalizeToVirtualDesktop_MapsAcrossMonitors(int x, int y, int left, int top, int width, int height, int expectedX, int expectedY)
    {
        var (absX, absY) = InputInjector.NormalizeToVirtualDesktop(x, y, left, top, width, height);

        Assert.Equal(expectedX, absX);
        Assert.Equal(expectedY, absY);
    }
}
