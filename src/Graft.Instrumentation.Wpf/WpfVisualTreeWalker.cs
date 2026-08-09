using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Graft.Instrumentation.Tree;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Walks the WPF visual tree into Protocol <see cref="TreeNode"/> graphs.
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
        CollectFrameworkChildren(element, window, boundsOrigin, depth + 1, state, children);

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
            Children = children,
        };
    }

    private static void CollectFrameworkChildren(
        DependencyObject parent,
        Window window,
        Visual boundsOrigin,
        int childDepth,
        WalkState state,
        List<TreeNode> sink
    )
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            if (state.NodeCount >= state.Options.MaxNodes)
            {
                state.Truncated = true;
                return;
            }

            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement frameworkChild)
            {
                if (childDepth > state.Options.MaxDepth)
                {
                    state.Truncated = true;
                    continue;
                }

                sink.Add(BuildNode(frameworkChild, window, boundsOrigin, childDepth, state));
            }
            else
            {
                // Non-FrameworkElement visuals are skipped as nodes but children are flattened.
                CollectFrameworkChildren(child, window, boundsOrigin, childDepth, state, sink);
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
