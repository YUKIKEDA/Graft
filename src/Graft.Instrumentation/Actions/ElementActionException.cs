namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// An element action failed with a stable Graft error code.
/// </summary>
public sealed class ElementActionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementActionException"/> class.
    /// </summary>
    /// <param name="code">Wire error code (e.g. <c>element.notActionable</c>).</param>
    /// <param name="message">Human-readable message.</param>
    public ElementActionException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>
    /// Gets the Graft error code.
    /// </summary>
    public string Code { get; }
}

#endif
