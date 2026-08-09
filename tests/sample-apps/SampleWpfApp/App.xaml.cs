using System.Windows;

namespace SampleWpfApp;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

#if GRAFT_TEST
        Graft.Instrumentation.Wpf.WpfGraft.Use();
        Graft.Instrumentation.Agent.Start();
#endif
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
#if GRAFT_TEST
        Graft.Instrumentation.Agent.Stop();
#endif
        base.OnExit(e);
    }
}
