using Graft.Protocol;

namespace Graft.Instrumentation.Pipe;

#if GRAFT_TEST

/// <summary>
/// Signals that request params failed validation.
/// </summary>
/// <remarks>
/// The wire code stays <see cref="GraftErrorCodes.SelectorInvalid"/>. Readers that treat a bad payload as a default still catch this type.
/// </remarks>
internal sealed class InvalidParamsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidParamsException"/> class.
    /// </summary>
    /// <param name="message">Human-readable message.</param>
    public InvalidParamsException(string message)
        : base(message) { }
}

#endif
