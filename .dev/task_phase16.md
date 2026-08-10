# Phase 16 — 右クリック + ContextMenu / MenuItem

受け入れ条件（要約）: **`RightClickAsync` / wire `rightClick`** で右クリックし、開いた **ContextMenu の MenuItem を既存 `InvokeAsync`** で選べるようにする。開いている ContextMenu を getTree / resolve に含める。  
含めない: Menu バー、サブメニュー、無効項目専用、複合 SelectContextMenu API、Avalonia、Inspector。  
参照: [project.md](./project.md) Q110〜。前フェーズ: [task_phase15.md](./task_phase15.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 範囲 | 右クリック + フラット ContextMenu の MenuItem |
| API | `RightClickAsync`（wire `rightClick`）。MenuItem は既存 `invoke` |
| ツリー | 開いている ContextMenu を getTree / resolve 対象に含める |
| 実装 | SendInput 右クリック + ContextIdle flush（`IsOpen` 直書きなし） |
| 待ち | RightClick は flush のみ。MenuItem 出現は呼び出し側 Wait |
| Sample E2E | RightClick → Invoke(MenuItem) → StatusText |
| 公開 | Scenario / MCP 薄い追従 |
| Phase 16 の次 | TabControl / Slider / 複数選択 など |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase16.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase15.md` / `graft-core.md` 更新

---

## Batch 1 — Input + tree + wire

- [x] `InputInjector.RightClick` + WPF flush
- [x] ContextMenu を VisualTree walk に含める
- [x] Protocol `rightClick` + Instrumentation / Core

---

## Batch 2 — Sample + Scenario / MCP

- [x] Sample ContextMenu + Fluent / Scenario E2E
- [x] schema / MCP
- [x] 完了チェック。次: Tab / Slider / 複数選択 など

---

## Phase 16 完了チェック

- [x] `RightClickAsync` / `rightClick` がある
- [x] 開いた MenuItem が resolve + invoke できる
- [x] Sample E2E が緑
- [x] Scenario / MCP から呼べる
- [x] Menu バー / サブメニュー等は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase17.md](./task_phase17.md) → Slider / 複数選択 → … → Avalonia → Inspector
