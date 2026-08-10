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
    private readonly OperationLog _operationLog;
    private Selector _effectiveSelector;
    private bool _healApplied;

    internal ElementQuery(
        AgentConnection connection,
        Selector selector,
        WaitOptions waitOptions,
        OperationLog operationLog
    )
    {
        _connection = connection;
        _selector = selector;
        _effectiveSelector = selector;
        _waitOptions = waitOptions;
        _operationLog = operationLog;
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
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot invoke over the wire.",
                    FailureSteps.Invoke,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .InvokeAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.Invoke, node.AutomationId);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.Invoke,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
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
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot setValue over the wire.",
                    FailureSteps.SetValue,
                    expected: value,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .SetValueAsync(node.AutomationId, value, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.SetValue, $"{node.AutomationId}={value}");
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.SetValue,
                    expected: value,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the element is present and actionable, then toggles it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when toggle succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or toggle failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot toggle over the wire.",
                    FailureSteps.Toggle,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .ToggleAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.Toggle, node.AutomationId);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.Toggle,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the element is present and actionable, then types literal text.
    /// </summary>
    /// <param name="text">Literal text (no chord DSL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when sendKeys succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or sendKeys failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task SendKeysAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot sendKeys over the wire.",
                    FailureSteps.SendKeys,
                    expected: text,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .SendKeysAsync(node.AutomationId, text, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.SendKeys, $"{node.AutomationId}={text}");
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.SendKeys,
                    expected: text,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the element is present, then scrolls it into view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identity of the scrolled element.</returns>
    /// <exception cref="GraftException">Wait, resolve, or scrollIntoView failed.</exception>
    public Task<ElementIdentity> ScrollIntoViewAsync(
        CancellationToken cancellationToken = default
    ) => ScrollIntoViewCoreAsync(index: null, cancellationToken);

    /// <summary>
    /// Waits until the list/combo is present, then scrolls the item at
    /// <paramref name="index"/> into view (realizing virtualized containers).
    /// </summary>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identity of the realized list item.</returns>
    /// <exception cref="GraftException">Wait, resolve, or scrollIntoView failed.</exception>
    public Task<ElementIdentity> ScrollIntoViewAsync(
        int index,
        CancellationToken cancellationToken = default
    ) => ScrollIntoViewCoreAsync(index, cancellationToken);

    /// <summary>
    /// Waits until the list/combo is actionable, then selects the item at
    /// <paramref name="index"/> (auto scroll/realize when needed).
    /// </summary>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or select failed.</exception>
    public async Task SelectAsync(int index, CancellationToken cancellationToken = default)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot select over the wire.",
                    FailureSteps.Select,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .SelectAsync(node.AutomationId, index, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.Select, $"{node.AutomationId}[{index}]");
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.Select,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the element is actionable, then expands it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when expand succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or expand failed.</exception>
    public async Task ExpandAsync(CancellationToken cancellationToken = default)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot expand over the wire.",
                    FailureSteps.Expand,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .ExpandAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.Expand, node.AutomationId);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.Expand,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the element is actionable, then collapses it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when collapse succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or collapse failed.</exception>
    public async Task CollapseAsync(CancellationToken cancellationToken = default)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot collapse over the wire.",
                    FailureSteps.Collapse,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .CollapseAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.Collapse, node.AutomationId);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.Collapse,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
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
    /// Includes <see cref="GraftException.Report"/> with diagnostics attachments when available.
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
        TreeNode? lastRoot = null;
        var sawElement = false;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var tree = await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false);
                lastRoot = tree.Root;
                var node = ResolveNode(tree.Root);
                sawElement = true;
                if (string.Equals(node.Name, expectedName, StringComparison.Ordinal))
                {
                    _operationLog.Record(FailureSteps.ExpectName, expectedName);
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
            throw await CreateFailureAsync(
                    GraftErrorCodes.ExpectFailed,
                    $"Expected name '{expectedName}' but was '{lastActual}'.",
                    FailureSteps.ExpectName,
                    expected: expectedName,
                    actual: lastActual,
                    timedOut: true,
                    treeRoot: lastRoot,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                $"Timed out after {timeout.TotalSeconds:0.###}s waiting for name '{expectedName}'.",
                FailureSteps.ExpectName,
                expected: expectedName,
                timedOut: true,
                treeRoot: lastRoot,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
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
        TreeNode? lastRoot = null;
        var sawNotActionable = false;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var tree = await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false);
                lastRoot = tree.Root;
                var node = ResolveNode(tree.Root);
                if (node.Enabled && node.Visible)
                {
                    return node;
                }

                lastActual = $"enabled={node.Enabled}, visible={node.Visible}";
                sawNotActionable = true;
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

        if (sawNotActionable && lastActual is not null)
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ElementNotActionable,
                    $"Element is not actionable ({lastActual}).",
                    FailureSteps.Wait,
                    actual: lastActual,
                    timedOut: true,
                    treeRoot: lastRoot,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                $"Timed out after {timeout.TotalSeconds:0.###}s waiting for an actionable element.",
                FailureSteps.Wait,
                timedOut: true,
                treeRoot: lastRoot,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task<GraftException> CreateFailureAsync(
        string code,
        string message,
        string step,
        string? expected = null,
        string? actual = null,
        bool timedOut = false,
        TreeNode? treeRoot = null,
        Exception? innerException = null,
        CancellationToken cancellationToken = default
    )
    {
        var tree = treeRoot;
        if (tree is null)
        {
            try
            {
                tree = (
                    await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false)
                ).Root;
            }
            catch (Exception)
            {
                // Best-effort: GraftException, OperationCanceledException, IO, etc.
                // Must not replace the original failure being reported.
            }
        }

        string? screenshotPath = null;
        try
        {
            var (_, pngBytes) = await _connection
                .ScreenshotAsync(cancellationToken)
                .ConfigureAwait(false);
            screenshotPath = Path.Combine(Path.GetTempPath(), $"graft-fail-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(screenshotPath, pngBytes, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Best-effort attachment; keep the original failure.
            screenshotPath = null;
        }

        IReadOnlyList<HealingCandidate>? healingCandidates = null;
        if (tree is not null)
        {
            var proposed = SelectorHealer.ProposeCandidates(tree, _selector);
            if (proposed.Count > 0)
            {
                healingCandidates = proposed;
            }
        }

        var recent = _operationLog.Snapshot();
        return new GraftException(
            code,
            message,
            new FailureReport
            {
                Step = step,
                Expected = expected,
                Actual = actual,
                TimedOut = timedOut,
                Selector = FailureReportSelector.FromSelector(_selector),
                RecentOperations = recent.Count == 0 ? null : recent,
                Tree = tree,
                ScreenshotPath = screenshotPath,
                HealingCandidates = healingCandidates,
            },
            innerException
        );
    }

    private async Task<ElementIdentity> ScrollIntoViewCoreAsync(
        int? index,
        CancellationToken cancellationToken
    )
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot scrollIntoView over the wire.",
                    FailureSteps.ScrollIntoView,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            var identity = await _connection
                .ScrollIntoViewAsync(node.AutomationId, index, cancellationToken)
                .ConfigureAwait(false);
            var detail =
                index is null
                    ? node.AutomationId
                    : $"{node.AutomationId}[{index}]->{identity.AutomationId}";
            _operationLog.Record(FailureSteps.ScrollIntoView, detail);
            return identity;
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.ScrollIntoView,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    private TreeNode ResolveNode(TreeNode root)
    {
        try
        {
            return TreeSelector.Resolve(root, _effectiveSelector);
        }
        catch (GraftException ex) when (ex.Code == GraftErrorCodes.ElementNotFound && !_healApplied)
        {
            if (!SelectorHealer.TryGetAutoHeal(root, _effectiveSelector, out var healed))
            {
                throw;
            }

            _effectiveSelector = healed;
            _healApplied = true;
            _operationLog.Record("heal", DescribeSelector(healed));
            return TreeSelector.Resolve(root, _effectiveSelector);
        }
    }

    private static string DescribeSelector(Selector selector)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(selector.AutomationId))
        {
            parts.Add($"automationId={selector.AutomationId}");
        }

        if (!string.IsNullOrWhiteSpace(selector.Name))
        {
            parts.Add($"name={selector.Name}");
        }

        if (!string.IsNullOrWhiteSpace(selector.ControlType))
        {
            parts.Add($"controlType={selector.ControlType}");
        }

        if (!string.IsNullOrWhiteSpace(selector.NearAutomationId))
        {
            parts.Add($"near={selector.NearAutomationId}");
        }

        return string.Join(',', parts);
    }

    private static TimeSpan PositiveOrDefault(TimeSpan value, TimeSpan fallback) =>
        value <= TimeSpan.Zero ? fallback : value;
}
