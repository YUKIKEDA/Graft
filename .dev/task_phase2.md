# Phase 2 タスク分解

受け入れ条件（要約）: Phase 1（M0〜M2 + Core `SetValueAsync`）の上に、(1) **構造化失敗診断**、(2) **宣言的 Scenario JSON** を載せ、LLM がテスト作成・失敗修正しやすい主経路にする。  
含めない: Avalonia、MCP（Phase 3）、自己修復（Phase 4）、`toggle`/キー/SendInput 本格化（Phase 1 余り・別 Batch 可）。  
参照: [project.md](./project.md) section 8・フェーズ表。前マイルストーン: [task_m2.md](./task_m2.md)。利用例: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## Batch 0 — 失敗診断の契約（モデルのみ）（ブランチ: `phase2/batch-0-failure-report-schema`）

- [x] `FailureReport`: 失敗ステップ、期待、実際、タイムアウト有無、対象セレクタ（project.md 必須最小）— `Graft.Core.Diagnostics`
- [x] Core に JSON シリアライズ可能な型 + 単体往復テスト（`FailureReportJson` / `FailureReportTests`）
- [x] エージェント常時添付はしない（既存方針）。Core が Expect/アクション失敗時に組み立てる前提を文書化（型 remarks + `graft-core.md`）

**完了条件:** スキーマ型とテストがある。まだ自動添付しなくてよい。  
**次:** Batch 1（Core 発行）へ。

---

## Batch 1 — Core が FailureReport を発行（ブランチ: `phase2/batch-1-failure-report-emit`）

- [x] `ExpectNameAsync` / `InvokeAsync` / `SetValueAsync`（および Wait）失敗時に `GraftException.Report` へ載せられる
- [x] デフォルト添付の土台: 対象セレクタ + 期待/実際 + タイムアウトフラグ（操作ログ・スクショは Batch 2）
- [x] テスト: 誤 Expect でレポートフィールドが埋まる（`WaitActionTests`）

**完了条件:** 失敗 1 件で最小診断がプログラムから読める。  
**次:** Batch 2（添付拡充）または Batch 3（Scenario）へ。

---

## Batch 2 — 診断添付の拡充（推奨）（ブランチ: `phase2/batch-2-failure-attachments`）

- [ ] 直近 N 操作ログ（Core 側リングバッファ）
- [ ] 失敗時ツリー（全文 or 差分の簡易版）。JSON Patch は後回し
- [ ] 失敗時スクショ参照（`screenshot` RPC → 一時ファイル or byte 参照）。常時添付はしない
- [ ] Sample / Core.Tests で 1 経路確認

**完了条件:** Expect 失敗時に「セレクタ + 期待/実際 + ツリー or スクショ」が揃う。  
**次:** Batch 3（Scenario JSON）へ。

---

## Batch 3 — Scenario JSON スキーマ（ブランチ: `phase2/batch-3-scenario-schema`）

- [ ] 交換形式の正本: JSON（JSON Schema を `.dev` または `src/Graft.Core` 近傍に置く）
- [ ] 最小ステップ: launch / invoke / setValue / expectName（名前は実装時に固定）
- [ ] Core 内にパーサ + 内部操作モデルへのコンパイル（Fluent 全面は不要）
- [ ] 単体: 小さな Scenario ファイルをパースできる

**完了条件:** JSON 1 本がメモリ上のステップ列になる。実行は Batch 4。  
**次:** Batch 4（実行）へ。

---

## Batch 4 — Scenario 実行器 + Sample 1 本（ブランチ: `phase2/batch-4-scenario-runner`）

- [ ] Scenario を `Application.Launch` + GetBy 操作に実行
- [ ] 失敗時は Batch 1/2 の FailureReport を返す
- [ ] SampleWpfApp 向け Scenario JSON 1 本（ボタン → StatusText）を `tests/sample-apps` に置く
- [ ] xUnit から Scenario 実行して緑

**完了条件:** SmokeClient 無しで Scenario JSON 経由の受け入れが 1 本緑。  
**次:** Phase 2 完了チェックへ。

---

## Phase 2 完了チェック

- [ ] 失敗時に必須最小の FailureReport が取れる
- [ ] （推奨）操作ログ / ツリー or スクショが添付できる
- [ ] Scenario JSON スキーマと実行器がある
- [ ] Sample 向け Scenario 1 本が緑
- [ ] Avalonia / MCP / 自己修復 / toggle・キー本格化は **含めない**

---

## 進め方メモ

- Phase 1 余り（`toggle`、キー、SendInput）は本ファイルの必須線に入れない。必要なら Parallel の小さな Batch で
- Fluent API と Scenario と（将来）MCP は同じ内部操作モデルに寄せる（project.md）
- 設計矛盾時は `project.md` 優先
