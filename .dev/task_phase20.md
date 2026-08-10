# Phase 20 — Menu バー（既存 `invoke`）

受け入れ条件（要約）: WPF **Menu バー**でトップレベルを既存 **`InvokeAsync`** で開き、**1段サブ**の MenuItem を同じく `invoke` できるようにする。開いているサブメニューを getTree / resolve に含める。  
含めない: 任意深さ、パス DSL、新 wire、無効項目専用、ContextMenu サブ、Avalonia、Inspector。  
参照: [project.md](./project.md) Q118〜。前フェーズ: [task_phase19.md](./task_phase19.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 範囲 | Menu バー、トップ + **1段サブ** |
| API | 既存 `InvokeAsync` / `invoke` のみ（新 wire なし） |
| ツリー | Menu / トップは常時。**開いているサブ**（`IsSubmenuOpen`）の MenuItem を追加 |
| Sample | File → Ping。`Invoke` → `Invoke` → StatusText |
| Scenario | 既存 `invoke` で薄い追従 |
| MCP | 変更なし |
| Phase 20 の次 | DataGrid 列キー+CheckBox（Phase 21）→ 複数行（Phase 22）→ Avalonia |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase20.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase19.md` / `graft-core.md` 更新

---

## Batch 1 — ツリー + Sample E2E

- [x] `WpfVisualTreeWalker` で開いているサブメニューを載せる
- [x] 必要なら MenuItem の `invoke` を安定化（サブ開閉）
- [x] Sample Menu + Fluent / Scenario E2E
- [x] 完了チェック

---

## Phase 20 完了チェック

- [x] トップ MenuItem を `InvokeAsync` で開ける
- [x] 1段サブの MenuItem を `InvokeAsync` できる
- [x] Sample Fluent + Scenario が緑
- [x] 任意深さ / パス DSL / 新 wire / Avalonia は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase21.md](./task_phase21.md) → 複数行選択 → Avalonia → Inspector
