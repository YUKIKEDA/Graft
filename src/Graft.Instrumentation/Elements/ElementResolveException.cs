namespace Graft.Instrumentation.Elements;

#if GRAFT_TEST

/// <summary>
/// Element resolution failed with a stable Graft error code.
/// </summary>
public sealed class ElementResolveException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementResolveException"/> class.
    /// </summary>
    /// <param name="code">Wire error code (e.g. <c>element.notFound</c>).</param>
    /// <param name="message">Human-readable message.</param>
    public ElementResolveException(string code, string message)
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
