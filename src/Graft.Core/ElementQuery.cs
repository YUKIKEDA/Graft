using System.Text.RegularExpressions;
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
    private readonly TreeBaseline _treeBaseline;
    private readonly OperationTimeline? _timeline;
    private readonly IReadOnlyList<RelativeStep> _relativeSteps;
    private Selector _effectiveSelector;
    private bool _healApplied;
    private TreeNode? _successRoot;

    internal ElementQuery(
        AgentConnection connection,
        Selector selector,
        WaitOptions waitOptions,
        OperationLog operationLog,
        TreeBaseline treeBaseline,
        IReadOnlyList<RelativeStep>? relativeSteps = null,
        OperationTimeline? timeline = null
    )
    {
        _connection = connection;
        _selector = selector;
        _effectiveSelector = selector;
        _waitOptions = waitOptions;
        _operationLog = operationLog;
        _treeBaseline = treeBaseline;
        _timeline = timeline;
        _relativeSteps = relativeSteps ?? [];
    }

    /// <summary>
    /// Narrows to a direct child matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Child criteria.</param>
    /// <returns>A new query scoped to the child.</returns>
    public ElementQuery Child(Selector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return WithRelative(new ChildStep(selector));
    }

    /// <summary>
    /// Narrows to a direct child with the given automation id.
    /// </summary>
    /// <param name="automationId">Child automation id.</param>
    /// <returns>A new query scoped to the child.</returns>
    public ElementQuery ChildByAutomationId(string automationId) => Child(Selector.ByAutomationId(automationId));

    /// <summary>
    /// Narrows to a direct child with the given name.
    /// </summary>
    /// <param name="name">Child name.</param>
    /// <returns>A new query scoped to the child.</returns>
    public ElementQuery ChildByName(string name) => Child(Selector.ByName(name));

    /// <summary>
    /// Narrows to a sibling matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Sibling criteria.</param>
    /// <returns>A new query scoped to the sibling.</returns>
    public ElementQuery Sibling(Selector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return WithRelative(new SiblingStep(selector));
    }

    /// <summary>
    /// Narrows to a sibling with the given automation id.
    /// </summary>
    /// <param name="automationId">Sibling automation id.</param>
    /// <returns>A new query scoped to the sibling.</returns>
    public ElementQuery SiblingByAutomationId(string automationId) => Sibling(Selector.ByAutomationId(automationId));

    /// <summary>
    /// Picks the zero-based Nth match among the current scope (or best-score ties).
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <returns>A new query with Nth applied.</returns>
    public ElementQuery Nth(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Nth index must be >= 0.");
        }

        if (_relativeSteps.Count == 0)
        {
            return new ElementQuery(
                _connection,
                new Selector
                {
                    AutomationId = _selector.AutomationId,
                    Name = _selector.Name,
                    ControlType = _selector.ControlType,
                    NearAutomationId = _selector.NearAutomationId,
                    Nth = index,
                },
                _waitOptions,
                _operationLog,
                _treeBaseline,
                _relativeSteps,
                _timeline
            );
        }

        return WithRelative(new NthStep(index));
    }

    /// <summary>
    /// Waits until the element is present and actionable, then invokes it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when invoke succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or invoke failed (may include <see cref="GraftException.Report"/>).</exception>
    public Task InvokeAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.Invoke, id => _connection.InvokeAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the element is present and actionable, then right-clicks it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when rightClick succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or rightClick failed (may include <see cref="GraftException.Report"/>).</exception>
    public Task RightClickAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.RightClick, id => _connection.RightClickAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the element is present and actionable, then double-clicks it (SendInput).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when doubleClick succeeds.</returns>
    public Task DoubleClickAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.DoubleClick, id => _connection.DoubleClickAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the element is present and actionable, then moves the cursor over it (SendInput).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when hover succeeds.</returns>
    public Task HoverAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.Hover, id => _connection.HoverAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until this element is actionable, then drags to <paramref name="toAutomationId"/> (SendInput).
    /// </summary>
    /// <param name="toAutomationId">Drop target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when drag succeeds.</returns>
    public Task DragAsync(string toAutomationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAutomationId);
        return RunActionAsync(
            FailureSteps.Drag,
            id => _connection.DragAsync(id, toAutomationId, cancellationToken),
            cancellationToken,
            detail: id => $"{id}->{toAutomationId}"
        );
    }

    /// <summary>
    /// Waits until the element is actionable, then left-clicks at the clickable point plus DIP offsets.
    /// </summary>
    /// <param name="offsetX">Horizontal DIP offset from the clickable point.</param>
    /// <param name="offsetY">Vertical DIP offset from the clickable point.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when clickAt succeeds.</returns>
    public Task ClickAtAsync(double offsetX, double offsetY, CancellationToken cancellationToken = default) =>
        RunActionAsync(
            FailureSteps.ClickAt,
            id => _connection.ClickAtAsync(id, offsetX, offsetY, cancellationToken),
            cancellationToken,
            detail: id => $"{id}@({offsetX},{offsetY})"
        );

    /// <summary>
    /// Waits until the element is actionable, then scrolls the mouse wheel over it (SendInput).
    /// </summary>
    /// <param name="delta">Wheel delta (typically multiples of 120; positive = away from user).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when wheel succeeds.</returns>
    public Task WheelAsync(int delta, CancellationToken cancellationToken = default) =>
        RunActionAsync(
            FailureSteps.Wheel,
            id => _connection.WheelAsync(id, delta, cancellationToken),
            cancellationToken,
            detail: id => $"{id}:{delta}"
        );

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
    public Task<WindowInfo?> InvokeOpeningWindowAsync(CancellationToken cancellationToken = default) =>
        InvokeOpeningWindowAsync(waitForNewWindow: true, cancellationToken);

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
    public async Task<WindowInfo?> InvokeOpeningWindowAsync(bool waitForNewWindow, CancellationToken cancellationToken = default)
    {
        HashSet<int>? knownIds = null;
        if (waitForNewWindow)
        {
            var before = await _connection.ListWindowsAsync(cancellationToken).ConfigureAwait(false);
            knownIds = before.Windows.Select(w => w.WindowId).ToHashSet();
        }

        var automationId = await SendActionAsync(
                FailureSteps.InvokeOpeningWindow,
                id => _connection.InvokeOpeningWindowAsync(id, cancellationToken),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!waitForNewWindow)
        {
            await RecordSuccessAsync(FailureSteps.InvokeOpeningWindow, $"{automationId};waitForNewWindow=false", cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var timeout = PositiveOrDefault(_waitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        WindowInfo? opened = null;
        var found = await PollUntilAsync(
                timeout,
                async ct =>
                {
                    var listed = await _connection.ListWindowsAsync(ct).ConfigureAwait(false);
                    var newborn = listed.Windows.FirstOrDefault(w => !knownIds!.Contains(w.WindowId));
                    if (newborn is null)
                    {
                        return false;
                    }

                    try
                    {
                        await _connection.SwitchWindowAsync(newborn.WindowId, ct).ConfigureAwait(false);
                    }
                    catch (GraftException ex) when (ex.Report is null)
                    {
                        throw await CreateFailureAsync(
                                ex.Code,
                                ex.Message,
                                FailureSteps.InvokeOpeningWindow,
                                cancellationToken: ct,
                                innerException: ex
                            )
                            .ConfigureAwait(false);
                    }

                    _successRoot = null;
                    _treeBaseline.Clear();
                    await RecordSuccessAsync(FailureSteps.InvokeOpeningWindow, $"{automationId}->windowId={newborn.WindowId}", ct)
                        .ConfigureAwait(false);
                    opened = newborn;
                    return true;
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (found)
        {
            return opened;
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
    public Task SetValueAsync(string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        return RunActionAsync(
            FailureSteps.SetValue,
            id => _connection.SetValueAsync(id, value, cancellationToken),
            cancellationToken,
            expected: value,
            detail: id => $"{id}={value}"
        );
    }

    /// <summary>
    /// Waits until the element is present and actionable, then toggles it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when toggle succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or toggle failed (may include <see cref="GraftException.Report"/>).</exception>
    public Task ToggleAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.Toggle, id => _connection.ToggleAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the element is present and actionable, then types literal text.
    /// </summary>
    /// <param name="text">Literal text (no chord DSL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when sendKeys succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or sendKeys failed (may include <see cref="GraftException.Report"/>).</exception>
    public Task SendKeysAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return RunActionAsync(
            FailureSteps.SendKeys,
            id => _connection.SendKeysAsync(id, text, cancellationToken),
            cancellationToken,
            expected: text,
            detail: id => $"{id}={text}"
        );
    }

    /// <summary>
    /// Waits until the element is present and actionable, then types text one character at a time.
    /// </summary>
    /// <param name="text">Literal text (no chord DSL). Existing text is left in place.</param>
    /// <param name="delay">Wait between Unicode scalars. Zero still types one scalar per input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when typeHuman succeeds.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> is negative or longer than <see cref="int.MaxValue"/> milliseconds.</exception>
    /// <exception cref="GraftException">Wait, resolve, or typeHuman failed (may include <see cref="GraftException.Report"/>).</exception>
    /// <remarks>
    /// The agent waits on its request thread, so the UI dispatcher can run debounce between characters.
    /// <see cref="SetValueAsync"/> and <see cref="SendKeysAsync"/> stay immediate.
    /// </remarks>
    public Task TypeHumanAsync(string text, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (delay < TimeSpan.Zero || delay.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(delay));
        }

        var delayMs = (int)delay.TotalMilliseconds;
        return RunActionAsync(
            FailureSteps.TypeHuman,
            id => _connection.TypeHumanAsync(id, text, delayMs, cancellationToken),
            cancellationToken,
            expected: text,
            detail: id => $"{id}={text};delayMs={delayMs}"
        );
    }

    /// <summary>
    /// Waits until the element is present and actionable, then presses one keyboard chord.
    /// </summary>
    /// <param name="keys">Chord DSL (e.g. <c>Control+A</c>, <c>Delete</c>). One call = one chord.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when pressKeys succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, invalid chord, or pressKeys failed (may include <see cref="GraftException.Report"/>).</exception>
    public Task PressAsync(string keys, CancellationToken cancellationToken = default)
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

        return RunActionAsync(
            FailureSteps.PressKeys,
            id => _connection.PressKeysAsync(id, keys, cancellationToken),
            cancellationToken,
            expected: keys,
            detail: id => $"{id}={keys}"
        );
    }

    /// <summary>
    /// Waits until the element is present, then scrolls it into view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identity of the scrolled element.</returns>
    /// <exception cref="GraftException">Wait, resolve, or scrollIntoView failed.</exception>
    public Task<ElementIdentity> ScrollIntoViewAsync(CancellationToken cancellationToken = default) =>
        ScrollIntoViewCoreAsync(index: null, cancellationToken);

    /// <summary>
    /// Waits until the list/combo is present, then scrolls the item at
    /// <paramref name="index"/> into view (realizing virtualized containers).
    /// </summary>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identity of the realized list item.</returns>
    /// <exception cref="GraftException">Wait, resolve, or scrollIntoView failed.</exception>
    public Task<ElementIdentity> ScrollIntoViewAsync(int index, CancellationToken cancellationToken = default) =>
        ScrollIntoViewCoreAsync(index, cancellationToken);

    /// <summary>
    /// Waits until the list/combo is actionable, then selects the item at
    /// <paramref name="index"/> (auto scroll/realize when needed).
    /// </summary>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or select failed.</exception>
    public Task SelectAsync(int index, CancellationToken cancellationToken = default) =>
        RunActionAsync(
            FailureSteps.Select,
            id => _connection.SelectAsync(id, index, cancellationToken),
            cancellationToken,
            detail: id => $"{id}[{index}]"
        );

    /// <summary>
    /// Waits until the list/combo is actionable, then selects the item whose name equals
    /// <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Item display / automation name (ordinal).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or select failed.</exception>
    public Task SelectAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return RunActionAsync(
            FailureSteps.Select,
            id => _connection.SelectByKeyAsync(id, key, cancellationToken),
            cancellationToken,
            detail: id => $"{id}[key={key}]"
        );
    }

    /// <summary>
    /// Waits until the TreeView is actionable, then expands along a slash-separated AutomationId
    /// path and selects the leaf.
    /// </summary>
    /// <param name="path">Slash-separated AutomationId segments (root TreeView not included).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectTree succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or selectTree failed.</exception>
    public Task SelectTreeAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return RunActionAsync(
            FailureSteps.SelectTree,
            id => _connection.SelectTreeAsync(id, path, cancellationToken),
            cancellationToken,
            detail: id => $"{id}:{path}"
        );
    }

    /// <summary>
    /// Waits until the ListBox or DataGrid is actionable, then replaces multi-selection with
    /// <paramref name="indexes"/> (auto scroll/realize when needed). Empty clears.
    /// </summary>
    /// <param name="indexes">Zero-based item indexes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectMany succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or selectMany failed.</exception>
    public Task SelectManyAsync(IReadOnlyList<int> indexes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(indexes);
        return RunActionAsync(
            FailureSteps.SelectMany,
            id => _connection.SelectManyAsync(id, indexes, cancellationToken),
            cancellationToken,
            detail: id => $"{id}[{string.Join(',', indexes)}]"
        );
    }

    /// <summary>
    /// Waits until the menu root is actionable, then selects a slash-separated AutomationId path.
    /// </summary>
    /// <param name="path">Slash-separated AutomationId segments (this query is the Menu / ContextMenu root).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectMenu succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or selectMenu failed.</exception>
    public Task SelectMenuAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return RunActionAsync(
            FailureSteps.SelectMenu,
            id => _connection.SelectMenuAsync(id, path, cancellationToken),
            cancellationToken,
            detail: id => $"{id}:{path}"
        );
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then returns cell display text by column index.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">Wait, resolve, or getCellText failed.</exception>
    public Task<string> GetCellTextAsync(int row, int column, CancellationToken cancellationToken = default) =>
        GetCellTextCoreAsync(row, column, columnKey: null, cancellationToken);

    /// <summary>
    /// Waits until the DataGrid is actionable, then returns cell display text by column Header.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">Wait, resolve, or getCellText failed.</exception>
    public Task<string> GetCellTextAsync(int row, string columnKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return GetCellTextCoreAsync(row, column: null, columnKey, cancellationToken);
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then sets a cell by column index.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="value">Replacement text (CheckBox: <c>True</c>/<c>False</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or setCellValue failed.</exception>
    public Task SetCellValueAsync(int row, int column, string value, CancellationToken cancellationToken = default) =>
        SetCellValueCoreAsync(row, column, columnKey: null, value, cancellationToken);

    /// <summary>
    /// Waits until the DataGrid is actionable, then sets a cell by column Header.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Replacement text (CheckBox: <c>True</c>/<c>False</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or setCellValue failed.</exception>
    public Task SetCellValueAsync(int row, string columnKey, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return SetCellValueCoreAsync(row, column: null, columnKey, value, cancellationToken);
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then selects a cell by column index.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectCell succeeds.</returns>
    public Task SelectCellAsync(int row, int column, CancellationToken cancellationToken = default) =>
        SelectCellCoreAsync(row, column, columnKey: null, cancellationToken);

    /// <summary>
    /// Waits until the DataGrid is actionable, then selects a cell by column Header.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectCell succeeds.</returns>
    public Task SelectCellAsync(int row, string columnKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return SelectCellCoreAsync(row, column: null, columnKey, cancellationToken);
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then selects the row whose cell at
    /// <paramref name="columnKey"/> equals <paramref name="value"/>.
    /// </summary>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Exact cell display text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectRow succeeds.</returns>
    public Task SelectRowAsync(string columnKey, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        ArgumentNullException.ThrowIfNull(value);
        return RunActionAsync(
            FailureSteps.SelectRow,
            id => _connection.SelectRowAsync(id, columnKey, value, cancellationToken),
            cancellationToken,
            detail: id => $"{id}[{columnKey}={value}]"
        );
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then clicks a column header (sort UI).
    /// </summary>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when clickColumnHeader succeeds.</returns>
    public Task ClickColumnHeaderAsync(string columnKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return RunActionAsync(
            FailureSteps.ClickColumnHeader,
            id => _connection.ClickColumnHeaderAsync(id, columnKey, cancellationToken),
            cancellationToken,
            detail: id => $"{id}:{columnKey}"
        );
    }

    /// <summary>
    /// Waits until the DataGrid is actionable, then adds a new row.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when addRow succeeds.</returns>
    public Task AddRowAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.AddRow, id => _connection.AddRowAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the DataGrid is actionable, then deletes selected rows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deleteSelectedRows succeeds.</returns>
    public Task DeleteSelectedRowsAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.DeleteSelectedRows, id => _connection.DeleteSelectedRowsAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the DataGrid cell text equals <paramref name="expectedText"/> (column index).
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
    public Task ExpectCellTextAsync(int row, int column, string expectedText, CancellationToken cancellationToken = default) =>
        ExpectCellTextCoreAsync(row, column, columnKey: null, expectedText, cancellationToken);

    /// <summary>
    /// Waits until the DataGrid cell text equals <paramref name="expectedText"/> (column Header).
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="expectedText">Expected cell display text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the text differs;
    /// <c>action.timeout</c> when the cell never matches in time.
    /// </exception>
    public Task ExpectCellTextAsync(int row, string columnKey, string expectedText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return ExpectCellTextCoreAsync(row, column: null, columnKey, expectedText, cancellationToken);
    }

    /// <summary>
    /// Waits until the element is actionable, then expands it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when expand succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or expand failed.</exception>
    public Task ExpandAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.Expand, id => _connection.ExpandAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Waits until the element is actionable, then collapses it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when collapse succeeds.</returns>
    /// <exception cref="GraftException">Wait, resolve, or collapse failed.</exception>
    public Task CollapseAsync(CancellationToken cancellationToken = default) =>
        RunActionAsync(FailureSteps.Collapse, id => _connection.CollapseAsync(id, cancellationToken), cancellationToken);

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
    public Task<TreeNode> ExpectNameAsync(string expectedName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedName);
        return ExpectAsync(
            FailureSteps.ExpectName,
            expectedName,
            node => (string.Equals(node.Name, expectedName, StringComparison.Ordinal), node.Name),
            actual => $"Expected name '{expectedName}' but was '{actual}'.",
            seconds => $"Timed out after {seconds:0.###}s waiting for name '{expectedName}'.",
            cancellationToken
        );
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
    public Task<TreeNode> ExpectSelectedAsync(bool expectedSelected, CancellationToken cancellationToken = default) =>
        ExpectBoolPropertyAsync(expectedSelected, static node => node.Selected, FailureSteps.ExpectSelected, "selected", cancellationToken);

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
    public Task<TreeNode> ExpectExpandedAsync(bool expectedExpanded, CancellationToken cancellationToken = default) =>
        ExpectBoolPropertyAsync(expectedExpanded, static node => node.Expanded, FailureSteps.ExpectExpanded, "expanded", cancellationToken);

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
    public Task<TreeNode> ExpectCheckedAsync(bool expectedChecked, CancellationToken cancellationToken = default) =>
        ExpectBoolPropertyAsync(expectedChecked, static node => node.Checked, FailureSteps.ExpectChecked, "checked", cancellationToken);

    /// <summary>
    /// Waits until the element's tree <c>enabled</c> equals <paramref name="expectedEnabled"/>.
    /// </summary>
    /// <param name="expectedEnabled">Expected enabled state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    public Task<TreeNode> ExpectEnabledAsync(bool expectedEnabled, CancellationToken cancellationToken = default) =>
        ExpectBoolPropertyAsync(expectedEnabled, static node => (bool?)node.Enabled, FailureSteps.ExpectEnabled, "enabled", cancellationToken);

    /// <summary>
    /// Waits until the element's tree <c>visible</c> equals <paramref name="expectedVisible"/>.
    /// </summary>
    /// <param name="expectedVisible">Expected visible state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    public Task<TreeNode> ExpectVisibleAsync(bool expectedVisible, CancellationToken cancellationToken = default) =>
        ExpectBoolPropertyAsync(expectedVisible, static node => (bool?)node.Visible, FailureSteps.ExpectVisible, "visible", cancellationToken);

    /// <summary>
    /// Waits until the element's tree <c>focused</c> is <c>true</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the state differs;
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// </exception>
    public Task<TreeNode> ExpectFocusedAsync(CancellationToken cancellationToken = default) =>
        ExpectBoolPropertyAsync(expected: true, static node => node.Focused, FailureSteps.ExpectFocused, "focused", cancellationToken);

    /// <summary>
    /// Waits until the element's <c>name</c> contains <paramref name="substring"/>.
    /// </summary>
    /// <param name="substring">Expected non-empty ordinal substring.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    public Task<TreeNode> ExpectNameContainsAsync(string substring, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(substring);
        return ExpectAsync(
            FailureSteps.ExpectNameContains,
            substring,
            node => (node.Name.Contains(substring, StringComparison.Ordinal), node.Name),
            actual => $"Expected name to contain '{substring}' but was '{actual}'.",
            seconds => $"Timed out after {seconds:0.###}s waiting for name containing '{substring}'.",
            cancellationToken
        );
    }

    /// <summary>
    /// Waits until the element's <c>name</c> matches <paramref name="pattern"/>.
    /// </summary>
    /// <param name="pattern">.NET regular expression pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    public Task<TreeNode> ExpectNameMatchesAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        var regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
        return ExpectAsync(
            FailureSteps.ExpectNameMatches,
            pattern,
            node => (regex.IsMatch(node.Name), node.Name),
            actual => $"Expected name to match /{pattern}/ but was '{actual}'.",
            seconds => $"Timed out after {seconds:0.###}s waiting for name matching /{pattern}/.",
            cancellationToken
        );
    }

    /// <summary>
    /// Waits until the element's tree <c>value</c> equals <paramref name="expectedValue"/>.
    /// </summary>
    /// <param name="expectedValue">Expected tree value (ordinal).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when the value differs or is not applicable;
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// </exception>
    public Task<TreeNode> ExpectValueAsync(string expectedValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);
        return ExpectAsync(
            FailureSteps.ExpectValue,
            expectedValue,
            node => (node.Value is not null && string.Equals(node.Value, expectedValue, StringComparison.Ordinal), node.Value ?? "n/a"),
            actual => $"Expected value '{expectedValue}' but was '{actual}'.",
            seconds => $"Timed out after {seconds:0.###}s waiting for value '{expectedValue}'.",
            cancellationToken
        );
    }

    /// <summary>
    /// Waits until the element's open ToolTip display text equals <paramref name="expectedToolTip"/>.
    /// </summary>
    /// <param name="expectedToolTip">Expected ToolTip text (ordinal).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when the expectation holds.</returns>
    /// <exception cref="GraftException">
    /// <c>expect.failed</c> when a mismatched ToolTip is observed until timeout, or
    /// <c>action.timeout</c> when the element never qualifies in time.
    /// </exception>
    public Task<TreeNode> ExpectToolTipAsync(string expectedToolTip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedToolTip);
        return ExpectAsync(
            FailureSteps.ExpectToolTip,
            expectedToolTip,
            node => (node.ToolTip is not null && string.Equals(node.ToolTip, expectedToolTip, StringComparison.Ordinal), node.ToolTip ?? "n/a"),
            actual => $"Expected toolTip '{expectedToolTip}' but was '{actual}'.",
            seconds => $"Timed out after {seconds:0.###}s waiting for toolTip '{expectedToolTip}'.",
            cancellationToken
        );
    }

    /// <summary>
    /// Waits until the element is present in the visual tree.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched node when found.</returns>
    public async Task<TreeNode> WaitForAsync(CancellationToken cancellationToken = default)
    {
        var timeout = PositiveOrDefault(_waitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        TreeNode? lastRoot = null;
        TreeNode? found = null;
        var matched = await PollUntilAsync(
                timeout,
                async ct =>
                {
                    try
                    {
                        var tree = await _connection.GetTreeAsync(ct).ConfigureAwait(false);
                        lastRoot = tree.Root;
                        var node = ResolveNode(tree.Root);
                        await RecordSuccessAsync(FailureSteps.WaitFor, node.AutomationId, ct).ConfigureAwait(false);
                        found = node;
                        return true;
                    }
                    catch (GraftException ex) when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
                    {
                        // Keep polling.
                        return false;
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (matched)
        {
            return found!;
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                $"Timed out after {timeout.TotalSeconds:0.###}s waiting for element to appear.",
                FailureSteps.WaitFor,
                timedOut: true,
                treeRoot: lastRoot,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until the element is present (<see cref="WaitForAsync"/> / ExpectTimeout), then captures a PNG clip of it.
    /// Open ToolTips and Popup overlays of this element or its descendants are composited in screen space.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot meta and PNG bytes.</returns>
    /// <exception cref="GraftException">Wait, resolve, or screenshot failed (may include <see cref="GraftException.Report"/>).</exception>
    public async Task<Screenshot> ScreenshotAsync(CancellationToken cancellationToken = default)
    {
        var node = await WaitForAsync(cancellationToken).ConfigureAwait(false);
        var automationId = string.IsNullOrWhiteSpace(node.AutomationId) ? null : node.AutomationId;
        int? runtimeId = automationId is null ? node.RuntimeId : null;
        try
        {
            var (meta, pngBytes) = await _connection.ScreenshotAsync(automationId, runtimeId, cancellationToken).ConfigureAwait(false);
            var shot = new Screenshot(meta.Format, meta.Width, meta.Height, pngBytes);
            await RecordSuccessAsync(FailureSteps.Screenshot, $"{shot.Width}x{shot.Height}:{shot.PngBytes.Length}", cancellationToken, shot.PngBytes)
                .ConfigureAwait(false);
            return shot;
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(ex.Code, ex.Message, FailureSteps.Screenshot, cancellationToken: cancellationToken, innerException: ex)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits until the element is not found or not visible.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the element is gone.</returns>
    public async Task ExpectGoneAsync(CancellationToken cancellationToken = default)
    {
        var timeout = PositiveOrDefault(_waitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        TreeNode? lastRoot = null;
        string? lastActual = null;
        var gone = await PollUntilAsync(
                timeout,
                async ct =>
                {
                    try
                    {
                        var tree = await _connection.GetTreeAsync(ct).ConfigureAwait(false);
                        lastRoot = tree.Root;
                        var node = ResolveNode(tree.Root);
                        if (!node.Visible)
                        {
                            await RecordSuccessAsync(FailureSteps.ExpectGone, "not-visible", ct).ConfigureAwait(false);
                            return true;
                        }

                        lastActual = $"visible={node.Visible}";
                        return false;
                    }
                    catch (GraftException ex) when (ex.Code is GraftErrorCodes.ElementNotFound)
                    {
                        await RecordSuccessAsync(FailureSteps.ExpectGone, "not-found", ct).ConfigureAwait(false);
                        return true;
                    }
                    catch (GraftException ex) when (ex.Code is GraftErrorCodes.ActionFailed)
                    {
                        // Keep polling.
                        return false;
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (gone)
        {
            return;
        }

        var goneTimeoutMessage =
            $"Timed out after {timeout.TotalSeconds:0.###}s waiting for element to be gone" + (lastActual is null ? "." : $" ({lastActual}).");
        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                goneTimeoutMessage,
                FailureSteps.ExpectGone,
                actual: lastActual,
                timedOut: true,
                treeRoot: lastRoot,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private Task<string> GetCellTextCoreAsync(int row, int? column, string? columnKey, CancellationToken cancellationToken) =>
        RunActionAsync(
            FailureSteps.GetCellText,
            id =>
                columnKey is null
                    ? _connection.GetCellTextAsync(id, row, column!.Value, cancellationToken)
                    : _connection.GetCellTextAsync(id, row, columnKey, cancellationToken),
            cancellationToken,
            detail: (id, _) => columnKey is null ? $"{id}[{row},{column}]" : $"{id}[{row},{columnKey}]"
        );

    private Task SetCellValueCoreAsync(int row, int? column, string? columnKey, string value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        return RunActionAsync(
            FailureSteps.SetCellValue,
            id =>
                columnKey is null
                    ? _connection.SetCellValueAsync(id, row, column!.Value, value, cancellationToken)
                    : _connection.SetCellValueAsync(id, row, columnKey, value, cancellationToken),
            cancellationToken,
            expected: value,
            detail: id => columnKey is null ? $"{id}[{row},{column}]={value}" : $"{id}[{row},{columnKey}]={value}"
        );
    }

    private Task SelectCellCoreAsync(int row, int? column, string? columnKey, CancellationToken cancellationToken) =>
        RunActionAsync(
            FailureSteps.SelectCell,
            id =>
                columnKey is null
                    ? _connection.SelectCellAsync(id, row, column!.Value, cancellationToken)
                    : _connection.SelectCellAsync(id, row, columnKey, cancellationToken),
            cancellationToken,
            detail: id => columnKey is null ? $"{id}[{row},{column}]" : $"{id}[{row},{columnKey}]"
        );

    private async Task ExpectCellTextCoreAsync(int row, int? column, string? columnKey, string expectedText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedText);
        var hostId = await RequireAutomationIdAsync(FailureSteps.ExpectCellText, cancellationToken, expectedText).ConfigureAwait(false);
        var timeout = PositiveOrDefault(_waitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        string? lastActual = null;
        var sawCell = false;
        var matched = await PollUntilAsync(
                timeout,
                async ct =>
                {
                    try
                    {
                        var actual = columnKey is null
                            ? await _connection.GetCellTextAsync(hostId, row, column!.Value, ct).ConfigureAwait(false)
                            : await _connection.GetCellTextAsync(hostId, row, columnKey, ct).ConfigureAwait(false);
                        sawCell = true;
                        if (string.Equals(actual, expectedText, StringComparison.Ordinal))
                        {
                            await RecordSuccessAsync(FailureSteps.ExpectCellText, expectedText, ct).ConfigureAwait(false);
                            return true;
                        }

                        lastActual = actual;
                        return false;
                    }
                    catch (GraftException ex) when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
                    {
                        // Still waiting for the cell / grid to be ready.
                        return false;
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (matched)
        {
            return;
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

    private Task<TreeNode> ExpectBoolPropertyAsync(
        bool expected,
        Func<TreeNode, bool?> getter,
        string step,
        string propertyName,
        CancellationToken cancellationToken
    )
    {
        var expectedText = expected ? "true" : "false";
        return ExpectAsync(
            step,
            expectedText,
            node =>
            {
                var actual = getter(node);
                var text = actual is null ? "n/a" : (actual.Value ? "true" : "false");
                return (actual is { } value && value == expected, text);
            },
            actual => $"Expected {propertyName} '{expectedText}' but was '{actual}'.",
            seconds => $"Timed out after {seconds:0.###}s waiting for {propertyName} '{expectedText}'.",
            cancellationToken
        );
    }

    private async Task<TreeNode> ExpectAsync(
        string step,
        string expected,
        Func<TreeNode, (bool Ok, string? Actual)> check,
        Func<string, string> mismatchMessage,
        Func<double, string> timeoutMessage,
        CancellationToken cancellationToken
    )
    {
        var timeout = PositiveOrDefault(_waitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        string? lastActual = null;
        TreeNode? lastRoot = null;
        var sawElement = false;
        TreeNode? matched = null;
        var found = await PollUntilAsync(
                timeout,
                async ct =>
                {
                    try
                    {
                        var tree = await _connection.GetTreeAsync(ct).ConfigureAwait(false);
                        lastRoot = tree.Root;
                        var node = ResolveNode(tree.Root);
                        sawElement = true;
                        var (ok, actual) = check(node);
                        if (ok)
                        {
                            await RecordSuccessAsync(step, expected, ct).ConfigureAwait(false);
                            matched = node;
                            return true;
                        }

                        lastActual = actual;
                        return false;
                    }
                    catch (GraftException ex) when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
                    {
                        // Still waiting for the element to appear / tree to be ready.
                        return false;
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (found)
        {
            return matched!;
        }

        if (sawElement && lastActual is not null)
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ExpectFailed,
                    mismatchMessage(lastActual),
                    step,
                    expected: expected,
                    actual: lastActual,
                    timedOut: true,
                    treeRoot: lastRoot,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        throw await CreateFailureAsync(
                GraftErrorCodes.ActionTimeout,
                timeoutMessage(timeout.TotalSeconds),
                step,
                expected: expected,
                timedOut: true,
                treeRoot: lastRoot,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task<bool> PollUntilAsync(TimeSpan timeout, Func<CancellationToken, Task<bool>> attempt, CancellationToken cancellationToken)
    {
        var poll = PositiveOrDefault(_waitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await attempt(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private Task RunActionAsync(
        string step,
        Func<string, Task> send,
        CancellationToken cancellationToken,
        string? expected = null,
        Func<string, string>? detail = null
    ) =>
        RunActionAsync<object?>(
            step,
            async id =>
            {
                await send(id).ConfigureAwait(false);
                return null;
            },
            cancellationToken,
            expected,
            detail is null ? null : (id, _) => detail(id)
        );

    private async Task<T> RunActionAsync<T>(
        string step,
        Func<string, Task<T>> send,
        CancellationToken cancellationToken,
        string? expected = null,
        Func<string, T, string>? detail = null,
        bool recordSuccess = true
    )
    {
        var automationId = await RequireAutomationIdAsync(step, cancellationToken, expected).ConfigureAwait(false);
        try
        {
            var result = await send(automationId).ConfigureAwait(false);
            if (recordSuccess)
            {
                await RecordSuccessAsync(step, detail?.Invoke(automationId, result) ?? automationId, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch (GraftException ex) when (ex.Report is null)
        {
            throw await CreateFailureAsync(ex.Code, ex.Message, step, expected: expected, cancellationToken: cancellationToken, innerException: ex)
                .ConfigureAwait(false);
        }
    }

    private Task<string> SendActionAsync(string step, Func<string, Task> send, CancellationToken cancellationToken) =>
        RunActionAsync(
            step,
            async id =>
            {
                await send(id).ConfigureAwait(false);
                return id;
            },
            cancellationToken,
            recordSuccess: false
        );

    private async Task<string> RequireAutomationIdAsync(string step, CancellationToken cancellationToken, string? expected = null)
    {
        var node = await WaitForActionableAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(node.AutomationId))
        {
            throw await CreateFailureAsync(
                    GraftErrorCodes.ActionFailed,
                    $"Resolved element has no automationId; cannot {step} over the wire.",
                    step,
                    expected: expected,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }

        return node.AutomationId;
    }

    private async Task<TreeNode> WaitForActionableAsync(CancellationToken cancellationToken)
    {
        var timeout = PositiveOrDefault(_waitOptions.ActionTimeout, WaitOptions.DefaultActionTimeout);
        string? lastActual = null;
        TreeNode? lastRoot = null;
        var sawNotActionable = false;
        TreeNode? actionable = null;
        var found = await PollUntilAsync(
                timeout,
                async ct =>
                {
                    try
                    {
                        var tree = await _connection.GetTreeAsync(ct).ConfigureAwait(false);
                        lastRoot = tree.Root;
                        var node = ResolveNode(tree.Root);
                        if (node.Enabled && node.Visible)
                        {
                            actionable = node;
                            return true;
                        }

                        lastActual = $"enabled={node.Enabled}, visible={node.Visible}";
                        sawNotActionable = true;
                        return false;
                    }
                    catch (GraftException ex) when (ex.Code is GraftErrorCodes.ElementNotFound or GraftErrorCodes.ActionFailed)
                    {
                        // Keep polling.
                        return false;
                    }
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (found)
        {
            return actionable!;
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
        _timeline?.MarkFailed();
        _successRoot = null;
        var tree = treeRoot;
        if (tree is null)
        {
            try
            {
                tree = (await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false)).Root;
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
            var (_, pngBytes) = await _connection.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
            var path = Path.Combine(Path.GetTempPath(), $"graft-fail-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(path, pngBytes, cancellationToken).ConfigureAwait(false);
            screenshotPath = path;
        }
        catch (Exception)
        {
            // Best-effort attachment; keep the original failure.
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
                TreeDiff = tree is null ? null : _treeBaseline.Diff(tree),
            },
            innerException
        );
    }

    private Task<ElementIdentity> ScrollIntoViewCoreAsync(int? index, CancellationToken cancellationToken) =>
        RunActionAsync(
            FailureSteps.ScrollIntoView,
            id => _connection.ScrollIntoViewAsync(id, index, cancellationToken),
            cancellationToken,
            detail: (id, identity) => index is null ? id : $"{id}[{index}]->{identity.AutomationId}"
        );

    private async Task RecordSuccessAsync(string action, string? detail, CancellationToken cancellationToken, byte[]? pngBytes = null)
    {
        if (_successRoot is not null)
        {
            _treeBaseline.Remember(_successRoot);
            _successRoot = null;
        }

        _operationLog.Record(action, detail);
        if (_timeline is not null)
        {
            await _timeline.CaptureAfterAsync(action, detail, cancellationToken, pngBytes).ConfigureAwait(false);
        }
    }

    private TreeNode ResolveNode(TreeNode root)
    {
        _successRoot = root;
        TreeNode node;
        try
        {
            node = TreeSelector.Resolve(root, _effectiveSelector);
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
            node = TreeSelector.Resolve(root, _effectiveSelector);
        }

        for (var i = 0; i < _relativeSteps.Count; )
        {
            var step = _relativeSteps[i];
            var nth = i + 1 < _relativeSteps.Count && _relativeSteps[i + 1] is NthStep n ? n.Index : (int?)null;

            switch (step)
            {
                case ChildStep child:
                    node = TreeSelector.ResolveChild(node, child.Selector, nth);
                    i += nth is null ? 1 : 2;
                    break;
                case SiblingStep sibling:
                    node = TreeSelector.ResolveSibling(root, node, sibling.Selector, nth);
                    i += nth is null ? 1 : 2;
                    break;
                case NthStep alone:
                    // Positional among siblings of current (same parent).
                    node = TreeSelector.ResolveSibling(root, node, new Selector(), alone.Index);
                    i++;
                    break;
                default:
                    i++;
                    break;
            }
        }

        return node;
    }

    private ElementQuery WithRelative(RelativeStep step)
    {
        var steps = new List<RelativeStep>(_relativeSteps.Count + 1);
        steps.AddRange(_relativeSteps);
        steps.Add(step);
        return new ElementQuery(_connection, _selector, _waitOptions, _operationLog, _treeBaseline, steps, _timeline);
    }

    internal abstract record RelativeStep;

    internal sealed record ChildStep(Selector Selector) : RelativeStep;

    internal sealed record SiblingStep(Selector Selector) : RelativeStep;

    internal sealed record NthStep(int Index) : RelativeStep;

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

    private static TimeSpan PositiveOrDefault(TimeSpan value, TimeSpan fallback) => value <= TimeSpan.Zero ? fallback : value;
}
