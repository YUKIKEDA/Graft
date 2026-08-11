# Phase 31 — SendInput 並列対策（X04）

受け入れ条件（要約）: 全解テストで SendInput 系が安定して緑になる。  
ID: `X04`（[competitive-gap.md](./competitive-gap.md)）。  
参照: [project.md](./project.md) Q137〜。前フェーズ: [task_phase29.md](./task_phase29.md)。

---

## 方針（確定）

プロセス横断 named mutex（`Local\Graft.UiSession`）を試作したが、**Launch 直列化だけでは不十分**だった。  
並列 `testhost` / IDE 存在下では `SetForegroundWindow` が失敗しやすく、SendInput（keys / click / ContextMenu）が外れる症状が残る。

| 項目 | 決定 |
| ---- | ---- |
| 正本 | 全解実行は **`dotnet test Graft.slnx -m:1` を必須** |
| mutex | **採用しない**（前景確保の問題は別途検討） |
| アセンブリ内 | 従来どおり `SampleUiCollection` / `McpUiCollection` |
| CI workflow | 本フェーズでは含めない（使うなら `-m:1`） |
| Must 扱い | **X04 = Done**（真の並列安定化は非目標。運用でゲート充足） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加・方針改訂を記録
- [x] `project.md` / `AGENTS.md` / `graft-core.md` / `competitive-gap.md` / `task_phase29.md` 更新
- [x] X04 を Done に更新（`-m:1` 正本）

---

## Batch 1 — 実装

- [x] mutex 実装は見送り（試作は撤回）
- [ ] 前景確保の強化（任意・別フェーズ）

---

## Phase 31 完了チェック

- [x] 全解テストの正本が `-m:1` である旨を文書化した
- [x] X04 を Done（`-m:1` 運用。真並列は非目標）
- [x] Avalonia / CI YAML は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase32.md](./task_phase32.md)（H02 Frame）→ [task_phase33.md](./task_phase33.md)（D06）→ Avalonia
