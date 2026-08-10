# Phase 15 — 公開 Screenshot（Session / Scenario / MCP）

受け入れ条件（要約）: 既存 wire `screenshot` を **第一級 API** として Session / Scenario / MCP に公開する。  
含めない: 要素クリップ、デスクトップ全体、画像 expect/diff、wire 変更、右クリック／Menu、Avalonia、Inspector。  
参照: [project.md](./project.md) Q107〜。前フェーズ: [task_phase14.md](./task_phase14.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| Fluent | `session.ScreenshotAsync()` → `Screenshot`（Format / Width / Height / PngBytes）+ `SaveAsync(path)` |
| 対象 | **現在ターゲット窓のみ**（既存 wire と同じ） |
| Scenario | `action: screenshot`、**`path` 必須**（画像 expect/diff なし） |
| MCP | `graft_screenshot` — path 任意、省略時 temp 書き → meta + path |
| E2E | Fluent: PNG シグネチャ + size>0 / Scenario: path に書いて存在確認 |
| wire | 変更なし（既存 `screenshot` RPC） |
| Phase 15 の次 | 右クリック + ContextMenu / MenuItem |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase15.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase14.md` / `graft-core.md` 更新

---

## Batch 1 — Core 公開 API

- [x] `Screenshot` 型 + `SaveAsync`
- [x] `GraftSession.ScreenshotAsync`

---

## Batch 2 — Scenario / MCP + E2E

- [x] schema / Runner / MCP
- [x] Sample Fluent + Scenario E2E
- [x] 完了チェック。次: **右クリック + ContextMenu / MenuItem**

---

## Phase 15 完了チェック

- [x] Session から `ScreenshotAsync` で meta+bytes が取れる
- [x] Scenario が path に PNG を書く
- [x] MCP が path（省略時 temp）+ meta を返す
- [x] Sample E2E（Fluent + Scenario）が緑
- [x] 要素クリップ / 画像 diff / wire 変更は **含めない**

---

## 進め方メモ

- 設計矛盾時は `project.md` 優先
- **次フェーズ:** 右クリック + ContextMenu / MenuItem → Tab/Slider/複数選択 → … → Avalonia → Inspector
