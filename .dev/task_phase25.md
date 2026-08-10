# Phase 25 — マウス高度

受け入れ条件（要約）: ダブルクリック / Hover / 要素→要素 DnD / クリック点相対 DIP オフセットクリック / ホイールを SendInput 経路で追加する。  
ID: `M04–M08`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: ContextMenu サブ（M03→Phase 26）、ToolTip 待ち（C03→Phase 29）、ウィンドウ絶対座標クリック、Avalonia。  
参照: [project.md](./project.md) Q130〜。前フェーズ: [task_phase24.md](./task_phase24.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| DnD | 要素→要素のみ（`DragAsync(toAutomationId)`） |
| オフセット | 既存クリック点（Peer→中心）からの DIP。`ClickAtAsync(offsetX, offsetY)`。常に SendInput。`invoke` は触らない |
| Hover | 移動 + 短い固定 dwell。ToolTip 待ちは含めない |
| Sample | MainWindow に Mouse セクション 1 つ |
| Scenario / MCP | 薄い追従 |
| Phase 25 の次 | メニュー深さ（Phase 26） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase24.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — SendInput + wire + Sample E2E

- [x] InputInjector / WpfInputInjection
- [x] Protocol / Invoker / Fluent / Scenario / MCP
- [x] Sample + E2E
- [x] 完了チェック

---

## Phase 25 完了チェック

- [x] DoubleClick / Hover / Drag / ClickAt / Wheel ができる
- [x] Sample Fluent + Scenario が緑
- [x] ToolTip / Frame / Avalonia / 絶対座標クリックは **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** Phase 26 メニュー深さ（M03, U02–U04）
