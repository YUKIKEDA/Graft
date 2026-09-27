# Graft — In-process UI testing for WPF & AvaloniaUI

[![CI](https://github.com/YUKIKEDA/Graft/actions/workflows/ci.yml/badge.svg)](https://github.com/YUKIKEDA/Graft/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

WPF / AvaloniaUI 向けの **in-process GUI E2E テスト** ツールです。

対象アプリにエージェントを事前組み込み、Visual Tree へ直接アクセスします。FlaUI などが使う UI Automation（UIA）の COM 越し走査ではなく、自社アプリ限定で TestComplete の Open Applications に近い精度を狙います。

> **現状:** WPF（.NET 8+）は利用できます。Avalonia アダプタは未実装です。パッケージ版は **0.1.0**（1.0 未満。公開 API は壊れることがあります）。NuGet.org への公開はタグ `v*` で行います。最初のタグを push するまでは、このリポジトリの `ProjectReference` でも使えます。

## なぜ in-process か

| ツール                                                    | アクセス方式                                     | 対象                              |
| --------------------------------------------------------- | ------------------------------------------------ | --------------------------------- |
| FlaUI / WinAppDriver / TestStack.White / Appium (Windows) | UIA (COM) ラップ                                 | 汎用 Windows                      |
| TestComplete                                              | フレームワーク別の in-process アクセス（非公開） | 商用・多フレームワーク            |
| **Graft**                                                 | 対象アプリへの事前組み込み                       | **自社 WPF / Avalonia、OSS 想定** |

サードパーティ製 exe のブラックボックステストは対象外です。Playwright が「自分たちの Web アプリ」を対象にするのと同じ立ち位置です。

プロセス注入（`CreateRemoteThread` + `LoadLibrary`）は使いません。アプリ自身がテスト用パッケージを参照し、起動時に名前付きパイプを立てます。

## 必要なもの

- Windows（インタラクティブなデスクトップセッション）
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- UI テストは画面ロックしないこと（Session 0 / オフスクリーン専用モードは非対応）

## このリポジトリで試す

```powershell
dotnet tool restore
dotnet build Graft.slnx

# 利用例の正本（ストーリー E2E）
dotnet test tests/sample-apps/SampleTodoApp.Tests

# コントロール網羅・Phase 回帰
dotnet test tests/sample-apps/SampleWpfApp.Tests

# ソリューション全体。SendInput 系は並列起動でフレークするため -m:1 必須
dotnet test Graft.slnx -m:1
```

サンプル:

| プロジェクト                            | 役割                                                        |
| --------------------------------------- | ----------------------------------------------------------- |
| `tests/sample-apps/SampleTodoApp`       | MVVM/DI/テーマ付き実アプリ（組み込み側の正本）              |
| `tests/sample-apps/SampleTodoApp.Tests` | Fluent API によるストーリー E2E（**テストの書き方の正本**） |
| `tests/sample-apps/SampleWpfApp.Tests`  | 機能マトリクス                                              |

詳細な API 一覧は [`.dev/graft-core.md`](.dev/graft-core.md) を見てください。

## 自分のアプリに組み込む

役割は次の 2 つに分かれます。

| 側               | 参照                        | やること                         |
| ---------------- | --------------------------- | -------------------------------- |
| 対象アプリ       | `Graft.Instrumentation.Wpf` | `GRAFT_TEST` 時だけ Agent を起動 |
| E2E プロジェクト | `Graft.Core` のみ           | `Application.LaunchAsync` で操作 |

公開パッケージはすべて**同じバージョン**です（`Graft.Core` 0.1.0 と `Graft.Instrumentation.Wpf` 0.1.0 を揃える）。ワイヤの互換は NuGet 版とは別の整数 `ProtocolVersion.Current` で、完全一致しないと `protocol.versionMismatch` になります。メッセージには双方のパッケージ版とプロトコル版が出ます。

| パッケージ | 誰が参照するか |
| --- | --- |
| `Graft.Instrumentation.Wpf` | 対象アプリ。`Graft.Instrumentation` と `Graft.Protocol` が依存として付く。GRAFT001 の DLL は `analyzers/dotnet/cs` に同梱 |
| `Graft.Instrumentation` | WPF パッケージの依存。直接参照しても GRAFT001 は同梱される |
| `Graft.Instrumentation.Analyzer` | 単体でも pack する。アプリは上のパッケージ経由で足りる |
| `Graft.Core` | テスト。`Graft.Protocol` は依存として付く |
| `Graft.TestUtilities` | テスト（任意）。xUnit の共有フィクスチャ |

```powershell
dotnet add package Graft.Instrumentation.Wpf --version 0.1.0
dotnet add package Graft.Core --version 0.1.0
```

NuGet の `Graft.Instrumentation` / `Graft.Instrumentation.Wpf` は `GraftTest` 構成で pack してあり、DLL に Agent と WPF パッチが入っています。呼び出し側の `#if GRAFT_TEST` と GRAFT001 は、これまでどおり利用プロジェクト側の記号で決まります。このリポジトリを `ProjectReference` するときは、Debug / Release の出力には Agent は入らないので、エージェントが要る参照は `Configuration=GraftTest` にしてください。

### 1. 対象アプリ（WPF）

NuGet 参照では `PackageReference` だけで props / targets が入ります。このリポジトリを直接参照する場合の csproj:

```xml
<Import Project="path\to\src\Graft.Instrumentation.Wpf\build\Graft.props" />

<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="path\to\src\Graft.Instrumentation.Wpf\Graft.Instrumentation.Wpf.csproj" />
</ItemGroup>

<Import Project="path\to\src\Graft.Instrumentation.Wpf\build\Graft.targets" />
```

起動（`App.xaml.cs` など）:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

#if GRAFT_TEST
    Graft.Instrumentation.Wpf.WpfGraft.Use();
    Graft.Instrumentation.Agent.Start();
#endif
}

protected override void OnExit(ExitEventArgs e)
{
#if GRAFT_TEST
    Graft.Instrumentation.Agent.Stop();
#endif
    base.OnExit(e);
}
```

有効化:

```powershell
dotnet build -p:GraftTest=true
# またはサンプルと同じ便利構成名
dotnet build -c GraftTest
```

`GraftTest=true`（または `-c GraftTest`）のときだけ、対象アプリと `Graft.Instrumentation` / `Graft.Instrumentation.Wpf` に記号 `GRAFT_TEST` が付きます。Debug / Release の参照には `Agent.Start`、パイプサーバー、WPF の Harmony パッチは入りません。`GRAFT_TEST` が無いコンパイルで `Agent.Start` を参照すると Analyzer **GRAFT001** がエラーになります。Debug 構成への自動紐づけはありません。

実行時はさらに `GRAFT_ENABLE=1` が無い限りパイプを立てません。`Application.LaunchAsync` がこの環境変数（パイプ名・トークン含む）を付与します。

### 2. テスト側

テストプロジェクトは `Graft.Core` だけを参照します。

```csharp
using Graft.Core;

await using var app = await Application.LaunchAsync(
    new LaunchOptions
    {
        AppPath = @"path\to\YourApp.csproj",
        Configuration = "GraftTest",
        Timeout = TimeSpan.FromSeconds(60),
    }
);

await app.GetByAutomationId("SampleButton").InvokeAsync();
await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
```

`AppPath` に `.csproj` を渡すと `dotnet run -c GraftTest` 相当で起動します。exe パスも渡せます。

## よく使う操作

```csharp
// 探索
app.GetByAutomationId("SaveButton");
app.GetByName("OK");
app.GetByControlType("Button");

// 操作
await app.GetByAutomationId("NameBox").SetValueAsync("hello");
await app.GetByAutomationId("AgreeCheck").ToggleAsync();
await app.GetByAutomationId("ItemList").SelectAsync(0);
await app.GetByAutomationId("ItemList").SelectAsync("表示名");
await app.GetByAutomationId("FileMenu").SelectMenuAsync("id1/id2/leaf");

// モーダル（素の InvokeAsync で ShowDialog を開くとハングしうる）
var detail = await app.GetByAutomationId("AddButton").InvokeOpeningWindowAsync();

// OS ダイアログはアプリ側の素の API のまま。テスト側で Arm
await app.ArmOpenFileAsync(@"C:\data\import.json");
_ = await app.GetByAutomationId("ImportButton")
    .InvokeOpeningWindowAsync(waitForNewWindow: false);

// 期待
await app.GetByAutomationId("StatusText").ExpectNameAsync("Saved");
await app.GetByAutomationId("Row").WaitForAsync();
await app.WaitForWindowAsync(automationId: "Main");
```

既定タイムアウトはアクション前待ち 5 秒 / Expect 10 秒 / 起動+Handshake 30 秒（`LaunchOptions` / `WaitOptions` で上書き可）。

失敗時は `GraftException.Report` にステップ・期待値・セレクタ・直近操作・ツリー・スクリーンショット参照が付きます。直前に成功した操作があるときは `treeDiff`（追加・削除・属性変更）も付きます。`GraftSession.IncludeTreeDiff = false` で外せます。セレクタ解決に失敗すると、信頼できる代替がある場合だけ一度自己修復します。

複数の Expect を 1 回の実行でまとめて見るときは `SoftAssert` を使います。`Check` は `GraftException` だけを貯め、スコープの破棄時に `expect.failed` を 1 回投げます。集約レポートの `failures` に個別の `FailureReport` が入ります。`Check` の外は従来どおり最初の失敗で止まります。

```csharp
await using (var soft = app.SoftAssert())
{
    await soft.Check(app.GetByAutomationId("StatusText").ExpectNameAsync("Saved"));
    await soft.Check(app.GetByAutomationId("CountText").ExpectNameAsync("1"));
}
```

### 操作タイムライン（任意）

```csharp
Timeline = new TimelineOptions
{
    OutputDirectory = timelineDir,
    Retention = TimelineRetention.Always, // または OnFailure
}
```

Dispose 後に `index.html` と `frames/*.png` が出力されます。

## アーキテクチャ

```
[Graft.Core / Graft.McpServer]
   │  名前付きパイプ（同一ユーザー ACL）
   │  4 byte 長さプレフィックス + JSON
   ▼
[対象アプリ内: Graft.Instrumentation.Wpf]
   ├─ Visual Tree Walker
   ├─ ネイティブ API → Peer → SendInput
   └─ パイプサーバー（GRAFT_ENABLE=1 のときだけ）
```

本番誤混入防止は 3 段です。

1. **コンパイル時:** `GRAFT_TEST` 外では `Agent.Start` API 自体が存在しない
2. **Analyzer:** `GRAFT_TEST` 未定義での参照は GRAFT001（Error）
3. **実行時:** `GRAFT_ENABLE=1` が無い限りパイプを立てない

### 既知の制限

- **WebView2 / `HwndHost` 配下:** Visual Tree Walker は WPF のビジュアルツリーを歩くため、`WebView2` などの `HwndHost` 派生コントロールはホスト要素までしか見えません。内部の DOM やネイティブ子ウィンドウの要素は `getTree` に現れず、`GetByAutomationId` などで操作できません。
- **SendInput 系の操作:** 実デスクトップへの入力注入のため、対話セッションが必要で、複数アプリを並列起動するとフレークします。テストプロジェクトをまたぐ実行は `-m:1` が必要です。1 つのテストプロジェクト内は `Graft.TestUtilities` のコレクションで直列化できます（下記）。

テストプロジェクト（`Graft.Core` と `Graft.TestUtilities` を参照）での雛形です。`CollectionDefinition` はテストアセンブリに置きます。

```csharp
public sealed class TodoAppFixture : Graft.TestUtilities.GraftAppFixture
{
    protected override Graft.Core.LaunchOptions CreateLaunchOptions() => new()
    {
        AppPath = @"path\to\App.csproj",
        Configuration = "GraftTest",
    };
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TodoAppCollection : ICollectionFixture<TodoAppFixture>
{
    public const string Name = "TodoApp";
}

[Collection(TodoAppCollection.Name)]
public sealed class TodoTests
{
    private readonly Graft.Core.GraftSession _session;

    public TodoTests(TodoAppFixture fixture) => _session = fixture.Session;
}
```

## その他の入口

Fluent API と同じ内部操作モデルに、次も載ります。

| 入口          | 場所                                                 | 用途                                                                            |
| ------------- | ---------------------------------------------------- | ------------------------------------------------------------------------------- |
| Scenario JSON | `ScenarioJson.ParseFile` → `ScenarioRunner.RunAsync` | 宣言的シナリオ。契約は [`.dev/scenario.schema.json`](.dev/scenario.schema.json) |
| MCP           | `src/Graft.McpServer`（stdio）                       | LLM / エージェント向け。`graft_launch` など原子ツール                           |

MCP サーバーは呼び出し元（LLM）を半信頼として扱い、受け取ったファイルパス（起動する `appPath`、読み込む Scenario、スクリーンショットの保存先、ダイアログ Arm のパス）を**許可ルート配下**に制限します。既定の許可ルートはサーバーの作業ディレクトリです。追加する場合は `GRAFT_MCP_ALLOWED_ROOTS`（`Path.PathSeparator` 区切り）、制限を外す場合は `GRAFT_MCP_ALLOW_ANY_PATH=1` をサーバーの環境変数に設定してください。

## リポジトリ構成

```
src/
  Graft.Instrumentation/         共有エージェント（パイプ、入力、契約）
  Graft.Instrumentation.Wpf/     WPF アダプタ + Graft.props/targets
  Graft.Instrumentation.Analyzer GRAFT001
  Graft.Protocol/                ワイヤ／ツリーの共有スキーマ
  Graft.Core/                    Launch、セレクタ、Wait/Expect、Scenario
  Graft.TestUtilities/           xUnit の共有アプリフィクスチャ
  Graft.McpServer/               MCP ホスト（stdio）
tests/sample-apps/               SampleTodoApp / SampleWpfApp
tools/Graft.SmokeClient/         Handshake + GetTree の手動検証
```

設計の正本は [`.dev/project.md`](.dev/project.md) です。WPF 機能ギャップ表は [`.dev/competitive-gap.md`](.dev/competitive-gap.md) です。

## 開発

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx
dotnet test Graft.slnx -m:1
```

- フォーマッタ: CSharpier
- コミット: Conventional Commits（`type(scope): 件名`。件名は日本語可）
- `src/` の公開 API は XML ドキュメント必須
- テストの Fact / Theory は `summary` + `remarks`（Preconditions / Steps / Expected）
- 貢献手順: [CONTRIBUTING.md](CONTRIBUTING.md)

### CI

GitHub Actions の **CI**（`windows-latest`）はフォーマット、ビルド、アプリ起動なしのテストです。SendInput を使う全解 E2E はインタラクティブな Windows セッションが必要なため、セルフホスト runner を用意したときだけ **UI** workflow が `main` で走ります（fork の PR では動きません）。Graft は仮想ディスプレイを提供しません。詳細は [CONTRIBUTING.md](CONTRIBUTING.md#ci) です。

## ライセンス

[MIT](LICENSE)

脆弱性の報告は [SECURITY.md](SECURITY.md) へお願いします。
