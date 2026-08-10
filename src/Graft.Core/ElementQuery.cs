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
    /// Waits until the element is present and actionable, then right-clicks it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when rightClick succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or rightClick failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task RightClickAsync(CancellationToken cancellationToken = default)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot rightClick over the wire.",
                    FailureSteps.RightClick,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .RightClickAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.RightClick, node.AutomationId);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.RightClick,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invokes the element via <c>invokeOpeningWindow</c> (BeginInvoke), optionally waiting for a new window.
    /// </summary>
    /// <remarks>
    /// Use this when the click opens a modal (<c>ShowDialog</c>). A plain <see cref="InvokeAsync"/>
    /// may hang until the dialog closes because the agent UI thread is blocked.
    /// For Graft OpenFile seam (no new WPF window), call the overload with
    /// <c>waitForNewWindow: false</c>.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly opened window (already selected as the agent target).</returns>
    /// <exception cref="GraftException">Wait, resolve, invoke, or window wait failed.</exception>
    public Task<WindowInfo?> InvokeOpeningWindowAsync(
        CancellationToken cancellationToken = default
    ) => InvokeOpeningWindowAsync(waitForNewWindow: true, cancellationToken);

    /// <summary>
    /// Invokes the element via <c>invokeOpeningWindow</c> (BeginInvoke), optionally waiting for a new window.
    /// </summary>
    /// <param name="waitForNewWindow">
    /// When true, waits for a new WPF window and switches to it.
    /// When false, only queues BeginInvoke (OpenFile seam / no new window) and returns null.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The newly opened window when <paramref name="waitForNewWindow"/> is true; otherwise
    /// <see langword="null"/>.
    /// </returns>
    /// <exception cref="GraftException">Wait, resolve, invoke, or window wait failed.</exception>
    public async Task<WindowInfo?> InvokeOpeningWindowAsync(
        bool waitForNewWindow,
        CancellationToken cancellationToken = default
    )
    {
        HashSet<int>? knownIds = null;
        if (waitForNewWindow)
        {
            var before = await _connection
                .ListWindowsAsync(cancellationToken)
                .ConfigureAwait(false);
            knownIds = before.Windows.Select(w => w.WindowId).ToHashSet();
        }

        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot invokeOpeningWindow over the wire.",
                    FailureSteps.InvokeOpeningWindow,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .InvokeOpeningWindowAsync(node.AutomationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.InvokeOpeningWindow,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }

        if (!waitForNewWindow)
        {
            _operationLog.Record(
                FailureSteps.InvokeOpeningWindow,
                $"{node.AutomationId};waitForNewWindow=false"
            );
            return null;
        }

        var timeout = PositiveOrDefault(
            _waitOptions.ExpectTimeout,
            WaitOptions.DefaultExpectTimeout
        );
        var poll = PositiveOrDefault(_waitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var listed = await _connection
                .ListWindowsAsync(cancellationToken)
                .ConfigureAwait(false);
            var newborn = listed.Windows.FirstOrDefault(w => !knownIds!.Contains(w.WindowId));
            if (newborn is not null)
            {
                try
                {
                    await _connection
                        .SwitchWindowAsync(newborn.WindowId, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GraftException ex) when (ex.Report is null)
                {
                    throw await CreateFailureAsync(
                            ex.Code,
                            ex.Message,
                            FailureSteps.InvokeOpeningWindow,
                            cancellationToken: cancellationToken,
                            innerException: ex
                        )
                        .ConfigureAwait(false);
                }

                _operationLog.Record(
                    FailureSteps.InvokeOpeningWindow,
                    $"{node.AutomationId}->windowId={newborn.WindowId}"
                );
                return newborn;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken)
                .ConfigureAwait(false);
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                $"Timed out after {timeout.TotalSeconds:0.###}s waiting for a new window after invokeOpeningWindow.",
                FailureSteps.InvokeOpeningWindow,
                timedOut: true,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until the element is present and actionable, then replaces its value.
    /// For <c>Slider</c>, <paramref name="value"/> is parsed as an invariant-culture double.
    /// </summary>
    /// <param name="value">Replacement text (empty string clears TextBox). Slider: invariant number string.</param>
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
    /// Waits until the element is present and actionable, then presses one keyboard chord.
    /// </summary>
    /// <param name="keys">Chord DSL (e.g. <c>Control+A</c>, <c>Delete</c>). One call = one chord.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when pressKeys succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, invalid chord, or pressKeys failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task PressAsync(string keys, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keys);

        try
        {
            _ = KeyChordParser.Parse(keys);
        }
        catch (ArgumentException ex)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, ex.Message, ex);
        }

        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot pressKeys over the wire.",
                    FailureSteps.PressKeys,
                    expected: keys,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .PressKeysAsync(node.AutomationId, keys, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.PressKeys, $"{node.AutomationId}={keys}");
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.PressKeys,
                    expected: keys,
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
    /// Waits until the DataGrid is actionable, then returns Text cell display text.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">Wait, resolve, or getCellText failed.</exception>
    public async Task<string> GetCellTextAsync(
        int row,
        int column,
        CancellationToken cancellationToken = default
    )
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot getCellText over the wire.",
                    FailureSteps.GetCellText,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            var text = await _connection
                .GetCellTextAsync(node.AutomationId, row, column, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.GetCellText, $"{node.AutomationId}[{row},{column}]");
            return text;
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.GetCellText,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then sets a Text cell via BeginEdit/CommitEdit.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or setCellValue failed.</exception>
    public async Task SetCellValueAsync(
        int row,
        int column,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(value);

        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot setCellValue over the wire.",
                    FailureSteps.SetCellValue,
                    expected: value,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        try
        {
            await _connection
                .SetCellValueAsync(node.AutomationId, row, column, value, cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(
                FailureSteps.SetCellValue,
                $"{node.AutomationId}[{row},{column}]={value}"
            );
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(
                    ex.Code,
                    ex.Message,
                    FailureSteps.SetCellValue,
                    expected: value,
                    cancellationToken: cancellationToken,
                    innerException: ex
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the DataGrid cell text equals <paramref name="expectedText"/>.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="expectedText">Expected cell display text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the text differs;
    /// <c>action.timeout</c> when the cell never matches in time.
    /// </exception>
    public async Task ExpectCellTextAsync(
        int row,
        int column,
        string expectedText,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(expectedText);

        var host = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(host.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    "Resolved element has no automationId; cannot expectCellText over the wire.",
                    FailureSteps.ExpectCellText,
                    expected: expectedText,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        var timeout = PositiveOrDefault(
            _waitOptions.ExpectTimeout,
            WaitOptions.DefaultExpectTimeout
        );
        var poll = PositiveOrDefault(_waitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;
        string? lastActual = null;
        var sawCell = false;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var actual = await _connection
                    .GetCellTextAsync(host.AutomationId, row, column, cancellationToken)
                    .ConfigureAwait(false);
                sawCell = true;
                if (string.Equals(actual, expectedText, StringComparison.Ordinal))
                {
                    _operationLog.Record(FailureSteps.ExpectCellText, expectedText);
                    return;
                }

                lastActual = actual;
            }
            catch (GraftException ex)
                when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
            {
                // Still waiting for the cell / grid to be ready.
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken)
                .ConfigureAwait(false);
        }

        if (sawCell && lastActual is not null)
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ExpectFailed,
                    $"Expected cell text '{expectedText}' but was '{lastActual}'.",
                    FailureSteps.ExpectCellText,
                    expected: expectedText,
                    actual: lastActual,
                    timedOut: true,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                $"Timed out after {timeout.TotalSeconds:0.###}s waiting for cell text '{expectedText}'.",
                FailureSteps.ExpectCellText,
                expected: expectedText,
                timedOut: true,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
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

    /// <summary>
    /// Waits until the element's tree <c>selected</c> equals <paramref name="expectedSelected"/>.
    /// </summary>
    /// <param name="expectedSelected">Expected selection state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the state differs or is not applicable;
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// </exception>
    public Task<TreeNode> ExpectSelectedAsync(
        bool expectedSelected,
        CancellationToken cancellationToken = default
    ) =>
        ExpectBoolPropertyAsync(
            expectedSelected,
            static node => node.Selected,
            FailureSteps.ExpectSelected,
            "selected",
            cancellationToken
        );

    /// <summary>
    /// Waits until the element's tree <c>expanded</c> equals <paramref name="expectedExpanded"/>.
    /// </summary>
    /// <param name="expectedExpanded">Expected expand state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the state differs or is not applicable;
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// </exception>
    public Task<TreeNode> ExpectExpandedAsync(
        bool expectedExpanded,
        CancellationToken cancellationToken = default
    ) =>
        ExpectBoolPropertyAsync(
            expectedExpanded,
            static node => node.Expanded,
            FailureSteps.ExpectExpanded,
            "expanded",
            cancellationToken
        );

    /// <summary>
    /// Waits until the element's tree <c>checked</c> equals <paramref name="expectedChecked"/>.
    /// </summary>
    /// <param name="expectedChecked">Expected checked state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the state differs or is not applicable;
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// </exception>
    public Task<TreeNode> ExpectCheckedAsync(
        bool expectedChecked,
        CancellationToken cancellationToken = default
    ) =>
        ExpectBoolPropertyAsync(
            expectedChecked,
            static node => node.Checked,
            FailureSteps.ExpectChecked,
            "checked",
            cancellationToken
        );

    private async Task<TreeNode> ExpectBoolPropertyAsync(
        bool expected,
        Func<TreeNode, bool?> getter,
        string step,
        string propertyName,
        CancellationToken cancellationToken
    )
    {
        var timeout = PositiveOrDefault(
            _waitOptions.ExpectTimeout,
            WaitOptions.DefaultExpectTimeout
        );
        var poll = PositiveOrDefault(_waitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;
        var expectedText = expected ? "true" : "false";

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
                var actual = getter(node);
                if (actual is { } value && value == expected)
                {
                    _operationLog.Record(step, expectedText);
                    return node;
                }

                lastActual = actual is null ? "n/a" : (actual.Value ? "true" : "false");
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
                    $"Expected {propertyName} '{expectedText}' but was '{lastActual}'.",
                    step,
                    expected: expectedText,
                    actual: lastActual,
                    timedOut: true,
                    treeRoot: lastRoot,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                $"Timed out after {timeout.TotalSeconds:0.###}s waiting for {propertyName} '{expectedText}'.",
                step,
                expected: expectedText,
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
            var detail = index is null
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
