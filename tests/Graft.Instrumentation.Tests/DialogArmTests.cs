using Graft.Instrumentation.Dialogs;

namespace Graft.Instrumentation.Tests;

public sealed class DialogArmTests
{
    /// <summary>
    /// Gets the open-file, save-file, and open-folder arms.
    /// </summary>
    public static TheoryData<DialogArm> Arms { get; } = new() { DialogArm.OpenFile, DialogArm.SaveFile, DialogArm.OpenFolder };

    /// <summary>
    /// ArmPath is one-shot: first consume returns the path, second is unarmed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Each of DialogArm.OpenFile, DialogArm.SaveFile, and DialogArm.OpenFolder starts clean (Reset)
    ///
    /// Steps:
    /// - ArmPath C:\a.txt
    /// - TryConsume twice
    ///
    /// Expected:
    /// - First consume returns C:\a.txt and canceled false; second returns false
    /// </remarks>
    [Theory]
    [MemberData(nameof(Arms))]
    public void ArmPath_IsOneShot(DialogArm arm)
    {
        arm.Reset();
        arm.ArmPath(@"C:\a.txt");
        Assert.True(arm.TryConsume(out var path, out var canceled));
        Assert.Equal(@"C:\a.txt", path);
        Assert.False(canceled);
        Assert.False(arm.TryConsume(out _, out _));
    }

    /// <summary>
    /// ArmCancel returns canceled true and a null path once.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Each arm from <see cref="Arms"/> is reset
    ///
    /// Steps:
    /// - ArmCancel, then TryConsume twice
    ///
    /// Expected:
    /// - First consume has canceled true and a null path; second returns false
    /// </remarks>
    [Theory]
    [MemberData(nameof(Arms))]
    public void ArmCancel_IsOneShot(DialogArm arm)
    {
        arm.Reset();
        arm.ArmCancel();
        Assert.True(arm.TryConsume(out var path, out var canceled));
        Assert.Null(path);
        Assert.True(canceled);
        Assert.False(arm.TryConsume(out _, out _));
    }

    /// <summary>
    /// A second ArmPath replaces a pending path that has not been consumed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Each arm from <see cref="Arms"/> is reset
    ///
    /// Steps:
    /// - ArmPath C:\a.txt, then ArmPath C:\b.txt, then TryConsume
    ///
    /// Expected:
    /// - The consumed path is C:\b.txt
    /// </remarks>
    [Theory]
    [MemberData(nameof(Arms))]
    public void ArmPath_OverwritePending(DialogArm arm)
    {
        arm.Reset();
        arm.ArmPath(@"C:\a.txt");
        arm.ArmPath(@"C:\b.txt");
        Assert.True(arm.TryConsume(out var path, out _));
        Assert.Equal(@"C:\b.txt", path);
    }

    /// <summary>
    /// Open file, save file, and open folder arms keep separate pending paths.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - DialogArm.OpenFile, DialogArm.SaveFile, and DialogArm.OpenFolder are reset
    ///
    /// Steps:
    /// - Arm each with a different path
    /// - Consume each
    ///
    /// Expected:
    /// - Open file returns C:\open.txt, save file returns C:\save.txt, open folder returns C:\folder
    /// </remarks>
    [Fact]
    public void Arms_AreIndependent()
    {
        DialogArm.OpenFile.Reset();
        DialogArm.SaveFile.Reset();
        DialogArm.OpenFolder.Reset();
        DialogArm.OpenFile.ArmPath(@"C:\open.txt");
        DialogArm.SaveFile.ArmPath(@"C:\save.txt");
        DialogArm.OpenFolder.ArmPath(@"C:\folder");

        Assert.True(DialogArm.OpenFile.TryConsume(out var openPath, out _));
        Assert.True(DialogArm.SaveFile.TryConsume(out var savePath, out _));
        Assert.True(DialogArm.OpenFolder.TryConsume(out var folderPath, out _));
        Assert.Equal(@"C:\open.txt", openPath);
        Assert.Equal(@"C:\save.txt", savePath);
        Assert.Equal(@"C:\folder", folderPath);
    }
}
