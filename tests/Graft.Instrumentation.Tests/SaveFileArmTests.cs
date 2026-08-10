using Graft.Instrumentation.Dialogs;

namespace Graft.Instrumentation.Tests;

public sealed class SaveFileArmTests
{
    /// <summary>
    /// ArmPath is one-shot: first consume returns path, second is unarmed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SaveFileArm starts clean (Reset)
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
        SaveFileArm.Reset();
        SaveFileArm.ArmPath(@"C:\a.txt");
        Assert.True(SaveFileArm.TryConsume(out var path, out var canceled));
        Assert.Equal(@"C:\a.txt", path);
        Assert.False(canceled);
        Assert.False(SaveFileArm.TryConsume(out _, out _));
    }

    /// <summary>
    /// ArmCancel returns canceled=true and null path once.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SaveFileArm.Reset
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
        SaveFileArm.Reset();
        SaveFileArm.ArmCancel();
        Assert.True(SaveFileArm.TryConsume(out var path, out var canceled));
        Assert.Null(path);
        Assert.True(canceled);
        Assert.False(SaveFileArm.TryConsume(out _, out _));
    }

    /// <summary>
    /// OpenFile and SaveFile arms do not share state.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Both arms Reset
    ///
    /// Steps:
    /// - Arm OpenFile path A and SaveFile path B
    /// - Consume each
    ///
    /// Expected:
    /// - Each returns its own path
    /// </remarks>
    [Fact]
    public void SaveFileArm_IsIndependentOfOpenFileArm()
    {
        OpenFileArm.Reset();
        SaveFileArm.Reset();
        OpenFileArm.ArmPath(@"C:\open.txt");
        SaveFileArm.ArmPath(@"C:\save.txt");
        Assert.True(SaveFileArm.TryConsume(out var savePath, out _));
        Assert.Equal(@"C:\save.txt", savePath);
        Assert.True(OpenFileArm.TryConsume(out var openPath, out _));
        Assert.Equal(@"C:\open.txt", openPath);
    }
}
