# Phase 29 — コントロール / キー穴（29a / 29b）

受け入れ条件（要約）: 対照表 Must のコントロール・キー穴を埋める。  
ID: `V03, V05, L04, L06, T03, T04, C01, C03–C06, K03, K04`（[competitive-gap.md](./competitive-gap.md)）。  
分割: **29a**（本 PR）→ **29b**（次）。  
含めない（全体）: Avalonia、Inspector、書式付き RichText、Password Get、Win/Meta。  
参照: [project.md](./project.md) Q135〜。前フェーズ: [task_phase28.md](./task_phase28.md)。

---

## 合意済み契約（grill）— 29a

| 項目 | 決定 |
| ---- | ---- |
| 束 | 29a → 29b の 2 PR |
| 29a ID | `V03`, `V05`, `T03`, `T04`, `K03`, `K04` |
| 29b ID | `L04`, `L06`, `C01`, `C03–C06` |
| V03 | `SetValue` → `Password`。wire/tree にパスワードを載せない |
| V05 | 平文 Get/Set のみ（書式なし） |
| T03/T04 | 既存 `ToggleAsync` / `ExpectCheckedAsync` + tree `checked` 拡張。グループ DSL なし |
| K03 | `ExpectFocusedAsync()` のみ |
| K04 | `F1`–`F12` + NumPad。**Win/Meta なし** |
| Sample / Scenario / MCP | Phase29a セクション。薄い追従（MCP は `expectFocused`） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase28.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — 29a 実装 + E2E

- [x] PasswordBox / RichTextBox setValue + value
- [x] Radio / Toggle checked + Toggle
- [x] ExpectFocused + F/NumPad chords
- [x] Sample + E2E
- [x] 完了チェック

---

## Phase 29a 完了チェック

- [x] PasswordBox Set / RichText 平文 R/W ができる
- [x] Radio / Toggle の ExpectChecked ができる
- [x] ExpectFocused + F/NumPad Press ができる
- [x] Sample Fluent + Scenario が緑
- [x] 29b ID / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** Phase 29b（`L04`, `L06`, `C01`, `C03–C06`）
