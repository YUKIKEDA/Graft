using Graft.Instrumentation.Dialogs;

namespace Graft.Instrumentation.Tests;

public sealed class MessageBoxArmTests
{
    /// <summary>
    /// ArmResult is one-shot: first consume returns result, second is unarmed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - MessageBoxArm starts clean (Reset)
    ///
    /// Steps:
    /// - ArmResult Yes
    /// - TryConsume twice
    ///
    /// Expected:
    /// - First: Yes; second: false
    /// </remarks>
    [Fact]
    public void ArmResult_IsOneShot()
    {
        MessageBoxArm.Reset();
        MessageBoxArm.ArmResult("Yes");
        Assert.True(MessageBoxArm.TryConsume(out var result));
        Assert.Equal("Yes", result);
        Assert.False(MessageBoxArm.TryConsume(out _));
    }

    /// <summary>
    /// Invalid result names are rejected.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - MessageBoxArm.Reset
    ///
    /// Steps:
    /// - ArmResult with unknown name
    ///
    /// Expected:
    /// - ArgumentException
    /// </remarks>
    [Fact]
    public void ArmResult_RejectsUnknownName()
    {
        MessageBoxArm.Reset();
        Assert.Throws<ArgumentException>(() => MessageBoxArm.ArmResult("Maybe"));
    }
}
