using System.Diagnostics;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using R3;
using SampleTodoApp.Services;
using SampleTodoApp.ViewModels;
using SampleTodoApp.Views;

namespace SampleTodoApp;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        WpfProviderInitializer.SetDefaultObservableSystem(ex =>
            Trace.WriteLine($"R3 UnhandledException:{ex}")
        );

#if GRAFT_TEST
        Graft.Instrumentation.Wpf.WpfGraft.Use();
        Graft.Instrumentation.Agent.Start();
#endif

        _services = new ServiceCollection()
            .AddSingleton<ITodoStore, JsonTodoStore>()
            .AddSingleton<ThemeService>()
            .AddSingleton<MainWindowViewModel>()
            .BuildServiceProvider();

        var mainVm = _services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = mainVm };
        MainWindow = window;
        window.Show();
        _ = mainVm.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
#if GRAFT_TEST
        Graft.Instrumentation.Agent.Stop();
#endif
        // Singleton IDisposable (incl. MainWindowViewModel) is disposed with the container.
        _services?.Dispose();
        _services = null;
        base.OnExit(e);
    }
}
