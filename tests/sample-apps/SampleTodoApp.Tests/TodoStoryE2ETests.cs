using Graft.Core;

namespace SampleTodoApp.Tests;

[Collection(SampleTodoUiCollection.Name)]
public sealed class TodoStoryE2ETests
{
    /// <summary>
    /// Add a todo via detail window and see it on the main grid.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty data directory selected via Settings (ArmOpenFolder + Browse)
    /// - Timeline Always under %TEMP%\graft-sample-todo-timeline\{leaf}\
    ///
    /// Steps:
    /// - InvokeOpeningWindowAsync AddButton → DetailWindow
    /// - SetValue DetailTitleBox, Select Status/Priority, Save
    /// - WaitForWindow Main; Expect StatusText ItemAdded
    /// - SelectRow Title=Graft E2E Task
    /// - Dispose session; expect timeline index.html
    ///
    /// Expected:
    /// - New row is selectable; StatusText is ItemAdded; timeline viewer exists
    /// </remarks>
    [Fact]
    public async Task Story_AddViaDetailWindow_AppearsInGrid()
    {
        var dataDir = NewDataDir();
        var timelineDir = TodoLaunch.ResolveTimelineDirectory(dataDir);
        try
        {
            await using (var app = await TodoLaunch.LaunchAsync(dataDir))
            {
                await app.GetByAutomationId("StatusText").WaitForAsync();
                var detail = await app.GetByAutomationId("AddButton").InvokeOpeningWindowAsync();
                Assert.NotNull(detail);
                Assert.Equal("DetailWindow", detail.AutomationId);

                await app.GetByAutomationId("DetailTitleBox").SetValueAsync("Graft E2E Task");
                await app.GetByAutomationId("DetailStatusCombo").SelectAsync(1); // 進行中
                await app.GetByAutomationId("DetailPriorityCombo").SelectAsync(2); // 高
                await app.GetByAutomationId("DetailSaveButton").InvokeAsync();

                await app.WaitForWindowAsync(automationId: "Main");
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ItemAdded");

                // SelectRow fails if the title is missing from the bound grid.
                await app.GetByAutomationId("TodoGrid").SelectRowAsync("Title", "Graft E2E Task");
            }

            AssertTimelineWritten(timelineDir);
        }
        finally
        {
            TryDelete(dataDir);
            TryDelete(timelineDir);
        }
    }

    /// <summary>
    /// Filter by search text and toggle theme with persistence side effects.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fixture filter-todos.json copied to data dir as todos.json
    /// - Timeline Always under %TEMP%\graft-sample-todo-timeline\{leaf}\
    ///
    /// Steps:
    /// - SetValue SearchBox ドキュメント
    /// - SelectRow Title=サンプル: ドキュメント更新
    /// - Settings → toggle DarkTheme → Close → Expect ThemeDark
    /// - Clear filters → Expect FiltersCleared
    /// - Dispose session; expect timeline index.html
    ///
    /// Expected:
    /// - Filter narrows rows; theme status updates; timeline viewer exists
    /// </remarks>
    [Fact]
    public async Task Story_FilterAndTheme_UpdatesStatus()
    {
        var dataDir = NewDataDir();
        var timelineDir = TodoLaunch.ResolveTimelineDirectory(dataDir);
        try
        {
            SeedDataFile(dataDir, "filter-todos.json");
            await using (var app = await TodoLaunch.LaunchAsync(dataDir))
            {
                await app.GetByAutomationId("SearchBox").SetValueAsync("ドキュメント");
                await app.GetByAutomationId("TodoGrid")
                    .SelectRowAsync("Title", "サンプル: ドキュメント更新");

                await app.GetByAutomationId("SettingsButton").InvokeAsync();
                await app.GetByAutomationId("SettingsView").WaitForAsync();
                await app.GetByAutomationId("SettingsDarkThemeCheckBox").ToggleAsync();
                await app.GetByAutomationId("SettingsCloseButton").InvokeAsync();
                await app.GetByAutomationId("SettingsView").ExpectGoneAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ThemeDark");

                await app.GetByAutomationId("ClearFiltersButton").InvokeAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("FiltersCleared");
            }

            AssertTimelineWritten(timelineDir);
        }
        finally
        {
            TryDelete(dataDir);
            TryDelete(timelineDir);
        }
    }

    /// <summary>
    /// Export then import JSON through real file dialogs (Graft seams).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fixture import-todos.json in test output
    /// - Timeline Always for each session
    ///
    /// Steps:
    /// - ArmSaveFile + Export → ExportDone
    /// - New session on fresh data dir
    /// - ArmOpenFile fixture + Import → ImportDone
    /// - SelectRow インポート済みタスク
    ///
    /// Expected:
    /// - Imported title is present after real file import; each session wrote index.html
    /// </remarks>
    [Fact]
    public async Task Story_ExportThenImport_RestoresItemsFromFile()
    {
        var dataDir = NewDataDir();
        var timelineDir = TodoLaunch.ResolveTimelineDirectory(dataDir);
        var exportPath = Path.Combine(dataDir, "exported.json");
        var importFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "import-todos.json");
        Assert.True(File.Exists(importFixture), importFixture);

        try
        {
            await using (var app = await TodoLaunch.LaunchAsync(dataDir))
            {
                await app.ArmSaveFileAsync(exportPath);
                _ = await app.GetByAutomationId("ExportButton")
                    .InvokeOpeningWindowAsync(waitForNewWindow: false);
                await app.GetByAutomationId("StatusText").ExpectNameAsync("ExportDone");
                Assert.True(File.Exists(exportPath));
            }

            AssertTimelineWritten(timelineDir);

            var importDir = NewDataDir();
            var importTimelineDir = TodoLaunch.ResolveTimelineDirectory(importDir);
            try
            {
                await using (var app2 = await TodoLaunch.LaunchAsync(importDir))
                {
                    await app2.ArmOpenFileAsync(importFixture);
                    _ = await app2
                        .GetByAutomationId("ImportButton")
                        .InvokeOpeningWindowAsync(waitForNewWindow: false);
                    await app2.GetByAutomationId("StatusText").ExpectNameAsync("ImportDone");
                    await app2.GetByAutomationId("TodoGrid")
                        .SelectRowAsync("Title", "インポート済みタスク");
                }

                AssertTimelineWritten(importTimelineDir);
            }
            finally
            {
                TryDelete(importDir);
                TryDelete(importTimelineDir);
            }
        }
        finally
        {
            TryDelete(dataDir);
            TryDelete(timelineDir);
        }
    }

    private static string NewDataDir() =>
        Path.Combine(Path.GetTempPath(), "graft-sample-todo", Guid.NewGuid().ToString("N"));

    private static void SeedDataFile(string dataDir, string fixtureFileName)
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureFileName);
        Assert.True(File.Exists(fixture), fixture);
        Directory.CreateDirectory(dataDir);
        File.Copy(fixture, Path.Combine(dataDir, "todos.json"), overwrite: true);
    }

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
