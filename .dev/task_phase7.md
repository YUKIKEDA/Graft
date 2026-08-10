# Phase 7 — ウィンドウ／モーダル

受け入れ条件（要約）: WPF の **マルチウィンドウ + モーダル（`ShowDialog`）** を操作できるようにし、Core Fluent →（薄い）Scenario / MCP まで揃える。競合（FlaUI / WinAppDriver 等）とのギャップのうち、**アプリ内 Window 面**を先に潰す。  
含めない: OS 共通ダイアログ実装、DataGrid 等の複雑 UI 拡充、Avalonia、Inspector、全窓マージツリー、owner リンク、素の `Invoke` での ShowDialog 開封。  
参照: [project.md](./project.md) Q13 / Q72〜。前フェーズ: [task_phase6.md](./task_phase6.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## 競合ギャップ（短い表・Batch 0）

| 領域 | Graft 現状 | 競合が強い理由 | 本ロードマップ |
| ---- | ---------- | -------------- | -------------- |
| マルチウィンドウ / WPF モーダル | MainWindow 固定 | 実アプリの基本 | **Phase 7** |
| OS 共通ダイアログ（OpenFile 等） | 未対応 | プロセス外 HWND / UIA | Phase 7 後の候補（方針別） |
| 複雑ホスト UI（DataGrid 等） | 部分的 | コントロール固有 | 窓面の後 |
| Avalonia | 未着手 | 第2 FW | **WPF カバレッジ後・最後寄り** |
| Inspector | 未着手 | 探索 UX | Avalonia の後 |

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| テーマ | WPF カバレッジ優先。Avalonia / Inspector は後ろへ |
| 最初の塊 | ウィンドウ面（マルチ + WPF モーダル）。OS ダイアログは後続 |
| 識別 | セッション内 `windowId` + メタ（title / automationId / isModal / isActive） |
| Core | `ListWindowsAsync` / `SwitchToWindowAsync(windowId)`。以降 GetBy/Expect は既定ターゲット窓 |
| 待ち | `WaitForWindowAsync`（title および／または automationId） |
| モーダル開封 | `InvokeOpeningWindowAsync`（BeginInvoke + 出現待ち、**既定で自動 Switch**）。素の Invoke で ShowDialog は非対応と明記 |
| ツリー | 既定ターゲット窓のみ。全窓マージしない |
| 公開経路 | Scenario/MCP 薄い追従（list/switch/wait/invokeOpeningWindow） |
| Phase 7 の次 | Phase 8 DataGrid 行 MVP + `checked`（[task_phase8.md](./task_phase8.md)） |

---

## Batch 0 — タスク文書（ブランチ: `phase7/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase7.md` を追加（短い競合ギャップ表含む）
- [x] `project.md` フェーズ表・決定ログへ Phase 7 を追記
- [x] `AGENTS.md` の Phase 参照を更新

**完了条件:** Batch 分割と契約が文書化されている。  
**次:** Batch 1（wire + WPF ターゲット窓）へ。

---

## Batch 1 — wire + WPF ターゲット窓（ブランチ: `phase7/batch-1-windows-wire`）

- [x] wire: `listWindows` / `switchWindow`（`windowId`）
- [x] WPF: `Application.Current.Windows` 一覧、既定ターゲット（起動時 MainWindow）
- [x] getTree / resolve / screenshot / アクションがターゲット窓を使う

**完了条件:** ターゲット切替後に子 Window のツリーが取れる。  
**次:** Batch 2（Core + モデルレス E2E）へ。

---

## Batch 2 — Core List/Switch/Wait + モデルレス E2E（ブランチ: `phase7/batch-2-core-modeless`）

- [x] Core: `ListWindowsAsync` / `SwitchToWindowAsync` / `WaitForWindowAsync`
- [x] Sample: モデルレス子 Window（`Show`）+ E2E（開く → list/wait/switch → 子上で Expect）

**完了条件:** モデルレス子窓を切替えて操作できる。  
**次:** Batch 3（モーダル開封）へ。

---

## Batch 3 — `InvokeOpeningWindow` + モーダル E2E（ブランチ: `phase7/batch-3-modal`）

- [x] `InvokeOpeningWindowAsync`（非同期投入 + 新窓待ち + 既定自動 Switch）
- [x] Sample: `ShowDialog` モーダル + E2E（専用 API 経由で開封・操作・閉じる）
- [x] 素の Invoke で ShowDialog を開くとハングしうる旨を文書化

**完了条件:** モーダルを専用経路で操作できる。  
**次:** Batch 4（Scenario / MCP + docs）へ。

---

## Batch 4 — Scenario / MCP + docs（ブランチ: `phase7/batch-4-docs`）

- [x] scenario schema / Runner / MCP: list・switch・wait・invokeOpeningWindow
- [x] Scenario E2E（薄い追従で可）
- [x] `graft-core.md` / 本ファイル完了チェック
- [x] 次フェーズメモ: OS ダイアログ方針 or 複雑 UI → Avalonia → Inspector

**完了条件:** 公開経路と文書が揃っている。  
**次:** Phase 7 完了チェック → Phase 8（DataGrid 行 MVP + `checked`）へ。

---

## Phase 7 完了チェック

- [x] `listWindows` / `switchWindow` とセッション内 `windowId` がある
- [x] モデルレス子 Window を切替えて操作できる
- [x] `InvokeOpeningWindow` でモーダルを開け、操作できる
- [x] Sample E2E が緑（モデルレス + モーダル）
- [x] Scenario / MCP から同操作が呼べる（薄い追従で可）
- [x] OS ダイアログ実装 / 複雑 UI 拡充 / Avalonia / Inspector は **含めない**

---

## 進め方メモ

- ShowDialog 開封は同期 Dispatcher.Invoke と相性が悪い → 専用 API（Q74）
- MCP / Scenario は Core の薄いラッパー
- 設計矛盾時は `project.md` 優先
- **次フェーズ:** [task_phase8.md](./task_phase8.md)（DataGrid 行中心 MVP + `checked`）→ セル R/W → OS ダイアログ方針 → … → Avalonia → Inspector（最後寄り）
