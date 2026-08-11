# Phase 34 — SampleTodoApp（利用ガイド正本）

受け入れ条件（要約）: 実ユースケース寄りの Todo サンプル（MVVM/DI/テーマ/実 JSON/詳細窓/Import・Export）とストーリー E2E 3 本を追加し、`graft-core.md` の正本例を差し替える。  
参照: [project.md](./project.md) Q141〜。前フェーズ: [task_phase33.md](./task_phase33.md)。  
含めない: トレイ、UiBench/CpuBench、SelfContained RID、WinForms 混在、CommunityToolkit.Mvvm、Avalonia、Scenario JSON 同梱（後追い可）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 関係 | 新規アプリ（`ToDoApp.Wpf` は着想のみ） |
| 構成 | MVVM + DI + テーマ。R3 + ObservableCollections + Microsoft.Extensions.DependencyInjection |
| 目的 | **利用ガイド正本**。`SampleWpfApp` は機能マトリクスのまま |
| スコープ | CRUD + フィルタ + テーマ + 詳細 Window + Export/Import（実ファイル + Graft シーム） |
| 配置 | `tests/sample-apps/SampleTodoApp` + `SampleTodoApp.Tests` |
| データ | 実 JSON。保存先は UI（OpenFolder）で設定。既定は LocalAppData/`GraftSampleTodo/Data`。選択は `settings.json` に保持 |
| Launch | `LaunchOptions.Environment` は Core 汎用として追加（Todo のデータ dir には使わない） |
| UI | DataGrid 一覧 + 詳細 Window + 設定オーバーレイ（UserControl: 保存先 / テーマ） |
| 初期データ | アプリ側シードなし（空一覧）。E2E が必要な状態は fixture を data dir に配置し `ArmOpenFolder` で切替 |
| E2E | ストーリー 3 本（追加→詳細編集／フィルタ・テーマ／Export→Import） |
| 順序 | Phase 34 完了後に Avalonia |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `graft-core.md` 更新

---

## Batch 1 — Core

- [x] `LaunchOptions.Environment` → `AppProcessLauncher` へ伝播
- [x] 契約テスト（Todo E2E で実証）

---

## Batch 2 — SampleTodoApp + Tests

- [x] アプリ（GraftTest 組み込み、AutomationId、JSON、テーマ、詳細窓、Import/Export）
- [x] Tests ストーリー 3 本 + fixture
- [x] `Graft.slnx` 登録
- [x] `graft-core.md` / `AGENTS.md` 正本差し替え
- [x] 完了チェック

---

## Phase 34 完了チェック

- [x] UI + `ArmOpenFolder` でデータ dir を切り替えられる
- [x] ストーリー E2E 3 本が緑
- [x] 利用ガイド正本が SampleTodoApp.Tests を指す
- [x] Avalonia は含めない

---

## 進め方メモ

- 設計矛盾時は本ファイル / `project.md` 優先
- **次:** Avalonia
