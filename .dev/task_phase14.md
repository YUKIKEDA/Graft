# Phase 14 — キー chord / 特殊キー（`pressKeys`）

受け入れ条件（要約）: リテラル `sendKeys` と分離した **`pressKeys` / `PressAsync`** で、1 呼び出し = 1 chord の修飾キー＋特殊キーを送れるようにし、Core Fluent → Scenario / MCP に揃える。  
含めない: F1–F12、Win キー、NumPad 専用、`typeHuman`、複数 chord 連結、公開 Screenshot、右クリック／Menu、Avalonia、Inspector。  
参照: [project.md](./project.md) Q103〜。前フェーズ: [task_phase13.md](./task_phase13.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| ロードマップ | Avalonia を後ろへ。WPF 競合ギャップ埋めを優先 |
| Phase 14 範囲 | **キー chord のみ** |
| API | **`PressAsync(keys)`**（`SendKeysAsync` はリテラルのまま） |
| wire | **`pressKeys`**（params: automationId + keys） |
| 単位 | **1 呼び出し = 1 chord** |
| フォーカス | resolve → フォーカス → chord（グローバルホットキー専用は含めない） |
| DSL | `Key` または `Mod+…+Key`（`Control` / `Alt` / `Shift`） |
| 語彙 | `A`–`Z`, `0`–`9`, `Enter`, `Tab`, `Escape`, `Backspace`, `Delete`, `Space`, `ArrowUp/Down/Left/Right` |
| Sample E2E | TextBox: SetValue → `Control+A` → `Delete` → Expect 空 |
| 公開経路 | Scenario / MCP 薄い追従 |
| Phase 14 の次 | **公開 Screenshot** → 右クリック/Menu → … → Avalonia |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase14.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase13.md` / `graft-core.md` 更新

---

## Batch 1 — Parser + SendInput + wire

- [x] chord パーサ（共有）+ ユニットテスト
- [x] `InputInjector` で chord 送信
- [x] Protocol `pressKeys` + Instrumentation / Wpf

---

## Batch 2 — Core + Sample E2E

- [x] `ElementQuery.PressAsync`
- [x] Sample Fluent E2E（Control+A / Delete）

---

## Batch 3 — Scenario / MCP + docs

- [x] schema / Runner / MCP
- [x] Scenario E2E
- [x] 完了チェック。次: **公開 Screenshot**

---

## Phase 14 完了チェック

- [x] `PressAsync` / `pressKeys` がある（`sendKeys` はリテラルのまま）
- [x] 合意語彙の chord が送れる
- [x] Sample E2E が緑
- [x] Scenario / MCP から呼べる
- [x] Screenshot / 右クリック / Avalonia 等は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** 公開 Screenshot → 右クリック/Menu → Tab/Slider/複数選択/DataGrid 列 → Avalonia → Inspector
