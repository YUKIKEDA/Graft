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
}
