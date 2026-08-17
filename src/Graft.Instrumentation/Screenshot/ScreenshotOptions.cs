using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Screenshot;

#if GRAFT_TEST

/// <summary>
/// Options for <c>screenshot</c> capture (window PNG, or an element clip when
/// <see cref="Selector"/> is set).
/// </summary>
public sealed class ScreenshotOptions
{
    /// <summary>
    /// Gets the default options instance (target window, no element clip).
    /// </summary>
    public static ScreenshotOptions Default { get; } = new();

    /// <summary>
    /// Gets an optional element selector. When unset, the target window is captured.
    /// </summary>
    public ElementSelector? Selector { get; init; }

    /// <summary>
    /// Gets a value indicating whether an element clip was requested.
    /// </summary>
    public bool HasElementSelector =>
        Selector is not null
        && (!string.IsNullOrWhiteSpace(Selector.AutomationId) || Selector.RuntimeId is not null);
}

#endif
