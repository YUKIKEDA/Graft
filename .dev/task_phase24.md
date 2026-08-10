# Phase 24 — 待ち / Expect / 画面遷移・進捗

受け入れ条件（要約）: 汎用 Wait（出現/消失）と Expect（enabled / visible / name contains|regex / value）、窓クローズ待ち、`TreeNode.value`、Sample（進捗ダイアログ→完了→同一窓内次パネル）を追加する。  
ID: `W06–W11`, `A04–A07`, `H03`, `C02`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: Frame 専用 DSL（H02）、トースト（W12）、ソフトアサート（A08）、要素クリップ、Avalonia。  
参照: [project.md](./project.md) Q128〜。前フェーズ: [task_phase23.md](./task_phase23.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| API | `ExpectEnabled` / `ExpectVisible` / `ExpectNameContains` / `ExpectNameMatches` / `ExpectValue`。`WaitFor`（出現）、`ExpectGone`（消失）、`WaitForWindowClosed` |
| value | `TreeNode.value`（string、非該当省略）。ProgressBar / Slider 等 |
| Sample | 進捗 Window（ProgressBar）→ Close → 同一窓内次パネル。Frame なし |
| Scenario / MCP | 薄い追従 |
| W11 | 専用 API なし |
| Phase 24 の次 | マウス高度（Phase 25） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase23.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — tree value + Wait/Expect + Sample E2E

- [x] `TreeNode.value` + WPF 供給
- [x] Fluent / Scenario / schema / MCP
- [x] Sample + E2E
- [x] 完了チェック

---

## Phase 24 完了チェック

- [x] 出現/消失待ちができる
- [x] ExpectEnabled / Visible / NameContains|Matches / Value ができる
- [x] WaitForWindowClosed ができる
- [x] 進捗→次画面 Sample Fluent + Scenario が緑
- [x] Frame DSL / Avalonia は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase25.md](./task_phase25.md)
