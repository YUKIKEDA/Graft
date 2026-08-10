# Phase 6 — ツリー状態（selected / expanded）

受け入れ条件（要約）: `getTree` の `TreeNode` に **`selected` / `expanded`（`bool?`）** を載せ、Core の Expect →（薄い）Scenario / MCP まで揃える。Phase 5 の select/expand を **ツリー状態で検証**できるようにする（診断・LLM 向け）。  
含めない: Avalonia、Inspector、`checked`、複数選択、ComboBox 項目 Expect の完了条件化、プロトコル major 版上げ、自己修復が selected/expanded を使うこと、項目キー/表示名指定、コンテナへの選択集約。  
参照: [project.md](./project.md) Q67〜。前フェーズ: [task_phase5.md](./task_phase5.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。実装が小さければ Batch 1+2 や 3+4 のマージ可（メモのみ。分割案は残す）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 表現 | `bool? selected` / `bool? expanded`。非該当は null／JSON 省略（既存 `WhenWritingNull`） |
| selected 意味 | 選択系のみ（ListBoxItem / ComboBoxItem / TreeViewItem 等）。CheckBox の checked は載せない |
| 載せるノード | **項目側** selected。**開閉対象側** expanded（TreeViewItem / Expander）。コンテナ集約はしない |
| Expect | `ExpectSelectedAsync(bool)` / `ExpectExpandedAsync(bool)`。プロパティ null は `expect.failed`（false 扱いしない） |
| 公開経路 | Scenario: `expectSelected` / `expectExpanded`（キーは `selected` / `expanded`）。MCP: `graft_expect_selected` / `graft_expect_expanded` |
| プロトコル | v1 のまま（加法的フィールド） |
| 受け入れ | 仮想化 ListBox の**実現済み**項目で ExpectSelected。TreeViewItem で ExpectExpanded（Expander は任意）。ComboBox 項目 Expect は完了条件外 |
| Phase 6 の次 | Avalonia → Inspector |

---

## Batch 0 — タスク文書（ブランチ: `phase6/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase6.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 6 を追記
- [x] `AGENTS.md` の Phase 参照を更新

**完了条件:** Batch 分割と契約が文書化されている。  
**次:** Batch 1（wire + WPF walker）へ。

---

## Batch 1 — wire + WPF walker（ブランチ: `phase6/batch-1-tree-state`）

- [x] `TreeNode` に `selected` / `expanded`（`bool?`）
- [x] WPF walker: 項目の IsSelected 等 → `selected`、TreeViewItem/Expander → `expanded`（非該当は null）
- [x] 単体または既存ツリー経路でフィールドが載ることを確認

**完了条件:** Sample 上で select/expand 後の getTree に状態が現れる。  
**次:** Batch 2（Core Expect）へ。

---

## Batch 2 — Core Expect（ブランチ: `phase6/batch-2-expect`）

- [x] `ExpectSelectedAsync(bool)` / `ExpectExpandedAsync(bool)`（ExpectName と同型ポーリング）
- [x] null（非該当）は `expect.failed`
- [x] FailureSteps / FailureReport 整合

**完了条件:** Core API から状態 Expect できる。  
**次:** Batch 3（Sample E2E）へ。

---

## Batch 3 — Sample E2E（ブランチ: `phase6/batch-3-sample-e2e`）

- [x] select → 実現済み `ListItem-xx` で `ExpectSelectedAsync(true)`
- [x] expand/collapse → `SampleTreeRoot` で `ExpectExpandedAsync`
- [ ] （任意）未選択側 `ExpectSelectedAsync(false)` の一打

**完了条件:** 副作用ではなくツリー状態 Expect で緑。  
**次:** Batch 4（Scenario / MCP + docs）へ。

---

## Batch 4 — Scenario / MCP + docs（ブランチ: `phase6/batch-4-docs`）

- [x] scenario schema: `expectSelected` / `expectExpanded`
- [x] ScenarioJson / Runner / MCP 原子ツール
- [x] Scenario E2E または既存 phase5 シナリオへの追記（薄い追従で可）
- [x] `graft-core.md` / 本ファイル完了チェック
- [x] 次フェーズメモ: Avalonia → Inspector → **改訂:** Phase 7 はウィンドウ／モーダル（[task_phase7.md](./task_phase7.md)）。Avalonia は後ろへ

**完了条件:** 公開経路と文書が揃っている。  
**次:** Phase 6 完了チェック → Phase 7（ウィンドウ／モーダル）へ。

---

## Phase 6 完了チェック

- [x] `TreeNode` に `selected` / `expanded`（`bool?`）がある
- [x] 非該当ノードでは省略／null
- [x] `ExpectSelectedAsync` / `ExpectExpandedAsync` がある（null は expect.failed）
- [x] ListBox 実現済み項目 + TreeViewItem の Sample E2E が緑
- [x] Scenario / MCP から同 Expect が呼べる（薄い追従で可）
- [x] Avalonia / Inspector / checked / 複数選択 / Combo 完了条件 / 自己修復利用は **含めない**

---

## 進め方メモ

- 仮想化は実現済みツリーがデフォルト（Q23）。未実現項目はツリーに現れない
- MCP / Scenario は Core Expect の薄いラッパー。操作ロジックを複製しない
- 設計矛盾時は `project.md` 優先
