# Phase 18 — Slider 値設定（`setValue` 拡張）

受け入れ条件（要約）: 既存 **`SetValueAsync` / wire `setValue`** を **Slider** に対応させ、Invariant の double 文字列を `Slider.Value` に書き、Sample の StatusText 副作用で検証する。  
含めない: 複数選択、専用 wire、tree の `value` フィールド、decimal 専用 E2E、全 RangeBase 抽象、Avalonia、Inspector。  
参照: [project.md](./project.md) Q114〜。前フェーズ: [task_phase17.md](./task_phase17.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 範囲 | **Slider のみ** |
| API | 既存 `SetValueAsync("75")` を拡張（InvariantCulture で double 解析 → `Slider.Value`） |
| 検証 | StatusText 副作用のみ（tree `value` は出さない） |
| Scenario | 既存 `setValue` |
| MCP | 変更なし |
| Phase 18 の次 | 複数選択（Phase 19）など |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase18.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase17.md` / `graft-core.md` 更新

---

## Batch 1 — Slider setValue + Sample E2E

- [x] `WpfElementValueSetter` で Slider をネイティブ対応（不正文字列は明確なエラー）
- [x] Sample Slider + ValueChanged → StatusText（例: `Slider 75`）
- [x] Fluent / Scenario E2E
- [x] 完了チェック。次: [task_phase19.md](./task_phase19.md)

---

## Phase 18 完了チェック

- [x] Slider で `SetValueAsync("75")` ができる
- [x] Sample StatusText が期待どおり更新される
- [x] Sample Fluent + Scenario が緑
- [x] 複数選択 / 専用 wire / tree value / Avalonia は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase19.md](./task_phase19.md) → … → Avalonia → Inspector
