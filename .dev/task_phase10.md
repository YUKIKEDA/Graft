# Phase 10 — OpenFile ダイアログ・シーム（方針 + MVP）

受け入れ条件（要約）: OS **OpenFile** を実ダイアログ操作ではなく **テスト用 Runtime シーム**で扱い、事前 Arm（単一パス OK / Cancel）→ 素の `OpenFileDialog.ShowDialog` がスタブ応答するまで Core Fluent → Scenario / MCP に揃える。  
含めない: 実 OS ダイアログの UIA/SendInput 操作、アプリ業務コードへの Graft API、SaveFile / Folder / MessageBox、複数選択、Avalonia、Inspector。  
参照: [project.md](./project.md) Q87〜。前フェーズ: [task_phase9.md](./task_phase9.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 成果物 | **方針 + 狭い MVP 実装**（文書だけで終わらない） |
| MVP 対象 | **OpenFile のみ**（`Microsoft.Win32.OpenFileDialog`） |
| 技術 | **Runtime シーム**（Harmony で `CommonItemDialog.RunDialog` を Prefix） |
| アプリ側 | **素の `OpenFileDialog`**。業務コードに Graft API は出さない |
| テスト専用ブート | `#if GRAFT_TEST` の `WpfGraft.Use` / `Agent.Start` は現状維持 |
| 操作モデル | **事前 Arm** → アプリが素の ShowDialog（トリガと応答を分離） |
| パッチ対象 | **`CommonItemDialog.RunDialog(IntPtr)`**（.NET の OpenFileDialog 実装。`ShowDialog` の `bool?` は回避） |
| 応答 | **単一パス OK** + **Cancel** |
| 未アーム | パッチは **元の RunDialog（実ダイアログ）へフォールバック** |
| 開封トリガ | **`InvokeOpeningWindowAsync(waitForNewWindow: false)`**（BeginInvoke。新 WPF 窓は出ない） |
| Arm 寿命 | **一回限り**（消費後クリア。未消費の再 Arm は上書き） |
| 公開経路 | Scenario / MCP 薄い追従（`armOpenFile` / `armOpenFileCancel`） |
| 含めない | 実 OS UIA、アプリへの Graft 業務依存、Save/Folder/MessageBox、複数選択、Avalonia、Inspector |
| Phase 10 の次 | **SaveFile シーム**（OpenFile と同型の薄い拡張） |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase10.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 10 を追記
- [x] `AGENTS.md` の Phase 参照を更新
- [x] `task_phase9.md` / `graft-core.md` の次フェーズメモを更新

---

## Batch 1 — Runtime シーム + Arm wire

- [x] Instrumentation: `OpenFileArm` + Protocol `armOpenFile` / `armOpenFileCancel`
- [x] Wpf: Lib.Harmony で `CommonItemDialog.RunDialog` Prefix（`OpenFileDialog` のみ）
- [x] `WpfGraft.Use` でパッチ適用（多重適用ガード）
- [x] ホスト側ユニット検証（Arm + ShowDialog）

---

## Batch 2 — Core + Sample

- [x] Core: `ArmOpenFileAsync` / `ArmOpenFileCancelAsync`（`GraftSession`）
- [x] Sample: 素の `OpenFileDialog` + StatusText 反映（Graft API なし）
- [x] 開封は `InvokeOpeningWindowAsync(waitForNewWindow: false)`

---

## Batch 3 — Fluent E2E

- [x] E2E: Arm(path) → invoke → Expect
- [x] E2E: ArmCancel → invoke → Expect
- [x] `graft-core.md` に OpenFile シームの短いメモ

---

## Batch 4 — Scenario / MCP + docs

- [x] scenario schema: `armOpenFile` / `armOpenFileCancel` + `waitForNewWindow`
- [x] ScenarioJson / Runner / MCP 原子ツール
- [x] Scenario E2E
- [x] 完了チェック。次フェーズメモ: **SaveFile シーム**

---

## Phase 10 完了チェック

- [x] 素の `OpenFileDialog` を Harmony `CommonItemDialog.RunDialog` シームで扱える（未アームは実ダイアログ）
- [x] `ArmOpenFile` / `ArmOpenFileCancel`（一回限り）がある
- [x] Sample 業務コードに Graft ダイアログ API が無い
- [x] Sample E2E（OK + Cancel、`waitForNewWindow: false`）が緑
- [x] Scenario / MCP から Arm が呼べる（薄い追従）
- [x] 実 OS UIA / アプリ Graft 業務依存 / Save・Folder・MessageBox / 複数選択 / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 実コモンダイアログ HWND は触らない（in-process Runtime シームが正本）
- アプリは素の `OpenFileDialog` のまま（Graft ラッパ不要）
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** SaveFile シーム → … → Avalonia → Inspector（最後寄り）
