# Phase 32 — Frame 遷移（H02）

受け入れ条件（要約）: Sample に `Frame` + 複数 Page を置き、ナビ後を既存 `WaitFor` / `Expect*` で検証する E2E が緑。  
ID: `H02`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: NavigationWindow、専用 Frame DSL、Scenario/MCP 専用 API、Avalonia、Inspector。  
参照: [project.md](./project.md) Q138〜。前フェーズ: [task_phase31.md](./task_phase31.md)。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 範囲 | **Frame のみ**（NavigationWindow は本 Must 外） |
| API | **専用 DSL なし**。既存探索 + `WaitFor` / `Expect*` |
| Sample | Frame + 2〜3 Page。ナビ後に表示テキスト等で検証 |
| Scenario / MCP | 薄い追従不要（既存操作で足りる） |
| Phase 32 の次 | Phase 33（操作タイムライン / D06） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `competitive-gap.md` / `task_phase31.md` / `graft-core.md` 更新

---

## Batch 1 — Sample + E2E

- [ ] Sample: Frame ホスト + Page 群 + ナビ UI（AutomationId 付き）
- [ ] Walker / ツリー: Frame 内コンテンツが既存探索で取れることを確認（必要なら最小修正）
- [ ] Core / Sample E2E: ナビ → WaitFor / Expect*
- [ ] 完了チェック

---

## Phase 32 完了チェック

- [ ] H02 が Sample E2E で緑
- [ ] `competitive-gap.md` の H02 を OK / Done に更新
- [ ] NavigationWindow・専用 DSL を入れてない
- [ ] Avalonia は含めない

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** [task_phase33.md](./task_phase33.md)（D06）
