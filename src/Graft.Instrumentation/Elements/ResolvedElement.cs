namespace Graft.Instrumentation.Elements;

#if GRAFT_TEST

/// <summary>
/// A live element resolved from the visual tree (framework-specific <see cref="Target"/>).
/// </summary>
public sealed class ResolvedElement
{
    /// <summary>
    /// Gets the framework-specific element (e.g. WPF <c>FrameworkElement</c>).
    /// </summary>
    public required object Target { get; init; }

    /// <summary>
    /// Gets the matched automation id.
    /// </summary>
    public required string AutomationId { get; init; }

    /// <summary>
    /// Gets the runtime id assigned during the resolve walk (same scheme as <c>getTree</c>).
    /// </summary>
    public int RuntimeId { get; init; }

    /// <summary>
    /// Gets the framework control type name (e.g. <c>Button</c>).
    /// </summary>
    public required string ControlType { get; init; }
}

#endif
