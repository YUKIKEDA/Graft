[English](CONTRIBUTING.md) | 日本語

# Contributing

Graft への貢献ありがとうございます。設計の正本は [`docs/design.md`](docs/design.md) です。エージェント向けの作業メモは [`AGENTS.md`](AGENTS.md) です。

## 必要なもの

- Windows（インタラクティブなデスクトップセッション）
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- UI / SendInput テストは画面ロックしないこと

## ビルドとテスト

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx

# ホスト CI 相当（アプリ起動なし。pack は含まない）
./build.ps1

# 全解。SendInput 系は並列起動でフレークするため -m:1 必須
dotnet test Graft.slnx -m:1
```

## Issues

Issue は必ずテンプレートから作る（空の Issue は無効にしてある）。各テンプレートはラベルを 1 つ付ける。

ラベルは `<軸>: <値>` の形にする（例: `type: bug`）。今ある軸は `type` だけ。`priority:` や `area:` のような軸を足すときも同じ形にする。

| Template   | Label            | 用途                                                 | Commit / PR type         |
| ---------- | ---------------- | ---------------------------------------------------- | ------------------------ |
| `bug`      | `type: bug`      | 仕様が決まっている振る舞いの不具合                   | `fix`                    |
| `feat`     | `type: feature`  | 呼び出し側が使える新しい機能                         | `feat`                   |
| `refactor` | `type: refactor` | 呼び出し側から見て振る舞いが変わらない内部の構造変更 | `refactor`               |
| `test`     | `type: test`     | テストだけの作業（カバレッジ、構成、ヘルパー）       | `test`                   |
| `design`   | `type: docs`     | `docs/design.md` の変更だけ（実装なし）              | `docs`                   |
| `task`     | `type: chore`    | ツール、規約、CI、その他のドキュメント               | `chore` / `build` / `ci` |

- ドキュメントだけを変える `task` の Issue は、ラベルを `type: docs` に付け替える
- 複数の Issue をまとめる親 Issue は、子 Issue と同じラベルを付け、子を sub-issue として紐付ける

## プルリクエスト

1. Conventional Commits のタイトル（`type(scope): 件名`。件名は英語）
2. 本文は [`.github/pull_request_template.md`](.github/pull_request_template.md) の見出しを守る
3. CSharpier をかけた状態にする（`dotnet csharpier format .`）
4. `src/` の公開 API には XML ドキュメント
5. 新しい Fact / Theory には `summary` + `remarks`（Preconditions / Steps / Expected）
6. GitHub-hosted の **CI** workflow が緑であること
7. `## Related` に `Closes #N` を独立した行で書く。ブランチの前に Issue を切る（`type/<issue-number>-<slug>`）

## CI

| Workflow                                 | Runner               | 内容                                                                                                                       |
| ---------------------------------------- | -------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| [`ci.yml`](.github/workflows/ci.yml)     | `windows-latest`     | フォーマット、ビルド、pack、アプリ起動なしのテスト（PR 含む）                                                              |
| [`pack.yml`](.github/workflows/pack.yml) | `windows-latest`     | タグ `v*` で `GraftTest` 構成を pack し、secret `NUGET_API_KEY` があれば NuGet.org へ push                                 |
| [`ui.yml`](.github/workflows/ui.yml)     | セルフホスト（任意） | `dotnet test Graft.slnx -m:1`。`main` への push または手動。**fork PR では動かない**（公開リポジトリのセルフホスト安全策） |

Graft は仮想ディスプレイを提供しません。GitHub-hosted の Windows runner では SendInput / 前景ウィンドウ前提の E2E を必須ゲートにしません（[`docs/design.md`](docs/design.md) Q27）。

### セルフホスト UI ジョブを有効にする

1. インタラクティブにログオンした Windows ユーザーで [self-hosted runner](https://docs.github.com/en/actions/hosting-your-own-runners) を **サービスではなくユーザープロセス** として起動する（Session 0 は不可）
2. ラベル `windows` と `interactive` を付ける（`self-hosted` は自動）
3. 画面ロック・スクリーンセーバーを切る
4. リポジトリの Actions variable `GRAFT_ENABLE_UI_CI` を `true` にする

variable が無いときは `ui.yml` はスキップされます。`workflow_dispatch` でも同じ runner が必要です。

## NuGet

公開パッケージの版は `src/Directory.Build.props` の `Version`（現在 0.1.0）で揃えます。タグ `v0.1.0` のように `v` + その版を push すると [pack.yml](.github/workflows/pack.yml) が `Configuration=GraftTest` で pack し、リポジトリ secret `NUGET_API_KEY`（nuget.org の API キー）があれば push します。secret が無いタグ push は pack までで止まります。

ローカル確認:

```powershell
dotnet pack src/Graft.Core/Graft.Core.csproj -c GraftTest -o artifacts
```
