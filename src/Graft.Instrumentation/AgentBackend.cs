using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Instrumentation.Windows;

namespace Graft.Instrumentation;

#if GRAFT_TEST

/// <summary>
/// Services one framework backend provides to the in-process agent.
/// </summary>
/// <remarks>
/// Register the whole object once with <see cref="Agent.Use"/>. An unset service causes that wire method
/// to return <c>action.failed</c>. WPF fills every slot from <c>WpfGraft.Use</c>.
/// </remarks>
public sealed class AgentBackend
{
    /// <summary>
    /// Gets the UI tree provider used for <c>getTree</c>.
    /// </summary>
    public IUiTreeProvider? TreeProvider { get; init; }

    /// <summary>
    /// Gets the screenshot provider used for <c>screenshot</c>.
    /// </summary>
    public IScreenshotProvider? ScreenshotProvider { get; init; }

    /// <summary>
    /// Gets the element resolver used by framework action implementations.
    /// </summary>
    public IElementResolver? ElementResolver { get; init; }

    /// <summary>
    /// Gets the element invoker.
    /// </summary>
    public IElementInvoker? ElementInvoker { get; init; }

    /// <summary>
    /// Gets the element value setter.
    /// </summary>
    public IElementValueSetter? ElementValueSetter { get; init; }

    /// <summary>
    /// Gets the element toggler.
    /// </summary>
    public IElementToggler? ElementToggler { get; init; }

    /// <summary>
    /// Gets the element key sender.
    /// </summary>
    public IElementKeySender? ElementKeySender { get; init; }

    /// <summary>
    /// Gets the element scroller.
    /// </summary>
    public IElementScroller? ElementScroller { get; init; }

    /// <summary>
    /// Gets the element chooser used for <c>select</c> and <c>selectMany</c>.
    /// </summary>
    public IElementChooser? ElementChooser { get; init; }

    /// <summary>
    /// Gets the menu selector used for <c>selectMenu</c>.
    /// </summary>
    public IMenuSelector? MenuSelector { get; init; }

    /// <summary>
    /// Gets the tree selector used for <c>selectTree</c>.
    /// </summary>
    public ITreeSelector? TreeSelector { get; init; }

    /// <summary>
    /// Gets the element expander used for <c>expand</c> and <c>collapse</c>.
    /// </summary>
    public IElementExpander? ElementExpander { get; init; }

    /// <summary>
    /// Gets the cell accessor used for <c>getCellText</c> and <c>setCellValue</c>.
    /// </summary>
    public IElementCellAccessor? ElementCellAccessor { get; init; }

    /// <summary>
    /// Gets the DataGrid operator.
    /// </summary>
    public IDataGridOperator? DataGridOperator { get; init; }

    /// <summary>
    /// Gets the window catalog used for <c>listWindows</c> and <c>switchWindow</c>.
    /// </summary>
    public IWindowCatalog? WindowCatalog { get; init; }
}

#endif
