# Phase 19 — ListBox 複数選択（`selectMany`）

受け入れ条件（要約）: 新 Fluent **`SelectManyAsync`** / wire **`selectMany`** で ListBox（`SelectionMode` が Multiple/Extended）の複数選択を置換セマンティクスで行い、項目 `ExpectSelected` + Sample 副作用で検証する。  
含めない: DataGrid 複数行、加算モード、項目キー指定、Avalonia、Inspector。  
参照: [project.md](./project.md) Q116〜。前フェーズ: [task_phase18.md](./task_phase18.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| ホスト | **ListBox のみ** |
| API | 新 `SelectManyAsync(indexes)` + wire `selectMany`（既存 `select` は単一のまま） |
| セマンティクス | **置換**。空 `indexes` はクリア |
| Single モード | **エラー** |
| Sample | 別 ListBox `SampleMultiList`（`Extended`）。`SampleList` は触らない |
| 検証 | 項目 `ExpectSelectedAsync` + StatusText 副作用 |
| Scenario / MCP | 薄い追従（schema + `graft_select_many`） |
| Phase 19 の次 | DataGrid 列キー／他列種 or Menu バー など → Avalonia → Inspector |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase19.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase18.md` / `graft-core.md` 更新

---

## Batch 1 — selectMany 本線 + Sample E2E

- [x] Protocol / Agent / WPF `selectMany`（ListBox、realize、置換、空クリア、Single エラー）
- [x] Core Fluent + Scenario + schema + MCP
- [x] Sample `SampleMultiList` + Fluent / Scenario E2E
- [x] 完了チェック

---

## Phase 19 完了チェック

- [x] `SelectManyAsync` で複数選択できる
- [x] 空 indexes でクリアできる
- [x] Single モード ListBox はエラー
- [x] Sample Fluent + Scenario が緑
- [x] DataGrid 複数行 / 加算モード / Avalonia は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** Menu バー／DataGrid 列キー など → Avalonia → Inspector
