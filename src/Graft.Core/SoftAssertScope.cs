using Graft.Core.Diagnostics;
using Graft.Protocol;

namespace Graft.Core;

/// <summary>
/// Collects <see cref="GraftException"/> failures and reports them together on dispose.
/// </summary>
/// <remarks>
/// <see cref="Check"/> awaits one operation. A <see cref="GraftException"/> is stored and the
/// next check still runs. Any other exception propagates immediately.
/// <see cref="DisposeAsync"/> throws one <see cref="GraftException"/> with code
/// <see cref="GraftErrorCodes.ExpectFailed"/> when at least one failure was stored.
/// The aggregate <see cref="FailureReport"/> uses step <see cref="FailureSteps.SoftAssert"/>
/// and lists each report under <c>failures</c>. Expect calls outside a check still fail at once.
/// </remarks>
public sealed class SoftAssertScope : IAsyncDisposable
{
    private readonly List<GraftException> _failures = [];
    private bool _disposed;

    /// <summary>
    /// Gets the number of <see cref="GraftException"/> failures stored so far.
    /// </summary>
    public int FailureCount => _failures.Count;

    /// <summary>
    /// Awaits <paramref name="operation"/> and stores a <see cref="GraftException"/> instead of throwing it.
    /// </summary>
    /// <param name="operation">One Expect, Wait, or action.</param>
    /// <returns>A task that completes when the operation finishes or its Graft failure is stored.</returns>
    /// <exception cref="ObjectDisposedException">This scope was already disposed.</exception>
    public async Task Check(Task operation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(operation);
        try
        {
            await operation.ConfigureAwait(false);
        }
        catch (GraftException ex)
        {
            _failures.Add(ex);
        }
    }

    /// <summary>
    /// Throws the aggregate failure when this scope stored any, then marks the scope disposed.
    /// </summary>
    /// <returns>A completed task when nothing failed.</returns>
    /// <exception cref="GraftException">One or more checks failed.</exception>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        if (_failures.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        throw Build();
    }

    private GraftException Build()
    {
        var reports = new List<FailureReport>(_failures.Count);
        foreach (var failure in _failures)
        {
            reports.Add(failure.Report ?? Fallback(failure));
        }

        var lines = new string[_failures.Count + 1];
        lines[0] = _failures.Count == 1 ? "1 soft assertion failed." : $"{_failures.Count} soft assertions failed.";
        for (var index = 0; index < _failures.Count; index++)
        {
            lines[index + 1] = Describe(index + 1, reports[index]);
        }

        return new GraftException(
            GraftErrorCodes.ExpectFailed,
            string.Join(Environment.NewLine, lines),
            new FailureReport
            {
                Step = FailureSteps.SoftAssert,
                Expected = "0 failures",
                Actual = $"{_failures.Count} failures",
                TimedOut = reports.Exists(static report => report.TimedOut),
                Selector = new FailureReportSelector(),
                Failures = reports,
            },
            _failures[0]
        );
    }

    private static string Describe(int number, FailureReport report)
    {
        if (report.Expected is not null || report.Actual is not null)
        {
            return $"[{number}] {report.Step}: expected '{report.Expected}', actual '{report.Actual}'";
        }

        return $"[{number}] {report.Step}";
    }

    private static FailureReport Fallback(GraftException failure) =>
        new()
        {
            Step = FailureSteps.SoftAssert,
            Expected = failure.Code,
            Actual = failure.Message,
            Selector = new FailureReportSelector(),
        };
}
