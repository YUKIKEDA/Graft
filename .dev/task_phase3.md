# Phase 3 タスク分解

受け入れ条件（要約）: Phase 1〜2（Instrumentation + Core Fluent / FailureReport / Scenario）の上に、**`Graft.McpServer`** を薄い公開層として置き、LLM が MCP tools 経由で同じ操作モデルを使えるようにする。  
含めない: Avalonia、自己修復（Phase 4）、HTTP MCP（初期は stdio）、Phase 1 余り（toggle / キー / SendInput）。  
参照: [project.md](./project.md) Q8 / Q10・フェーズ表。前フェーズ: [task_phase2.md](./task_phase2.md)。Core 利用: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## Batch 0 — MCP ホスト骨格（ブランチ: `phase3/batch-0-mcp-skeleton`）

- [x] `src/Graft.McpServer` コンソール（stdio、`ModelContextProtocol` + Hosting）
- [x] ソリューション / テストプロジェクト登録
- [x] ヘルス用最小ツール `graft_ping`（Core 操作はまだ不要）
- [x] テスト: Stdio クライアントで `ListTools` に `graft_ping` が見える（`McpHostSmokeTests`）

**完了条件:** MCP ホストが立ち上がり、ツール一覧が取れる。  
**次:** Batch 1（Scenario / Core ツール公開）へ。

---

## Batch 1 — Scenario 実行ツール（ブランチ: `phase3/batch-1-mcp-scenario-run`）

- [x] `graft_run_scenario`（JSON 文字列またはパス → `ScenarioJson` + `ScenarioRunner`）
- [x] 成功時: 簡潔な ok JSON（`CallToolResult`）
- [x] 失敗時: `GraftException` / `FailureReport` を JSON テキストで返す（`IsError=true`）
- [x] Sample scenario を MCP クライアント経由で 1 経路緑（`McpScenarioRunTests`）

**完了条件:** LLM が Scenario JSON を渡して Sample を走らせられる。  
**次:** Batch 2（Fluent 相当の原子ツール）へ。

---

## Batch 2 — 原子ツール（推奨）（ブランチ: `phase3/batch-2-mcp-atomic-tools`）

- [ ] セッション付き原子ツール: `graft_launch` / `graft_invoke` / `graft_set_value` / `graft_expect_name` / `graft_dispose`（名前は実装時に固定）
- [ ] サーバ内で `GraftSession` を保持（1 セッション想定で開始）
- [ ] 失敗時は Batch 1 と同様に FailureReport を返す
- [ ] テスト: launch → invoke → expect → dispose の 1 経路

**完了条件:** Scenario 無しでも MCP だけで短い UI 操作が完走する。  
**次:** Phase 3 完了チェックへ。

---

## Phase 3 完了チェック

- [ ] `Graft.McpServer` が stdio で起動する
- [ ] Scenario 実行ツールがある
- [ ] （推奨）原子ツールでセッション操作ができる
- [ ] 失敗時に FailureReport 相当がツール結果から読める
- [ ] Avalonia / HTTP MCP / 自己修復 / toggle・キー本格化は **含めない**

---

## 進め方メモ

- MCP は Core の薄いラッパー。操作ロジックを McpServer に複製しない（project.md Q8 / Q10）
- 初期 transport は **stdio**（Cursor / ローカル LLM 向け）。HTTP は需要後
- 設計矛盾時は `project.md` 優先
