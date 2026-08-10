# Phase 27 — 探索・パス・キー指定

受け入れ条件（要約）: Name/ControlType 第一級特定、相対セレクタ（子・兄弟・nth）、リストのキー選択、ツリーパス選択を追加する。  
ID: `F02, F04, F05, L05, E04`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: CSS/XPath、Header ツリーパス、DataGrid 行キー（Phase 28）、F03 強化、ファジー、Inspector、Avalonia。  
参照: [project.md](./project.md) Q133〜。前フェーズ: [task_phase26.md](./task_phase26.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 束 | F02, F04, F05, L05, E04 を 1 PR |
| F02 | `GetByName` / `GetByControlType`・ハード一致 |
| F04+L05 | `SelectAsync(string key)`（Name）。`element.ambiguous`。DataGrid キーは Phase 28 |
| F05 | `Child` / `Sibling` / `Nth`（getTree）。CSS/XPath なし |
| E04 | `SelectTreeAsync`（AutomationId `/`、Expand + 葉 Selected） |
| Wire | `select` に `key`（index XOR）、新 `selectTree` |
| Sample / Scenario / MCP | 最小拡張 + 薄い追従 |
| Phase 27 の次 | DataGrid 残り（Phase 28） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase26.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — 実装 + E2E

- [x] F02/F05 Core セレクタ
- [x] select key + selectTree
- [x] Sample + E2E
- [x] 完了チェック

---

## Phase 27 完了チェック

- [x] GetByName / GetByControlType ができる
- [x] Child / Sibling / Nth ができる
- [x] SelectAsync(key) / SelectTreeAsync ができる
- [x] Sample Fluent + Scenario が緑
- [x] DataGrid キー / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase28.md](./task_phase28.md) DataGrid 残り（G06–G10）
