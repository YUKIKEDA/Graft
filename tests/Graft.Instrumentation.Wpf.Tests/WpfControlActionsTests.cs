using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;

namespace Graft.Instrumentation.Wpf.Tests;

/// <summary>
/// Guards per-control-type handlers that run before the built-in WPF action paths.
/// </summary>
public sealed class WpfControlActionsTests
{
    /// <summary>
    /// Registration rejects a null handler before storing it.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - No window is required
    ///
    /// Steps:
    /// - RegisterInvoke, RegisterSetValue, and RegisterToggle with a null handler
    ///
    /// Expected:
    /// - Each call throws ArgumentNullException
    /// </remarks>
    [Fact]
    public void Register_RejectsNullHandler()
    {
        Assert.Throws<ArgumentNullException>(() => WpfControlActions.RegisterInvoke<Button>(null!));
        Assert.Throws<ArgumentNullException>(() => WpfControlActions.RegisterSetValue<Button>(null!));
        Assert.Throws<ArgumentNullException>(() => WpfControlActions.RegisterToggle<Button>(null!));
    }

    /// <summary>
    /// Runs the registry checks on the single WPF <see cref="Application"/> owned by <see cref="WpfUiCaptureTests"/>.
    /// </summary>
    /// <param name="window">Shown window whose content is a <see cref="StackPanel"/>.</param>
    internal static void Exercise(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var panel = (StackPanel)window.Content;
        var grid = new VendorGrid { Width = 80, Height = 24 };
        AutomationProperties.SetAutomationId(grid, "VendorGrid");
        var status = new TextBlock { Text = "Ready" };
        AutomationProperties.SetAutomationId(status, "HandlerStatus");
        var button = new Button { Content = "Go" };
        AutomationProperties.SetAutomationId(button, "HandlerButton");
        button.Click += (_, _) => status.Text = "Clicked";
        panel.Children.Add(grid);
        panel.Children.Add(status);
        panel.Children.Add(button);
        window.UpdateLayout();

        var baseInvokes = 0;
        WpfControlActions.RegisterInvoke<Control>(_ =>
        {
            baseInvokes++;
            return true;
        });
        WpfControlActions.RegisterInvoke<VendorGrid>(target =>
        {
            target.Invokes++;
            return true;
        });
        WpfControlActions.RegisterSetValue<VendorGrid>(
            (target, value) =>
            {
                target.Value = value;
                return true;
            }
        );
        WpfControlActions.RegisterToggle<VendorGrid>(target =>
        {
            target.Toggles++;
            return true;
        });

        var invoker = AgentServices.ElementInvoker ?? throw new InvalidOperationException("Element invoker was not registered.");
        var setter = AgentServices.ElementValueSetter ?? throw new InvalidOperationException("Element value setter was not registered.");
        var toggler = AgentServices.ElementToggler ?? throw new InvalidOperationException("Element toggler was not registered.");
        var selector = new ElementSelector { AutomationId = "VendorGrid" };
        invoker.Invoke(selector);
        setter.SetValue(selector, "42");
        toggler.Toggle(selector);

        Assert.Equal(1, grid.Invokes);
        Assert.Equal(0, baseInvokes);
        Assert.Equal("42", grid.Value);
        Assert.Equal(1, grid.Toggles);

        WpfControlActions.Clear();
        WpfControlActions.RegisterInvoke<Control>(_ =>
        {
            baseInvokes++;
            return true;
        });
        invoker.Invoke(selector);
        Assert.Equal(1, baseInvokes);
        Assert.Equal(1, grid.Invokes);

        WpfControlActions.Clear();
        WpfControlActions.RegisterInvoke<Button>(_ => false);
        invoker.Invoke(new ElementSelector { AutomationId = "HandlerButton" });
        Assert.Equal("Clicked", status.Text);

        status.Text = "Ready";
        WpfControlActions.RegisterInvoke<Button>(_ => true);
        invoker.Invoke(new ElementSelector { AutomationId = "HandlerButton" });
        Assert.Equal("Ready", status.Text);
        WpfControlActions.Clear();
    }

    private sealed class VendorGrid : Control
    {
        public int Invokes { get; set; }

        public string? Value { get; set; }

        public int Toggles { get; set; }
    }
}
