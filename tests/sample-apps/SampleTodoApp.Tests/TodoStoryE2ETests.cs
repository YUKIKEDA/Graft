using Graft.Core;

namespace SampleTodoApp.Tests;

[Collection(SampleTodoUiCollection.Name)]
public sealed class TodoStoryE2ETests
{
    /// <summary>
    /// End-to-end SampleTodoApp story: settings, CRUD, import/export, filters, theme, checkbox edit/delete.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fresh LocalAppData (TodoLaunch.ResetPersistedAppState)
    /// - Empty data dir selected via Settings (ArmOpenFolder + Browse)
    /// - Fixtures/filter-todos.json available for Import
    /// - Timeline Always under %TEMP%\graft-sample-todo-timeline\{leaf}\
    ///
    /// Steps:
    /// - Add via DetailWindow → ItemAdded
    /// - ArmOpenFile filter-todos + Import → ImportDone (replaces list with 3 rows)
    /// - SearchBox filter → SelectRow → ClearFilters
    /// - PriorityFilter → ClearFilters
    /// - StatusFilter → ClearFilters
    /// - Settings → DarkTheme → ThemeDark
    /// - TodoSelectAllCheckBox → uncheck 2 rows (leave 1) → Edit title → Delete → Export
    /// - Dispose session; expect timeline index.html
    ///
    /// Expected:
    /// - StatusText updates at each stage; export file exists; timeline viewer exists
    /// </remarks>
    [Fact]
    public async Task Story_FullWorkflow_CoversFiltersThemeAndCrud()
    {
        var dataDir = NewDataDir();
        var timelineDir = TodoLaunch.ResolveTimelineDirectory(dataDir);
        var importFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "filter-todos.json");
        var exportPath = Path.Combine(dataDir, "exported.json");
        Assert.True(File.Exists(importFixture), importFixture);

        try
        {
            await using (var app = await TodoLaunch.LaunchAsync(dataDir))
            {
                // --- Add ---
                var detail = await app.GetByAutomationId("AddButton").InvokeOpeningWindowAsync();
                Assert.NotNull(detail);
                Assert.Equal("DetailWindow", detail.AutomationId);
                await app.GetByAutomationId("DetailTitleBox").SetValueAsync("Graft E2E Task");
                await app.GetByAutomationId("DetailStatusCombo").SelectAsync(1); // 進行中
                await app.GetByAutomationId("DetailPriorityCombo").SelectAsync(2); // 高
                await app.GetByAutomationId("DetailSaveButton").InvokeAsync();
                await app.WaitForWindowAsync(automationId: "Main");
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ItemAdded");
                await app.GetByAutomationId("TodoGrid").SelectRowAsync("Title", "Graft E2E Task");

                // --- Import (replaces items with fixture) ---
                await app.ArmOpenFileAsync(importFixture);
                _ = await app.GetByAutomationId("ImportButton")
                    .InvokeOpeningWindowAsync(waitForNewWindow: false);
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ImportDone");
                await app.GetByAutomationId("TodoGrid")
                    .SelectRowAsync("Title", "サンプル: ドキュメント更新");

                // --- Search filter ---
                await app.GetByAutomationId("SearchBox").SetValueAsync("ドキュメント");
                await app.GetByAutomationId("TodoGrid")
                    .SelectRowAsync("Title", "サンプル: ドキュメント更新");
                await app.GetByAutomationId("ClearFiltersButton").InvokeAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("FiltersCleared");

                // --- Priority filter ---
                await app.GetByAutomationId("PriorityFilter").SelectAsync(3); // 高
                await app.GetByAutomationId("TodoGrid")
                    .SelectRowAsync("Title", "サンプル: 設計レビュー");
                await app.GetByAutomationId("ClearFiltersButton").InvokeAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("FiltersCleared");

                // --- Status filter ---
                await app.GetByAutomationId("StatusFilter").SelectAsync(3); // 完了
                await app.GetByAutomationId("TodoGrid")
                    .SelectRowAsync("Title", "サンプル: 完了済みタスク");
                await app.GetByAutomationId("ClearFiltersButton").InvokeAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("FiltersCleared");

                // --- Theme ---
                await app.GetByAutomationId("SettingsButton").InvokeAsync();
                await app.GetByAutomationId("SettingsView").WaitForAsync();
                await app.GetByAutomationId("SettingsDarkThemeCheckBox").ToggleAsync();
                await app.GetByAutomationId("SettingsCloseButton").InvokeAsync();
                await app.GetByAutomationId("SettingsView").ExpectGoneAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ThemeDark");

                // --- Checkbox: select all → leave one → edit → delete ---
                await app.GetByAutomationId("TodoSelectAllCheckBox").ToggleAsync();
                await app.GetByAutomationId("TodoRowCheck_1").ToggleAsync();
                await app.GetByAutomationId("TodoRowCheck_3").ToggleAsync();

                // Id 2 (ドキュメント更新) remains checked — Edit enables only for a single check.
                await app.GetByAutomationId("EditButton").WaitForAsync();
                var edit = await app.GetByAutomationId("EditButton").InvokeOpeningWindowAsync();
                Assert.NotNull(edit);
                Assert.Equal("DetailWindow", edit.AutomationId);
                await app.GetByAutomationId("DetailTitleBox").SetValueAsync("編集済みタスク");
                await app.GetByAutomationId("DetailSaveButton").InvokeAsync();
                await app.WaitForWindowAsync(automationId: "Main");
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ItemUpdated");
                await app.GetByAutomationId("TodoGrid").SelectRowAsync("Title", "編集済みタスク");

                await app.GetByAutomationId("DeleteButton").InvokeAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ItemDeleted");

                // --- Export remaining rows ---
                await app.ArmSaveFileAsync(exportPath);
                _ = await app.GetByAutomationId("ExportButton")
                    .InvokeOpeningWindowAsync(waitForNewWindow: false);
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ExportDone");
                Assert.True(File.Exists(exportPath), exportPath);
            }

            AssertTimelineWritten(timelineDir);
        }
        finally
        {
            TryDelete(dataDir);
            TryDelete(timelineDir);
        }
    }

    private static string NewDataDir() =>
        Path.Combine(Path.GetTempPath(), "graft-sample-todo", Guid.NewGuid().ToString("N"));

    private static void AssertTimelineWritten(string timelineDir)
    {
        var index = Path.Combine(timelineDir, "index.html");
        Assert.True(File.Exists(index), index);
        Assert.True(Directory.Exists(Path.Combine(timelineDir, "frames")), timelineDir);
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(timelineDir, "frames"), "*.png"));
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
}
