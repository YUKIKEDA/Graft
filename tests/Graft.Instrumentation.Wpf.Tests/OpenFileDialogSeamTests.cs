using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Wpf;
using Microsoft.Win32;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// Verifies the Harmony <c>CommonItemDialog.RunDialog</c> OpenFile arm seam.
/// </summary>
public sealed class OpenFileDialogSeamTests
{
    /// <summary>
    /// Armed path makes <see cref="OpenFileDialog.ShowDialog()"/> return OK without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the RunDialog patch
    ///
    /// Steps:
    /// - OpenFileArm.ArmPath
    /// - OpenFileDialog.ShowDialog
    ///
    /// Expected:
    /// - Result is true and FileName matches the arm
    /// </remarks>
    [StaFact]
    public void ArmPath_ShowDialog_ReturnsArmedFileName()
    {
        WpfGraft.ResetForTests();
        OpenFileArm.Reset();
        WpfGraft.Use();

        const string path = @"C:\graft-seam-ok.txt";
        OpenFileArm.ArmPath(path);

        var dialog = new OpenFileDialog();
        var result = dialog.ShowDialog();

        Assert.True(result);
        Assert.Equal(path, dialog.FileName);
    }

    /// <summary>
    /// Armed cancel makes <see cref="OpenFileDialog.ShowDialog()"/> return false without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the RunDialog patch
    ///
    /// Steps:
    /// - OpenFileArm.ArmCancel
    /// - OpenFileDialog.ShowDialog
    ///
    /// Expected:
    /// - Result is false
    /// </remarks>
    [StaFact]
    public void ArmCancel_ShowDialog_ReturnsFalse()
    {
        WpfGraft.ResetForTests();
        OpenFileArm.Reset();
        WpfGraft.Use();

        OpenFileArm.ArmCancel();

        var dialog = new OpenFileDialog();
        var result = dialog.ShowDialog();

        Assert.False(result);
    }
}
