# Graft.Core — caller guide (M2)

## Who does what

| Side                           | Example project           | Package                     | What it does                                                                          |
| ------------------------------ | ------------------------- | --------------------------- | ------------------------------------------------------------------------------------- |
| **App under test (canonical)** | `SampleTodoApp`           | `Graft.Instrumentation.Wpf` | Real MVVM/DI/themed app. Agent when `GRAFT_TEST` is defined                           |
| **E2E (canonical)**            | `SampleTodoApp.Tests`     | **`Graft.Core` only**       | One story E2E (data directory → add → Import → filter → theme → edit/delete → Export) |
| **Feature matrix**             | `SampleWpfApp` / `.Tests` | same                        | Control coverage and phase regression                                                 |

SmokeClient and `Graft.Core.Tests` verify the library itself. **The canonical way to write a product test is `tests/sample-apps/SampleTodoApp.Tests`.**

## App under test (embed)

Build with `GraftTest=true` or `-c GraftTest`, and start the agent only at startup:

```csharp
#if GRAFT_TEST
Graft.Instrumentation.Wpf.WpfGraft.Use();
Graft.Instrumentation.Agent.Start();
#endif
```

(Example: `tests/sample-apps/SampleTodoApp/App.xaml.cs`)

For a control the built-in map does not know, register `WpfControlActions.RegisterInvoke` / `RegisterSetValue` / `RegisterToggle` under the same `#if GRAFT_TEST`. `true` finishes the action. `false` continues with native, then Peer, then SendInput. The registration closest to the runtime type wins. Disabled or hidden elements are not passed to a handler. Telerik, DevExpress, and Syncfusion implementations are not shipped. `WpfGraft.ResetForTests` clears the registrations too.

## Test side (controller)

```csharp
using Graft.Core;

var dataDir = Path.Combine(Path.GetTempPath(), "my-todo-e2e");
Directory.CreateDirectory(dataDir);

var timelineDir = Path.Combine(Path.GetTempPath(), "my-todo-timeline");
await using var app = await Application.LaunchAsync(
    new LaunchOptions
    {
        AppPath = @"path\to\SampleTodoApp.csproj",
        Configuration = "GraftTest",
        Timeout = TimeSpan.FromSeconds(90),
        // Optional action timeline. After dispose: index.html / frames/*.png
        Timeline = new TimelineOptions
        {
            OutputDirectory = timelineDir,
            Retention = TimelineRetention.Always,
        },
    }
);

// Settings is a UserControl overlay. Changing the data directory is OpenFolder + ArmOpenFolder
await app.GetByAutomationId("SettingsButton").InvokeAsync();
await app.GetByAutomationId("SettingsView").WaitForAsync();
await app.ArmOpenFolderAsync(dataDir);
_ = await app.GetByAutomationId("SettingsBrowseDataDirectoryButton")
    .InvokeOpeningWindowAsync(waitForNewWindow: false);
await app.GetByAutomationId("SettingsCloseButton").InvokeAsync();
await app.GetByAutomationId("StatusText").ExpectNameAsync("DataDirectoryChanged");

// A modal detail window uses InvokeOpeningWindowAsync (a plain Invoke of ShowDialog can hang)
var detail = await app.GetByAutomationId("AddButton").InvokeOpeningWindowAsync();
await app.GetByAutomationId("DetailTitleBox").SetValueAsync("Graft E2E Task");
await app.GetByAutomationId("DetailSaveButton").InvokeAsync();
await app.WaitForWindowAsync(automationId: "Main");
await app.GetByAutomationId("StatusText").ExpectNameAsync("ItemAdded");
await app.GetByAutomationId("TodoGrid").SelectRowAsync("Title", "Graft E2E Task");
```

The real file is [`TodoStoryE2ETests.cs`](../tests/sample-apps/SampleTodoApp.Tests/TodoStoryE2ETests.cs).
A feature-matrix example is [`MainWindowE2ETests.cs`](../tests/sample-apps/SampleWpfApp.Tests/Windows/MainWindowE2ETests.cs).

