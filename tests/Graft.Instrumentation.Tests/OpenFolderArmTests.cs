using Graft.Instrumentation.Dialogs;

namespace Graft.Instrumentation.Tests;

public sealed class OpenFolderArmTests
{
    /// <summary>
    /// ArmPath is one-shot: first consume returns path, second is unarmed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - OpenFolderArm starts clean (Reset)
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
        OpenFolderArm.Reset();
        OpenFolderArm.ArmPath(@"C:\folder");
        Assert.True(OpenFolderArm.TryConsume(out var path, out var canceled));
        Assert.Equal(@"C:\folder", path);
        Assert.False(canceled);
        Assert.False(OpenFolderArm.TryConsume(out _, out _));
    }

    /// <summary>
    /// ArmCancel returns canceled=true and null path once.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - OpenFolderArm.Reset
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
        OpenFolderArm.Reset();
        OpenFolderArm.ArmCancel();
        Assert.True(OpenFolderArm.TryConsume(out var path, out var canceled));
        Assert.Null(path);
        Assert.True(canceled);
        Assert.False(OpenFolderArm.TryConsume(out _, out _));
    }

    /// <summary>
    /// OpenFolder arm does not share state with OpenFile arm.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Both arms Reset
    ///
    /// Steps:
    /// - Arm OpenFile and OpenFolder with different paths
    /// - Consume each
    ///
    /// Expected:
    /// - Each returns its own path
    /// </remarks>
    [Fact]
    public void OpenFolderArm_IsIndependentOfOpenFileArm()
    {
        OpenFileArm.Reset();
        OpenFolderArm.Reset();
        OpenFileArm.ArmPath(@"C:\file.txt");
        OpenFolderArm.ArmPath(@"C:\folder");
        Assert.True(OpenFolderArm.TryConsume(out var folderPath, out _));
        Assert.Equal(@"C:\folder", folderPath);
        Assert.True(OpenFileArm.TryConsume(out var filePath, out _));
        Assert.Equal(@"C:\file.txt", filePath);
    }
}
