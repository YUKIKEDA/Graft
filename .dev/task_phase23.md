# Phase 23 — 競合シナリオ対照表（WPF Must 洗い出し）

受け入れ条件（要約）: FlaUI / WinAppDriver / TestStack.White / Appium(Windows) クラスの **自社デスクトップ E2E シナリオ** を洗い出し、[competitive-gap.md](./competitive-gap.md) に Graft 現状と優先を記録し、**Must を確定**する。  
**実装コードは含めない。** Avalonia は Must 完了まで禁止。  
参照: [project.md](./project.md) Q125〜。前フェーズ: [task_phase22.md](./task_phase22.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| Avalonia | Must 確定・完了まで禁止 |
| 比較軸 | デスクトップ操作・検証シナリオ（FlaUI 系）。Playwright DX / TestComplete Spy は必須にしない |
| ゲート | 確定 Must がすべて Sample E2E 緑になるまで Avalonia 禁止 |
| Phase 23 | 文書のみ。Must は表で確定済み（Q127） |
| 画面遷移 | 出現/消失待ち + 進捗→完了→次画面は Must（W06–W11 等） |
| 任意除外 | K05 / V06 / W12 / A08 / P02。Inspector（F08）も任意 |
| 次 | [task_phase24.md](./task_phase24.md) → … → Avalonia |

---

## Batch 0 — 対照表 + ロードマップ

- [x] `.dev/competitive-gap.md` を追加
- [x] 本ファイル `.dev/task_phase23.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase22.md` / `graft-core.md` 更新

---

## Phase 23 完了チェック

- [x] 対照表に操作・待ち・検証・コントロール・遷移・安定性の洗い出しがある
- [x] 各行に Graft 現状と優先ラベルがある
- [x] Must 確定と仮 Phase 分割がある
- [x] 実装コード変更は **含めない**
- [x] レビューで Must を確定（Q127）

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先。シナリオ正本は `competitive-gap.md`
- **次:** [task_phase24.md](./task_phase24.md) → … → Avalonia
