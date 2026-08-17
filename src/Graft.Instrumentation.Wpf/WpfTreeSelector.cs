using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Walks TreeView AutomationId paths on the UI dispatcher.
/// </summary>
internal sealed class WpfTreeSelector : ITreeSelector
{
    /// <inheritdoc />
    public void SelectTree(ElementSelector selector, string path)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var segments = SplitPath(path);
        if (segments.Length == 0)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.path must contain at least one AutomationId segment.");
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot selectTree.");
        }

        if (dispatcher.CheckAccess())
        {
            SelectTreeOnUiThread(selector, path, segments);
            return;
        }

        dispatcher.Invoke(() => SelectTreeOnUiThread(selector, path, segments), DispatcherPriority.Normal);
    }

    private static void SelectTreeOnUiThread(ElementSelector selector, string path, string[] segments)
    {
        var root = ResolveTreeRoot(selector);
        ItemsControl current = root;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var item = FindTreeItem(current, segment, path);
            if (!item.IsEnabled)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotActionable,
                    $"Tree item '{segment}' is not actionable (enabled=False) for path '{path}'."
                );
            }

            var isLast = i == segments.Length - 1;
            if (isLast)
            {
                item.IsSelected = true;
                item.BringIntoView();
                item.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
                return;
            }

            if (!item.HasItems)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"Tree item '{segment}' has no children; cannot continue path '{path}'."
                );
            }

            item.IsExpanded = true;
            item.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            current = item;
        }
    }

    private static TreeView ResolveTreeRoot(ElementSelector selector)
    {
        var resolver =
            AgentServices.ElementResolver
            ?? throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "No element resolver is registered. Call WpfGraft.Use() before Agent.Start()."
            );

        var resolved = resolver.Resolve(selector);
        if (resolved.Target is not FrameworkElement element)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Resolved target is not a FrameworkElement (got {resolved.Target.GetType().Name})."
            );
        }

        if (element is not TreeView treeView)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"selectTree root must be TreeView (got {element.GetType().Name}).");
        }

        if (!treeView.IsEnabled || !treeView.IsVisible)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{resolved.AutomationId}' is not actionable (enabled={treeView.IsEnabled}, visible={treeView.IsVisible})."
            );
        }

        return treeView;
    }

    private static TreeViewItem FindTreeItem(ItemsControl parent, string automationId, string path)
    {
        foreach (var item in parent.Items)
        {
            var container = item as TreeViewItem ?? parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container is null)
            {
                continue;
            }

            var id = AutomationProperties.GetAutomationId(container);
            if (string.Equals(id, automationId, StringComparison.Ordinal))
            {
                return container;
            }
        }

        throw new ElementResolveException(
            GraftErrorCodes.ElementNotFound,
            $"Tree item '{automationId}' was not found under current node for path '{path}'."
        );
    }

    private static string[] SplitPath(string path)
    {
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
