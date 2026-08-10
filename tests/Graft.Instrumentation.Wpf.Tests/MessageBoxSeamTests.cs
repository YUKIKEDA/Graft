using System.Windows;
using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Wpf;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// Verifies the Harmony <see cref="MessageBox.Show"/> arm seam.
/// </summary>
public sealed class MessageBoxSeamTests
{
    /// <summary>
    /// Armed Yes makes MessageBox.Show return Yes without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the MessageBox patch
    ///
    /// Steps:
    /// - MessageBoxArm.ArmResult Yes
    /// - MessageBox.Show with YesNo
    ///
    /// Expected:
    /// - Result is Yes
    /// </remarks>
    [StaFact]
    public void ArmYes_Show_ReturnsYes()
    {
        WpfGraft.ResetForTests();
        MessageBoxArm.Reset();
        WpfGraft.Use();

        MessageBoxArm.ArmResult("Yes");
        var result = MessageBox.Show("q", "t", MessageBoxButton.YesNo);
        Assert.Equal(MessageBoxResult.Yes, result);
    }

    /// <summary>
    /// Armed No makes MessageBox.Show return No without UI.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; WpfGraft.Use installed the MessageBox patch
    ///
    /// Steps:
    /// - MessageBoxArm.ArmResult No
    /// - MessageBox.Show with YesNo
    ///
    /// Expected:
    /// - Result is No
    /// </remarks>
    [StaFact]
    public void ArmNo_Show_ReturnsNo()
    {
        WpfGraft.ResetForTests();
        MessageBoxArm.Reset();
        WpfGraft.Use();

        MessageBoxArm.ArmResult("No");
        var result = MessageBox.Show("q", "t", MessageBoxButton.YesNo);
        Assert.Equal(MessageBoxResult.No, result);
    }
}
