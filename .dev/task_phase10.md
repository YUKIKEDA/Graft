# Phase 10 — OpenFile ダイアログ・シーム（方針 + MVP）

受け入れ条件（要約）: OS **OpenFile** を実ダイアログ操作ではなく **テスト用シーム**で扱い、事前 Arm（単一パス OK / Cancel）→ Graft ラッパ経由の開封まで Core Fluent → Scenario / MCP に揃える。  
含めない: 実 OS ダイアログの UIA/SendInput 操作、素の `OpenFileDialog` ランタイムフック、SaveFile / Folder / MessageBox、複数選択、Avalonia、Inspector。  
参照: [project.md](./project.md) Q87〜。前フェーズ: [task_phase9.md](./task_phase9.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 成果物 | **方針 + 狭い MVP 実装**（文書だけで終わらない） |
| MVP 対象 | **OpenFile のみ** |
| 技術 | **テスト用シーム**（実コモンダイアログ UI は出さない） |
| 操作モデル | **事前 Arm** → アプリがラッパを呼ぶ（トリガと応答を分離） |
| シーム仕込み | **Graft 提供ラッパ**（例: `GraftDialogs.ShowOpenFile`）。素の `Microsoft.Win32.OpenFileDialog` はそのまま実ダイアログ（非対応と明記） |
| 応答 | **単一パス OK** + **Cancel** |
| 未アーム | ラッパは **実 `OpenFileDialog` にフォールバック** |
| 開封トリガ | 同期モーダルになりうるため **`InvokeOpeningWindow`** を正（Phase 7 と揃える） |
| Arm 寿命 | **一回限り**（消費後クリア。未消費の再 Arm は上書き） |
| 公開経路 | Scenario / MCP 薄い追従（`armOpenFile` / `armOpenFileCancel`） |
| 含めない | 実 OS 操作、Harmony 等フック、Save/Folder/MessageBox、複数選択、Avalonia、Inspector |
| Phase 10 の次 | **SaveFile シーム**（OpenFile と同型の薄い拡張） |

---

## Batch 0 — タスク文書（ブランチ: `phase10/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase10.md` を追加
- [x] `project.md` フェーズ表・決定ログへ Phase 10 を追記
- [x] `AGENTS.md` の Phase 参照を更新
- [x] `task_phase9.md` / `graft-core.md` の次フェーズメモを更新

**完了条件:** Batch 分割と契約が文書化されている。  
**次:** Batch 1（ラッパ + Arm wire）へ。

---

## Batch 1 — ラッパ + Arm wire（ブランチ: `phase10/batch-1-openfile-seam`）

- [ ] Instrumentation: Graft OpenFile ラッパ（アーム時はスタブ応答、未アームは実ダイアログ）
- [ ] Protocol: `armOpenFile`（path）/ `armOpenFileCancel`
- [ ] Arm は一回限り（消費後クリア／上書き）
- [ ] ホスト側ユニット検証（アーム有無）

**完了条件:** エージェント側で Arm → ラッパ応答が動く。  
**次:** Batch 2（Core + Sample）へ。

---

## Batch 2 — Core + Sample（ブランチ: `phase10/batch-2-openfile-core-sample`）

- [ ] Core: `ArmOpenFileAsync(path)` / `ArmOpenFileCancelAsync()`
- [ ] Sample: ラッパ経由の OpenFile ボタン + 選択結果を StatusText 等へ反映
- [ ] 開封は `InvokeOpeningWindowAsync` を使う Sample / テスト手順

**完了条件:** Sample から Arm → 開封 → 結果反映ができる。  
**次:** Batch 3（E2E）へ。

---

## Batch 3 — Fluent E2E（ブランチ: `phase10/batch-3-openfile-e2e`）

- [ ] E2E: Arm(path) → InvokeOpeningWindow → Expect（選択パス）
- [ ] E2E: ArmCancel → InvokeOpeningWindow → Expect（キャンセル側）
- [ ] `graft-core.md` に OpenFile シームの短いメモ

**完了条件:** OK / Cancel の Fluent E2E が緑。  
**次:** Batch 4（Scenario / MCP + docs）へ。

---

## Batch 4 — Scenario / MCP + docs（ブランチ: `phase10/batch-4-docs`）

- [ ] scenario schema: `armOpenFile` / `armOpenFileCancel`
- [ ] ScenarioJson / Runner / MCP 原子ツール
- [ ] Scenario E2E（薄い追従）
- [ ] `project.md` / 本ファイル完了チェック。次フェーズメモ: **SaveFile シーム**

**完了条件:** 公開経路と文書が揃っている。  
**次:** Phase 10 完了チェック → SaveFile シームへ。

---

## Phase 10 完了チェック

- [ ] Graft OpenFile ラッパがある（未アームは実ダイアログ）
- [ ] `ArmOpenFile` / `ArmOpenFileCancel`（一回限り）がある
- [ ] Sample E2E（OK + Cancel、`InvokeOpeningWindow`）が緑
- [ ] Scenario / MCP から Arm が呼べる（薄い追従）
- [ ] 実 OS 操作 / 素 OpenFileDialog フック / Save・Folder・MessageBox / 複数選択 / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- 実コモンダイアログ HWND は触らない（in-process シームが正本）
- 素の `OpenFileDialog.ShowDialog` 直呼びは Graft 非対応（ラッパへ寄せる）
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** SaveFile シーム → … → Avalonia → Inspector（最後寄り）
