using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Instrumentation.Wpf;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// WPF capture / resolve / invoke tests share one STA + <see cref="Application"/> lifetime
/// (WPF allows only one Application per AppDomain; StaFact threads differ per method).
/// </summary>
public sealed class WpfUiCaptureTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// GetTree, screenshot, resolve, invoke, setValue, toggle, SendInput invoke succeed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; single Application for this method
    /// - Window with Sample controls is shown as MainWindow
    /// - WpfGraft.Use registered
    ///
    /// Steps:
    /// - Call GetTree / screenshot / resolve as before
    /// - Invoke SampleButton then GetTree StatusText
    /// - setValue SampleTextBox then GetTree name
    /// - Toggle SampleCheckBox then expect name On
    /// - Invoke SampleMouseTarget (SendInput fallback) then StatusText MouseHit
    /// - Invoke a disabled button
    ///
    /// Expected:
    /// - SampleButton name/bounds and PNG signature as before
    /// - After invoke, StatusText name is "Clicked 1"
    /// - After setValue, SampleTextBox name matches the set text
    /// - After toggle, SampleCheckBox name is On
    /// - After mouse-target invoke, StatusText is MouseHit
    /// - Disabled button → element.notActionable
    /// </remarks>
    [StaFact]
    public void GetTreeScreenshotResolveInvokeAndSetValue_OnShownWindow_Succeed()
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
            var buttonNode = FindByAutomationId(tree.Root, "SampleButton");
            Assert.NotNull(buttonNode);
            Assert.Equal("Click Me", buttonNode.Name);
            Assert.Equal("Button", buttonNode.ControlType);
            Assert.True(buttonNode.Bounds.Width > 0, "Expected positive width.");
            Assert.True(buttonNode.Bounds.Height > 0, "Expected positive height.");

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

            var resolver =
                AgentServices.ElementResolver
                ?? throw new InvalidOperationException("Element resolver was not registered.");
            var resolved = resolver.Resolve(new ElementSelector { AutomationId = "SampleButton" });
            Assert.Equal("SampleButton", resolved.AutomationId);
            Assert.Equal("Button", resolved.ControlType);
            Assert.IsType<Button>(resolved.Target);
            Assert.True(resolved.RuntimeId > 0);

            var notFound = Assert.Throws<ElementResolveException>(() =>
                resolver.Resolve(new ElementSelector { AutomationId = "DoesNotExist" })
            );
            Assert.Equal(GraftErrorCodes.ElementNotFound, notFound.Code);

            var invalid = Assert.Throws<ElementResolveException>(() =>
                resolver.Resolve(new ElementSelector { AutomationId = "  " })
            );
            Assert.Equal(GraftErrorCodes.SelectorInvalid, invalid.Code);

            var invoker =
                AgentServices.ElementInvoker
                ?? throw new InvalidOperationException("Element invoker was not registered.");
            invoker.Invoke(new ElementSelector { AutomationId = "SampleButton" });

            var afterInvoke = treeProvider.GetTree(new GetTreeOptions());
            var status = FindByAutomationId(afterInvoke.Root, "StatusText");
            Assert.NotNull(status);
            Assert.Equal("Clicked 1", status.Name);

            var valueSetter =
                AgentServices.ElementValueSetter
                ?? throw new InvalidOperationException("Element value setter was not registered.");
            const string typed = "hello-graft";
            valueSetter.SetValue(new ElementSelector { AutomationId = "SampleTextBox" }, typed);

            var afterSetValue = treeProvider.GetTree(new GetTreeOptions());
            var textBoxNode = FindByAutomationId(afterSetValue.Root, "SampleTextBox");
            Assert.NotNull(textBoxNode);
            Assert.Equal(typed, textBoxNode.Name);

            var toggler =
                AgentServices.ElementToggler
                ?? throw new InvalidOperationException("Element toggler was not registered.");
            toggler.Toggle(new ElementSelector { AutomationId = "SampleCheckBox" });
            var afterToggle = treeProvider.GetTree(new GetTreeOptions());
            var checkBoxNode = FindByAutomationId(afterToggle.Root, "SampleCheckBox");
            Assert.NotNull(checkBoxNode);
            Assert.Equal("On", checkBoxNode.Name);

            // Border has no Invoke pattern — exercises SendInput click fallback.
            invoker.Invoke(new ElementSelector { AutomationId = "SampleMouseTarget" });
            var afterMouse = treeProvider.GetTree(new GetTreeOptions());
            var statusAfterMouse = FindByAutomationId(afterMouse.Root, "StatusText");
            Assert.NotNull(statusAfterMouse);
            Assert.Equal("MouseHit", statusAfterMouse.Name);

            var disabled = new Button { Content = "Nope", IsEnabled = false };
            AutomationProperties.SetAutomationId(disabled, "DisabledButton");
            ((StackPanel)window.Content).Children.Add(disabled);
            window.UpdateLayout();

            var notActionable = Assert.Throws<ElementActionException>(() =>
                invoker.Invoke(new ElementSelector { AutomationId = "DisabledButton" })
            );
            Assert.Equal(GraftErrorCodes.ElementNotActionable, notActionable.Code);

            // Duplicate automationIds → ambiguous.
            var dupA = new Button { Content = "A" };
            AutomationProperties.SetAutomationId(dupA, "DupId");
            var dupB = new Button { Content = "B" };
            AutomationProperties.SetAutomationId(dupB, "DupId");
            var panel = (StackPanel)window.Content;
            panel.Children.Add(dupA);
            panel.Children.Add(dupB);
            window.UpdateLayout();

            var ambiguous = Assert.Throws<ElementResolveException>(() =>
                resolver.Resolve(new ElementSelector { AutomationId = "DupId" })
            );
            Assert.Equal(GraftErrorCodes.ElementAmbiguous, ambiguous.Code);
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

        var clickCount = 0;
        var button = new Button
        {
            Content = "Click Me",
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetAutomationId(button, "SampleButton");
        button.Click += (_, _) =>
        {
            clickCount++;
            status.Text = $"Clicked {clickCount}";
        };

        var checkBox = new CheckBox { Content = "Off" };
        AutomationProperties.SetAutomationId(checkBox, "SampleCheckBox");
        checkBox.Checked += (_, _) => checkBox.Content = "On";
        checkBox.Unchecked += (_, _) => checkBox.Content = "Off";

        var mouseTarget = new Border
        {
            Background = System.Windows.Media.Brushes.LightGray,
            Height = 36,
            Focusable = true,
        };
        AutomationProperties.SetAutomationId(mouseTarget, "SampleMouseTarget");
        AutomationProperties.SetName(mouseTarget, "MouseReady");
        mouseTarget.MouseLeftButtonDown += (_, _) =>
        {
            status.Text = "MouseHit";
            AutomationProperties.SetName(mouseTarget, "MouseHit");
        };

        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(status);
        panel.Children.Add(textBox);
        panel.Children.Add(button);
        panel.Children.Add(checkBox);
        panel.Children.Add(mouseTarget);

        return new Window
        {
            Title = "Graft Sample WPF App",
            Width = 480,
            Height = 420,
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
