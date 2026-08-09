using Graft.Core.Diagnostics;

namespace Graft.Core;

/// <summary>
/// Controller-side failure carrying a stable <see cref="Graft.Protocol.GraftErrorCodes"/> value.
/// </summary>
public sealed class GraftException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraftException"/> class.
    /// </summary>
    /// <param name="code">Stable error code (wire / Core vocabulary).</param>
    /// <param name="message">Human-readable message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public GraftException(string code, string message, Exception? innerException = null)
        : this(code, message, report: null, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraftException"/> class with diagnostics.
    /// </summary>
    /// <param name="code">Stable error code (wire / Core vocabulary).</param>
    /// <param name="message">Human-readable message.</param>
    /// <param name="report">Optional structured failure report (Core Expect / action failures).</param>
    /// <param name="innerException">Optional inner exception.</param>
    public GraftException(
        string code,
        string message,
        FailureReport? report,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
        Report = report;
    }

    /// <summary>
    /// Gets the stable error code (see <see cref="Graft.Protocol.GraftErrorCodes"/>).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the structured failure report when Core attached one; otherwise <see langword="null"/>.
    /// </summary>
    public FailureReport? Report { get; }
}
