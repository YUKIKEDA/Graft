using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Windows;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Tracks session-local window ids and the current agent target window.
/// </summary>
internal sealed class WpfWindowHost : IWindowCatalog
{
    private static readonly PropertyInfo? IsShowingAsDialogProperty =
        typeof(Window).GetProperty("IsShowingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? typeof(Window).GetProperty("IsModal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    private readonly object _gate = new();
    private readonly Dictionary<Window, int> _idsByWindow = new();
    private readonly Dictionary<int, Window> _windowsById = new();
    private int _nextId = 1;
    private Window? _target;

    /// <inheritdoc />
    public ListWindowsResult ListWindows() =>
        InvokeOnUi(() =>
        {
            PruneClosed_NoLock();
            EnsureIds_NoLock();
            var list = new List<WindowInfo>();
            foreach (Window window in Application.Current.Windows)
            {
                if (!_idsByWindow.TryGetValue(window, out var id))
                {
                    continue;
                }

                list.Add(ToInfo(window, id));
            }

            return new ListWindowsResult { Windows = list };
        });

    /// <inheritdoc />
    public void SwitchWindow(int windowId) =>
        InvokeOnUi(() =>
        {
            PruneClosed_NoLock();
            if (!_windowsById.TryGetValue(windowId, out var window))
            {
                throw new ElementResolveException(GraftErrorCodes.WindowNotFound, $"Window id {windowId} was not found.");
            }

            _target = window;
        });

    /// <summary>
    /// Gets the current target window (defaults to <see cref="Application.MainWindow"/>).
    /// </summary>
    public Window GetTargetWindow() =>
        InvokeOnUi(() =>
        {
            PruneClosed_NoLock();
            if (_target is not null && Application.Current.Windows.OfType<Window>().Contains(_target))
            {
                return _target;
            }

            var main = Application.Current?.MainWindow ?? throw new InvalidOperationException("Main window was not found.");
            EnsureId_NoLock(main);
            _target = main;
            return main;
        });

    private T InvokeOnUi<T>(Func<T> action)
    {
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("WPF Application.Current is not available.");

        if (dispatcher.CheckAccess())
        {
            lock (_gate)
            {
                return action();
            }
        }

        return dispatcher.Invoke(
            () =>
            {
                lock (_gate)
                {
                    return action();
                }
            },
            DispatcherPriority.Normal
        );
    }

    private void InvokeOnUi(Action action) =>
        InvokeOnUi(() =>
        {
            action();
            return true;
        });

    private void PruneClosed_NoLock()
    {
        var closed = _idsByWindow.Keys.Where(w => !IsOpen(w)).ToList();
        foreach (var window in closed)
        {
            if (_idsByWindow.Remove(window, out var id))
            {
                _windowsById.Remove(id);
            }

            if (ReferenceEquals(_target, window))
            {
                _target = null;
            }
        }
    }

    private void EnsureIds_NoLock()
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        foreach (Window window in app.Windows)
        {
            EnsureId_NoLock(window);
        }
    }

    private void EnsureId_NoLock(Window window)
    {
        if (_idsByWindow.ContainsKey(window))
        {
            return;
        }

        var id = _nextId++;
        _idsByWindow[window] = id;
        _windowsById[id] = window;
    }

    private static bool IsOpen(Window window)
    {
        try
        {
            return Application.Current is not null && Application.Current.Windows.OfType<Window>().Contains(window);
        }
        catch
        {
            return false;
        }
    }

    private static WindowInfo ToInfo(Window window, int id) =>
        new()
        {
            WindowId = id,
            Title = window.Title ?? string.Empty,
            AutomationId = AutomationProperties.GetAutomationId(window) ?? string.Empty,
            IsModal = IsShowingAsDialogProperty?.GetValue(window) is true,
            IsActive = window.IsActive,
        };
}
