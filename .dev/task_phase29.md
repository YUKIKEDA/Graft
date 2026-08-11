# Phase 29 — コントロール / キー穴（29a / 29b）

受け入れ条件（要約）: 対照表 Must のコントロール・キー穴を埋める。  
ID: `V03, V05, L04, L06, T03, T04, C01, C03–C06, K03, K04`（[competitive-gap.md](./competitive-gap.md)）。  
分割: **29a**（完了）→ **29b**（本 PR）。  
含めない（全体）: Avalonia、Inspector、書式付き RichText、Password Get、Win/Meta、Calendar セル UI、ToolBar overflow、Popup 開閉専用 API。  
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

## 合意済み契約（grill）— 29b

| 項目 | 決定 |
| ---- | ---- |
| 束 | 29b 全 ID を **1 PR** |
| C01 | `DatePicker.SelectedDate` を `yyyy-MM-dd`（Invariant）で Set/Expect。Calendar UI なし |
| L04 | 既存 `Expand`/`Collapse`/`ExpectExpanded` + tree `expanded` ← `ComboBox.IsDropDownOpen` |
| L06 | 行は既存 ListBox API。GridView セルは Header キーで **Read のみ** |
| C03 | `ExpectToolTipAsync(text)`（通常 Hover → Expect）。閉時は tree に載せない |
| C04 | 専用 API なし。ToolBar/StatusBar を Sample/E2E で正式化（overflow なし） |
| C05 | `IsOpen` 時だけ Popup `Child` をツリー合流。開閉専用 API なし |
| C06 | `TextBlock` 内 `Hyperlink` をツリー＋`Click`。NavigateUri Expect なし |
| Sample / Scenario / MCP | Phase29b セクション。薄い追従（MCP は `expectToolTip`） |

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

## Batch 2 — 29b 実装 + E2E

- [x] DatePicker value / ComboBox Expand / ListView GridView cell Read
- [x] ToolTip Expect + Popup 合流 + Hyperlink tree/Click
- [x] ExpectToolTipAsync + Scenario/MCP
- [x] Sample Phase29b + Fluent/Scenario E2E
- [x] 完了チェック

---

## Phase 29b 完了チェック

- [x] DatePicker Set/Expect（`yyyy-MM-dd`）ができる
- [x] ComboBox Expand/Collapse/ExpectExpanded ができる
- [x] ListView 行選択 + GridView セル Read ができる
- [x] ExpectToolTip / ToolBar·StatusBar / Popup / Hyperlink ができる
- [x] Sample Fluent + Scenario が緑
- [x] Avalonia / Inspector / Calendar UI / overflow は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase31.md](./task_phase31.md) SendInput 並列（`X04`、正本 `-m:1`）
