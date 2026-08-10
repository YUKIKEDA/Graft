# Phase 31 — SendInput 並列対策（X04）

受け入れ条件（要約）: アセンブリ横断で SampleWpfApp + SendInput が同時に走っても安定する。  
ID: `X04`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: Avalonia、Inspector、GitHub Actions workflow、`ConnectAsync` への mutex、オプトアウト API。  
参照: [project.md](./project.md) Q137〜。前フェーズ: [task_phase29.md](./task_phase29.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 正本 | プロセス横断 named mutex。`-m:1` は補助 |
| 範囲 | `LaunchAsync` 取得 → `GraftSession.Dispose` 解放 |
| 名前 | `Local\Graft.UiSession` |
| 待ち | キュー専用上限（既定 15 分、Launch timeout より長い方）。失敗は `action.timeout`。Connect/Handshake は従来どおり Launch timeout |
| 所有 | Mutex は専用スレッドで保持（async Dispose から安全に Release） |
| Abandoned | 所有権引き継ぎで続行 |
| ConnectAsync | mutex なし |
| Collection | アセンブリ内直列は残す |
| CI workflow | 含めない |
| 受け入れ | `dotnet test Graft.slnx`（`-m:1` なし）緑 + mutex 単体 |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `task_phase29.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — 実装 + 検証

- [x] `UiSessionLock` + Launch/Dispose 統合
- [x] mutex 単体テスト
- [x] `dotnet test Graft.slnx`（並列）緑
- [x] 完了チェック

---

## Phase 31 完了チェック

- [x] セッション寿命で `Local\Graft.UiSession` を保持する
- [x] mutex 待ちタイムアウトが `action.timeout` になる
- [x] `dotnet test Graft.slnx`（`-m:1` なし）が緑
- [x] Avalonia / CI YAML / Connect mutex は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次フェーズ:** Avalonia（Must 全緑後）または任意項目
