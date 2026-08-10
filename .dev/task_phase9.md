# Phase 9 — DataGrid セル R/W（Text 列 MVP）

受け入れ条件（要約）: WPF **DataGrid** の **Text 列セル**をホスト＋`(rowIndex, columnIndex)` で読み書きし、`ExpectCellText` まで Core Fluent → Scenario / MCP に揃える。  
含めない: 列キー／Header 指定、CheckBox/Template 列、`SelectionUnit=Cell`、ソート／リサイズ、複数選択、ツリーへの `DataGridCell` 露出、OS ダイアログ、Avalonia、Inspector。  
参照: [project.md](./project.md) Q82〜。前フェーズ: [task_phase8.md](./task_phase8.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 範囲 | **読取＋書込 MVP**（表示テキスト読取 + 編集コミット） |
| 指定 | ホスト（DataGrid `automationId`）＋ **`(rowIndex, columnIndex)`** |
| API / wire | `GetCellTextAsync` / `SetCellValueAsync` ＋ wire `getCellText` / `setCellValue` |
| 書込 | **BeginEdit → 値設定 → CommitEdit**（Enter／フォーカス移動は必須にしない） |
| 列型 | **`DataGridTextColumn` のみ** |
| 検証 | **`ExpectCellTextAsync(row, col, expected)`**（ポーリング、既存 Expect と同型） |
| 公開経路 | Scenario / MCP 薄い追従（`getCellText` / `setCellValue` / `expectCellText`） |
| Sample | **FullRow + Single のまま**。編集可能 Text 列を足す（行 MVP を壊さない） |
| ツリー | **`DataGridCell` は出さない**。読取 SoT は `getCellText` |
| 含めない | 列キー、CheckBox/Template 列、Cell 選択、ソート／リサイズ、複数選択、OS ダイアログ、Avalonia、Inspector |
| Phase 9 の次 | **OS 共通ダイアログ方針** |

---

## Batch 0 — タスク文書（ブランチ: `phase9/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase9.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 9 を追記
- [x] `AGENTS.md` の Phase 参照を更新
- [x] `task_phase8.md` / `graft-core.md` の次フェーズメモを更新

**完了条件:** Batch 分割と契約が文書化されている。  
**次:** Batch 1（wire + WPF セル読取）へ。

---

## Batch 1 — wire + WPF セル読取（ブランチ: `phase9/datagrid-cell-rw`）

- [x] Protocol: `getCellText`（params: automationId, row, column → text）
- [x] WPF: 行 realize → Text 列セルの表示文字列を返す
- [x] Core: `GetCellTextAsync(row, column)`
- [x] ホスト側ユニット／薄い検証 — Sample E2E で担保

**完了条件:** index 指定でセル文字列が読める。  
**次:** Batch 2（書込 CommitEdit）へ。

---

## Batch 2 — セル書込（ブランチ: `phase9/datagrid-cell-rw`）

- [x] Protocol: `setCellValue`（params: automationId, row, column, value）
- [x] WPF: BeginEdit → 編集要素へ値 → CommitEdit（読取専用列は `action.failed`）
- [x] Core: `SetCellValueAsync(row, column, value)`
- [x] Sample: `SampleGrid` を編集可能 Text 列に（FullRow/Single 維持）

**完了条件:** Text 列セルへ書き込み、再読取で反映される。  
**次:** Batch 3（Expect + E2E）へ。

---

## Batch 3 — Expect + Sample E2E（ブランチ: `phase9/datagrid-cell-rw`）

- [x] `ExpectCellTextAsync(row, column, expected)`
- [x] Sample Fluent E2E: set → expect（必要なら get）
- [x] `graft-core.md` にセル R/W の短いメモ

**完了条件:** Fluent E2E が緑。  
**次:** Batch 4（Scenario / MCP + docs）へ。

---

## Batch 4 — Scenario / MCP + docs（ブランチ: `phase9/datagrid-cell-rw`）

- [x] scenario schema: `getCellText` / `setCellValue` / `expectCellText`
- [x] ScenarioJson / Runner / MCP 原子ツール
- [x] Scenario E2E（薄い追従）
- [x] `project.md` / 本ファイル完了チェック。次フェーズメモ: **OS 共通ダイアログ方針**

**完了条件:** 公開経路と文書が揃っている。  
**次:** Phase 9 完了チェック → OS ダイアログ方針へ。

---

## Phase 9 完了チェック

- [x] `GetCellTextAsync` / `SetCellValueAsync`（ホスト＋row/column）がある
- [x] Text 列で BeginEdit/CommitEdit 書込ができる
- [x] `ExpectCellTextAsync` がある
- [x] Sample E2E（FullRow/Single + 編集可能 Text）が緑
- [x] Scenario / MCP から同操作が呼べる（薄い追従）
- [x] 列キー / CheckBox・Template 列 / Cell 選択 / ツリー DataGridCell / OS ダイアログ / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 行操作（Phase 8）と共存。セル API が正本でツリーにセルを載せない
- 列キー指定・他列種は後続拡張
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** OS 共通ダイアログ方針 → … → Avalonia → Inspector（最後寄り）
