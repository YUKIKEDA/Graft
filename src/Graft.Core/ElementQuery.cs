using Graft.Core.Diagnostics;
using Graft.Core.Selectors;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core;

/// <summary>
/// Lazy element query: wait + resolve against getTree, then act or expect.
/// </summary>
public sealed class ElementQuery
{
    private readonly AgentConnection _connection;
    private readonly Selector _selector;
    private readonly WaitOptions _waitOptions;

    internal ElementQuery(AgentConnection connection, Selector selector, WaitOptions waitOptions)
    {
        _connection = connection;
        _selector = selector;
        _waitOptions = waitOptions;
    }

    /// <summary>
    /// Waits until the element is present and actionable, then invokes it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when invoke succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or invoke failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task InvokeAsync(CancellationToken cancellationToken = default)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw CreateFailure(
                GraftErrorCodes.ActionFailed,
                "Resolved element has no automationId; cannot invoke over the wire.",
                FailureSteps.Invoke
            );
        }

        try
        {
            await _connection
                .InvokeAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw CreateFailure(ex.Code, ex.Message, FailureSteps.Invoke, innerException: ex);
        }
    }

    /// <summary>
    /// Waits until the element is present and actionable, then replaces its value.
    /// </summary>
    /// <param name="value">Replacement text (empty string clears).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setValue succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or setValue failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task SetValueAsync(string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw CreateFailure(
                GraftErrorCodes.ActionFailed,
                "Resolved element has no automationId; cannot setValue over the wire.",
                FailureSteps.SetValue
            );
        }

        try
        {
            await _connection
                .SetValueAsync(node.AutomationId, value, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw CreateFailure(
                ex.Code,
                ex.Message,
                FailureSteps.SetValue,
                expected: value,
                innerException: ex
            );
        }
    }

    /// <summary>
    /// Waits until the element's <c>name</c> equals <paramref name="expectedName"/>.
    /// </summary>
    /// <param name="expectedName">Expected tree node name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the name differs after the element is found;
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// Includes <see cref="GraftException.Report"/> with minimum diagnostics.
    /// </exception>
    public async Task<TreeNode> ExpectNameAsync(
        string expectedName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(expectedName);

        var timeout = PositiveOrDefault(
            _waitOptions.ExpectTimeout,
            WaitOptions.DefaultExpectTimeout
        );
        var poll = PositiveOrDefault(_waitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;

        string? lastActual = null;
        var sawElement = false;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var tree = await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false);
                var node = TreeSelector.Resolve(tree.Root, _selector);
                sawElement = true;
                if (string.Equals(node.Name, expectedName, StringComparison.Ordinal))
                {
                    return node;
                }

                lastActual = node.Name;
            }
            catch (GraftException ex)
                when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
            {
                // Still waiting for the element to appear / tree to be ready.
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken)
                .ConfigureAwait(false);
        }

        if (sawElement && lastActual is not null)
        {
            throw CreateFailure(
                GraftErrorCodes.ExpectFailed,
                $"Expected name '{expectedName}' but was '{lastActual}'.",
                FailureSteps.ExpectName,
                expected: expectedName,
                actual: lastActual,
                timedOut: true
            );
        }

        throw CreateFailure(
            GraftErrorCodes.ActionTimeout,
            $"Timed out after {timeout.TotalSeconds:0.###}s waiting for name '{expectedName}'.",
            FailureSteps.ExpectName,
            expected: expectedName,
            timedOut: true
        );
    }

    private async Task<TreeNode> WaitForActionableAsync(CancellationToken cancellationToken)
    {
        var timeout = PositiveOrDefault(
            _waitOptions.ActionTimeout,
            WaitOptions.DefaultActionTimeout
        );
        var poll = PositiveOrDefault(_waitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;

        string? lastActual = null;
        GraftException? lastNotActionable = null;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var tree = await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false);
                var node = TreeSelector.Resolve(tree.Root, _selector);
                if (node.Enabled && node.Visible)
                {
                    return node;
                }

                lastActual = $"enabled={node.Enabled}, visible={node.Visible}";
                lastNotActionable = CreateFailure(
                    GraftErrorCodes.ElementNotActionable,
                    $"Element '{node.AutomationId}' is not actionable ({lastActual}).",
                    FailureSteps.Wait,
                    actual: lastActual,
                    timedOut: true
                );
            }
            catch (GraftException ex)
                when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
            {
                // Keep polling.
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken)
                .ConfigureAwait(false);
        }

        if (lastNotActionable is not null)
        {
            throw lastNotActionable;
        }

        throw CreateFailure(
            GraftErrorCodes.ActionTimeout,
            $"Timed out after {timeout.TotalSeconds:0.###}s waiting for an actionable element.",
            FailureSteps.Wait,
            timedOut: true
        );
    }

    private GraftException CreateFailure(
        string code,
        string message,
        string step,
        string? expected = null,
        string? actual = null,
        bool timedOut = false,
        Exception? innerException = null
    ) =>
        new(
            code,
            message,
            new FailureReport
            {
                Step = step,
                Expected = expected,
                Actual = actual,
                TimedOut = timedOut,
                Selector = FailureReportSelector.FromSelector(_selector),
            },
            innerException
        );

    private static TimeSpan PositiveOrDefault(TimeSpan value, TimeSpan fallback) =>
        value <= TimeSpan.Zero ? fallback : value;
}
