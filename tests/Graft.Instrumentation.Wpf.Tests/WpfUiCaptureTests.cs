using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
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
    /// GetTree, screenshot, resolve, invoke, setValue, and toggle succeed on a Sample-like window.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - STA thread; single Application for this method
    /// - Window with Sample controls is shown as MainWindow
    /// - WpfGraft.Use registered
    ///
    /// Steps:
    /// - Call GetTree / screenshot / resolve as before
    /// - Capture SampleButton clip, collapsed empty clip, open Popup opener+child clips, open ToolTip node clip, ancestor clip with ToolTip, window with open ToolTip, runtimeId after open ToolTip, window with open ContextMenu
    /// - Invoke SampleButton then GetTree StatusText
    /// - setValue SampleTextBox then GetTree name
    /// - Toggle SampleCheckBox then expect name On
    /// - Resolve SampleMouseTarget (SendInput click is covered by SampleWpfApp.Tests E2E)
    /// - Invoke a disabled button
    ///
    /// Expected:
    /// - SampleButton name/bounds and PNG signature as before
    /// - Element clip smaller than window; collapsed → notActionable; Popup/ToolTip/ContextMenu overlays have PNG signature
    /// - After invoke, StatusText name is "Clicked 1"
    /// - After setValue, SampleTextBox name matches the set text
    /// - After toggle, SampleCheckBox name is On
    /// - SampleMouseTarget resolves as Border
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

            var buttonClip = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "SampleButton" },
                }
            );
            Assert.Equal("png", buttonClip.Meta.Format);
            Assert.True(buttonClip.Meta.Width > 0);
            Assert.True(buttonClip.Meta.Height > 0);
            Assert.True(
                buttonClip.Meta.Width < capture.Meta.Width
                    || buttonClip.Meta.Height < capture.Meta.Height,
                "Element clip should be smaller than the window screenshot."
            );
            Assert.True(
                buttonClip.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on element clip."
            );

            var collapsed = new Button { Content = "Hidden", Visibility = Visibility.Collapsed };
            AutomationProperties.SetAutomationId(collapsed, "CollapsedShotButton");
            ((StackPanel)window.Content).Children.Add(collapsed);
            window.UpdateLayout();
            var emptyClip = Assert.Throws<ElementActionException>(() =>
                screenshotProvider.Capture(
                    new ScreenshotOptions
                    {
                        Selector = new ElementSelector { AutomationId = "CollapsedShotButton" },
                    }
                )
            );
            Assert.Equal(GraftErrorCodes.ElementNotActionable, emptyClip.Code);

            var popupHost = new Button
            {
                Content = "OpenPopup",
                Width = 80,
                Height = 32,
            };
            AutomationProperties.SetAutomationId(popupHost, "PopupShotOpener");
            var popupButton = new Button
            {
                Content = "InPopup",
                Width = 72,
                Height = 28,
            };
            AutomationProperties.SetAutomationId(popupButton, "PopupShotButton");
            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = popupHost,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                StaysOpen = true,
                Child = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    Padding = new Thickness(8),
                    Child = popupButton,
                },
            };
            var popupGrid = new Grid();
            popupGrid.Children.Add(popupHost);
            popupGrid.Children.Add(popup);
            ((StackPanel)window.Content).Children.Add(popupGrid);
            window.UpdateLayout();
            var openerClosed = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "PopupShotOpener" },
                }
            );
            popup.IsOpen = true;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            var openerClip = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "PopupShotOpener" },
                }
            );
            Assert.True(openerClip.Meta.Width > 0);
            Assert.True(openerClip.Meta.Height > 0);
            Assert.True(
                openerClip.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on opener clip with popup."
            );
            Assert.True(
                openerClip.Meta.Width > openerClosed.Meta.Width
                    || openerClip.Meta.Height > openerClosed.Meta.Height,
                "Open Popup targeting the opener should be composited into the opener clip."
            );
            var popupClip = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "PopupShotButton" },
                }
            );
            Assert.True(popupClip.Meta.Width > 0);
            Assert.True(popupClip.Meta.Height > 0);
            Assert.True(
                popupClip.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on popup clip."
            );

            var tipSection = new StackPanel { Margin = new Thickness(8) };
            AutomationProperties.SetAutomationId(tipSection, "TipShotSection");
            tipSection.Children.Add(new TextBlock { Text = "Tip section" });
            var tipHost = new Button { Content = "TipHost" };
            AutomationProperties.SetAutomationId(tipHost, "TipShotHost");
            var tip = new ToolTip { Content = "HelloTip" };
            tipHost.ToolTip = tip;
            tipSection.Children.Add(tipHost);
            ((StackPanel)window.Content).Children.Add(tipSection);
            window.UpdateLayout();
            tip.IsOpen = true;
            window.UpdateLayout();
            var treeWithTip = treeProvider.GetTree(new GetTreeOptions());
            var tipHostNode = FindByAutomationId(treeWithTip.Root, "TipShotHost");
            Assert.NotNull(tipHostNode);
            var tipNode = FindByControlType(tipHostNode, "ToolTip");
            Assert.NotNull(tipNode);
            Assert.Equal("HelloTip", tipNode.Name);
            var tipClip = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { RuntimeId = tipNode.RuntimeId },
                }
            );
            Assert.True(tipClip.Meta.Width > 0);
            Assert.True(tipClip.Meta.Height > 0);
            Assert.True(
                tipClip.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on tooltip clip."
            );
            var sectionClip = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "TipShotSection" },
                }
            );
            Assert.True(sectionClip.Meta.Width > 0);
            Assert.True(sectionClip.Meta.Height > 0);
            Assert.True(
                sectionClip.Meta.Width > tipClip.Meta.Width
                    || sectionClip.Meta.Height > tipClip.Meta.Height,
                "Ancestor clip should be larger than the tooltip composite."
            );
            Assert.True(
                sectionClip.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on ancestor clip with tooltip."
            );
            var windowWithTip = screenshotProvider.Capture(ScreenshotOptions.Default);
            Assert.True(windowWithTip.Meta.Width > 0);
            Assert.True(windowWithTip.Meta.Height > 0);
            Assert.True(
                windowWithTip.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on window screenshot with tooltip."
            );

            var afterTip = new Button { Content = "AfterTip" };
            AutomationProperties.SetAutomationId(afterTip, "AfterOpenTip");
            ((StackPanel)window.Content).Children.Add(afterTip);
            window.UpdateLayout();
            var treeAfterTip = treeProvider.GetTree(new GetTreeOptions());
            var afterTipNode = FindByAutomationId(treeAfterTip.Root, "AfterOpenTip");
            Assert.NotNull(afterTipNode);
            var afterById = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "AfterOpenTip" },
                }
            );
            var afterByRuntime = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { RuntimeId = afterTipNode.RuntimeId },
                }
            );
            Assert.Equal(afterById.Meta.Width, afterByRuntime.Meta.Width);
            Assert.Equal(afterById.Meta.Height, afterByRuntime.Meta.Height);

            var menuHost = new Button
            {
                Content = "MenuHost",
                Width = 96,
                Height = 32,
            };
            AutomationProperties.SetAutomationId(menuHost, "ContextMenuShotHost");
            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem { Header = "ShotPing" });
            menuHost.ContextMenu = contextMenu;
            ((StackPanel)window.Content).Children.Add(menuHost);
            window.UpdateLayout();
            var hostOnly = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "ContextMenuShotHost" },
                }
            );
            contextMenu.IsOpen = true;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            var hostWithMenu = screenshotProvider.Capture(
                new ScreenshotOptions
                {
                    Selector = new ElementSelector { AutomationId = "ContextMenuShotHost" },
                }
            );
            Assert.True(hostWithMenu.Meta.Width > 0);
            Assert.True(hostWithMenu.Meta.Height > 0);
            Assert.True(
                hostWithMenu.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on host clip with context menu."
            );
            Assert.True(
                hostWithMenu.Meta.Width > hostOnly.Meta.Width
                    || hostWithMenu.Meta.Height > hostOnly.Meta.Height,
                "Open ContextMenu should be composited with its host, not host-only."
            );
            var windowWithMenu = screenshotProvider.Capture(ScreenshotOptions.Default);
            Assert.True(windowWithMenu.Meta.Width > 0);
            Assert.True(windowWithMenu.Meta.Height > 0);
            Assert.True(
                windowWithMenu.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
                "Expected PNG signature on window screenshot with context menu."
            );
            contextMenu.IsOpen = false;

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

            // SendInput click needs a real foreground HWND; cover that in SampleWpfApp.Tests.
            var mouseResolved = resolver.Resolve(
                new ElementSelector { AutomationId = "SampleMouseTarget" }
            );
            Assert.IsType<Border>(mouseResolved.Target);

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

    private static TreeNode? FindByControlType(TreeNode node, string controlType)
    {
        if (string.Equals(node.ControlType, controlType, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindByControlType(child, controlType);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
