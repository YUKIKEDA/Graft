using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace SampleTodoApp.FlaUI.Tests;

[Collection(SampleTodoUiCollection.Name)]
public sealed class TodoStoryFlaUITests
{
    /// <summary>
    /// FlaUI mirror of SampleTodoApp.Tests Story_FullWorkflow (no Graft Arm* / Timeline).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Debug SampleTodoApp.exe
    /// - settings.json pre-seeded to temp data dir (Browse skipped)
    /// - Fixtures/filter-todos.json for Import via real OpenFileDialog
    ///
    /// Steps:
    /// - Add via DetailWindow → ItemAdded
    /// - Import filter-todos → ImportDone
    /// - Search / Priority / Status filters + Clear
    /// - Settings → DarkTheme → ThemeDark
    /// - Select all → uncheck 2 → Edit → Delete → Export (real SaveFileDialog)
    ///
    /// Expected:
    /// - StatusText updates; export file exists
    /// </remarks>
    [Fact]
    public void Story_FullWorkflow_CoversFiltersThemeAndCrud()
    {
        // UIA3 + common dialogs are more reliable on an STA thread.
        StaRunner.Run(RunStory);
    }

    private static void RunStory()
    {
        var dataDir = NewDataDir();
        var importFixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", "filter-todos.json"));

        // Keep path short: Save dialog uses keyboard entry (ValuePattern often times out).
        var exportPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"graft-flaui-export-{Guid.NewGuid():N}.json"));

        Assert.True(File.Exists(importFixture), importFixture);

        try
        {
            using var session = FlaUITodoLaunch.Launch(dataDir);
            var main = session.MainWindow;
            var automation = session.Automation;

            // Click (not Invoke): returns without waiting for ShowDialog / file dialogs.
            ClickButton(main, "AddButton");
            var detail = FlaUITodoLaunch.WaitForWindowById(automation, "DetailWindow", TimeSpan.FromSeconds(15));
            SetText(detail, "DetailTitleBox", "Graft E2E Task");
            SelectComboIndex(detail, "DetailStatusCombo", 1);
            SelectComboIndex(detail, "DetailPriorityCombo", 2);
            ClickButton(detail, "DetailSaveButton");
            FlaUITodoLaunch.WaitForStatus(main, "ItemAdded", TimeSpan.FromSeconds(15));
            SelectGridRowByTitle(main, "Graft E2E Task");

            // UIA Invoke returns before Win32 ShowDialog finishes; complete dialog on a worker STA.
            InvokeBlockingWithFileDialog(
                main,
                "ImportButton",
                (auto, owner) => Win32FileDialog.CompleteOpen(auto, owner, importFixture, TimeSpan.FromSeconds(25))
            );
            FlaUITodoLaunch.WaitForStatus(main, "ImportDone", TimeSpan.FromSeconds(20));
            SelectGridRowByTitle(main, "サンプル: ドキュメント更新");

            SetText(main, "SearchBox", "ドキュメント");
            SelectGridRowByTitle(main, "サンプル: ドキュメント更新");
            ClickButton(main, "ClearFiltersButton");
            FlaUITodoLaunch.WaitForStatus(main, "FiltersCleared", TimeSpan.FromSeconds(10));

            SelectComboIndex(main, "PriorityFilter", 3);
            SelectGridRowByTitle(main, "サンプル: 設計レビュー");
            ClickButton(main, "ClearFiltersButton");
            FlaUITodoLaunch.WaitForStatus(main, "FiltersCleared", TimeSpan.FromSeconds(10));

            SelectComboIndex(main, "StatusFilter", 3);
            SelectGridRowByTitle(main, "サンプル: 完了済みタスク");
            ClickButton(main, "ClearFiltersButton");
            FlaUITodoLaunch.WaitForStatus(main, "FiltersCleared", TimeSpan.FromSeconds(10));

            ClickButton(main, "SettingsButton");
            Assert.NotNull(Retry.WhileNull(() => main.FindFirstDescendant(c => c.ByAutomationId("SettingsView")), TimeSpan.FromSeconds(10)).Result);
            ToggleCheckBox(main, "SettingsDarkThemeCheckBox");
            ClickButton(main, "SettingsCloseButton");
            FlaUITodoLaunch.WaitGone(main, "SettingsView", TimeSpan.FromSeconds(10));
            FlaUITodoLaunch.WaitForStatus(main, "ThemeDark", TimeSpan.FromSeconds(10));

            ToggleCheckBox(main, "TodoSelectAllCheckBox");
            ToggleCheckBox(main, "TodoRowCheck_1");
            ToggleCheckBox(main, "TodoRowCheck_3");

            WaitEnabled(main, "EditButton", TimeSpan.FromSeconds(10));
            ClickButton(main, "EditButton");
            detail = FlaUITodoLaunch.WaitForWindowById(automation, "DetailWindow", TimeSpan.FromSeconds(15));
            SetText(detail, "DetailTitleBox", "編集済みタスク");
            ClickButton(detail, "DetailSaveButton");
            FlaUITodoLaunch.WaitForStatus(main, "ItemUpdated", TimeSpan.FromSeconds(15));
            SelectGridRowByTitle(main, "編集済みタスク");

            ClickButton(main, "DeleteButton");
            FlaUITodoLaunch.WaitForStatus(main, "ItemDeleted", TimeSpan.FromSeconds(15));

            InvokeBlockingWithFileDialog(
                main,
                "ExportButton",
                (auto, owner) => Win32FileDialog.CompleteSave(auto, owner, exportPath, TimeSpan.FromSeconds(25))
            );
            FlaUITodoLaunch.WaitForStatus(main, "ExportDone", TimeSpan.FromSeconds(20));
            Assert.True(File.Exists(exportPath), exportPath);
        }
        finally
        {
            TryDelete(dataDir);
            TryDeleteFile(exportPath);
            FlaUITodoLaunch.ResetPersistedAppState();
        }
    }

    private static string NewDataDir() => Path.Combine(Path.GetTempPath(), "graft-sample-todo-flaui", Guid.NewGuid().ToString("N"));

    private static void ClickButton(AutomationElement root, string automationId)
    {
        var el = WaitElement(root, automationId, requireEnabled: true);
        el.Focus();
        if (el.Patterns.Invoke.IsSupported)
        {
            el.Patterns.Invoke.Pattern.Invoke();
        }
        else
        {
            el.Click();
        }

        Thread.Sleep(150);
    }

    private static void InvokeBlockingWithFileDialog(
        AutomationElement root,
        string buttonAutomationId,
        Action<global::FlaUI.UIA3.UIA3Automation, AutomationElement> completeDialog
    )
    {
        Exception? dialogError = null;
        var ownerHandle = root.Properties.NativeWindowHandle.ValueOrDefault;
        var dialogThread = StaRunner.Start(() =>
        {
            try
            {
                Thread.Sleep(1000);
                using var dialogAutomation = new global::FlaUI.UIA3.UIA3Automation();
                var owner = dialogAutomation.FromHandle(ownerHandle) ?? throw new InvalidOperationException("Owner window handle lost.");
                completeDialog(dialogAutomation, owner);
            }
            catch (Exception ex)
            {
                dialogError = ex;
            }
        });

        var button = WaitElement(root, buttonAutomationId, requireEnabled: true);
        root.AsWindow().SetForeground();
        button.Focus();
        button.Patterns.Invoke.Pattern.Invoke();

        Assert.True(dialogThread.Join(TimeSpan.FromSeconds(60)), "File dialog worker timed out.");
        if (dialogError is not null)
        {
            throw new AggregateException($"File dialog failed after {buttonAutomationId}.", dialogError);
        }
    }

    private static void WaitEnabled(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var ok = Retry
            .WhileFalse(
                () =>
                {
                    var el = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                    return el is not null && el.IsEnabled;
                },
                timeout
            )
            .Result;
        Assert.True(ok, $"{automationId} did not become enabled.");
    }

    private static void SetText(AutomationElement root, string automationId, string value)
    {
        var el = WaitElement(root, automationId, requireEnabled: true);
        var box = el.AsTextBox();
        box.Focus();
        box.Text = value;
    }

    private static void SelectComboIndex(AutomationElement root, string automationId, int index)
    {
        var el = WaitElement(root, automationId, requireEnabled: true);
        el.AsComboBox().Select(index);
    }

    private static void ToggleCheckBox(AutomationElement root, string automationId)
    {
        var el = WaitElement(root, automationId, requireEnabled: true);
        el.AsCheckBox().Toggle();
    }

    private static AutomationElement WaitElement(AutomationElement root, string automationId, bool requireEnabled)
    {
        var el = Retry
            .WhileNull(
                () =>
                {
                    var found = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                    if (found is null)
                    {
                        return null;
                    }

                    if (requireEnabled && !found.IsEnabled)
                    {
                        return null;
                    }

                    return found;
                },
                TimeSpan.FromSeconds(15)
            )
            .Result;
        return el ?? throw new TimeoutException($"Element AutomationId='{automationId}' not found/enabled.");
    }

    private static void SelectGridRowByTitle(AutomationElement main, string title)
    {
        var grid = main.FindFirstDescendant(c => c.ByAutomationId("TodoGrid")) ?? throw new TimeoutException("TodoGrid");

        var cell = Retry
            .WhileNull(
                () =>
                {
                    foreach (var text in grid.FindAllDescendants(c => c.ByControlType(ControlType.Text)))
                    {
                        if (string.Equals(text.Name, title, StringComparison.Ordinal))
                        {
                            return text;
                        }
                    }

                    foreach (var dataItem in grid.FindAllDescendants(c => c.ByControlType(ControlType.DataItem)))
                    {
                        if (
                            dataItem.Name?.Contains(title, StringComparison.Ordinal) == true
                            || dataItem.FindFirstDescendant(c => c.ByName(title)) is not null
                        )
                        {
                            return dataItem;
                        }
                    }

                    return null;
                },
                TimeSpan.FromSeconds(10)
            )
            .Result;

        Assert.NotNull(cell);
        cell.Click();
        Thread.Sleep(100);
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
