# Phase 4 — 自己修復セレクタ

受け入れ条件（要約）: Core に **代替セレクタ候補の生成**と、**高信頼で一意なとき一回だけ自動再解決**を載せ、`FailureReport` に候補を添付する。マッチは厳密スコアの緩和のみ。  
含めない: Avalonia、ファジー（編集距離等）、シナリオ JSON のディスク書き換え、新 MCP ツール。  
参照: [project.md](./project.md) Q11 / Q22・フェーズ表。前フェーズ: [task_phase3.md](./task_phase3.md) / [task_phase1_leftover.md](./task_phase1_leftover.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## Batch 0 — タスク文書（ブランチ: `phase4/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase4.md` を追加

**完了条件:** Batch 分割と完了条件が文書化されている。  
**次:** Batch 1（FailureReport 候補フィールド）へ。

---

## Batch 1 — FailureReport 候補フィールド（ブランチ: `phase4/batch-1-report-candidates`）

- [x] `HealingCandidate` 型（score / selector / reason）
- [x] `FailureReport.HealingCandidates`（JSON: `healingCandidates`）
- [x] JSON 往復テスト

**完了条件:** レポートに候補配列を載せられる。  
**次:** Batch 2（SelectorHealer）へ。

---

## Batch 2 — SelectorHealer（ブランチ: `phase4/batch-2-healer`）

- [x] 緩和バリアント（基準の部分集合）で一意解決できる候補を列挙
- [x] ノード由来の安定セレクタ（Name + ControlType + Near）を候補化（上限 N）
- [x] `TryGetAutoHeal`: 最高スコアが一意・閾値以上・対象に automationId あり（緩和候補を優先）
- [x] 単体テスト（合成ツリー）
- [x] AutomationId 指定時はハード一致（不一致ならスコア 0）。古い id は fail-closed し、heal で他基準へ落とせる

**完了条件:** 合成ツリーで候補生成と auto-heal 判定が検証できる。  
**次:** Batch 3（ElementQuery 統合）へ。

---

## Batch 3 — ElementQuery 自動再試行（ブランチ: `phase4/batch-3-auto-retry`）

- [x] 実効セレクタ（heal 成功時のみ差し替え、一回限り）
- [x] Wait / Expect の `notFound` で TryHeal → 再 Resolve
- [x] 失敗時 `FailureReport` に候補添付（意図セレクタは従来どおり `selector`）

**完了条件:** 一意候補ならアクションまで通り、複数候補時は失敗＋候補のみ。  
**次:** Batch 4（Sample / docs）へ。

---

## Batch 4 — Sample + docs（ブランチ: `phase4/batch-4-sample-docs`）

- [x] Sample Window に `AutomationId="Main"`
- [x] E2E: 誤 AutomationId + Name/ControlType/Near で auto-heal 成功
- [x] E2E または Core.Tests: 誤 AutomationId のみでタイムアウトし `healingCandidates` 非空
- [x] `graft-core.md` 更新、本ファイル完了チェック

**完了条件:** Sample / Core.Tests で受け入れ経路が緑。  
**次:** Phase 4 完了チェックへ。

---

## Phase 4 完了チェック

- [x] `notFound` / Wait タイムアウト時に `FailureReport.healingCandidates` が読める
- [x] 高信頼一意候補なら同一クエリでアクションまで通る（一回限り）
- [x] 複数候補時は自動適用せず失敗＋候補のみ
- [x] Sample または Core.Tests で緑の受け入れ経路がある
- [x] Avalonia / ファジー / シナリオ書き換え / 新 MCP ツールは **含めない**

---

## 進め方メモ

- 自己修復ロジックは Core。Instrumentation はツリー属性をヒントとして返すだけ（新 wire なし）
- MCP は FailureReport JSON の追加フィールドで透過
- 設計矛盾時は `project.md` 優先（本 Phase の AutomationId ハード一致は fail-closed + heal のための明示決定）
