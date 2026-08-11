# Phase 33 — 操作タイムライン（D06）

受け入れ条件（要約）: セッションオプションで操作完了後フレームを集め、出力先に PNG 連番 + HTML ビューア（速度・操作名字幕）を確定できる。  
ID: `D06`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: GIF / FFmpeg / ImageSharp、Scenario/MCP 専用面、真の FPS 録画、Playwright video 互換、Avalonia。  
参照: [project.md](./project.md) Q139〜。前フェーズ: [task_phase32.md](./task_phase32.md)。

---

## 合意済み契約（grill）

| 項目 | 決定 |
| ---- | ---- |
| 製品形 | 常時オプトイン（起動オプション） |
| 保持 | `Always` / `OnFailure`（既定 `Always`） |
| 確定 | Dispose 自動 + 明示 Save。**出力先ディレクトリはオプション必須** |
| フレーム | **操作フック・完了後 1 枚**（壁時計 FPS なし）。エンコード相当の「見せ方」は HTML 側 |
| 成果物 | PNG 連番 + 同梱 HTML（自動再生・前/次・速度・フレーム番号・**操作名字幕**） |
| 依存 | 第三者画像ライブラリなし（既存 PNG スクショ延長） |
| 配置 | 全部 `Graft.Core` |
| API 面 | Core セッションオプションのみ（Scenario/MCP は後追い可） |
| ゲート | **Must**。Avalonia は D06 完了後 |
| Phase 33 の次 | Avalonia（残 Must なし前提） |

---

## Batch 0 — タスク文書

- [x] 本ファイル追加
- [x] `project.md` / `AGENTS.md` / `competitive-gap.md` / `graft-core.md` 更新

---

## Batch 1 — Core

- [ ] セッションオプション（有効化・保持モード・出力先・フレーム表示既定など）
- [ ] 公開操作完了後にスクショ＋ラベルを溜める
- [ ] Save / Dispose で PNG + `index.html`（＋必要なら manifest）確定
- [ ] OnFailure 時は成功パスで成果物破棄

---

## Batch 2 — Sample / 契約テスト

- [ ] オプトイン E2E（短い操作列 → HTML/PNG 存在）
- [ ] OnFailure 振る舞いの単体または契約テスト
- [ ] 完了チェック

---

## Phase 33 完了チェック

- [ ] D06 が Sample / 契約で緑
- [ ] `competitive-gap.md` の D06 を OK / Done に更新
- [ ] GIF / FFmpeg / ImageSharp を入れてない
- [ ] Avalonia は含めない
- [ ] ゲート上の残 Must が空であることを文書化

---

## 進め方メモ

- 設計矛盾時は `project.md` / `competitive-gap.md` 優先
- **次:** Avalonia（Must 全 Done 後）
