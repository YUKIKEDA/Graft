using Graft.Instrumentation.Dialogs;

namespace Graft.Instrumentation.Tests;

public sealed class OpenFileArmTests
{
    /// <summary>
    /// ArmPath is one-shot: first consume returns path, second is unarmed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - OpenFileArm starts clean (Reset)
    ///
    /// Steps:
    /// - ArmPath
    /// - TryConsume twice
    ///
    /// Expected:
    /// - First: path OK; second: false
    /// </remarks>
    [Fact]
    public void ArmPath_IsOneShot()
    {
        OpenFileArm.Reset();
        OpenFileArm.ArmPath(@"C:\a.txt");
        Assert.True(OpenFileArm.TryConsume(out var path, out var canceled));
        Assert.Equal(@"C:\a.txt", path);
        Assert.False(canceled);
        Assert.False(OpenFileArm.TryConsume(out _, out _));
    }

    /// <summary>
    /// ArmCancel returns canceled=true and null path once.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - OpenFileArm.Reset
    ///
    /// Steps:
    /// - ArmCancel → TryConsume
    ///
    /// Expected:
    /// - canceled true, path null
    /// </remarks>
    [Fact]
    public void ArmCancel_IsOneShot()
    {
        OpenFileArm.Reset();
        OpenFileArm.ArmCancel();
        Assert.True(OpenFileArm.TryConsume(out var path, out var canceled));
        Assert.Null(path);
        Assert.True(canceled);
        Assert.False(OpenFileArm.TryConsume(out _, out _));
    }

    /// <summary>
    /// Re-arm overwrites a pending unused arm.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - OpenFileArm.Reset
    ///
    /// Steps:
    /// - ArmPath A → ArmPath B → TryConsume
    ///
    /// Expected:
    /// - Returns B
    /// </remarks>
    [Fact]
    public void ArmPath_OverwritePending()
    {
        OpenFileArm.Reset();
        OpenFileArm.ArmPath(@"C:\a.txt");
        OpenFileArm.ArmPath(@"C:\b.txt");
        Assert.True(OpenFileArm.TryConsume(out var path, out _));
        Assert.Equal(@"C:\b.txt", path);
    }
}
