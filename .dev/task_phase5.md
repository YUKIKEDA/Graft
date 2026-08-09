# Phase 5 — WPF 残アクション（scroll / select / expand）

受け入れ条件（要約）: 競合パリティ追いかけではなく **Graft ロードマップ完遂**の一環として、WPF に残っていた論理操作 **`scrollIntoView` / `select` / `expand`・`collapse`** を載せ、Core Fluent →（薄い）Scenario / MCP まで揃える。  
含めない: Avalonia、Inspector、ファジー自己修復、複数選択、expand パス一括、`append` / `typeHuman`、ツリーへの `selected`/`expanded` フィールド（**直後の次フェーズ**で実施と明記）。  
参照: [project.md](./project.md) Q41 / Q23 / Q66〜。前フェーズ: [task_phase4.md](./task_phase4.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。Scenario / MCP は各アクション Batch の末尾、または Phase 末の薄い追従 Batch でよい。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 順 | `scrollIntoView` → `select` → `expand`/`collapse` |
| scroll 二系統 | 実現済み: `GetBy(el).ScrollIntoViewAsync()`（引数なし）。未実現: `GetBy(list).ScrollIntoViewAsync(index)` |
| scroll 受け入れ | 仮想化 ListBox/ListView（画面外 index → realize + 操作可能） |
| scroll 成功時 | identity 返却（少なくとも `automationId`）。ElementQuery チェーン返却は後付け可 |
| 項目キー | index が正本。表示名 / データキー指定は **次 Batch 候補**（Phase 5 完了条件外でもタスクに残す） |
| select | 単一選択・index 正本。ListBox/ComboBox/ListView。内部で自動 scroll/realize |
| expand | `ExpandAsync()` / `CollapseAsync()`（状態指定）。トグル・パス一括は後回し |
| Expect | Phase 5 完了は Sample **副作用**で十分。ツリー `selected`/`expanded` は **Phase 5 直後** |
| Phase 5 の次 | ツリー状態フィールド → Avalonia →（さらに後）Inspector |

---

## Batch 0 — タスク文書（ブランチ: `phase5/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase5.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 5 を追記

**完了条件:** Batch 分割と契約が文書化されている。  
**次:** Batch 1（scrollIntoView）へ。

---

## Batch 1 — `scrollIntoView`（ブランチ: `phase5/batch-1-scroll-into-view`）

- [ ] wire: `scrollIntoView`（`automationId`、任意 `index`）
- [ ] WPF: 要素 `BringIntoView` / リスト `ScrollIntoView` + コンテナ生成
- [ ] 成功時 result に実現項目の identity（`automationId` 必須）
- [ ] Core: `ScrollIntoViewAsync()` / `ScrollIntoViewAsync(int index)`
- [ ] Sample: 仮想化 ListBox（十分な項目数）+ E2E 1 経路
- [ ] （任意この Batch 末尾）Scenario / MCP 薄い追従

**完了条件:** 仮想化リストの画面外 index を scroll し、返った identity で後続操作またはツリー確認ができる。  
**次:** Batch 2（select）へ。

---

## Batch 2 — `select`（ブランチ: `phase5/batch-2-select`）

- [ ] wire: `select`（`automationId` = リスト/コンボ、`index`）
- [ ] WPF: 単一選択。未実現なら内部で scroll/realize してから選択
- [ ] Core: `SelectAsync(int index)`
- [ ] Sample: 選択で StatusText 等が変わる副作用 + E2E
- [ ] （任意）Scenario / MCP 追従

**完了条件:** index 指定の単一選択が仮想化 ListBox（および ComboBox いずれか）で緑。  
**次:** Batch 3（expand/collapse）へ。

---

## Batch 3 — `expand` / `collapse`（ブランチ: `phase5/batch-3-expand-collapse`）

- [ ] wire: `expand` / `collapse`（または `setExpanded` + bool）
- [ ] WPF: TreeViewItem / Expander 等
- [ ] Core: `ExpandAsync()` / `CollapseAsync()`
- [ ] Sample: 展開で子や Status が変わる副作用 + E2E
- [ ] （任意）Scenario / MCP 追従

**完了条件:** 状態指定で開閉できる。  
**次:** Batch 4（docs / 完了チェック、未追従の Scenario・MCP があればここで）へ。

---

## Batch 4 — docs + 追従の締め（ブランチ: `phase5/batch-4-docs`）

- [ ] 未追従なら Scenario schema / MCP ツールを揃える
- [ ] `graft-core.md` 更新
- [ ] 本ファイル完了チェック
- [ ] 次フェーズメモ: ツリー `selected`/`expanded`（診断・LLM）→ Avalonia

**完了条件:** 公開経路と文書が揃っている。  
**次:** Phase 5 完了チェック → ツリー状態フェーズへ。

---

## 直後にやる（Phase 5 完了条件外・明記）

- [ ] `TreeNode` に `selected` / `expanded`（または同等）を載せ、Expect 可能にする
- [ ] （候補）scroll/select の **項目キー / 表示名** 指定
- [ ] Avalonia アダプタ
- [ ] Inspector

---

## Phase 5 完了チェック

- [ ] 仮想化リストで `scrollIntoView(index)` が identity を返す
- [ ] 実現済み要素の引数なし `scrollIntoView` がある
- [ ] `select(index)` が単一選択でき、必要なら自動 scroll する
- [ ] `expand` / `collapse` で状態指定できる
- [ ] Sample 副作用ベースの E2E が緑
- [ ] Scenario / MCP から同操作が呼べる（薄い追従で可）
- [ ] Avalonia / Inspector / ツリー selected・expanded / 複数選択 / パス一括は **含めない**

---

## 進め方メモ

- 論理操作順は従来どおり **ネイティブ → Peer/Provider → SendInput**（Q40）
- 仮想化は実現済みツリーがデフォルト。realize/scroll は本 Phase の API が担う（Q23）
- MCP / Scenario は Core の薄いラッパー。操作ロジックを複製しない
- 設計矛盾時は `project.md` 優先
