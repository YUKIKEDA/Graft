# Phase 8 — DataGrid 行中心 MVP + `checked`

受け入れ条件（要約）: WPF **DataGrid** を **行中心 MVP** で操作・検証できるようにし、既存の `scrollIntoView` / `select` / `ExpectSelected` をホスト＋index で再利用する。同一フェーズの **最終薄い Batch** でツリー `checked`（CheckBox 等）も閉じる。  
含めない: セル座標 API、編集コミット、ソート／リサイズ、複数選択、`SelectionUnit=Cell`、OS ダイアログ、Avalonia、Inspector。  
参照: [project.md](./project.md) Q77〜。前フェーズ: [task_phase7.md](./task_phase7.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| テーマ | WPF **DataGrid**（複雑ホスト UI）。OS ダイアログは後続候補のまま |
| 受け入れ | **行中心 MVP**: 仮想化行の scroll/realize、行 `select`、`ExpectSelected` |
| API | 既存 **`scrollIntoView` / `select`** をホスト＋index で再利用。新 wire は増やさない |
| ツリー | 実現済み **`DataGridRow` + `selected`**。行に安定 `automationId` |
| Sample | **`SelectionUnit=FullRow` + `SelectionMode=Single` のみ** |
| 公開経路 | 既存 Scenario ステップで薄い E2E。DataGrid 専用 MCP ツールは作らない |
| `checked` | **同一 Phase・最終薄い Batch**（`TreeNode.checked` + Expect + Scenario/MCP） |
| 含めない | セル座標 API、編集コミット、ソート／リサイズ、複数選択、Cell 選択モード、OS ダイアログ、Avalonia、Inspector |
| Phase 8 の次 | DataGrid **セル R/W**（別フェーズ） |

---

## Batch 0 — タスク文書（ブランチ: `phase8/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase8.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 8 を追記
- [x] `AGENTS.md` の Phase 参照を更新
- [x] `task_phase7.md` / `graft-core.md` の次フェーズメモを更新

**完了条件:** Batch 分割と契約が文書化されている。  
**次:** Batch 1（Sample DataGrid + 行ツリー）へ。

---

## Batch 1 — Sample DataGrid + 行ツリー（ブランチ: `phase8/batch-1-datagrid-tree`）

- [ ] Sample: DataGrid（`SelectionUnit=FullRow` / `SelectionMode=Single`）、行に安定 `automationId`
- [ ] WPF ツリー: 実現済み `DataGridRow` を列挙し `selected` を解決（`ResolveSelected` 拡張）
- [ ] ホスト解決: DataGrid を scroll/select のホストとして認識できるようにする（必要なら）
- [ ] ユニット／薄いホスト検証（行ノードが木に出る）

**完了条件:** 木に行が出て、選択状態が読める。  
**次:** Batch 2（scroll / select 行操作）へ。

---

## Batch 2 — scroll / select 行操作（ブランチ: `phase8/batch-2-datagrid-actions`）

- [ ] `scrollIntoView`（ホスト＝DataGrid、index＝行）で仮想化行を realize
- [ ] `select`（同）で単一行選択
- [ ] 既存 Core Fluent / wire のまま（新メソッドなしが原則）
- [ ] ホスト側ユニットテスト（仮想化あり／なしの最小）

**完了条件:** index 指定で行を見える化し選択できる。  
**次:** Batch 3（Expect + Sample E2E）へ。

---

## Batch 3 — Expect + Sample E2E（ブランチ: `phase8/batch-3-datagrid-e2e`）

- [ ] `ExpectSelectedAsync` で DataGrid 行を検証（既存 API）
- [ ] Sample E2E: scroll → select → ExpectSelected（Scenario JSON でも薄い追従可）
- [ ] `graft-core.md` に DataGrid 行操作の短いメモ

**完了条件:** 行中心 MVP の Sample E2E が緑。  
**次:** Batch 4（`checked`）へ。

---

## Batch 4 — `checked`（薄い最終 Batch）（ブランチ: `phase8/batch-4-checked`）

- [ ] `TreeNode.checked`（`bool?`、非該当は null/省略）
- [ ] WPF: CheckBox（必要なら ToggleButton 系の最小）で解決
- [ ] `ExpectCheckedAsync` + Scenario/MCP 薄い追従
- [ ] Sample に CheckBox 1 個＋薄い E2E
- [ ] `project.md` / 本ファイル完了チェック。次フェーズメモ: **DataGrid セル R/W**

**完了条件:** `checked` が木と Expect で使える。  
**次:** Phase 8 完了チェック → セル R/W フェーズへ。

---

## Phase 8 完了チェック

- [ ] DataGrid 行を scrollIntoView / select できる（ホスト＋index）
- [ ] 実現済み行の `selected` と `ExpectSelected` が動く
- [ ] Sample E2E（FullRow / Single）が緑
- [ ] `TreeNode.checked` + `ExpectChecked`（Scenario/MCP 薄い追従）がある
- [ ] セル API / 編集 / ソート / 複数選択 / OS ダイアログ / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- ListBox の Phase 5/6 パターン（仮想化・index・`selected`）を DataGridRow に寄せる
- 新 wire / DataGrid 専用 MCP は作らない
- セル編集・セル選択は次フェーズ
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** DataGrid **セル R/W** →（その後）OS ダイアログ方針など → … → Avalonia → Inspector（最後寄り）
