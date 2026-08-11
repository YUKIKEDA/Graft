using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

namespace SampleTodoApp.FlaUI.Tests;

internal sealed class FlaUITodoSession : IDisposable
{
    public FlaUITodoSession(Application app, UIA3Automation automation, Window mainWindow)
    {
        App = app;
        Automation = automation;
        MainWindow = mainWindow;
    }

    public Application App { get; }

    public UIA3Automation Automation { get; }

    public Window MainWindow { get; set; }

    public void Dispose()
    {
        try
        {
            if (!App.HasExited)
            {
                App.Close();
            }
        }
        catch
        {
            // best-effort
        }

        try
        {
            if (!App.HasExited)
            {
                App.Kill();
            }
        }
        catch
        {
            // best-effort
        }

        Automation.Dispose();
        App.Dispose();
    }
}
