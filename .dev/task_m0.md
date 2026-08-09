# M0 タスク分解

受け入れ条件（要約）: SampleWpfApp を SmokeClient が Launch → Handshake + GetTree で `SampleButton` の name/bounds 取得。`GRAFT_TEST` 外は Start 不可 / `GRAFT001`。  
参照: [project.md](./project.md) section 7。

レビュー負荷を抑えるため **Batch 単位**で進める。1 Batch = 小さめの差分。

---

## Batch 0 — リポジトリ土台・雛形（今ここ）

- [x] `.gitignore`（.NET / VS）
- [x] `Directory.Build.props`（共通 TFM 等）
- [x] `Graft.slnx` を正本（クラシック `Graft.sln` は使わない／gitignore）
- [x] 空プロジェクト作成・参照関係だけ結ぶ
  - [x] `src/Graft.Protocol`
  - [x] `src/Graft.Instrumentation` → Protocol
  - [x] `src/Graft.Instrumentation.Wpf` → Instrumentation（`net8.0-windows` + WPF）
  - [x] `src/Graft.Instrumentation.Analyzer`（中身は Batch 4）
  - [x] `tests/sample-apps/SampleWpfApp`
  - [x] `tools/Graft.SmokeClient`
- [x] `dotnet build Graft.slnx` が通る
- [x] Core / Avalonia / McpServer の空スケルトンは作らない

**完了条件:** ソリューションがビルドできる。挙動はまだ不要。 ✅ Batch 0 完了  
**レビューポイント:** 構成・参照・命名だけ。ロジックは見なくてよい。  
**次:** レビュー OK なら Batch 1（SampleWpfApp UI）へ。

---

## Batch 1 — SampleWpfApp UI（ブランチ: `m0/batch-1-sample-ui`）

- [x] ウィンドウに Button（`AutomationId=SampleButton`）+ TextBox + TextBlock
- [x] Button クリックで TextBlock 文言が変わる（`Clicked N`）
- [x] `Configuration=GraftTest` または `-p:GraftTest=true` で `GRAFT_TEST` が付く土台（Agent 接続は次以降）
- [ ] アプリ単体で起動して UI を目視確認（レビュー時）

**完了条件:** サンプルが手起動でき、クリック反応が見える。  
**起動:** `dotnet run --project tests/sample-apps/SampleWpfApp`  
**GraftTest ビルド確認:** `dotnet build tests/sample-apps/SampleWpfApp -c GraftTest` または `-p:GraftTest=true`

---

## Batch 2 — Protocol（エンベロープのみ）（ブランチ: `m0/batch-2-protocol`）

- [x] 長さプレフィックス + JSON の読み書きヘルパ（`FrameIO` / `JsonMessageCodec`）
- [x] 要求/応答モデル `{ v, id, method, params }` / `{ v, id, ok, result|error }`
- [x] `error`: `{ code, message, details? }` + `GraftErrorCodes`
- [x] `v = 1` 定数（`ProtocolVersion.Current`）
- [x] 単体テスト（`tests/Graft.Protocol.Tests`、MemoryStream 往復）

**完了条件:** フレームの往復をメモリストリーム等で検証できる。  
**確認:** `dotnet test tests/Graft.Protocol.Tests`

---

## Batch 3 — Agent 起動ゲート（パイプはまだスタブ可）（ブランチ: `m0/batch-3-agent-gate`）

- [x] `Agent.Start` / `Stop`（`#if GRAFT_TEST` で API を囲む。Instrumentation パッケージは GRAFT_TEST 付きでビルド）
- [x] 実行時: `GRAFT_ENABLE` が無いなら何もしない
- [x] `GRAFT_PIPE_NAME` / `GRAFT_CONNECT_TOKEN` を読む口（`GraftEnvironment` / `AgentSession`）
- [x] SampleWpfApp から `GRAFT_TEST` 時のみ `Agent.Start()` / `Stop()` 呼び出し
- [x] Wpf パッケージ参照を Sample に追加
- [x] `tests/Graft.Instrumentation.Tests` でゲートを検証

**完了条件:** 記号付きビルドで Start が呼ばれ、記号なしビルドでは呼び出し側から呼べない（`#if GRAFT_TEST`）。  
**確認:** `dotnet test tests/Graft.Instrumentation.Tests`  
**Sample (GraftTest):** `dotnet build tests/sample-apps/SampleWpfApp -c GraftTest`

---

## Batch 4 — Analyzer `GRAFT001`（ブランチ: `m0/batch-4-analyzer`）

- [x] `GRAFT_TEST` 未定義での `Agent.Start` 参照を Error
- [x] Instrumentation.Wpf から Analyzer を自動導入
- [x] 違反サンプル or テストで検知を確認（`tests/Graft.Instrumentation.Analyzer.Tests`）

**完了条件:** 記号なしコンパイルで `GRAFT001` が出る。  
**確認:** `dotnet test tests/Graft.Instrumentation.Analyzer.Tests`

---

## Batch 5 — パイプサーバー + Handshake（ブランチ: `m0/batch-5-pipe-handshake`）

- [x] 同一ユーザー ACL の名前付きパイプ待受（`PipeOptions.CurrentUserOnly`）
- [x] 単一クライアント（切断後再接続可）
- [x] Handshake: `v` 一致 + `GRAFT_CONNECT_TOKEN`（method `handshake`, params `{ token }`）
- [x] 失敗時コード: `handshake.rejected` / `protocol.versionMismatch` 等

**完了条件:** クライアントから Handshake 成功/失敗を確認できる。  
**確認:** `dotnet test tests/Graft.Instrumentation.Tests`

---

## Batch 6 — GetTree（WPF）（ブランチ: `m0/batch-6-gettree-wpf`）

- [x] UI ディスパッチャへマーシャリング
- [x] Phase 1 必須フィールド（runtimeId, controlType, name, automationId, bounds, enabled, visible, focused, children）
- [x] bounds はウィンドウクライアント論理 DIP
- [x] デフォルト上限 深さ25 / ノード2000、`truncated`
- [x] `SampleButton` が木に出る

**完了条件:** Handshake 後 GetTree で SampleButton の name/bounds が取れる（一時クライアントでも可）。  
**確認:** `dotnet test tests/Graft.Instrumentation.Wpf.Tests` / `dotnet test tests/Graft.Instrumentation.Tests`

---

## Batch 7 — SmokeClient（M0 受け入れ）（ブランチ: `m0/batch-7-smoke-client`）

- [x] Launch モード（環境変数付与して Sample 起動 → Handshake → GetTree）
- [x] Connect モード（手起動済みへ接続）
- [x] SampleButton を見つけて name/bounds を表示して終了コード 0
- [x] 失敗時は安定エラーコードを出して非 0

**完了条件:** SmokeClient Launch 一本で M0 受け入れを再現できる。  
**確認:** `dotnet run --project tools/Graft.SmokeClient -- launch`

---

## M0 完了チェック（全 Batch 後）

- [x] SmokeClient Launch → SampleButton の name/bounds 取得成功
- [x] `GRAFT_ENABLE` 無しではパイプが立たない
- [x] `GRAFT_TEST` 外で Start 呼び出しがビルドエラー（`GRAFT001`）
- [x] props/targets・Screenshot・invoke・Core は **含めない**（M1/M2）

---

## 進め方メモ

- 実装は **Batch 0 → 1 → …** の順。次 Batch に進む前にレビューしやすいサイズで止める
- 設計の正本は `project.md`。矛盾したら project.md を優先して task を直す
