# Graft.Core — 利用側ガイド（M2）

## 役割分担

| 側 | プロジェクト例 | 参照するパッケージ | やること |
| -- | -------------- | ------------------ | -------- |
| **対象アプリ（正本）** | `SampleTodoApp` | `Graft.Instrumentation.Wpf` | MVVM/DI/テーマ付き実アプリ。`GRAFT_TEST` で Agent |
| **E2E（正本）** | `SampleTodoApp.Tests` | **`Graft.Core` のみ** | ストーリー E2E 1 本（保存先→追加→Import→フィルタ→テーマ→編集／削除→Export） |
| **機能マトリクス** | `SampleWpfApp` / `.Tests` | 同上 | コントロール網羅・Phase 回帰 |

SmokeClient / `Graft.Core.Tests` はライブラリ自身の検証用。**プロダクト側の書き方の正本は `tests/sample-apps/SampleTodoApp.Tests`。**

## 対象アプリ側（組み込み）

`GraftTest=true` または `-c GraftTest` でビルドし、起動時だけ Agent を立てる:

```csharp
#if GRAFT_TEST
Graft.Instrumentation.Wpf.WpfGraft.Use();
Graft.Instrumentation.Agent.Start();
#endif
```

（実装例: `tests/sample-apps/SampleTodoApp/App.xaml.cs`）

## テスト側（コントローラ）

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
        // 操作タイムライン（任意）。Dispose 後に index.html / frames/*.png
        Timeline = new TimelineOptions
        {
            OutputDirectory = timelineDir,
            Retention = TimelineRetention.Always,
        },
    }
);

// 設定は UserControl オーバーレイ。保存先変更は OpenFolder + ArmOpenFolder
await app.GetByAutomationId("SettingsButton").InvokeAsync();
await app.GetByAutomationId("SettingsView").WaitForAsync();
await app.ArmOpenFolderAsync(dataDir);
_ = await app.GetByAutomationId("SettingsBrowseDataDirectoryButton")
    .InvokeOpeningWindowAsync(waitForNewWindow: false);
await app.GetByAutomationId("SettingsCloseButton").InvokeAsync();
await app.GetByAutomationId("StatusText").ExpectNameAsync("DataDirectoryChanged");

