using System.IO;
using System.Windows;
using Microsoft.Win32;
using ObservableCollections;
using R3;
using SampleTodoApp.Models;
using SampleTodoApp.Services;
using SampleTodoApp.Views;

namespace SampleTodoApp.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    private readonly ITodoStore _store;
    private readonly ThemeService _theme;
    private readonly List<TodoItem> _master = [];
    private readonly ObservableList<TodoItem> _visible = [];
    private readonly HashSet<int> _checkedIds = [];
    private DisposableBag _disposables;
    private int _nextId = 1;

    public MainWindowViewModel(ITodoStore store, ThemeService theme)
    {
        _store = store;
        _theme = theme;
        ItemsView = _visible.ToNotifyCollectionChanged();
        if (ItemsView is IDisposable itemsViewDisposable)
        {
            itemsViewDisposable.AddTo(ref _disposables);
        }

        SearchText = new BindableReactiveProperty<string>(string.Empty).AddTo(ref _disposables);
        StatusFilter = new BindableReactiveProperty<string>(string.Empty).AddTo(ref _disposables);
        PriorityFilter = new BindableReactiveProperty<string>(string.Empty).AddTo(ref _disposables);
        StatusMessage = new BindableReactiveProperty<string>("Ready").AddTo(ref _disposables);
        IsDarkTheme = new BindableReactiveProperty<bool>().AddTo(ref _disposables);
        IsSettingsOpen = new BindableReactiveProperty<bool>().AddTo(ref _disposables);
        Settings = new BindableReactiveProperty<SettingsViewModel?>().AddTo(ref _disposables);

        SelectionCount = new BindableReactiveProperty<int>(0).AddTo(ref _disposables);
        HeaderCheckState = new BindableReactiveProperty<bool?>(false).AddTo(ref _disposables);
        var hasSelection = SelectionCount.Select(static c => c > 0);
        var hasSingleSelection = SelectionCount.Select(static c => c == 1);

        AddCommand = new AsyncReactiveCommand(() => OpenDetailAsync(isNew: true)).AddTo(
            ref _disposables
        );
        EditCommand = hasSingleSelection
            .ToAsyncReactiveCommand(() => OpenDetailAsync(isNew: false))
            .AddTo(ref _disposables);
        EditRowCommand = new ReactiveCommand<TodoItem>(item =>
            _ = OpenDetailAsync(isNew: false, target: item)
        ).AddTo(ref _disposables);
        DeleteCommand = hasSelection
            .ToAsyncReactiveCommand(DeleteSelectedAsync)
            .AddTo(ref _disposables);
        ExportCommand = new AsyncReactiveCommand(ExportAsync).AddTo(ref _disposables);
        ImportCommand = new AsyncReactiveCommand(ImportAsync).AddTo(ref _disposables);
        OpenSettingsCommand = new ReactiveCommand(_ => OpenSettings()).AddTo(ref _disposables);
        ClearFiltersCommand = new ReactiveCommand(_ => ClearFilters()).AddTo(ref _disposables);

        Observable
            .CombineLatest(
                SearchText,
                StatusFilter,
                PriorityFilter,
                static (_, _, _) => Unit.Default
            )
            .Subscribe(_ => RefreshVisible())
            .AddTo(ref _disposables);
    }

    public INotifyCollectionChangedSynchronizedViewList<TodoItem> ItemsView { get; }

    public BindableReactiveProperty<string> SearchText { get; }

    public BindableReactiveProperty<string> StatusFilter { get; }

    public BindableReactiveProperty<string> PriorityFilter { get; }

    public BindableReactiveProperty<string> StatusMessage { get; }

    public BindableReactiveProperty<int> SelectionCount { get; }

    /// <summary>
    /// Tri-state header checkbox: false / true / null (partial).
    /// </summary>
    public BindableReactiveProperty<bool?> HeaderCheckState { get; }

    public BindableReactiveProperty<bool> IsDarkTheme { get; }

    public BindableReactiveProperty<bool> IsSettingsOpen { get; }

    public BindableReactiveProperty<SettingsViewModel?> Settings { get; }

    public ReactiveCommand AddCommand { get; }

    public ReactiveCommand EditCommand { get; }

    /// <summary>
    /// Double-click row edit. Parameter is the <see cref="TodoItem"/> under the cursor.
    /// </summary>
    public ReactiveCommand<TodoItem> EditRowCommand { get; }

    public ReactiveCommand DeleteCommand { get; }

    public ReactiveCommand ExportCommand { get; }

    public ReactiveCommand ImportCommand { get; }

    public ReactiveCommand OpenSettingsCommand { get; }

    public ReactiveCommand ClearFiltersCommand { get; }

    public async Task InitializeAsync() =>
        ApplyLoaded(await _store.LoadAsync().ConfigureAwait(true));

    public int GetVisibleCheckedCount() => _visible.Count(static i => i.IsChecked);

    public void ToggleItemChecked(TodoItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        SetItemChecked(item, !item.IsChecked);
    }

    public void ToggleSelectAllVisible() =>
        SetAllVisibleChecked(GetVisibleCheckedCount() < _visible.Count);

    public void SetItemChecked(TodoItem item, bool isChecked)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.IsChecked = isChecked;
        if (isChecked)
        {
            _checkedIds.Add(item.Id);
        }
        else
        {
            _checkedIds.Remove(item.Id);
        }

        PublishCheckedSelection();
    }

    public void SetAllVisibleChecked(bool isChecked)
    {
        foreach (var item in _visible)
        {
            item.IsChecked = isChecked;
            if (isChecked)
            {
                _checkedIds.Add(item.Id);
            }
            else
            {
                _checkedIds.Remove(item.Id);
            }
        }

        PublishCheckedSelection();
    }

    public void Dispose()
    {
        CloseSettings();
        _disposables.Dispose();
    }

    private void ClearCheckedSelection()
    {
        _checkedIds.Clear();
        foreach (var item in _visible)
        {
            item.IsChecked = false;
        }

        PublishCheckedSelection();
    }

    private void PublishCheckedSelection()
    {
        SelectionCount.Value = _checkedIds.Count;
        var total = _visible.Count;
        var selected = GetVisibleCheckedCount();
        HeaderCheckState.Value =
            total == 0 || selected == 0 ? false
            : selected == total ? true
            : null;
    }

    private TodoItem? GetSingleCheckedItem()
    {
        if (_checkedIds.Count != 1)
        {
            return null;
        }

        var id = _checkedIds.First();
        return _visible.FirstOrDefault(i => i.Id == id) ?? _master.FirstOrDefault(i => i.Id == id);
    }

    private void ClearFilters()
    {
        SearchText.Value = string.Empty;
        StatusFilter.Value = string.Empty;
        PriorityFilter.Value = string.Empty;
        StatusMessage.Value = "FiltersCleared";
    }

    private async Task OpenDetailAsync(bool isNew, TodoItem? target = null)
    {
        TodoItem draft;
        if (isNew)
        {
            draft = new TodoItem
            {
                Id = _nextId,
                Title = string.Empty,
                Status = "未着手",
                Priority = "中",
            };
        }
        else
        {
            var source = target ?? GetSingleCheckedItem();
            if (source is null)
            {
                StatusMessage.Value = "NoSelection";
                return;
            }

            draft = Clone(source);
        }

        using var vm = new ItemDetailViewModel(draft, isNew);
        var window = new ItemDetailWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow,
        };
        _theme.ApplyDarkTitleBar(window, IsDarkTheme.Value);
        if (window.ShowDialog() != true)
        {
            StatusMessage.Value = "DetailCancelled";
            return;
        }

        var saved = Clone(vm.Item);
        saved.IsDone = saved.Status == "完了";
        if (isNew)
        {
            _master.Add(saved);
            _nextId = Math.Max(_nextId, saved.Id) + 1;
            StatusMessage.Value = "ItemAdded";
        }
        else
        {
            var index = _master.FindIndex(i => i.Id == saved.Id);
            if (index >= 0)
            {
                _master[index] = saved;
            }

            StatusMessage.Value = "ItemUpdated";
        }

        await PersistAsync().ConfigureAwait(true);
        RefreshVisible();
    }

    private async Task DeleteSelectedAsync()
    {
        if (_checkedIds.Count == 0)
        {
            StatusMessage.Value = "NoSelection";
            return;
        }

        var ids = _checkedIds.ToHashSet();
        _master.RemoveAll(i => ids.Contains(i.Id));
        ClearCheckedSelection();
        StatusMessage.Value = ids.Count == 1 ? "ItemDeleted" : $"ItemsDeleted:{ids.Count}";
        await PersistAsync().ConfigureAwait(true);
        RefreshVisible();
    }

    private void OpenSettings()
    {
        if (IsSettingsOpen.Value)
        {
            return;
        }

        var vm = new SettingsViewModel(
            _store.DataDirectory,
            IsDarkTheme.Value,
            ApplyDataDirectoryAsync,
            ApplyThemeAsync,
            CloseSettings
        );
        Settings.Value = vm;
        IsSettingsOpen.Value = true;
    }

    private void CloseSettings()
    {
        IsSettingsOpen.Value = false;
        var vm = Settings.Value;
        Settings.Value = null;
        vm?.Dispose();
    }

    private async Task ApplyThemeAsync(bool isDark)
    {
        IsDarkTheme.Value = isDark;
        _theme.Apply(isDark);
        StatusMessage.Value = isDark ? "ThemeDark" : "ThemeLight";
        await PersistAsync().ConfigureAwait(true);
    }

    private async Task ApplyDataDirectoryAsync(string directory)
    {
        if (_master.Count > 0 || File.Exists(_store.DataFilePath))
        {
            await PersistAsync().ConfigureAwait(true);
        }

        await _store.SetDataDirectoryAsync(directory).ConfigureAwait(true);
        ApplyLoaded(await _store.LoadAsync().ConfigureAwait(true));
        StatusMessage.Value = "DataDirectoryChanged";
    }

    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = "todos-export.json",
        };
        if (dialog.ShowDialog() != true)
        {
            StatusMessage.Value = "ExportCancelled";
            return;
        }

        await _store.ExportAsync(Snapshot(), dialog.FileName).ConfigureAwait(true);
        StatusMessage.Value = "ExportDone";
    }

    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
        if (dialog.ShowDialog() != true)
        {
            StatusMessage.Value = "ImportCancelled";
            return;
        }

        var imported = await _store.ImportAsync(dialog.FileName).ConfigureAwait(true);

        // Import replaces items only — keep the user's current theme.
        ApplyLoaded(imported, applyTheme: false);
        StatusMessage.Value = "ImportDone";
        await PersistAsync().ConfigureAwait(true);
    }

    private void ApplyLoaded(ProjectData data, bool applyTheme = true)
    {
        _master.Clear();
        _master.AddRange(data.Items.OrderBy(i => i.Id).Select(Clone));
        _nextId = _master.Count == 0 ? 1 : _master.Max(i => i.Id) + 1;
        if (applyTheme)
        {
            IsDarkTheme.Value = data.IsDarkTheme;
            _theme.Apply(IsDarkTheme.Value);
        }

        ClearCheckedSelection();
        RefreshVisible();
        StatusMessage.Value = $"Loaded {_master.Count} item(s)";
    }

    private async Task PersistAsync() => await _store.SaveAsync(Snapshot()).ConfigureAwait(true);

    private ProjectData Snapshot() =>
        new() { IsDarkTheme = IsDarkTheme.Value, Items = _master.Select(Clone).ToList() };

    private void RefreshVisible()
    {
        var search = SearchText.Value ?? string.Empty;
        var status = StatusFilter.Value ?? string.Empty;
        var priority = PriorityFilter.Value ?? string.Empty;

        IEnumerable<TodoItem> query = _master;
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(i => i.Priority == priority);
        }

        var list = query.OrderBy(i => i.Id).Select(Clone).ToList();
        _visible.Clear();
        foreach (var item in list)
        {
            item.IsChecked = _checkedIds.Contains(item.Id);
            _visible.Add(item);
        }

        // Drop checks for ids that no longer exist in master.
        _checkedIds.RemoveWhere(id => _master.TrueForAll(i => i.Id != id));
        PublishCheckedSelection();
    }

    private static TodoItem Clone(TodoItem item) =>
        new()
        {
            Id = item.Id,
            Title = item.Title,
            Status = item.Status,
            Priority = item.Priority,
            IsDone = item.IsDone,
        };
}
