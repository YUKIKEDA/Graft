using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
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
    /// Resolves a live element using the same visual-tree walk as <see cref="Capture"/>.
    /// </summary>
    /// <param name="root">Window to search.</param>
    /// <param name="selector">automationId required; runtimeId optional.</param>
    /// <returns>The unique match.</returns>
    /// <exception cref="ElementResolveException">Invalid selector, not found, or ambiguous.</exception>
    public static ResolvedElement Resolve(Window root, ElementSelector selector) =>
        ResolveCore(root, selector, requireAutomationId: true);

    /// <summary>
    /// Resolves a live element for screenshot: <c>automationId</c> and/or <c>runtimeId</c>.
    /// </summary>
    /// <param name="root">Window to search.</param>
    /// <param name="selector">automationId and/or runtimeId.</param>
    /// <returns>The unique match.</returns>
    /// <exception cref="ElementResolveException">Invalid selector, not found, or ambiguous.</exception>
    public static ResolvedElement ResolveForScreenshot(Window root, ElementSelector selector) =>
        ResolveCore(root, selector, requireAutomationId: false);

    private static ResolvedElement ResolveCore(
        Window root,
        ElementSelector selector,
        bool requireAutomationId
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        var automationId = string.IsNullOrWhiteSpace(selector.AutomationId)
            ? null
            : selector.AutomationId.Trim();

        if (requireAutomationId && automationId is null)
        {
            throw new ElementResolveException(
                GraftErrorCodes.SelectorInvalid,
                "params.automationId is required."
            );
        }

        if (!requireAutomationId && automationId is null && selector.RuntimeId is null)
        {
            throw new ElementResolveException(
                GraftErrorCodes.SelectorInvalid,
                "params.automationId or params.runtimeId is required."
            );
        }

        // runtimeId is assigned by getTree (default depth/maxNodes). An untruncated
        // walk renumbers later nodes and would screenshot the wrong visual.
        var walkOptions = selector.RuntimeId is not null
            ? new GetTreeOptions()
            : new GetTreeOptions { MaxDepth = int.MaxValue / 4, MaxNodes = int.MaxValue / 4 };
        var state = new WalkState(walkOptions);
        var matches = new List<(object Target, int RuntimeId, string ControlType)>();
        CollectMatches(root, depth: 0, state, automationId, selector.RuntimeId, matches);

        if (matches.Count == 0)
        {
            var detail = automationId is not null
                ? $"automationId '{automationId}'"
                : $"runtimeId {selector.RuntimeId}";
            throw new ElementResolveException(
                GraftErrorCodes.ElementNotFound,
                $"No element matched {detail}."
            );
        }

        if (matches.Count > 1)
        {
            var detail = automationId is not null
                ? $"automationId '{automationId}'"
                : $"runtimeId {selector.RuntimeId}";
            throw new ElementResolveException(
                GraftErrorCodes.ElementAmbiguous,
                $"Multiple elements matched {detail} ({matches.Count})."
            );
        }

        var (target, runtimeId, controlType) = matches[0];
        return new ResolvedElement
        {
            Target = target,
            AutomationId = automationId ?? string.Empty,
            RuntimeId = runtimeId,
            ControlType = controlType,
        };
    }

    private static void CollectMatches(
        FrameworkElement element,
        int depth,
        WalkState state,
        string? automationId,
        int? runtimeIdFilter,
        List<(object Target, int RuntimeId, string ControlType)> matches
    )
    {
        state.NodeCount++;
        var runtimeId = state.NextRuntimeId++;
        var elementAutomationId = AutomationProperties.GetAutomationId(element) ?? string.Empty;
        if (IsMatch(elementAutomationId, automationId, runtimeId, runtimeIdFilter))
        {
            matches.Add((element, runtimeId, element.GetType().Name));
        }

        CollectFrameworkChildren(
            element,
            depth + 1,
            state,
            child => CollectMatches(child, depth + 1, state, automationId, runtimeIdFilter, matches)
        );

        CollectHyperlinkMatches(element, state, automationId, runtimeIdFilter, matches);
        CollectOpenToolTipMatches(element, depth, state, automationId, runtimeIdFilter, matches);
    }

    private static void CollectHyperlinkMatches(
        FrameworkElement element,
        WalkState state,
        string? automationId,
        int? runtimeIdFilter,
        List<(object Target, int RuntimeId, string ControlType)> matches
    )
    {
        if (element is not TextBlock textBlock)
        {
            return;
        }

        foreach (var hyperlink in EnumerateHyperlinks(textBlock.Inlines))
        {
            if (state.NodeCount >= state.Options.MaxNodes)
            {
                state.Truncated = true;
                return;
            }

            state.NodeCount++;
            var runtimeId = state.NextRuntimeId++;
            var linkId = AutomationProperties.GetAutomationId(hyperlink) ?? string.Empty;
            if (IsMatch(linkId, automationId, runtimeId, runtimeIdFilter))
            {
                matches.Add((hyperlink, runtimeId, nameof(Hyperlink)));
            }
        }
    }

    private static void CollectOpenToolTipMatches(
        FrameworkElement element,
        int depth,
        WalkState state,
        string? automationId,
        int? runtimeIdFilter,
        List<(object Target, int RuntimeId, string ControlType)> matches
    )
    {
        if (element.ToolTip is not ToolTip { IsOpen: true } toolTip)
        {
            return;
        }

        CollectMatches(toolTip, depth + 1, state, automationId, runtimeIdFilter, matches);
    }

    private static bool IsMatch(
        string elementAutomationId,
        string? automationId,
        int runtimeId,
        int? runtimeIdFilter
    )
    {
        var idMatches =
            automationId is null
            || string.Equals(elementAutomationId, automationId, StringComparison.Ordinal);
        var runtimeMatches = runtimeIdFilter is null || runtimeIdFilter == runtimeId;
        return idMatches && runtimeMatches;
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

        AppendHyperlinkChildren(element, window, boundsOrigin, children, state);
        AppendOpenToolTipChild(element, window, boundsOrigin, children, state, depth);

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
            ToolTip = ResolveToolTip(element),
            Children = children,
        };
    }

    private static void AppendHyperlinkChildren(
        FrameworkElement element,
        Window window,
        Visual boundsOrigin,
        List<TreeNode> children,
        WalkState state
    )
    {
        if (element is not TextBlock textBlock)
        {
            return;
        }

        var parentBounds = ResolveBounds(element, window, boundsOrigin);
        foreach (var hyperlink in EnumerateHyperlinks(textBlock.Inlines))
        {
            if (state.NodeCount >= state.Options.MaxNodes)
            {
                state.Truncated = true;
                return;
            }

            state.NodeCount++;
            var runtimeId = state.NextRuntimeId++;
            children.Add(
                new TreeNode
                {
                    RuntimeId = runtimeId,
                    ControlType = nameof(Hyperlink),
                    Name = ResolveHyperlinkName(hyperlink),
                    AutomationId = AutomationProperties.GetAutomationId(hyperlink) ?? string.Empty,
                    Bounds = parentBounds,
                    Enabled = hyperlink.IsEnabled,
                    Visible = textBlock.IsVisible,
                    Focused = false,
                    Children = Array.Empty<TreeNode>(),
                }
            );
        }
    }

    private static void AppendOpenToolTipChild(
        FrameworkElement element,
        Window window,
        Visual boundsOrigin,
        List<TreeNode> children,
        WalkState state,
        int depth
    )
    {
        if (element.ToolTip is not ToolTip { IsOpen: true } toolTip)
        {
            return;
        }

        if (state.NodeCount >= state.Options.MaxNodes)
        {
            state.Truncated = true;
            return;
        }

        children.Add(BuildNode(toolTip, window, boundsOrigin, depth + 1, state));
    }

    private static IEnumerable<Hyperlink> EnumerateHyperlinks(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Hyperlink hyperlink:
                    yield return hyperlink;
                    break;
                case Span span:
                    foreach (var nested in EnumerateHyperlinks(span.Inlines))
                    {
                        yield return nested;
                    }

                    break;
            }
        }
    }

    private static string ResolveHyperlinkName(Hyperlink hyperlink)
    {
        var automationName = AutomationProperties.GetName(hyperlink);
        if (!string.IsNullOrEmpty(automationName))
        {
            return automationName;
        }

        var builder = new StringBuilder();
        foreach (var inline in hyperlink.Inlines)
        {
            if (inline is Run run)
            {
                builder.Append(run.Text);
            }
        }

        return builder.ToString();
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
            ComboBox comboBox => comboBox.IsDropDownOpen,
            _ => null,
        };

    private static bool? ResolveChecked(FrameworkElement element) =>
        element switch
        {
            // CheckBox / RadioButton / ToggleButton
            ToggleButton toggle => toggle.IsChecked,
            _ => null,
        };

    private static string? ResolveValue(FrameworkElement element) =>
        element switch
        {
            RangeBase range => range.Value.ToString("G", CultureInfo.InvariantCulture),
            RichTextBox richTextBox => ReadRichTextPlain(richTextBox),
            DatePicker { SelectedDate: { } date } => date.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),

            // PasswordBox intentionally omitted (Phase 29a: do not expose Password over the tree).
            _ => null,
        };

    private static string? ResolveToolTip(FrameworkElement element)
    {
        if (element.ToolTip is ToolTip { IsOpen: true } toolTip)
        {
            return ReadToolTipContent(toolTip.Content);
        }

        return null;
    }

    private static string ReadToolTipContent(object? content) =>
        content switch
        {
            null => string.Empty,
            string text => text,
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            TextBox textBox => textBox.Text ?? string.Empty,
            ContentControl { Content: string text } => text,
            ContentControl contentControl => ReadToolTipContent(contentControl.Content),
            _ => Convert.ToString(content, CultureInfo.InvariantCulture) ?? string.Empty,
        };

    private static string ReadRichTextPlain(RichTextBox richTextBox)
    {
        var text = new TextRange(
            richTextBox.Document.ContentStart,
            richTextBox.Document.ContentEnd
        ).Text;
        return text.TrimEnd('\r', '\n');
    }

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

        // Open Menu bar submenu: same idea — Items walk only. Visual/Popup walk would
        // duplicate MenuItem nodes (Phase 20 + Phase 29b open-Popup merge).
        if (parent is MenuItem { IsSubmenuOpen: true } openSubmenu)
        {
            WalkMenuItems(openSubmenu, state, onFrameworkChild);
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

            // Closed Popup skipped; open Popup Child is merged (Phase 29b C05).
            // ContextMenu / Menu submenu content still walked via owner paths below.
            if (child is Popup popup)
            {
                if (
                    popup.IsOpen
                    && popup.Child is FrameworkElement popupChild
                    && state.NodeCount < state.Options.MaxNodes
                )
                {
                    onFrameworkChild(popupChild);
                }

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
