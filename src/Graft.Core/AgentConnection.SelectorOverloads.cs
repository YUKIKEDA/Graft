using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core;

/// <summary>
/// Automation-id overloads for <see cref="AgentConnection"/> element actions.
/// </summary>
public sealed partial class AgentConnection
{
    /// <summary>
    /// Calls <c>invoke</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when invoke succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task InvokeAsync(string automationId, CancellationToken cancellationToken = default) =>
        InvokeAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>rightClick</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when rightClick succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task RightClickAsync(string automationId, CancellationToken cancellationToken = default) =>
        RightClickAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>doubleClick</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when doubleClick succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task DoubleClickAsync(string automationId, CancellationToken cancellationToken = default) =>
        DoubleClickAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>hover</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when hover succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task HoverAsync(string automationId, CancellationToken cancellationToken = default) =>
        HoverAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>drag</c> from one automation id to another.
    /// </summary>
    /// <param name="automationId">Source automation id.</param>
    /// <param name="toAutomationId">Destination automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when drag succeeds.</returns>
    public Task DragAsync(string automationId, string toAutomationId, CancellationToken cancellationToken = default) =>
        DragAsync(ByAutomationId(automationId), ByAutomationId(toAutomationId), cancellationToken);

    /// <summary>
    /// Calls <c>clickAt</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="offsetX">Horizontal offset from the element origin.</param>
    /// <param name="offsetY">Vertical offset from the element origin.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when clickAt succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task ClickAtAsync(string automationId, double offsetX, double offsetY, CancellationToken cancellationToken = default) =>
        ClickAtAsync(ByAutomationId(automationId), offsetX, offsetY, cancellationToken);

    /// <summary>
    /// Calls <c>wheel</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="delta">Wheel delta.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when wheel succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task WheelAsync(string automationId, int delta, CancellationToken cancellationToken = default) =>
        WheelAsync(ByAutomationId(automationId), delta, cancellationToken);

    /// <summary>
    /// Calls <c>setValue</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="value">Replacement value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SetValueAsync(string automationId, string value, CancellationToken cancellationToken = default) =>
        SetValueAsync(ByAutomationId(automationId), value, cancellationToken);

    /// <summary>
    /// Calls <c>toggle</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when toggle succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task ToggleAsync(string automationId, CancellationToken cancellationToken = default) =>
        ToggleAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>sendKeys</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="text">Text to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when sendKeys succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SendKeysAsync(string automationId, string text, CancellationToken cancellationToken = default) =>
        SendKeysAsync(ByAutomationId(automationId), text, cancellationToken);

    /// <summary>
    /// Calls <c>typeHuman</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="text">Literal text to type.</param>
    /// <param name="delayMs">Milliseconds to wait between Unicode scalars.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when typeHuman succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task TypeHumanAsync(string automationId, string text, int delayMs, CancellationToken cancellationToken = default) =>
        TypeHumanAsync(ByAutomationId(automationId), text, delayMs, cancellationToken);

    /// <summary>
    /// Calls <c>pressKeys</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="keys">Key chord.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when pressKeys succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task PressKeysAsync(string automationId, string keys, CancellationToken cancellationToken = default) =>
        PressKeysAsync(ByAutomationId(automationId), keys, cancellationToken);

    /// <summary>
    /// Calls <c>scrollIntoView</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target element or list automation id.</param>
    /// <param name="index">Optional list item index (virtualized lists).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identity of the scrolled element.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task<ElementIdentity> ScrollIntoViewAsync(string automationId, int? index = null, CancellationToken cancellationToken = default) =>
        ScrollIntoViewAsync(ByAutomationId(automationId), index, cancellationToken);

    /// <summary>
    /// Calls <c>select</c> by index for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">List or combo automation id.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SelectAsync(string automationId, int index, CancellationToken cancellationToken = default) =>
        SelectAsync(ByAutomationId(automationId), index, cancellationToken);

    /// <summary>
    /// Calls <c>select</c> by key for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">List / combo / tab automation id.</param>
    /// <param name="key">Item key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SelectByKeyAsync(string automationId, string key, CancellationToken cancellationToken = default) =>
        SelectByKeyAsync(ByAutomationId(automationId), key, cancellationToken);

    /// <summary>
    /// Calls <c>selectTree</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">TreeView automation id.</param>
    /// <param name="path">Slash-separated item path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectTree succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SelectTreeAsync(string automationId, string path, CancellationToken cancellationToken = default) =>
        SelectTreeAsync(ByAutomationId(automationId), path, cancellationToken);

