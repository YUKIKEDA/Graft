# Phase 22 — DataGrid 複数行選択（`selectMany` 拡張）

受け入れ条件（要約）: 既存 **`SelectManyAsync` / wire `selectMany`** を WPF **DataGrid 行**（`SelectionMode=Extended`、`SelectionUnit=FullRow`）に拡張する。置換セマンティクス・空 indexes でクリア・Single はエラー。  
含めない: セル選択、Template 列、新 wire、Avalonia、Inspector。  
参照: [project.md](./project.md) Q123〜。前フェーズ: [task_phase21.md](./task_phase21.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| Sample | 別 DataGrid `SampleMultiGrid`（Extended）。`SampleGrid` は Single のまま |
| API | 既存 `SelectManyAsync` / `selectMany`（新 wire なし） |
| セマンティクス | 置換。空でクリア。Single はエラー |
| SelectionUnit | `FullRow` のみ（非 FullRow は action.failed） |
| 検証 | 行 `ExpectSelectedAsync` + StatusText。Fluent に空クリア必須 |
| Scenario | 既存 `selectMany` 薄い追従 |
| MCP | 新ツールなし（説明文のみ DataGrid 言及可） |
| Phase 22 の次 | Avalonia → Inspector |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase22.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase21.md` / `graft-core.md` 更新

---

## Batch 1 — chooser / Sample / E2E

- [x] `WpfElementChooser.SelectMany` を DataGrid 行対応
- [x] Sample `SampleMultiGrid` + Fluent / Scenario E2E
- [x] 完了チェック

---

## Phase 22 完了チェック

- [x] Extended DataGrid で複数行選択できる
- [x] 空 indexes でクリアできる
- [x] Single DataGrid は action.failed
- [x] Sample Fluent + Scenario が緑
- [x] セル選択 / Template / Avalonia は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** Avalonia → Inspector
