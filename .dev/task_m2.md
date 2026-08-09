# M2 タスク分解

受け入れ条件（要約）: M1 に加え、`Graft.Core` の **Launch** 経由で **xUnit テスト 1 本が緑**（SmokeClient 無しが主経路）。  
含めない: Avalonia、Scenario JSON、MCP、自己修復セレクタ。  
参照: [project.md](./project.md) section 7（M2）・接続/セレクタ/待機の決定事項。前マイルストーン: [task_m1.md](./task_m1.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。1 Batch = 小さめの差分。

**作業順（project.md）:** Core Launch・待機・スコアリングセレクタ・エラーコード + xUnit 1本。

---

## Batch 0 — `Graft.Core` / `Graft.Core.Tests` 土台（ブランチ: `m2/batch-0-core-skeleton`）

- [x] `src/Graft.Core` を追加（`net8.0`、FW 非依存）。参照: `Graft.Protocol` のみ
- [x] `tests/Graft.Core.Tests` を追加（xUnit）。`Application` 型の存在確認 Fact 1 本
- [x] `Graft.slnx` に両プロジェクトを登録
- [x] Avalonia / McpServer / TestUtilities / Scenario の空スケルトンは作らない

**完了条件:** `dotnet build Graft.slnx` が通る。挙動はまだ不要。  
**確認:** `dotnet build src/Graft.Core` / `dotnet test tests/Graft.Core.Tests`  
**レビューポイント:** 構成・参照・命名だけ。  
**次:** レビュー OK なら Batch 1（パイプクライアント）へ。

---

## Batch 1 — パイプクライアント + Handshake / Connect（ブランチ: `m2/batch-1-pipe-client`）

- [x] SmokeClient 相当を Core に追加（`AgentConnection`）。Protocol の `FrameIO` / `JsonMessageCodec` を再利用
- [x] 低レベル API: `Application.ConnectAsync` / `AgentConnection.ConnectAsync` → Handshake 成功まで
- [x] ワイヤ RPC: `getTree` / `invoke`（`screenshot` / `setValue` は見送り）
- [x] 失敗時は `GraftException` + 安定 `GraftErrorCodes`
- [x] 単体: Instrumentation Agent + fake provider で Handshake + getTree / invoke / 拒否トークン

**完了条件:** Core から Handshake + getTree（または invoke）が通る。Launch はまだ不要。  
**確認:** `dotnet test tests/Graft.Core.Tests --filter Connect`  
**次:** レビュー OK なら Batch 2（Launch）へ。

---

## Batch 2 — `Application.Launch`（ブランチ: `m2/batch-2-launch`）

- [ ] 公開主経路: `Application.LaunchAsync(LaunchOptions)`（名前は仮。Process 起動 + 環境変数付与 + Connect + Handshake）
- [ ] 起動時に付与: `GRAFT_ENABLE=1`, `GRAFT_PIPE_NAME`（ランナー生成）、`GRAFT_CONNECT_TOKEN`
- [ ] 既定タイムアウト: 起動+Handshake **30s**（Options で上書き可）
- [ ] 既定寿命: Dispose / using 終了で対象プロセスを終了（セッション再利用はオプトイン・後回し可）
- [ ] SampleWpfApp パス解決（SmokeClient `SampleLauncher` 相当を Core またはテストヘルパへ）
- [ ] 低レベル `Connect` は残すがドキュメント第一級にはしない（コメント / XML で明示）

**完了条件:** Core の Launch 一本で SampleWpfApp に接続し getTree まで取れる（テストまたは薄い手動）。  
**確認:** `dotnet test tests/Graft.Core.Tests --filter Launch`  
**次:** レビュー OK なら Batch 3（セレクタ）へ。

---

## Batch 3 — スコアリングセレクタ（Core 側）（ブランチ: `m2/batch-3-selectors`）

- [ ] 公開セレクタモデル（複合キー）: 最低 `automationId` / `name` / `controlType`（近傍パスは仮実装 or スタブでも可だが重み枠は用意）
- [ ] 仮重み（project.md Q49）: automationId=100, name=40, controlType=15, 近傍パス=20, **閾値=60**
- [ ] GetTree 結果に対して Core 側でスコアリング。最高点が閾値以上 → 採用。同点 → `element.ambiguous`。無し → `element.notFound`
- [ ] ショートハンド: `ByAutomationId`（または同等）→ automationId 単独スコア
- [ ] エージェント側の `automationId` 必須解決は維持（wire invoke の params）。Core は解決した `automationId` / `runtimeId` を渡してよい
- [ ] 単体: 合成 TreeNode でのスコア・閾値・同点・ショートハンド

**完了条件:** Core 上でセレクタ → 一意要素（または安定エラー）に解決できる。  
**確認:** `dotnet test tests/Graft.Core.Tests --filter Selector`  
**次:** レビュー OK なら Batch 4（待機・操作 API）へ。

---

## Batch 4 — 待機 / Expect / 要素操作 API（ブランチ: `m2/batch-4-wait-actions`）

- [ ] Wait / Expect は **Core 側**（ポーリング + GetTree）。エージェントに wait method は足さない
- [ ] 既定タイムアウト: アクション前待ち **5s** / Expect **10s**（Options で上書き）
- [ ] アクション前: 要素が存在するまで待ち → wire `invoke`（必要なら enabled/visible も見る）
- [ ] Expect: 例）StatusText の `name` が期待値になるまで待ち。失敗は `expect.failed` / タイムアウトは `action.timeout`
- [ ] 公開 API の薄い面（名前は仮）: `GetBy(...).InvokeAsync()` / `ExpectNameAsync(...)` 程度で M2 受け入れに足りれば十分
- [ ] Fluent 全面・Scenario・自己修復は含めない

**完了条件:** Launch 済みセッションに対し、Core API だけで「ボタン invoke → StatusText 期待」が書ける。  
**確認:** `dotnet test tests/Graft.Core.Tests`（統合 or フェイク）  
**次:** レビュー OK なら Batch 5（xUnit 受け入れ）へ。

---

## Batch 5 — xUnit 受け入れ 1 本（ブランチ: `m2/batch-5-xunit-acceptance`）

- [ ] `Graft.Core.Tests`（または専用テスト）に **統合テスト 1 本**: Launch(SampleWpfApp) → invoke(SampleButton) → Expect StatusText=`Clicked 1`
- [ ] SmokeClient を呼ばない（主経路は Core）
- [ ] ビルド: Sample は `-p:GraftTest=true` / `-c GraftTest` で起動できること（既存 props）
- [ ] `Graft.TestUtilities` パッケージ化は任意・後回し（xUnit 生でよい）
- [ ] 必要なら `task_m2.md` / 短い利用メモを `.dev` に追記

**完了条件:** SmokeClient 無しで、Core Launch 経由の xUnit 1 本が緑。  
**確認:** `dotnet test tests/Graft.Core.Tests --filter <受け入れテスト名>`  
**次:** M2 完了チェックへ。

---

## M2 完了チェック（全 Batch 後）

- [ ] `Graft.Core` がソリューションにあり Protocol のみに依存する
- [ ] `Application.Launch`（相当）で Sample を起動し Handshake できる
- [ ] Core 側スコアリングセレクタ（少なくとも automationId ショートハンド）が動く
- [ ] Wait / Expect が Core 側で動き、安定エラーコードを返す
- [ ] xUnit 1 本が Launch → invoke → StatusText 変化で緑（SmokeClient 非依存）
- [ ] Avalonia / Scenario / MCP / 自己修復は **含めない**

---

## 進め方メモ

- 実装は **Batch 0 → 1 → …** の順。次 Batch に進む前にレビューしやすいサイズで止める
- 設計の正本は `project.md`。矛盾したら project.md を優先して task を直す
- M0/M1 のパイプ・Handshake・GetTree・screenshot・invoke・setValue・Analyzer は前提として壊さない
- SmokeClient は診断ツールとして残す。Core への内部移譲リファクタは任意（M2 必須ではない）
- wire method 名は camelCase 継続。Wait/Expect 用の新 wire は増やさない
- `Graft.TestUtilities` は「まず xUnit」方針だが、M2 受け入れは Core.Tests の 1 本で足りる

---

## 未決で Batch 中に固定してよいこと（小さめ）

| 項目 | 仮決め（実装時に task / コードで確定） |
| ---- | -------------------------------------- |
| 公開入口型名 | `Application.LaunchAsync` + 戻り値 `IGraftSession` / `GraftApp` 等（Batch 2 で固定） |
| 要素 API | `session.GetBy(selector).InvokeAsync()` + `ExpectNameAsync` 最小面（Batch 4） |
| 近傍パススコア | M2 は重み定数とフックのみでも可。受け入れテストは automationId でよい |
| Launch の exe/csproj | SmokeClient と同様、csproj なら `dotnet run` / 既存ビルド成果物パスを踏襲（Batch 2） |
| 統合テストの配置 | 既定は `tests/Graft.Core.Tests`。遅延・排他が必要なら後で分離 |
| SmokeClient 共有化 | 必須ではない。重複許容 → 後で Core 参照に寄せる |
