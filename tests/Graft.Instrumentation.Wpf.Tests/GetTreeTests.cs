using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Graft.Instrumentation.Tree;
using Graft.Instrumentation.Wpf;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf.Tests;

public sealed class GetTreeTests : IDisposable
{
    private Application? _application;
    private Window? _window;

    public void Dispose()
    {
        WpfGraft.ResetForTests();

        if (_window is not null)
        {
            _window.Close();
            _window = null;
        }

        _application = null;
    }

    /// <summary>
    /// WPF visual-tree capture includes SampleButton name and non-empty bounds.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread with a shown Window containing AutomationId=SampleButton
    /// - WpfGraft.Use registered
    ///
    /// Steps:
    /// - Call IUiTreeProvider.GetTree on the UI thread
    /// - Find node with automationId SampleButton
    ///
    /// Expected:
    /// - name is "Click Me"
    /// - bounds width and height are greater than 0
    /// - truncated is false for the small sample tree
    /// </remarks>
    [StaFact]
    public void GetTree_OnUiThread_IncludesSampleButtonNameAndBounds()
    {
        EnsureApplication();
        ShowSampleWindow();
        WpfGraft.ResetForTests();
        WpfGraft.Use();

        var provider =
            AgentServices.TreeProvider
            ?? throw new InvalidOperationException("Tree provider was not registered.");
        var result = provider.GetTree(new GetTreeOptions());

        Assert.False(result.Truncated);
        var button = FindByAutomationId(result.Root, "SampleButton");
        Assert.NotNull(button);
        Assert.Equal("Click Me", button.Name);
        Assert.Equal("Button", button.ControlType);
        Assert.True(button.Bounds.Width > 0, "Expected positive width.");
        Assert.True(button.Bounds.Height > 0, "Expected positive height.");
    }

    private void EnsureApplication()
    {
        _application =
            Application.Current
            ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    }

    private void ShowSampleWindow()
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

        _window = new Window
        {
            Title = "Graft Sample WPF App",
            Width = 480,
            Height = 320,
            Content = panel,
        };
        _window.Show();
        _window.UpdateLayout();
        _application!.MainWindow = _window;
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
