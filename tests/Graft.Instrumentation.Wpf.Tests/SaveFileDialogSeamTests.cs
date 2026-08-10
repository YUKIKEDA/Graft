using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Wpf;
using Microsoft.Win32;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// Verifies the Harmony <c>CommonItemDialog.RunDialog</c> SaveFile arm seam.
/// </summary>
public sealed class SaveFileDialogSeamTests
{
    /// <summary>
    /// Armed path makes <see cref="SaveFileDialog.ShowDialog()"/> return OK without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the RunDialog patch
    ///
    /// Steps:
    /// - SaveFileArm.ArmPath
    /// - SaveFileDialog.ShowDialog
    ///
    /// Expected:
    /// - Result is true and FileName matches the arm
    /// </remarks>
    [StaFact]
    public void ArmPath_ShowDialog_ReturnsArmedFileName()
    {
        WpfGraft.ResetForTests();
        SaveFileArm.Reset();
        WpfGraft.Use();

        const string path = @"C:\graft-seam-save-ok.txt";
        SaveFileArm.ArmPath(path);

        var dialog = new SaveFileDialog();
        var result = dialog.ShowDialog();

        Assert.True(result);
        Assert.Equal(path, dialog.FileName);
    }

    /// <summary>
    /// Armed cancel makes <see cref="SaveFileDialog.ShowDialog()"/> return false without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the RunDialog patch
    ///
    /// Steps:
    /// - SaveFileArm.ArmCancel
    /// - SaveFileDialog.ShowDialog
    ///
    /// Expected:
    /// - Result is false
    /// </remarks>
    [StaFact]
    public void ArmCancel_ShowDialog_ReturnsFalse()
    {
        WpfGraft.ResetForTests();
        SaveFileArm.Reset();
        WpfGraft.Use();

        SaveFileArm.ArmCancel();

        var dialog = new SaveFileDialog();
        var result = dialog.ShowDialog();

        Assert.False(result);
    }
}
