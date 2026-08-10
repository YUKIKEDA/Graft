# Phase 17 — TabControl 選択（`select` 拡張）

受け入れ条件（要約）: 既存 **`SelectAsync(index)` / wire `select`** を **TabControl** ホストに対応させ、TabItem の `ExpectSelectedAsync` + Sample 副作用で検証する。  
含めない: Slider、複数選択、ヘッダー文字列指定、Menu バー、Avalonia、Inspector。  
参照: [project.md](./project.md) Q112〜。前フェーズ: [task_phase16.md](./task_phase16.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 範囲 | **TabControl のみ** |
| API | 既存 `SelectAsync(index)` を TabControl に拡張（新 wire なし） |
| 検証 | TabItem `ExpectSelectedAsync` + StatusText 副作用 |
| Scenario | 既存 `select` で 1 本 |
| MCP | 変更なし |
| Phase 17 の次 | Slider（Phase 18） / 複数選択 など |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase17.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase16.md` / `graft-core.md` 更新

---

## Batch 1 — TabControl select + Sample E2E

- [x] `WpfElementChooser` で TabControl を明示対応
- [x] Sample TabControl + Fluent / Scenario E2E
- [x] 完了チェック。次: [task_phase18.md](./task_phase18.md)

---

## Phase 17 完了チェック

- [x] TabControl で `SelectAsync(index)` ができる
- [x] TabItem の `ExpectSelectedAsync` が使える
- [x] Sample Fluent + Scenario が緑
- [x] Slider / 複数選択 / ヘッダー指定は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase18.md](./task_phase18.md) → 複数選択 → … → Avalonia → Inspector
