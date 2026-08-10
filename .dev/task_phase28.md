# Phase 28 — DataGrid 残り

受け入れ条件（要約）: Template 列 R/W、セル選択、列キー+値での行特定、ヘッダークリックソート、行追加/削除を追加する。  
ID: `G06–G10`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: Template 内 Button Invoke、複数セル選択、フィルタ / 列リサイズ / 列 DnD、Avalonia、Inspector。  
参照: [project.md](./project.md) Q134〜。前フェーズ: [task_phase27.md](./task_phase27.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 束 | G06–G10 を 1 PR |
| G06 | Template: Get=表示テキスト。Set=単一 TextBox/CheckBox のみ |
| G07 | `SelectCellAsync`（単一）。Cell / CellOrRowHeader のみ |
| G08 | `SelectRowAsync(columnKey, value)`。複数一致は `element.ambiguous` |
| G09 | ヘッダークリックソートのみ（`ClickColumnHeaderAsync`） |
| G10 | `AddRowAsync` / `DeleteSelectedRowsAsync` |
| Sample | 専用 DataGrid 1 つ。既存グリッド非破壊 |
| Scenario / MCP | 薄い追従 |
| Phase 28 の次 | Phase 29（コントロール / キー穴） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase27.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — 実装 + E2E

- [x] Template + SelectCell
- [x] SelectRow / ClickColumnHeader / AddRow / DeleteSelectedRows
- [x] Sample + E2E
- [x] 完了チェック

---

## Phase 28 完了チェック

- [x] Template 列 Get/Set ができる
- [x] SelectCell / SelectRow / ClickColumnHeader / Add/Delete ができる
- [x] Sample Fluent + Scenario が緑
- [x] フィルタ・列 DnD / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase29.md](./task_phase29.md) コントロール / キー穴（29a → 29b）
