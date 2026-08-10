# Phase 12 — OpenFolder ダイアログ・シーム（Open/Save 同型 MVP）

受け入れ条件（要約）: OS **Folder** 選択を Open/Save と同型の **Runtime シーム**で扱い、事前 Arm（単一パス OK / Cancel）→ 素の `OpenFolderDialog.ShowDialog` がスタブ応答するまで Core Fluent → Scenario / MCP に揃える。  
含めない: 複数フォルダ選択、WinForms `FolderBrowserDialog`、MessageBox、実 OS UIA、アプリ業務コードへの Graft API、Avalonia、Inspector。  
参照: [project.md](./project.md) Q95〜。前フェーズ: [task_phase11.md](./task_phase11.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 成果物 | **方針 + 狭い MVP 実装**（Open/Save の薄い拡張） |
| MVP 対象 | **`Microsoft.Win32.OpenFolderDialog`**（.NET 8+、結果は `FolderName`） |
| 技術 | **Runtime シーム**（既存 Harmony `CommonItemDialog.RunDialog` Prefix を共有） |
| アプリ側 | **素の `OpenFolderDialog`**。業務コードに Graft API は出さない |
| 操作モデル | **事前 Arm** → アプリが素の ShowDialog |
| 応答 | **単一フォルダパス OK** + **Cancel** |
| 未アーム | 元の RunDialog（実ダイアログ）へフォールバック |
| 開封トリガ | **`InvokeOpeningWindowAsync(waitForNewWindow: false)`** |
| Arm 寿命 | **一回限り**（Open/Save Arm とは独立ストア） |
| 公開経路 | Scenario / MCP 薄い追従（`armOpenFolder` / `armOpenFolderCancel`） |
| 含めない | Multiselect、FolderBrowserDialog、MessageBox、実 OS UIA、アプリ Graft 業務依存、Avalonia、Inspector |
| Phase 12 の次 | **MessageBox シーム** |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase12.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 12 を追記
- [x] `AGENTS.md` / `task_phase11.md` / `graft-core.md` の次フェーズメモを更新

---

## Batch 1 — Seam + Arm wire

- [x] `OpenFolderArm` + Protocol `armOpenFolder` / `armOpenFolderCancel`
- [x] `CommonItemDialogPatch` で `OpenFolderDialog`（`FolderName`）も介入
- [x] ホスト側ユニット検証

---

## Batch 2 — Core + Sample

- [x] Core: `ArmOpenFolderAsync` / `ArmOpenFolderCancelAsync`
- [x] Sample: 素の `OpenFolderDialog` + StatusText
- [x] 開封は `waitForNewWindow: false`

---

## Batch 3 — Fluent / Scenario / MCP + docs

- [x] Fluent E2E（OK + Cancel）
- [x] scenario schema / Runner / MCP
- [x] Scenario E2E
- [x] 完了チェック。次: **MessageBox シーム**

---

## Phase 12 完了チェック

- [x] 素の `OpenFolderDialog` を Runtime シームで扱える（未アームは実ダイアログ）
- [x] `ArmOpenFolder` / `ArmOpenFolderCancel`（一回限り、他 Arm と独立）
- [x] Sample 業務コードに Graft ダイアログ API が無い
- [x] Sample E2E + Scenario / MCP が緑
- [x] Multiselect / FolderBrowserDialog / MessageBox / 実 OS UIA / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- Open/Save と同型。結果プロパティは `FolderName`（`FileName` ではない）
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** MessageBox シーム → … → Avalonia → Inspector（最後寄り）
