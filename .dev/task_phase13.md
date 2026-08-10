# Phase 13 — MessageBox シーム（Runtime MVP）

受け入れ条件（要約）: WPF **`MessageBox.Show`** を Runtime シームで扱い、事前 Arm（`MessageBoxResult`）→ 素の `Show` がスタブ応答するまで Core Fluent → Scenario / MCP に揃える。  
含めない: WinForms MessageBox、カスタムダイアログ、メッセージ本文の Assert、Avalonia、Inspector。  
参照: [project.md](./project.md) Q99〜。前フェーズ: [task_phase12.md](./task_phase12.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 成果物 | **方針 + 狭い MVP 実装** |
| MVP 対象 | **`System.Windows.MessageBox.Show`**（主要オーバーロード） |
| 技術 | **Runtime シーム**（Harmony Prefix、未アームは実 MessageBox） |
| アプリ側 | **素の `MessageBox.Show`**。業務コードに Graft API は出さない |
| 操作モデル | **事前 Arm** → アプリが素の Show |
| 応答 | **`MessageBoxResult`**（`OK` / `Cancel` / `Yes` / `No` / `None`） |
| 開封トリガ | **`InvokeOpeningWindowAsync(waitForNewWindow: false)`** |
| Arm 寿命 | **一回限り**（他 Arm と独立） |
| 公開経路 | Scenario / MCP 薄い追従（`armMessageBox` + `result` 文字列） |
| 含めない | WinForms MessageBox、カスタムダイアログ、本文 Assert、Avalonia、Inspector |
| Phase 13 の次 | Phase 14 キー chord（[task_phase14.md](./task_phase14.md)）。Avalonia は後ろ |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase13.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase12.md` / `graft-core.md` 更新

---

## Batch 1 — Seam + Arm wire

- [x] `MessageBoxArm` + Protocol `armMessageBox`
- [x] Harmony で `MessageBox.Show` オーバーロードを Prefix
- [x] ホスト側ユニット検証

---

## Batch 2 — Core + Sample

- [x] Core: `ArmMessageBoxAsync(result)`
- [x] Sample: 素の Yes/No MessageBox + StatusText
- [x] 開封は `waitForNewWindow: false`

---

## Batch 3 — Fluent / Scenario / MCP + docs

- [x] Fluent E2E（Yes + No）
- [x] scenario schema / Runner / MCP
- [x] Scenario E2E
- [x] 完了チェック。次: **Avalonia**

---

## Phase 13 完了チェック

- [x] 素の `MessageBox.Show` を Runtime シームで扱える（未アームは実 UI）
- [x] `ArmMessageBox`（一回限り）がある
- [x] Sample 業務コードに Graft ダイアログ API が無い
- [x] Sample E2E + Scenario / MCP が緑
- [x] WinForms / カスタムダイアログ / 本文 Assert / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- ファイル系シームと同型の Arm モデル。戻り値は `MessageBoxResult`
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase14.md](./task_phase14.md)（キー chord）→ Screenshot → 右クリック/Menu → … → Avalonia → Inspector
