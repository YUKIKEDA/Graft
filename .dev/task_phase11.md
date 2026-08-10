# Phase 11 — SaveFile ダイアログ・シーム（OpenFile 同型 MVP）

受け入れ条件（要約）: OS **SaveFile** を OpenFile と同型の **Runtime シーム**で扱い、事前 Arm（単一パス OK / Cancel）→ 素の `SaveFileDialog.ShowDialog` がスタブ応答するまで Core Fluent → Scenario / MCP に揃える。  
含めない: 実 OS UIA、アプリ業務コードへの Graft API、Folder / MessageBox、複数選択、Avalonia、Inspector。  
参照: [project.md](./project.md) Q91〜。前フェーズ: [task_phase10.md](./task_phase10.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 成果物 | **方針 + 狭い MVP 実装**（OpenFile の薄い拡張） |
| MVP 対象 | **SaveFile のみ**（`Microsoft.Win32.SaveFileDialog`） |
| 技術 | **Runtime シーム**（既存 Harmony `CommonItemDialog.RunDialog` Prefix を共有） |
| アプリ側 | **素の `SaveFileDialog`**。業務コードに Graft API は出さない |
| 操作モデル | **事前 Arm** → アプリが素の ShowDialog |
| 応答 | **単一パス OK** + **Cancel** |
| 未アーム | 元の RunDialog（実ダイアログ）へフォールバック |
| 開封トリガ | **`InvokeOpeningWindowAsync(waitForNewWindow: false)`** |
| Arm 寿命 | **一回限り**（OpenFile Arm とは独立ストア） |
| 公開経路 | Scenario / MCP 薄い追従（`armSaveFile` / `armSaveFileCancel`） |
| 含めない | 実 OS UIA、アプリ Graft 業務依存、Folder/MessageBox、複数選択、Avalonia、Inspector |
| Phase 11 の次 | Phase 12 OpenFolder シーム（[task_phase12.md](./task_phase12.md)） |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase11.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 11 を追記
- [x] `AGENTS.md` / `task_phase10.md` / `graft-core.md` の次フェーズメモを更新

---

## Batch 1 — Seam + Arm wire

- [x] `SaveFileArm` + Protocol `armSaveFile` / `armSaveFileCancel`
- [x] `CommonItemDialogPatch` で `SaveFileDialog` も介入（OpenFile と共有）
- [x] ホスト側ユニット検証

---

## Batch 2 — Core + Sample

- [x] Core: `ArmSaveFileAsync` / `ArmSaveFileCancelAsync`
- [x] Sample: 素の `SaveFileDialog` + StatusText
- [x] 開封は `waitForNewWindow: false`

---

## Batch 3 — Fluent / Scenario / MCP + docs

- [x] Fluent E2E（OK + Cancel）
- [x] scenario schema / Runner / MCP
- [x] Scenario E2E
- [x] 完了チェック。次: **Folder シーム**

---

## Phase 11 完了チェック

- [x] 素の `SaveFileDialog` を Runtime シームで扱える（未アームは実ダイアログ）
- [x] `ArmSaveFile` / `ArmSaveFileCancel`（一回限り、OpenFile と独立）
- [x] Sample 業務コードに Graft ダイアログ API が無い
- [x] Sample E2E + Scenario / MCP が緑
- [x] Folder / MessageBox / 実 OS UIA / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- OpenFile と同型。設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase12.md](./task_phase12.md)（OpenFolder）→ MessageBox → … → Avalonia → Inspector（最後寄り）
