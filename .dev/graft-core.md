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
- セレクタ: `GetBy(Selector.…)` または `GetByAutomationId`
- テキスト入力: `GetByAutomationId(…).SetValueAsync(value)`（エージェント wire `setValue`）
- 失敗診断: Expect / Wait / Invoke / SetValue 失敗時に `GraftException.Report`（最小: step / expected / actual / timedOut / selector。添付: `recentOperations` / `tree` / `screenshotPath`）。エージェントは RPC ごとに常時添付しない。添付は失敗時ベストエフォート
- Scenario JSON: `ScenarioJson.ParseFile` → `ScenarioRunner.RunAsync`（`launch` / `invoke` / `setValue` / `expectName`）。契約は `.dev/scenario.schema.json`。例: `tests/sample-apps/SampleWpfApp.Tests/Scenarios/`
- MCP: `Graft.McpServer`（stdio）。`graft_ping` / `graft_run_scenario`（失敗時は `IsError` + FailureReport JSON）
- 未実装（Phase 1 余り / 後続）: `toggle`、キー入力、SendInput フォールバック
