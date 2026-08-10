# Graft.Core — 利用側ガイド（M2）

## 役割分担

| 側 | プロジェクト例 | 参照するパッケージ | やること |
| -- | -------------- | ------------------ | -------- |
| **対象アプリ** | `SampleWpfApp` | `Graft.Instrumentation.Wpf` | `GRAFT_TEST` ビルドで `WpfGraft.Use()` + `Agent.Start()` |
| **E2E テスト** | `SampleWpfApp.Tests` | **`Graft.Core` のみ** | `Application.LaunchAsync` → `GetBy…` で操作・検証 |

SmokeClient / `Graft.Core.Tests` はライブラリ自身の検証用。**プロダクト側の書き方の正本は `tests/sample-apps/SampleWpfApp.Tests`。**

## 対象アプリ側（組み込み）

`GraftTest=true` または `-c GraftTest` でビルドし、起動時だけ Agent を立てる:

```csharp
#if GRAFT_TEST
Graft.Instrumentation.Wpf.WpfGraft.Use();
Graft.Instrumentation.Agent.Start();
#endif
```

（実装例: `tests/sample-apps/SampleWpfApp/App.xaml.cs`）

## テスト側（コントローラ）

```csharp
using Graft.Core;

await using var app = await Application.LaunchAsync(
    new LaunchOptions
    {
        AppPath = @"path\to\YourApp.csproj", // or .exe
        Configuration = "GraftTest",         // csproj のとき GRAFT_TEST 付きで起動
        Timeout = TimeSpan.FromSeconds(60),  // 起動+Handshake（既定 30s）
    }
);

await app.GetByAutomationId("SampleButton").InvokeAsync();
await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");

await app.GetByAutomationId("SampleTextBox").SetValueAsync("hello-graft");
await app.GetByAutomationId("SampleTextBox").ExpectNameAsync("hello-graft");
// Dispose でパイプ切断 + 対象プロセス終了
```

Scenario JSON 経路:

```csharp
using Graft.Core.Scenario;

var scenario = ScenarioJson.ParseFile(@"path\to\sample-main-window.scenario.json");
await ScenarioRunner.RunAsync(
    scenario,
    new ScenarioRunOptions { AppPath = @"path\to\YourApp.csproj" }
);
```

実ファイル: [`MainWindowE2ETests.cs`](../tests/sample-apps/SampleWpfApp.Tests/MainWindowE2ETests.cs) / [`ScenarioE2ETests.cs`](../tests/sample-apps/SampleWpfApp.Tests/ScenarioE2ETests.cs)

## 実行

```bash
dotnet test tests/sample-apps/SampleWpfApp.Tests
```

## 補足