## Run

```bash
dotnet test tests/sample-apps/SampleTodoApp.Tests
# Feature matrix:
dotnet test tests/sample-apps/SampleWpfApp.Tests
# Optional and manual (FlaUI comparison. Needs an interactive desktop):
dotnet test tests/sample-apps/SampleTodoApp.FlaUI.Tests
```

## Notes

- `ConnectAsync` is a low-level API for a process whose pipe is already up (not a first-class documented step)
- Wait / Expect timeouts are `app.WaitOptions` (action 5s / Expect 10s by default)
- Selectors: `GetBy(Selector.…)` / `GetByAutomationId` / `GetByName` / `GetByControlType`. `AutomationId`, `Name`, and `ControlType` are hard matches (a miss is `element.notFound`). Relative: `Child` / `Sibling` / `Nth` (Phase 27)
- List key selection (Phase 27): `SelectAsync("Item 35")` (wire `select` + `key`). Tree path: `SelectTreeAsync("Root/Child/Leaf")` (wire `selectTree`)
- Self-heal (Phase 4): on a resolve failure, Core computes alternate selector candidates. A relaxed candidate that is a subset of the original conditions is retried once during that call when it is unique and trusted. The healed selector lasts for the rest of the call, including later polls, and the next call on the same query starts from the original selector. A stableIdentity candidate (Name+ControlType+Near) is unrelated to the original selector, so it is reported and not applied. On failure, candidates are attached as `FailureReport.healingCandidates` (the scenario file is not rewritten; there is no fuzzy match)
- Text input: `GetByAutomationId(…).SetValueAsync(value)` (agent wire `setValue`. TextBox replace. **PasswordBox** assigns `Password` and does not put it on the tree or the value. **RichTextBox** replaces the whole plain text. **Slider** parses an InvariantCulture double string into `Value`. **DatePicker** parses `yyyy-MM-dd` into `SelectedDate`). Keys: `SendKeysAsync(text)` (literal, immediate). Human-paced: `TypeHumanAsync(text, delay)` (wire `typeHuman`. One Unicode scalar at a time. `delay` is between scalars and is waited on the request thread, so a debounce can run in the gap. Existing text is not cleared. Zero is allowed). Chord / special keys: `PressAsync("Control+A")` / `F5` / `NumPad0` and similar (wire `pressKeys`. One call is one chord. **No Win/Meta**)
- Toggle: `GetByAutomationId(…).ToggleAsync()` (CheckBox / RadioButton / ToggleButton. Radio moves to the selected side)
- Focus (Phase 29a): `ExpectFocusedAsync()` (tree `focused`)
- ToolTip (Phase 29b): `ExpectToolTipAsync(text)` (tree `toolTip` only while open). Phase 35 also merges an open tip as a child of the owner (`ControlType = ToolTip`)
- Scroll: `ScrollIntoViewAsync()` (already realized) / `ScrollIntoViewAsync(index)` (list, virtualization-aware, returns identity)
- Select: `SelectAsync(index)` (single; scrolls and realizes internally). Hosts: ListBox / **ListView** / ComboBox / **DataGrid (row)** / **TabControl**. Multi-select: `SelectManyAsync(indexes)` (wire `selectMany`. **ListBox** Multiple/Extended, **DataGrid** Extended+FullRow. Replaces. An empty array clears)
- Expand: `ExpandAsync()` / `CollapseAsync()` (TreeViewItem / Expander / **ComboBox** `IsDropDownOpen`)
- Tree state (Phase 6/8/24/29a/29b): `TreeNode.selected` / `expanded` / `checked` (`bool?`, CheckBox/Radio/Toggle), `enabled` / `visible` / `focused`, optional `value` (Slider/ProgressBar/RichText plain text/DatePicker and similar. Password is omitted), optional `toolTip` (only while open). `ExpectSelectedAsync` / `ExpectExpandedAsync` / `ExpectCheckedAsync` / `ExpectEnabledAsync` / `ExpectVisibleAsync` / `ExpectFocusedAsync` / `ExpectValueAsync` / `ExpectToolTipAsync` / `ExpectNameContainsAsync` / `ExpectNameMatchesAsync`. Appear with `WaitForAsync`, disappear with `ExpectGoneAsync`, window with `WaitForWindowClosedAsync`
- DataGrid row (Phase 8): host plus index for `ScrollIntoViewAsync` / `SelectAsync`. A realized `DataGridRow` has `selected`
- DataGrid / ListView cell (Phase 9/21/28/29b): host plus `(row, column)` or `(row, columnKey)` (Header string) for `GetCellTextAsync` / `ExpectCellTextAsync`. DataGrid can Set (Text/CheckBox/Template). **ListView+GridView is read-only**. Cells are not emitted on the tree
- DataGrid advanced (Phase 28): `SelectCellAsync(row, column|columnKey)` (SelectionUnit Cell/CellOrRowHeader). `SelectRowAsync(columnKey, value)` (independent of display order; ambiguity is `element.ambiguous`). `ClickColumnHeaderAsync(columnKey)` (sort UI). `AddRowAsync` / `DeleteSelectedRowsAsync`
- Windows (Phase 7): `ListWindowsAsync` / `SwitchToWindowAsync(windowId)` / `WaitForWindowAsync(title:, automationId:)` (switches by default). getTree / resolve / screenshot / actions use the current target window only
- Screenshot (Phase 15 / 35): `session.ScreenshotAsync()` → `Screenshot` (Format / Width / Height / PngBytes) + `SaveAsync(path)`. Current target window. An open ToolTip / Popup / ContextMenu is composited in screen coordinates. Scenario `screenshot` requires a path. MCP `graft_screenshot` makes the path optional (temp when omitted)
- Element clip (Phase 35 / P02 Must): `GetBy…().ScreenshotAsync()`. Inside a window, the clip is the intersection of the window RTB and the bounds (an empty intersection is `element.notActionable`). Under an open Popup, the clip uses the popup root RTB. An open ToolTip is a child of the owner (`ExpectToolTip` / the `toolTip` string remain). **An open ToolTip or Popup composites that element's overlay, and its descendants, in screen coordinates** (a parent-container screenshot still includes the tip inside it). The wire is the existing `screenshot` plus the public selector fields (`automationId`, `name`, `controlType`, `nearAutomationId`, `nth`). Core does not send `runtimeId`. Scenario/MCP take an optional `automationId` only. No automatic scroll. Done
- Right-click (Phase 16): `RightClickAsync()` (wire `rightClick`). A MenuItem in the open ContextMenu uses the normal `InvokeAsync` (getTree / resolve include an open ContextMenu)
- Advanced mouse (Phase 25): `DoubleClickAsync` / `HoverAsync` / `DragAsync(toAutomationId)` (element to element) / `ClickAtAsync(offsetX, offsetY)` (DIP relative to the click point) / `WheelAsync(delta)`. All of them use SendInput. `invoke` stays a semantic click
- Menu bar (Phase 20): top level and one submenu both use the existing `InvokeAsync`. MenuItems of an open submenu (`IsSubmenuOpen`) are included in getTree / resolve
- Menu depth (Phase 26): `SelectMenuAsync("id1/id2/leaf")` on a root (Menu / open ContextMenu) (wire `selectMenu`). Segments are AutomationIds. Right-click a ContextMenu first. A disabled item is `element.notActionable`
- Opening a modal: `GetBy…().InvokeOpeningWindowAsync()` (BeginInvoke, then by default wait for the new window and switch). **A plain `InvokeAsync` that opens `ShowDialog` can hang** (not supported)
- OpenFile seam (Phase 10): the app keeps a plain `OpenFileDialog`. `WpfGraft.Use` replaces `CommonItemDialog.RunDialog` with Harmony. The test does `ArmOpenFileAsync(path)` / `ArmOpenFileCancelAsync()` → `InvokeOpeningWindowAsync(waitForNewWindow: false)` → Expect. Unarmed calls fall through to the real dialog. Application code does not call a Graft dialog API
- SaveFile seam (Phase 11): the same pattern for a plain `SaveFileDialog`. `ArmSaveFileAsync` / `ArmSaveFileCancelAsync` (independent of the OpenFile arm)
- OpenFolder seam (Phase 12): a plain `OpenFolderDialog`. `ArmOpenFolderAsync` / `ArmOpenFolderCancelAsync` (the result is `FolderName`. Independent of the other arms)
- MessageBox seam (Phase 13): a plain `MessageBox.Show`. `ArmMessageBoxAsync(result)` (`OK`/`Cancel`/`Yes`/`No`/`None`). Unarmed calls show the real MessageBox
- Failure diagnosis: Expect / Wait / each action failure throws `GraftException.Report` (minimum: step / expected / actual / timedOut / selector. Attachments: `recentOperations` / `tree` / `screenshotPath` / `healingCandidates` / `treeDiff`). Session operations (dialog arms, window list / switch / wait, and session `ScreenshotAsync`) attach the same report, with an empty selector. `screenshotPath` is `%TEMP%\graft-fail-<guid>.png`. Graft does not delete that file, and it is separate from the timeline directory. `treeDiff` is a diagnostic diff against the previous successful getTree (added/removed/changed). It is omitted when there is no baseline, or when `GraftSession.IncludeTreeDiff = false`. Switching the target window drops the baseline. The agent does not attach this on every RPC. Attachment is best-effort on failure
- Soft assert: `Check` on `session.SoftAssert()` stores `GraftException`, and `DisposeAsync` throws `expect.failed` once (step `softAssert`; individual reports are in `failures`). Expect outside `Check` still fails immediately. Scenario / MCP have no soft step
- Scenario JSON: `ScenarioJson.ParseFile` → `ScenarioRunner.RunAsync` (plus `armOpenFile` / `armSaveFile` / `armOpenFolder` / `armMessageBox` and the cell and window steps). The contract is [`docs/scenario.schema.json`](scenario.schema.json). Examples: `tests/sample-apps/SampleWpfApp.Tests/Scenarios/`
- MCP: `Graft.McpServer` (stdio). Atomic tools include dialog arms and the cell and window steps. Failure is `IsError` plus FailureReport JSON
- invoke / setValue fall back native → Peer → SendInput (click / clear+type)
- Whole-solution test parallelism (Phase 31 / X04): the canonical command is `dotnet test Graft.slnx -m:1` (inside an assembly, `SampleUiCollection` / `McpUiCollection`; the caller template is `Graft.TestUtilities.GraftAppFixture`). A process mutex was not adopted because it still could not keep the SendInput foreground. X04 is Done as a practice
- Frame navigation (Phase 32 / H02): sample `SampleFrame` plus page navigation. No dedicated DSL (existing WaitFor / Expect). Done
- Action timeline (Phase 33 / D06): `LaunchOptions.Timeline` (`OutputDirectory` required, `Always`/`OnFailure`). After each action, a PNG plus `index.html` (pace and captions). `ScreenshotAsync` (window or element clip) uses the PNG it already took as the frame (it does not recapture the window). `SaveTimeline()` / dispose finalizes it. Done
- SampleTodoApp (Phase 34): canonical caller guide. R3 + ObservableCollections + MS.DI, real JSON (settings UserControl overlay for the data directory / `OpenFolderDialog` and the theme. LocalAppData `settings.json`), a detail Window, Export/Import seams. E2E isolation is the Settings overlay plus `ArmOpenFolder`. One story E2E (including filter / theme / checked edit and delete) plus `Timeline` Always (`%TEMP%\graft-sample-todo-timeline\{leaf}\index.html`). `LaunchOptions.Environment` is a general Core option (optional). Done
- Not implemented yet: Inspector and image diff (P03) are optional or out of scope. The matrix is [`docs/competitive-gap.md`](competitive-gap.md)
