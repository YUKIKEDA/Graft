using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Walks the WPF visual tree into Protocol <see cref="TreeNode"/> graphs or live element matches.
/// </summary>
internal static class WpfVisualTreeWalker
{
    /// <summary>
    /// Captures <paramref name="root"/> and descendants with depth/node limits.
    /// </summary>
    /// <param name="root">Window used as tree root and bounds origin.</param>
    /// <param name="options">Capture limits.</param>
    /// <returns>Tree result including truncation flag.</returns>
    public static GetTreeResult Capture(Window root, GetTreeOptions options)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(options);

        var state = new WalkState(options);
        var boundsOrigin = root.Content as Visual ?? root;
        var treeRoot = BuildNode(root, root, boundsOrigin, depth: 0, state);

        return new GetTreeResult { Root = treeRoot, Truncated = state.Truncated };
    }

    /// <summary>
    /// Resolves a live <see cref="FrameworkElement"/> using the same visual-tree walk as <see cref="Capture"/>.
    /// </summary>
    /// <param name="root">Window to search.</param>
    /// <param name="selector">automationId required; runtimeId optional.</param>
    /// <returns>The unique match.</returns>
    /// <exception cref="ElementResolveException">Invalid selector, not found, or ambiguous.</exception>
    public static ResolvedElement Resolve(Window root, ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        if (string.IsNullOrWhiteSpace(selector.AutomationId))
        {
            throw new ElementResolveException(
                GraftErrorCodes.SelectorInvalid,
                "params.automationId is required."
            );
        }

        var automationId = selector.AutomationId.Trim();

        // Resolve walks without GetTree truncation so invoke targets are not missed.
        var state = new WalkState(
            new GetTreeOptions { MaxDepth = int.MaxValue / 4, MaxNodes = int.MaxValue / 4 }
        );
        var matches = new List<(FrameworkElement Element, int RuntimeId)>();
        CollectMatches(root, depth: 0, state, automationId, selector.RuntimeId, matches);

        if (matches.Count == 0)
        {
            throw new ElementResolveException(
                GraftErrorCodes.ElementNotFound,
                $"No element matched automationId '{automationId}'."
            );
        }

        if (matches.Count > 1)
        {
            throw new ElementResolveException(
                GraftErrorCodes.ElementAmbiguous,
                $"Multiple elements matched automationId '{automationId}' ({matches.Count})."
            );
        }

        var (element, runtimeId) = matches[0];
        return new ResolvedElement
        {
            Target = element,
            AutomationId = automationId,
            RuntimeId = runtimeId,
            ControlType = element.GetType().Name,
        };
    }

    private static void CollectMatches(
        FrameworkElement element,
        int depth,
        WalkState state,
        string automationId,
        int? runtimeIdFilter,
        List<(FrameworkElement Element, int RuntimeId)> matches
    )
    {
        state.NodeCount++;
        var runtimeId = state.NextRuntimeId++;
        var elementAutomationId = AutomationProperties.GetAutomationId(element) ?? string.Empty;
        if (
            string.Equals(elementAutomationId, automationId, StringComparison.Ordinal)
            && (runtimeIdFilter is null || runtimeIdFilter == runtimeId)
        )
        {
            matches.Add((element, runtimeId));
        }

        CollectFrameworkChildren(
            element,
            depth + 1,
            state,
            child => CollectMatches(child, depth + 1, state, automationId, runtimeIdFilter, matches)
        );
    }

    private static TreeNode BuildNode(
        FrameworkElement element,
        Window window,
        Visual boundsOrigin,
        int depth,
        WalkState state
    )
    {
        state.NodeCount++;
        var runtimeId = state.NextRuntimeId++;
        var children = new List<TreeNode>();
        CollectFrameworkChildren(
            element,
            depth + 1,
            state,
            child => children.Add(BuildNode(child, window, boundsOrigin, depth + 1, state))
        );

        return new TreeNode
        {
            RuntimeId = runtimeId,
            ControlType = element.GetType().Name,
            Name = ResolveName(element),
            AutomationId = AutomationProperties.GetAutomationId(element) ?? string.Empty,
            Bounds = ResolveBounds(element, window, boundsOrigin),
            Enabled = element.IsEnabled,
            Visible = element.IsVisible,
            Focused = element.IsFocused,
            Selected = ResolveSelected(element),
            Expanded = ResolveExpanded(element),
            Checked = ResolveChecked(element),
            Value = ResolveValue(element),
            Children = children,
        };
    }

    private static bool? ResolveSelected(FrameworkElement element) =>
        element switch
        {
            // ComboBoxItem inherits ListBoxItem — one arm covers both.
            ListBoxItem listItem => listItem.IsSelected,
            TreeViewItem treeItem => treeItem.IsSelected,
            TabItem tabItem => tabItem.IsSelected,
            DataGridRow row => row.IsSelected,
            _ => null,
        };

    private static bool? ResolveExpanded(FrameworkElement element) =>
        element switch
        {
            TreeViewItem treeItem => treeItem.IsExpanded,
            Expander expander => expander.IsExpanded,
            _ => null,
        };

    private static bool? ResolveChecked(FrameworkElement element) =>
        element switch
        {
            CheckBox checkBox => checkBox.IsChecked,
            _ => null,
        };

    private static string? ResolveValue(FrameworkElement element) =>
        element switch
        {
            RangeBase range => range.Value.ToString("G", CultureInfo.InvariantCulture),
            _ => null,
        };

    private static void CollectFrameworkChildren(
        DependencyObject parent,
        int childDepth,
        WalkState state,
        Action<FrameworkElement> onFrameworkChild
    )
    {
        // Apply before FE / non-FE branching so non-FrameworkElement chains cannot
        // recurse past MaxDepth (they are flattened at the same childDepth).
        if (childDepth > state.Options.MaxDepth)
        {
            state.Truncated = true;
            return;
        }

        // Open ContextMenu: walk MenuItem containers via Items (Popup is outside owner visuals).
        // Avoid VisualTreeHelper here — it often rediscovers the same MenuItem instances.
        if (parent is ContextMenu { IsOpen: true } openMenu)
        {
            WalkMenuItems(openMenu, state, onFrameworkChild);
            return;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            if (state.NodeCount >= state.Options.MaxNodes)
            {
                state.Truncated = true;
                return;
            }

            var child = VisualTreeHelper.GetChild(parent, i);

            // Popup content is walked via owner ContextMenu / MenuItem.IsSubmenuOpen (avoids duplicates).
            if (child is Popup)
            {
                continue;
            }

            if (child is FrameworkElement frameworkChild)
            {
                onFrameworkChild(frameworkChild);
            }
            else
            {
                // Non-FrameworkElement visuals are skipped as nodes but children are flattened.
                CollectFrameworkChildren(child, childDepth, state, onFrameworkChild);
            }
        }

        // Open ContextMenu lives in a Popup (not under the owner's visual children).
        if (
            parent is FrameworkElement { ContextMenu: { IsOpen: true } menu }
            && state.NodeCount < state.Options.MaxNodes
        )
        {
            onFrameworkChild(menu);
        }
        else if (
            parent is FrameworkElement { ContextMenu: { IsOpen: true } }
            && state.NodeCount >= state.Options.MaxNodes
        )
        {
            state.Truncated = true;
        }

        // Open Menu bar submenu also lives in a Popup (Phase 20).
        if (parent is MenuItem { IsSubmenuOpen: true } openSubmenu)
        {
            WalkMenuItems(openSubmenu, state, onFrameworkChild);
        }
    }

    private static void WalkMenuItems(
        ItemsControl menu,
        WalkState state,
        Action<FrameworkElement> onFrameworkChild
    )
    {
        foreach (var item in menu.Items)
        {
            if (state.NodeCount >= state.Options.MaxNodes)
            {
                state.Truncated = true;
                return;
            }

            var container =
                item as FrameworkElement
                ?? menu.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            if (container is not null)
            {
                onFrameworkChild(container);
            }
        }
    }

    private static string ResolveName(FrameworkElement element)
    {
        var automationName = AutomationProperties.GetName(element);
        if (!string.IsNullOrEmpty(automationName))
        {
            return automationName;
        }

        return element switch
        {
            Window window => window.Title ?? string.Empty,
            Button button when button.Content is string text => text,
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            TextBox textBox => textBox.Text ?? string.Empty,
            MenuItem { Header: string menuHeader } => menuHeader,
            HeaderedItemsControl { Header: string itemsHeader } => itemsHeader,
            HeaderedContentControl { Header: string header } => header,
            ContentControl { Content: string content } => content,
            _ => element.Name ?? string.Empty,
        };
    }

    private static ElementBounds ResolveBounds(
        FrameworkElement element,
        Window window,
        Visual boundsOrigin
    )
    {
        if (ReferenceEquals(element, window))
        {
            return new ElementBounds
            {
                X = 0,
                Y = 0,
                Width = window.ActualWidth,
                Height = window.ActualHeight,
            };
        }

        if (!element.IsLoaded || (element.ActualWidth <= 0 && element.ActualHeight <= 0))
        {
            return new ElementBounds();
        }

        try
        {
            var transform = element.TransformToVisual(boundsOrigin);
            var rect = transform.TransformBounds(
                new Rect(0, 0, element.ActualWidth, element.ActualHeight)
            );
            return new ElementBounds
            {
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
            };
        }
        catch (InvalidOperationException)
        {
            return new ElementBounds();
        }
    }

    private sealed class WalkState(GetTreeOptions options)
    {
        public GetTreeOptions Options { get; } = options;

        public int NextRuntimeId { get; set; } = 1;

        public int NodeCount { get; set; }

        public bool Truncated { get; set; }
    }
}
