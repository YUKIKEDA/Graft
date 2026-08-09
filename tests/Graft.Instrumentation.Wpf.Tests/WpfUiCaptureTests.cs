using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Instrumentation.Wpf;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// WPF capture tests share one STA + <see cref="Application"/> lifetime
/// (WPF allows only one Application per AppDomain; StaFact threads differ per method).
/// </summary>
public sealed class WpfUiCaptureTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// GetTree and screenshot capture succeed against a shown Sample-like window.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; single Application for this method
    /// - Window with AutomationId=SampleButton is shown as MainWindow
    /// - WpfGraft.Use registered
    ///
    /// Steps:
    /// - Call IUiTreeProvider.GetTree and find SampleButton
    /// - Call IScreenshotProvider.Capture
    ///
    /// Expected:
    /// - SampleButton name/bounds are present
    /// - screenshot meta is png with positive size; raw bytes have PNG signature
    /// </remarks>
    [StaFact]
    public void GetTreeAndScreenshot_OnShownWindow_Succeed()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Window? window = null;
        try
        {
            window = CreateSampleWindow();
            window.Show();
            window.UpdateLayout();
            app.MainWindow = window;

            WpfGraft.ResetForTests();
            WpfGraft.Use();

            var treeProvider =
                AgentServices.TreeProvider
                ?? throw new InvalidOperationException("Tree provider was not registered.");
            var tree = treeProvider.GetTree(new GetTreeOptions());
            Assert.False(tree.Truncated);
            var button = FindByAutomationId(tree.Root, "SampleButton");
            Assert.NotNull(button);
            Assert.Equal("Click Me", button.Name);
            Assert.Equal("Button", button.ControlType);
            Assert.True(button.Bounds.Width > 0, "Expected positive width.");
            Assert.True(button.Bounds.Height > 0, "Expected positive height.");

            var screenshotProvider =
                AgentServices.ScreenshotProvider
                ?? throw new InvalidOperationException("Screenshot provider was not registered.");
            var capture = screenshotProvider.Capture(ScreenshotOptions.Default);
            Assert.Equal("png", capture.Meta.Format);
            Assert.True(capture.Meta.Width > 0);
            Assert.True(capture.Meta.Height > 0);
            Assert.True(capture.Meta.ByteLength > 0);
            Assert.Equal(capture.PngBytes.Length, capture.Meta.ByteLength);
            Assert.True(
                capture.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on captured bytes."
            );
        }
        finally
        {
            WpfGraft.ResetForTests();
            window?.Close();
            app.Shutdown();
        }
    }

    private static Window CreateSampleWindow()
    {
        var status = new TextBlock { Text = "Ready" };
        AutomationProperties.SetAutomationId(status, "StatusText");

        var textBox = new TextBox();
        AutomationProperties.SetAutomationId(textBox, "SampleTextBox");

        var button = new Button
        {
            Content = "Click Me",
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetAutomationId(button, "SampleButton");

        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(status);
        panel.Children.Add(textBox);
        panel.Children.Add(button);

        return new Window
        {
            Title = "Graft Sample WPF App",
            Width = 480,
            Height = 320,
            Content = panel,
        };
    }

    private static TreeNode? FindByAutomationId(TreeNode node, string automationId)
    {
        if (string.Equals(node.AutomationId, automationId, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindByAutomationId(child, automationId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
