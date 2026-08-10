# Phase 21 — DataGrid 列キー + CheckBox 列

受け入れ条件（要約）: DataGrid セル API に **`columnKey`（Header 文字列）** を追加し、**`DataGridCheckBoxColumn`** を既存 `getCellText` / `setCellValue` / `ExpectCellText` で扱う（値は `"True"` / `"False"`）。  
含めない: Template 列、複数行選択（Phase 22）、セル選択、Avalonia、Inspector。  
参照: [project.md](./project.md) Q120〜。前フェーズ: [task_phase20.md](./task_phase20.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 列キー | `Header` 文字列、Ordinal 完全一致。重複は ambiguous |
| API | Fluent overload + wire `column` **xor** `columnKey` |
| CheckBox | 既存セル API。`"True"` / `"False"` |
| Sample | 既存 `SampleGrid` に CheckBox 列（Header `Active`） |
| Scenario / MCP | `columnKey` 薄い追従 |
| Phase 21 の次 | DataGrid 複数行選択（Phase 22）→ Avalonia → Inspector |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase21.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase20.md` / `graft-core.md` 更新

---

## Batch 1 — accessor / wire / Core / Sample E2E

- [x] 列キー解決 + CheckBox 列 R/W
- [x] Core Fluent overload + Scenario/schema/MCP
- [x] Sample + Fluent / Scenario E2E
- [x] 完了チェック

---

## Phase 21 完了チェック

- [x] `columnKey` で Text セルを読める
- [x] CheckBox 列を `"True"`/`"False"` で読み書きできる
- [x] Sample Fluent + Scenario が緑
- [x] Template 列 / 複数行選択 / Avalonia は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase22.md](./task_phase22.md) → Avalonia → Inspector