// モーダル詳細窓は InvokeOpeningWindowAsync（素の Invoke は ShowDialog でハングしうる）
var detail = await app.GetByAutomationId("AddButton").InvokeOpeningWindowAsync();
await app.GetByAutomationId("DetailTitleBox").SetValueAsync("Graft E2E Task");
await app.GetByAutomationId("DetailSaveButton").InvokeAsync();
await app.WaitForWindowAsync(automationId: "Main");
await app.GetByAutomationId("StatusText").ExpectNameAsync("ItemAdded");
await app.GetByAutomationId("TodoGrid").SelectRowAsync("Title", "Graft E2E Task");
```

実ファイル: [`TodoStoryE2ETests.cs`](../tests/sample-apps/SampleTodoApp.Tests/TodoStoryE2ETests.cs)  
機能マトリクス例: [`MainWindowE2ETests.cs`](../tests/sample-apps/SampleWpfApp.Tests/MainWindowE2ETests.cs)

## 実行

```bash
dotnet test tests/sample-apps/SampleTodoApp.Tests
# 機能マトリクス:
dotnet test tests/sample-apps/SampleWpfApp.Tests
# 任意・手動（FlaUI 比較。対話デスクトップ前提）:
dotnet test tests/sample-apps/SampleTodoApp.FlaUI.Tests
```

## 補足

- `ConnectAsync` は既にパイプが立っているプロセス向けの低レベル API（ドキュメント第一級ではない）
- Wait / Expect タイムアウトは `app.WaitOptions`（アクション 5s / Expect 10s 既定）
- セレクタ: `GetBy(Selector.…)` / `GetByAutomationId` / `GetByName` / `GetByControlType`。`AutomationId`・`Name`・`ControlType` はハード一致（不一致は `element.notFound`）。相対: `Child` / `Sibling` / `Nth`（Phase 27）
- リストキー選択（Phase 27）: `SelectAsync("Item 35")`（wire `select` + `key`）。ツリーパス: `SelectTreeAsync("Root/Child/Leaf")`（wire `selectTree`）
- 自己修復（Phase 4）: 解決失敗時に Core が代替セレクタ候補を算出。高信頼で一意なら同一 `ElementQuery` で一回だけ自動再解決し、以降そのセレクタを使う。失敗時は `FailureReport.healingCandidates` に候補を添付（シナリオファイルは書き換えない。ファジー一致はしない）
- テキスト入力: `GetByAutomationId(…).SetValueAsync(value)`（エージェント wire `setValue`。TextBox 置換。**PasswordBox** は `Password` 代入（tree/value には載せない）。**RichTextBox** は平文全文置換。**Slider** は InvariantCulture の double 文字列 → `Value`。**DatePicker** は `yyyy-MM-dd` → `SelectedDate`）。キー入力: `SendKeysAsync(text)`（リテラル）。chord / 特殊キー: `PressAsync("Control+A")` / `F5` / `NumPad0` 等（wire `pressKeys`。1 呼び出し = 1 chord。**Win/Meta なし**）
- トグル: `GetByAutomationId(…).ToggleAsync()`（CheckBox / RadioButton / ToggleButton。Radio は選択側へ）
- フォーカス（Phase 29a）: `ExpectFocusedAsync()`（tree `focused`）
- ToolTip（Phase 29b）: `ExpectToolTipAsync(text)`（開いているときだけ tree `toolTip`）。Phase 35 で開時はオーナーの子ノード（`ControlType = ToolTip`）としても合流
- スクロール: `ScrollIntoViewAsync()`（実現済み要素）/ `ScrollIntoViewAsync(index)`（リスト。仮想化対応、identity 返却）
- 選択: `SelectAsync(index)`（単一。内部で自動 scroll/realize）。ホストは ListBox / **ListView** / ComboBox / **DataGrid（行）** / **TabControl**。複数選択: `SelectManyAsync(indexes)`（wire `selectMany`。**ListBox** Multiple/Extended、**DataGrid** Extended+FullRow。置換。空配列でクリア）
- 開閉: `ExpandAsync()` / `CollapseAsync()`（TreeViewItem / Expander / **ComboBox** `IsDropDownOpen`）
- ツリー状態（Phase 6/8/24/29a/29b）: `TreeNode.selected` / `expanded` / `checked`（`bool?`、CheckBox/Radio/Toggle）、`enabled` / `visible` / `focused`、任意 `value`（Slider/ProgressBar/RichText 平文/DatePicker 等。Password は載せない）、任意 `toolTip`（開時のみ）。`ExpectSelectedAsync` / `ExpectExpandedAsync` / `ExpectCheckedAsync` / `ExpectEnabledAsync` / `ExpectVisibleAsync` / `ExpectFocusedAsync` / `ExpectValueAsync` / `ExpectToolTipAsync` / `ExpectNameContainsAsync` / `ExpectNameMatchesAsync`。出現 `WaitForAsync`、消失 `ExpectGoneAsync`、窓 `WaitForWindowClosedAsync`
- DataGrid 行（Phase 8）: ホスト＋index で `ScrollIntoViewAsync` / `SelectAsync`。実現済み `DataGridRow` に `selected`
- DataGrid / ListView セル（Phase 9/21/28/29b）: ホスト＋`(row, column)` または `(row, columnKey)`（Header 文字列）で `GetCellTextAsync` / `ExpectCellTextAsync`。DataGrid は Set 可（Text/CheckBox/Template）。**ListView+GridView は Read のみ**。ツリーにセルは出さない
- DataGrid 高度（Phase 28）: `SelectCellAsync(row, column|columnKey)`（SelectionUnit Cell/CellOrRowHeader）。`SelectRowAsync(columnKey, value)`（表示順非依存・曖昧は `element.ambiguous`）。`ClickColumnHeaderAsync(columnKey)`（ソート UI）。`AddRowAsync` / `DeleteSelectedRowsAsync`
- ウィンドウ（Phase 7）: `ListWindowsAsync` / `SwitchToWindowAsync(windowId)` / `WaitForWindowAsync(title:, automationId:)`（既定で自動 Switch）。getTree / resolve / screenshot / アクションは既定ターゲット窓のみ
- Screenshot（Phase 15 / 35）: `session.ScreenshotAsync()` → `Screenshot`（Format / Width / Height / PngBytes）+ `SaveAsync(path)`。現在ターゲット窓。開いている ToolTip / Popup / ContextMenu は画面座標で合成。Scenario `screenshot` は path 必須。MCP `graft_screenshot` は path 任意（省略時 temp）
- 要素クリップ（Phase 35 / P02 Must）: `GetBy…().ScreenshotAsync()`。窓内は窓 RTB の bounds 交差クリップ（空交差は `element.notActionable`）。開いた Popup 配下は Popup ルート RTB。開時 ToolTip はオーナーの子ノード（`ExpectToolTip` / `toolTip` 文字列は残す）。**開いている ToolTip / Popup は、撮った要素とその子孫の overlay を画面座標で合成**（親コンテナ SS でも内側の Tip が乗る）。wire は既存 `screenshot` + 任意 `automationId` / `runtimeId`。Scenario/MCP は任意 `automationId` のみ。自動 scroll なし。Done（[task_phase35.md](./task_phase35.md)）
- 右クリック（Phase 16）: `RightClickAsync()`（wire `rightClick`）。開いた ContextMenu の MenuItem は通常の `InvokeAsync`（getTree / resolve に開いている ContextMenu を含む）
- マウス高度（Phase 25）: `DoubleClickAsync` / `HoverAsync` / `DragAsync(toAutomationId)`（要素→要素）/ `ClickAtAsync(offsetX, offsetY)`（クリック点相対 DIP）/ `WheelAsync(delta)`。いずれも SendInput。`invoke` は意味的クリックのまま
- Menu バー（Phase 20）: トップレベル / 1段サブとも既存 `InvokeAsync`。開いているサブメニュー（`IsSubmenuOpen`）の MenuItem を getTree / resolve に含む
- メニュー深さ（Phase 26）: ルート（Menu / 開いた ContextMenu）上の `SelectMenuAsync("id1/id2/leaf")`（wire `selectMenu`）。セグメントは AutomationId。ContextMenu は先に `RightClickAsync`。無効項目は `element.notActionable`
- モーダル開封: `GetBy…().InvokeOpeningWindowAsync()`（BeginInvoke + 既定で新窓待ち + 自動 Switch）。**素の `InvokeAsync` で `ShowDialog` を開くとハングしうる**（非対応）
- OpenFile シーム（Phase 10）: アプリは素の `OpenFileDialog`。`WpfGraft.Use` が Harmony で `CommonItemDialog.RunDialog` を差し替え。テストは `ArmOpenFileAsync(path)` / `ArmOpenFileCancelAsync()` → `InvokeOpeningWindowAsync(waitForNewWindow: false)` → Expect。未アーム時は実ダイアログへフォールバック。業務コードに Graft ダイアログ API は不要
- SaveFile シーム（Phase 11）: 同上で素の `SaveFileDialog`。`ArmSaveFileAsync` / `ArmSaveFileCancelAsync`（OpenFile Arm と独立）
- OpenFolder シーム（Phase 12）: 素の `OpenFolderDialog`。`ArmOpenFolderAsync` / `ArmOpenFolderCancelAsync`（結果は `FolderName`。他 Arm と独立）
- MessageBox シーム（Phase 13）: 素の `MessageBox.Show`。`ArmMessageBoxAsync(result)`（`OK`/`Cancel`/`Yes`/`No`/`None`）。未アームは実 MessageBox
- 失敗診断: Expect / Wait / 各アクション失敗時に `GraftException.Report`（最小: step / expected / actual / timedOut / selector。添付: `recentOperations` / `tree` / `screenshotPath` / `healingCandidates`）。エージェントは RPC ごとに常時添付しない。添付は失敗時ベストエフォート
- Scenario JSON: `ScenarioJson.ParseFile` → `ScenarioRunner.RunAsync`（上記に加え `armOpenFile` / `armSaveFile` / `armOpenFolder` / `armMessageBox` / セル・窓系）。契約は `.dev/scenario.schema.json`。例: `tests/sample-apps/SampleWpfApp.Tests/Scenarios/`
- MCP: `Graft.McpServer`（stdio）。原子ツールにダイアログ Arm 系とセル・窓系を含む。失敗時は `IsError` + FailureReport JSON
- invoke / setValue はネイティブ → Peer → SendInput フォールバック（クリック / クリア+タイプ）
- 全解テスト並列（Phase 31 / X04）: 正本は `dotnet test Graft.slnx -m:1`（アセンブリ内は `SampleUiCollection` / `McpUiCollection`）。プロセス mutex だけでは SendInput 前景不足が残るため未採用。X04 は運用 Done（[task_phase31.md](./task_phase31.md)）
- Frame 遷移（Phase 32 / H02）: Sample `SampleFrame` + Page ナビ。専用 DSL なし（既存 WaitFor / Expect）。Done（[task_phase32.md](./task_phase32.md)）
- 操作タイムライン（Phase 33 / D06）: `LaunchOptions.Timeline`（`OutputDirectory` 必須、`Always`/`OnFailure`）。操作完了後 PNG + `index.html`（速度・字幕）。`ScreenshotAsync`（窓・要素クリップ）は撮った PNG をフレームにする（窓を撮り直さない）。`SaveTimeline()` / Dispose で確定。Done（[task_phase33.md](./task_phase33.md)）
- SampleTodoApp（Phase 34）: 利用ガイド正本。R3 + ObservableCollections + MS.DI、実 JSON（設定 UserControl オーバーレイで保存先/`OpenFolderDialog`・テーマ。LocalAppData `settings.json`）、詳細 Window、Export/Import シーム。E2E 隔離は Settings オーバーレイ + `ArmOpenFolder`。ストーリー E2E 1 本（フィルタ／テーマ／チェック編集削除含む）+ `Timeline` Always（`%TEMP%\graft-sample-todo-timeline\{leaf}\index.html`）。`LaunchOptions.Environment` は Core 汎用（任意）。Done（[task_phase34.md](./task_phase34.md)）
- 未実装（後続）: **Avalonia**（Phase 35 後）。正本は [competitive-gap.md](./competitive-gap.md)。typeHuman / Inspector / 画像 diff（P03）等は任意または非目標
