using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

namespace SampleTodoApp.FlaUI.Tests;

internal static class FlaUITodoLaunch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static FlaUITodoSession Launch(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        ResetPersistedAppState();
        WriteSettings(dataDir);

        var exe = TodoAppLocator.EnsureDebugExe();
        var app = Application.Launch(exe);
        var automation = new UIA3Automation();
        try
        {
            var main = Retry
                .WhileNull(
                    () =>
                    {
                        app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(30));
                        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
                        if (window is null)
                        {
                            return null;
                        }

                        if (
                            !window.Properties.AutomationId.TryGetValue(out var id)
                            || !string.Equals(id, "Main", StringComparison.Ordinal)
                        )
                        {
                            return null;
                        }

                        return window;
                    },
                    TimeSpan.FromSeconds(60)
                )
                .Result;

            if (main is null)
            {
                throw new TimeoutException("Main window (AutomationId=Main) did not appear.");
            }

            WaitForStatusContains(main, "Loaded", TimeSpan.FromSeconds(30));
            return new FlaUITodoSession(app, automation, main);
        }
        catch
        {
            automation.Dispose();
            try
            {
                if (!app.HasExited)
                {
                    app.Kill();
                }
            }
            catch
            {
                // best-effort
            }

            app.Dispose();
            throw;
        }
    }

    public static void ResetPersistedAppState()
    {
        var appRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GraftSampleTodo"
        );
        try
        {
            if (Directory.Exists(appRoot))
            {
                Directory.Delete(appRoot, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }

    public static void WriteSettings(string dataDir)
    {
        var appRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GraftSampleTodo"
        );
        Directory.CreateDirectory(appRoot);
        var settingsPath = Path.Combine(appRoot, "settings.json");
        var json = JsonSerializer.Serialize(
            new { dataDirectory = Path.GetFullPath(dataDir) },
            JsonOptions
        );
        File.WriteAllText(settingsPath, json);
    }

    public static Window WaitForWindowById(
        UIA3Automation automation,
        string automationId,
        TimeSpan timeout
    )
    {
        var window = Retry
            .WhileNull(
                () =>
                {
                    var desktop = automation.GetDesktop();
                    var el = desktop.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                    return el?.AsWindow();
                },
                timeout
            )
            .Result;
        return window
            ?? throw new TimeoutException($"Window AutomationId='{automationId}' not found.");
    }

    public static void WaitForStatus(Window main, string expected, TimeSpan timeout)
    {
        var ok = Retry
            .WhileFalse(
                () =>
                {
                    var status = main.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"));
                    var name = status?.Name ?? string.Empty;
                    return string.Equals(name, expected, StringComparison.Ordinal);
                },
                timeout
            )
            .Result;
        if (!ok)
        {
            var status = main.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"));
            throw new TimeoutException(
                $"StatusText expected '{expected}' but was '{status?.Name}'."
            );
        }
    }

    public static void WaitForStatusContains(Window main, string fragment, TimeSpan timeout)
    {
        var ok = Retry
            .WhileFalse(
                () =>
                {
                    var status = main.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"));
                    var name = status?.Name ?? string.Empty;
                    return name.Contains(fragment, StringComparison.Ordinal);
                },
                timeout
            )
            .Result;
        if (!ok)
        {
            var status = main.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"));
            throw new TimeoutException(
                $"StatusText expected to contain '{fragment}' but was '{status?.Name}'."
            );
        }
    }

    public static void WaitGone(Window window, string automationId, TimeSpan timeout)
    {
        var gone = Retry
            .WhileFalse(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)) is null,
                timeout
            )
            .Result;
        if (!gone)
        {
            throw new TimeoutException($"Element AutomationId='{automationId}' did not disappear.");
        }
    }
}
