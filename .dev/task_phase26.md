# Phase 26 — メニュー深さ

受け入れ条件（要約）: Menu / ContextMenu の任意深さパス DSL（`SelectMenuAsync` / wire `selectMenu`）、ContextMenu サブメニュー、無効項目の明示エラーを追加する。  
ID: `M03, U02–U04`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: Header 解決、`/` エスケープ、ToolTip、探索強化（Phase 27）、Avalonia、Inspector。  
参照: [project.md](./project.md) Q131〜。前フェーズ: [task_phase25.md](./task_phase25.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| API | ルート `ElementQuery.SelectMenuAsync(string path)` |
| パス | `/` 区切り、AutomationId のみ。ルート自身はパスに含めない |
| ContextMenu | `RightClickAsync` で開く → ContextMenu（Id 付き）をルートに `SelectMenuAsync` |
| Wire | 新 `selectMenu`（agent 一括、`IMenuSelector`） |
| U04 | `element.notActionable`（メッセージに path / automationId） |
| Sample | Menu 3 段 + ContextMenu サブ + 無効 1。StatusText 検証 |
| Scenario / MCP | 薄い追従 |
| Phase 26 の次 | 探索・パス・キー指定（Phase 27） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase25.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — selectMenu + Sample E2E

- [x] Protocol / `IMenuSelector` / Pipe / Fluent / Scenario / MCP
- [x] Sample Menu / ContextMenu 拡張
- [x] Fluent + Scenario E2E
- [x] 完了チェック

---

## Phase 26 完了チェック

- [x] Menu バーで深いパス選択ができる
- [x] ContextMenu サブを RightClick + SelectMenu できる
- [x] 無効項目が `element.notActionable` になる
- [x] Sample Fluent + Scenario が緑
- [x] Header 解決 / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase27.md](./task_phase27.md) 探索・パス・キー指定（F02, F04, F05, L05, E04）
