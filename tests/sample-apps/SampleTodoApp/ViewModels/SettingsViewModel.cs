using Microsoft.Win32;
using R3;

namespace SampleTodoApp.ViewModels;

public sealed class SettingsViewModel : IDisposable
{
    private readonly Func<string, Task> _applyDataDirectoryAsync;
    private readonly Func<bool, Task> _applyThemeAsync;
    private readonly Action _close;
    private DisposableBag _disposables;

    public SettingsViewModel(
        string dataDirectory,
        bool isDarkTheme,
        Func<string, Task> applyDataDirectoryAsync,
        Func<bool, Task> applyThemeAsync,
        Action close
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        _applyDataDirectoryAsync =
            applyDataDirectoryAsync
            ?? throw new ArgumentNullException(nameof(applyDataDirectoryAsync));
        _applyThemeAsync =
            applyThemeAsync ?? throw new ArgumentNullException(nameof(applyThemeAsync));
        _close = close ?? throw new ArgumentNullException(nameof(close));

        DataDirectory = new BindableReactiveProperty<string>(dataDirectory).AddTo(ref _disposables);
        IsDarkTheme = new BindableReactiveProperty<bool>(isDarkTheme).AddTo(ref _disposables);

        BrowseDataDirectoryCommand = new AsyncReactiveCommand(BrowseDataDirectoryAsync).AddTo(
            ref _disposables
        );
        CloseCommand = new ReactiveCommand(_ => _close()).AddTo(ref _disposables);

        IsDarkTheme
            .Skip(1)
            .SubscribeAwait(async (dark, _) => await _applyThemeAsync(dark).ConfigureAwait(true))
            .AddTo(ref _disposables);
    }

    public BindableReactiveProperty<string> DataDirectory { get; }

    public BindableReactiveProperty<bool> IsDarkTheme { get; }

    public ReactiveCommand BrowseDataDirectoryCommand { get; }

    public ReactiveCommand CloseCommand { get; }

    public void Dispose() => _disposables.Dispose();

    private async Task BrowseDataDirectoryAsync()
    {
        var dialog = new OpenFolderDialog { Title = "データ保存先を選択" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _applyDataDirectoryAsync(dialog.FolderName).ConfigureAwait(true);
        DataDirectory.Value = dialog.FolderName;
    }
}
