# Contributing

Graft への貢献ありがとうございます。設計の正本は [`.dev/project.md`](.dev/project.md) です。エージェント向けの作業メモは [`AGENTS.md`](AGENTS.md) です。

## 必要なもの

- Windows（インタラクティブなデスクトップセッション）
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- UI / SendInput テストは画面ロックしないこと

## ビルドとテスト

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx

# ホスト CI 相当（アプリ起動なし）
dotnet test tests/Graft.Protocol.Tests
dotnet test tests/Graft.Instrumentation.Tests
dotnet test tests/Graft.Instrumentation.Analyzer.Tests
dotnet test tests/Graft.Instrumentation.Wpf.Tests
dotnet test tests/Graft.Core.Tests --filter "Category!=UI"
dotnet test tests/Graft.McpServer.Tests --filter "Category!=UI"

# 全解。SendInput 系は並列起動でフレークするため -m:1 必須
dotnet test Graft.slnx -m:1
```

## プルリクエスト

1. Conventional Commits のタイトル（`type(scope): 件名`。件名は日本語可）
2. 本文は [`.github/pull_request_template.md`](.github/pull_request_template.md) の見出しを守る
3. CSharpier をかけた状態にする（`dotnet csharpier format .`）
4. `src/` の公開 API には XML ドキュメント
5. 新しい Fact / Theory には `summary` + `remarks`（Preconditions / Steps / Expected）
6. GitHub-hosted の **CI** workflow が緑であること

Avalonia アダプタは WPF の残 Must 完了後です。詳細は [`.dev/competitive-gap.md`](.dev/competitive-gap.md) を見てください。

## CI

| Workflow | Runner | 内容 |
| -------- | ------ | ---- |
| [`ci.yml`](.github/workflows/ci.yml) | `windows-latest` | フォーマット、ビルド、アプリ起動なしのテスト（PR 含む） |
| [`ui.yml`](.github/workflows/ui.yml) | セルフホスト（任意） | `dotnet test Graft.slnx -m:1`。`main` への push または手動。**fork PR では動かない**（公開リポジトリのセルフホスト安全策） |

Graft は仮想ディスプレイを提供しません。GitHub-hosted の Windows runner では SendInput / 前景ウィンドウ前提の E2E を必須ゲートにしません（[`.dev/project.md`](.dev/project.md) Q27）。

### セルフホスト UI ジョブを有効にする

1. インタラクティブにログオンした Windows ユーザーで [self-hosted runner](https://docs.github.com/en/actions/hosting-your-own-runners) を **サービスではなくユーザープロセス** として起動する（Session 0 は不可）
2. ラベル `windows` と `interactive` を付ける（`self-hosted` は自動）
3. 画面ロック・スクリーンセーバーを切る
4. リポジトリの Actions variable `GRAFT_ENABLE_UI_CI` を `true` にする

variable が無いときは `ui.yml` はスキップされます。`workflow_dispatch` でも同じ runner が必要です。
