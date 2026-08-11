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
    private MainWindowViewModel? _mainVm;

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
            .AddTransient<MainWindowViewModel>()
            .BuildServiceProvider();

        _mainVm = _services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = _mainVm };
        MainWindow = window;
        window.Show();
        _ = _mainVm.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
#if GRAFT_TEST
        Graft.Instrumentation.Agent.Stop();
#endif
        _mainVm?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