- `ConnectAsync` は既にパイプが立っているプロセス向けの低レベル API（ドキュメント第一級ではない）
- Wait / Expect タイムアウトは `app.WaitOptions`（アクション 5s / Expect 10s 既定）
- セレクタ: `GetBy(Selector.…)` または `GetByAutomationId`。`AutomationId` 指定時はハード一致（不一致は `element.notFound`）
- 自己修復（Phase 4）: 解決失敗時に Core が代替セレクタ候補を算出。高信頼で一意なら同一 `ElementQuery` で一回だけ自動再解決し、以降そのセレクタを使う。失敗時は `FailureReport.healingCandidates` に候補を添付（シナリオファイルは書き換えない。ファジー一致はしない）
- テキスト入力: `GetByAutomationId(…).SetValueAsync(value)`（エージェント wire `setValue`。TextBox 置換。**Slider** は InvariantCulture の double 文字列 → `Value`）。キー入力: `SendKeysAsync(text)`（リテラル）。chord / 特殊キー: `PressAsync("Control+A")`（wire `pressKeys`。1 呼び出し = 1 chord）
- トグル: `GetByAutomationId(…).ToggleAsync()`（状態フリップ）
- スクロール: `ScrollIntoViewAsync()`（実現済み要素）/ `ScrollIntoViewAsync(index)`（リスト。仮想化対応、identity 返却）
- 選択: `SelectAsync(index)`（単一。内部で自動 scroll/realize）。ホストは ListBox / ComboBox / **DataGrid（行）** / **TabControl**。複数選択: `SelectManyAsync(indexes)`（wire `selectMany`。ListBox の Multiple/Extended のみ。置換。空配列でクリア）
- 開閉: `ExpandAsync()` / `CollapseAsync()`（状態指定）
- ツリー状態（Phase 6/8）: `TreeNode.selected` / `expanded` / `checked`（`bool?`、非該当は省略）。`ExpectSelectedAsync` / `ExpectExpandedAsync` / `ExpectCheckedAsync`（null は expect.failed）
- DataGrid 行（Phase 8）: ホスト＋index で `ScrollIntoViewAsync` / `SelectAsync`。実現済み `DataGridRow` に `selected`
- DataGrid セル（Phase 9）: ホスト＋`(row, column)` で `GetCellTextAsync` / `SetCellValueAsync` / `ExpectCellTextAsync`（Text 列のみ。BeginEdit→CommitEdit。ツリーにセルは出さない）
- ウィンドウ（Phase 7）: `ListWindowsAsync` / `SwitchToWindowAsync(windowId)` / `WaitForWindowAsync(title:, automationId:)`（既定で自動 Switch）。getTree / resolve / screenshot / アクションは既定ターゲット窓のみ
- Screenshot（Phase 15）: `ScreenshotAsync()` → `Screenshot`（Format / Width / Height / PngBytes）+ `SaveAsync(path)`。現在ターゲット窓。Scenario `screenshot` は path 必須。MCP `graft_screenshot` は path 任意（省略時 temp）
- 右クリック（Phase 16）: `RightClickAsync()`（wire `rightClick`）。開いた ContextMenu の MenuItem は通常の `InvokeAsync`（getTree / resolve に開いている ContextMenu を含む）
- Menu バー（Phase 20）: トップレベル / 1段サブとも既存 `InvokeAsync`。開いているサブメニュー（`IsSubmenuOpen`）の MenuItem を getTree / resolve に含む
- モーダル開封: `GetBy…().InvokeOpeningWindowAsync()`（BeginInvoke + 既定で新窓待ち + 自動 Switch）。**素の `InvokeAsync` で `ShowDialog` を開くとハングしうる**（非対応）
- OpenFile シーム（Phase 10）: アプリは素の `OpenFileDialog`。`WpfGraft.Use` が Harmony で `CommonItemDialog.RunDialog` を差し替え。テストは `ArmOpenFileAsync(path)` / `ArmOpenFileCancelAsync()` → `InvokeOpeningWindowAsync(waitForNewWindow: false)` → Expect。未アーム時は実ダイアログへフォールバック。業務コードに Graft ダイアログ API は不要
- SaveFile シーム（Phase 11）: 同上で素の `SaveFileDialog`。`ArmSaveFileAsync` / `ArmSaveFileCancelAsync`（OpenFile Arm と独立）
- OpenFolder シーム（Phase 12）: 素の `OpenFolderDialog`。`ArmOpenFolderAsync` / `ArmOpenFolderCancelAsync`（結果は `FolderName`。他 Arm と独立）
- MessageBox シーム（Phase 13）: 素の `MessageBox.Show`。`ArmMessageBoxAsync(result)`（`OK`/`Cancel`/`Yes`/`No`/`None`）。未アームは実 MessageBox
- 失敗診断: Expect / Wait / 各アクション失敗時に `GraftException.Report`（最小: step / expected / actual / timedOut / selector。添付: `recentOperations` / `tree` / `screenshotPath` / `healingCandidates`）。エージェントは RPC ごとに常時添付しない。添付は失敗時ベストエフォート
- Scenario JSON: `ScenarioJson.ParseFile` → `ScenarioRunner.RunAsync`（上記に加え `armOpenFile` / `armSaveFile` / `armOpenFolder` / `armMessageBox` / セル・窓系）。契約は `.dev/scenario.schema.json`。例: `tests/sample-apps/SampleWpfApp.Tests/Scenarios/`
- MCP: `Graft.McpServer`（stdio）。原子ツールにダイアログ Arm 系とセル・窓系を含む。失敗時は `IsError` + FailureReport JSON
- invoke / setValue はネイティブ → Peer → SendInput フォールバック（クリック / クリア+タイプ）
- 未実装（後続）: Menu 任意深さ／パス DSL、ContextMenu サブ、DataGrid 列キー／他列種／複数行選択、Avalonia、Inspector、ファジー自己修復、シナリオ自動書き換え、`typeHuman`、要素クリップ Screenshot、画像 expect/diff
