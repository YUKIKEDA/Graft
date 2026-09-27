namespace Graft.Protocol;

/// <summary>
/// Thrown when <see cref="SelectorScoring"/> cannot choose one node.
/// </summary>
public sealed class SelectorMatchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectorMatchException"/> class.
    /// </summary>
    /// <param name="code">Graft error code (<see cref="GraftErrorCodes"/>).</param>
    /// <param name="message">Explanation.</param>
    public SelectorMatchException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Gets the Graft error code.
    /// </summary>
    public string Code { get; }
}
