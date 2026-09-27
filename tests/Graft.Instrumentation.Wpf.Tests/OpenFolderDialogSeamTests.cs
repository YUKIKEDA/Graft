using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Wpf;
using Microsoft.Win32;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// Verifies the Harmony <c>CommonItemDialog.RunDialog</c> OpenFolder arm seam.
/// </summary>
public sealed class OpenFolderDialogSeamTests
{
    /// <summary>
    /// Armed path makes <see cref="OpenFolderDialog.ShowDialog()"/> return OK without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the RunDialog patch
    ///
    /// Steps:
    /// - DialogArm.OpenFolder.ArmPath
    /// - OpenFolderDialog.ShowDialog
    ///
    /// Expected:
    /// - Result is true and FolderName matches the arm
    /// </remarks>
    [StaFact]
    public void ArmPath_ShowDialog_ReturnsArmedFolderName()
    {
        WpfGraft.ResetForTests();
        DialogArm.OpenFolder.Reset();
        WpfGraft.Use();

        const string path = @"C:\graft-seam-folder-ok";
        DialogArm.OpenFolder.ArmPath(path);

        var dialog = new OpenFolderDialog();
        var result = dialog.ShowDialog();

        Assert.True(result);
        Assert.Equal(path, dialog.FolderName);
    }

    /// <summary>
    /// Armed cancel makes <see cref="OpenFolderDialog.ShowDialog()"/> return false without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the RunDialog patch
    ///
    /// Steps:
    /// - DialogArm.OpenFolder.ArmCancel
    /// - OpenFolderDialog.ShowDialog
    ///
    /// Expected:
    /// - Result is false
    /// </remarks>
    [StaFact]
    public void ArmCancel_ShowDialog_ReturnsFalse()
    {
        WpfGraft.ResetForTests();
        DialogArm.OpenFolder.Reset();
        WpfGraft.Use();

        DialogArm.OpenFolder.ArmCancel();

        var dialog = new OpenFolderDialog();
        var result = dialog.ShowDialog();

        Assert.False(result);
    }
}
