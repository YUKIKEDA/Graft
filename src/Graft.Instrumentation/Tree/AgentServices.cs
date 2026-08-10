using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Windows;

namespace Graft.Instrumentation.Tree;

#if GRAFT_TEST

/// <summary>
/// Process-wide agent service registration (framework adapters).
/// </summary>
public static class AgentServices
{
    private static IUiTreeProvider? _treeProvider;
    private static IScreenshotProvider? _screenshotProvider;
    private static IElementResolver? _elementResolver;
    private static IElementInvoker? _elementInvoker;
    private static IElementValueSetter? _elementValueSetter;
    private static IElementToggler? _elementToggler;
    private static IElementKeySender? _elementKeySender;
    private static IElementScroller? _elementScroller;
    private static IElementChooser? _elementChooser;
    private static IElementExpander? _elementExpander;
    private static IElementCellAccessor? _elementCellAccessor;
    private static IWindowCatalog? _windowCatalog;

    /// <summary>
    /// Gets the registered UI tree provider, if any.
    /// </summary>
    public static IUiTreeProvider? TreeProvider => _treeProvider;

    /// <summary>
    /// Gets the registered screenshot provider, if any.
    /// </summary>
    public static IScreenshotProvider? ScreenshotProvider => _screenshotProvider;

    /// <summary>
    /// Gets the registered element resolver, if any.
    /// </summary>
    public static IElementResolver? ElementResolver => _elementResolver;

    /// <summary>
    /// Gets the registered element invoker, if any.
    /// </summary>
    public static IElementInvoker? ElementInvoker => _elementInvoker;

    /// <summary>
    /// Gets the registered element value setter, if any.
    /// </summary>
    public static IElementValueSetter? ElementValueSetter => _elementValueSetter;

    /// <summary>
    /// Gets the registered element toggler, if any.
    /// </summary>
    public static IElementToggler? ElementToggler => _elementToggler;

    /// <summary>
    /// Gets the registered element key sender, if any.
    /// </summary>
    public static IElementKeySender? ElementKeySender => _elementKeySender;

    /// <summary>
    /// Gets the registered element scroller, if any.
    /// </summary>
    public static IElementScroller? ElementScroller => _elementScroller;

    /// <summary>
    /// Gets the registered element chooser (<c>select</c>), if any.
    /// </summary>
    public static IElementChooser? ElementChooser => _elementChooser;

    /// <summary>
    /// Gets the registered element expander, if any.
    /// </summary>
    public static IElementExpander? ElementExpander => _elementExpander;

    /// <summary>
    /// Gets the registered DataGrid cell accessor, if any.
    /// </summary>
    public static IElementCellAccessor? ElementCellAccessor => _elementCellAccessor;

    /// <summary>
    /// Gets the registered window catalog, if any.
    /// </summary>
    public static IWindowCatalog? WindowCatalog => _windowCatalog;

    /// <summary>
    /// Registers the UI tree provider used for <c>getTree</c>.
    /// </summary>
    /// <param name="provider">Framework-specific provider.</param>
    public static void RegisterTreeProvider(IUiTreeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _treeProvider = provider;
    }

    /// <summary>
    /// Registers the screenshot provider used for <c>screenshot</c>.
    /// </summary>
    /// <param name="provider">Framework-specific provider.</param>
    public static void RegisterScreenshotProvider(IScreenshotProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _screenshotProvider = provider;
    }

    /// <summary>
    /// Registers the element resolver used for invoke / setValue.
    /// </summary>
    /// <param name="resolver">Framework-specific resolver.</param>
    public static void RegisterElementResolver(IElementResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _elementResolver = resolver;
    }

    /// <summary>
    /// Registers the element invoker used for <c>invoke</c>.
    /// </summary>
    /// <param name="invoker">Framework-specific invoker.</param>
    public static void RegisterElementInvoker(IElementInvoker invoker)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        _elementInvoker = invoker;
    }

    /// <summary>
    /// Registers the element value setter used for <c>setValue</c>.
    /// </summary>
    /// <param name="setter">Framework-specific setter.</param>
    public static void RegisterElementValueSetter(IElementValueSetter setter)
    {
        ArgumentNullException.ThrowIfNull(setter);
        _elementValueSetter = setter;
    }

    /// <summary>
    /// Registers the element toggler used for <c>toggle</c>.
    /// </summary>
    /// <param name="toggler">Framework-specific toggler.</param>
    public static void RegisterElementToggler(IElementToggler toggler)
    {
        ArgumentNullException.ThrowIfNull(toggler);
        _elementToggler = toggler;
    }

    /// <summary>
    /// Registers the element key sender used for <c>sendKeys</c>.
    /// </summary>
    /// <param name="keySender">Framework-specific key sender.</param>
    public static void RegisterElementKeySender(IElementKeySender keySender)
    {
        ArgumentNullException.ThrowIfNull(keySender);
        _elementKeySender = keySender;
    }

    /// <summary>
    /// Registers the element scroller used for <c>scrollIntoView</c>.
    /// </summary>
    /// <param name="scroller">Framework-specific scroller.</param>
    public static void RegisterElementScroller(IElementScroller scroller)
    {
        ArgumentNullException.ThrowIfNull(scroller);
        _elementScroller = scroller;
    }

    /// <summary>
    /// Registers the element chooser used for <c>select</c>.
    /// </summary>
    /// <param name="chooser">Framework-specific chooser.</param>
    public static void RegisterElementChooser(IElementChooser chooser)
    {
        ArgumentNullException.ThrowIfNull(chooser);
        _elementChooser = chooser;
    }

    /// <summary>
    /// Registers the element expander used for <c>expand</c> / <c>collapse</c>.
    /// </summary>
    /// <param name="expander">Framework-specific expander.</param>
    public static void RegisterElementExpander(IElementExpander expander)
    {
        ArgumentNullException.ThrowIfNull(expander);
        _elementExpander = expander;
    }

    /// <summary>
    /// Registers the DataGrid cell accessor used for <c>getCellText</c> / <c>setCellValue</c>.
    /// </summary>
    /// <param name="accessor">Framework-specific cell accessor.</param>
    public static void RegisterElementCellAccessor(IElementCellAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _elementCellAccessor = accessor;
    }

    /// <summary>
    /// Registers the window catalog used for <c>listWindows</c> / <c>switchWindow</c>.
    /// </summary>
    /// <param name="catalog">Framework-specific catalog.</param>
    public static void RegisterWindowCatalog(IWindowCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _windowCatalog = catalog;
    }

    /// <summary>
    /// Clears registered services (tests).
    /// </summary>
    public static void Reset()
    {
        _treeProvider = null;
        _screenshotProvider = null;
        _elementResolver = null;
        _elementInvoker = null;
        _elementValueSetter = null;
        _elementToggler = null;
        _elementKeySender = null;
        _elementScroller = null;
        _elementChooser = null;
        _elementExpander = null;
        _elementCellAccessor = null;
        _windowCatalog = null;
        OpenFileArm.Reset();
        SaveFileArm.Reset();
        OpenFolderArm.Reset();
    }
}

#endif
