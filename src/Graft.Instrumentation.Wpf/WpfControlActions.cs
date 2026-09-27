using System.Windows;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Per-control-type handlers for invoke, setValue, and toggle.
/// </summary>
/// <remarks>
/// Call from the app under test, next to <see cref="WpfGraft.Use"/>, inside <c>GRAFT_TEST</c>.
/// The handler for the most specific registered type assignable to the live control runs first.
/// Return <see langword="true"/> when the handler performed the action.
/// Return <see langword="false"/> to continue with the built-in native, peer, then SendInput path.
/// A disabled or hidden element never reaches a handler.
/// No Telerik, DevExpress, or Syncfusion implementation is included.
/// </remarks>
public static class WpfControlActions
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, Func<DependencyObject, bool>> InvokeHandlers = [];
    private static readonly Dictionary<Type, Func<DependencyObject, string, bool>> SetValueHandlers = [];
    private static readonly Dictionary<Type, Func<DependencyObject, bool>> ToggleHandlers = [];

    /// <summary>
    /// Registers an invoke handler for <typeparamref name="T"/> and its subclasses.
    /// </summary>
    /// <typeparam name="T">Control type to match.</typeparam>
    /// <param name="handler">Returns <see langword="true"/> when it performed the invoke.</param>
    public static void RegisterInvoke<T>(Func<T, bool> handler)
        where T : DependencyObject
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (Gate)
        {
            InvokeHandlers[typeof(T)] = target => handler((T)target);
        }
    }

    /// <summary>
    /// Registers a setValue handler for <typeparamref name="T"/> and its subclasses.
    /// </summary>
    /// <typeparam name="T">Control type to match.</typeparam>
    /// <param name="handler">Returns <see langword="true"/> when it applied <c>value</c>.</param>
    public static void RegisterSetValue<T>(Func<T, string, bool> handler)
        where T : DependencyObject
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (Gate)
        {
            SetValueHandlers[typeof(T)] = (target, value) => handler((T)target, value);
        }
    }

    /// <summary>
    /// Registers a toggle handler for <typeparamref name="T"/> and its subclasses.
    /// </summary>
    /// <typeparam name="T">Control type to match.</typeparam>
    /// <param name="handler">Returns <see langword="true"/> when it performed the toggle.</param>
    public static void RegisterToggle<T>(Func<T, bool> handler)
        where T : DependencyObject
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (Gate)
        {
            ToggleHandlers[typeof(T)] = target => handler((T)target);
        }
    }

    /// <summary>
    /// Removes every registered handler.
    /// </summary>
    public static void Clear()
    {
        lock (Gate)
        {
            InvokeHandlers.Clear();
            SetValueHandlers.Clear();
            ToggleHandlers.Clear();
        }
    }

    internal static bool TryInvoke(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var handler = Find(InvokeHandlers, target.GetType());
        return handler is not null && handler(target);
    }

    internal static bool TrySetValue(DependencyObject target, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(value);
        var handler = Find(SetValueHandlers, target.GetType());
        return handler is not null && handler(target, value);
    }

    internal static bool TryToggle(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var handler = Find(ToggleHandlers, target.GetType());
        return handler is not null && handler(target);
    }

    private static THandler? Find<THandler>(Dictionary<Type, THandler> handlers, Type type)
        where THandler : class
    {
        lock (Gate)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (handlers.TryGetValue(current, out var handler))
                {
                    return handler;
                }
            }
        }

        return null;
    }
}
