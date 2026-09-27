[English](conventions.md) | 日本語

# 規約

エージェントも強制するコーディングの詳細は [`.cursor/rules/csharp-tooling.mdc`](../.cursor/rules/csharp-tooling.mdc) と [`.cursor/rules/testing.mdc`](../.cursor/rules/testing.mdc) にある。製品の振る舞いは [`docs/design.md`](design.md) にある。

## 配置

- ソリューションは **`Graft.slnx`**。`Graft.sln` はコミットしない（gitignore 済み）
- ライブラリは `src/` の分割のままにする。`Graft.Core`、`Graft.Protocol`、`Graft.Instrumentation`、`Graft.Instrumentation.Wpf`、`Graft.Instrumentation.Analyzer`、`Graft.McpServer`、`Graft.TestUtilities`。一つにまとめない
- テストは `tests/` のまま。サンプルアプリは `tests/sample-apps/` のまま
- `tools/Graft.SmokeClient` は手動の診断クライアントとして残す
- 残す決定は `docs/` に置く。`.dev/` はローカルの作業置き場で、gitignore する

## ターゲットとアナライザー

- ライブラリの既定は `net8.0`。WPF プロジェクトは `net8.0-windows`
- Nullable は有効。`TreatWarningsAsErrors` は false。StyleCop は警告のまま
- 整形は CSharpier（`dotnet csharpier`）。ローカルツールは `.config/dotnet-tools.json` で固定する
- `src/` の公開 API には XML ドキュメントが要る（`summary`、必要なら `param` / `returns`）。`tools/` とサンプルアプリには要らない
- Fact と Theory には、英語の `summary` と `remarks` が要る。見出しは Preconditions、Steps、Expected。Theory はメソッドにつき remarks 一つ

## 確認

- ビルドやテストに効く変更の PR の前に、Windows では `./build.ps1` を実行する（tool restore、CSharpier の check、`Graft.slnx` の build、アプリを起動しない単位テスト）。pack はこのスクリプトに入らない
- アプリ起動と SendInput のテストは `dotnet test Graft.slnx -m:1`。`./build.ps1` には入らない
- ホスト CI は `.github/workflows/ci.yml`。対話的な UI CI は `.github/workflows/ui.yml` で、オプトインのまま

## サンプルの言語

サンプルアプリの UI 文字列、XAML、フィクスチャ JSON は日本語のままでよい。テスト対象のアプリである。Win32 ダイアログの名前は、OS の表示のままにする。日本語のテストデータには、英語のコメントで理由を書く。[`docs/language.md`](language.md) を見る。