    /// <summary>
    /// Calls <c>selectMany</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">ListBox or DataGrid automation id.</param>
    /// <param name="indexes">Zero-based item indexes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectMany succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SelectManyAsync(string automationId, IReadOnlyList<int> indexes, CancellationToken cancellationToken = default) =>
        SelectManyAsync(ByAutomationId(automationId), indexes, cancellationToken);

    /// <summary>
    /// Calls <c>selectMenu</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Menu or open ContextMenu automation id.</param>
    /// <param name="path">Slash-separated menu path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectMenu succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SelectMenuAsync(string automationId, string path, CancellationToken cancellationToken = default) =>
        SelectMenuAsync(ByAutomationId(automationId), path, cancellationToken);

    /// <summary>
    /// Calls <c>getCellText</c> for a DataGrid cell by column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task<string> GetCellTextAsync(string automationId, int row, int column, CancellationToken cancellationToken = default) =>
        GetCellTextAsync(ByAutomationId(automationId), row, column, cancellationToken);

    /// <summary>
    /// Calls <c>getCellText</c> for a DataGrid cell by column Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task<string> GetCellTextAsync(string automationId, int row, string columnKey, CancellationToken cancellationToken = default) =>
        GetCellTextAsync(ByAutomationId(automationId), row, columnKey, cancellationToken);

    /// <summary>
    /// Calls <c>setCellValue</c> for a DataGrid cell by column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SetCellValueAsync(string automationId, int row, int column, string value, CancellationToken cancellationToken = default) =>
        SetCellValueAsync(ByAutomationId(automationId), row, column, value, cancellationToken);

    /// <summary>
    /// Calls <c>setCellValue</c> for a DataGrid cell by column Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SetCellValueAsync(string automationId, int row, string columnKey, string value, CancellationToken cancellationToken = default) =>
        SetCellValueAsync(ByAutomationId(automationId), row, columnKey, value, cancellationToken);

    /// <summary>
    /// Calls <c>selectCell</c> by column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectCell succeeds.</returns>
    public Task SelectCellAsync(string automationId, int row, int column, CancellationToken cancellationToken = default) =>
        SelectCellAsync(ByAutomationId(automationId), row, column, cancellationToken);

    /// <summary>
    /// Calls <c>selectCell</c> by column Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectCell succeeds.</returns>
    public Task SelectCellAsync(string automationId, int row, string columnKey, CancellationToken cancellationToken = default) =>
        SelectCellAsync(ByAutomationId(automationId), row, columnKey, cancellationToken);

    /// <summary>
    /// Calls <c>selectRow</c> by column Header key and cell value.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Exact cell display text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectRow succeeds.</returns>
    public Task SelectRowAsync(string automationId, string columnKey, string value, CancellationToken cancellationToken = default) =>
        SelectRowAsync(ByAutomationId(automationId), columnKey, value, cancellationToken);

    /// <summary>
    /// Calls <c>clickColumnHeader</c> for the DataGrid with the given automation id.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when clickColumnHeader succeeds.</returns>
    public Task ClickColumnHeaderAsync(string automationId, string columnKey, CancellationToken cancellationToken = default) =>
        ClickColumnHeaderAsync(ByAutomationId(automationId), columnKey, cancellationToken);

    /// <summary>
    /// Calls <c>addRow</c> for the DataGrid with the given automation id.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when addRow succeeds.</returns>
    public Task AddRowAsync(string automationId, CancellationToken cancellationToken = default) =>
        AddRowAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>deleteSelectedRows</c> for the DataGrid with the given automation id.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deleteSelectedRows succeeds.</returns>
    public Task DeleteSelectedRowsAsync(string automationId, CancellationToken cancellationToken = default) =>
        DeleteSelectedRowsAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>expand</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when expand succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task ExpandAsync(string automationId, CancellationToken cancellationToken = default) =>
        ExpandAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>collapse</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when collapse succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task CollapseAsync(string automationId, CancellationToken cancellationToken = default) =>
        CollapseAsync(ByAutomationId(automationId), cancellationToken);

    /// <summary>
    /// Calls <c>invokeOpeningWindow</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when invokeOpeningWindow succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task InvokeOpeningWindowAsync(string automationId, CancellationToken cancellationToken = default) =>
        InvokeOpeningWindowAsync(ByAutomationId(automationId), cancellationToken);

    private static SelectorQuery ByAutomationId(string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return new SelectorQuery { AutomationId = automationId };
    }
}
