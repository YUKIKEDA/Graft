# Phase 35 — 要素クリップ Screenshot（P02）

受け入れ条件（要約）: 要素の PNG クリップを第一級化する。窓内は既存窓キャプチャの bounds 交差クリップ。開いた Popup 配下は Popup ルートを RTB してクリップ。開いた ToolTip はツリー子ノードとして撮れる。開いている ToolTip / Popup はホストと画面座標で合成する。  
ID: `P02`（[competitive-gap.md](./competitive-gap.md)）。  
含めない: 画像 expect/diff（P03）、JPEG、デスクトップ全体、画面 BitBlt、自動 `scrollIntoView`、invoke 等への `runtimeId` 展開、Scenario/MCP の controlType セレクタ、閉じた ToolTip のツリー掲載、Avalonia、Inspector。  
参照: [project.md](./project.md) Q143〜。前フェーズ: [task_phase34.md](./task_phase34.md)。利用メモ: [graft-core.md](./graft-core.md)。

実装 PR はフェーズ完了時に 1 本（分割しない）。

---

## 合意済み契約（grill）

| 項目            | 決定                                                                                                                                                                                    |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 優先            | P02 を **Must** に昇格                                                                                                                                                                  |
| ゲート          | Must 全緑まで **Avalonia 再禁止**。本フェーズの次が Avalonia                                                                                                                            |
| Fluent          | `GetBy…().ScreenshotAsync()` → 既存 `Screenshot`。`session.ScreenshotAsync()` は窓全体＋開いている ToolTip/Popup/ContextMenu を合成                                                                 |
| 待ち            | 出現待ちのみ（`WaitFor`）。`enabled` は要求しない。自動 scroll なし                                                                                                                     |
| 窓内            | ターゲット窓の `RenderTargetBitmap` を要素 bounds で交差クリップ                                                                                                                        |
| 空交差          | `element.notActionable`（完全に窓外 / Collapsed / サイズ 0）                                                                                                                            |
| 窓外            | 開いた Popup ルートを RTB → その座標系でクリップ（BitBlt なし）                                                                                                                         |
| Popup 範囲      | 解決できた要素の Visual が開いた Popup 配下ならすべて（C05 Popup / ContextMenu / Menu サブ / Combo ドロップダウン）                                                                     |
| ToolTip         | 開時のみオーナーの **子ノード**（`ControlType = ToolTip`）。`ExpectToolTipAsync` と `toolTip` 文字列は残す。要素 SS は撮った要素＋**子孫の開時 overlay** を合成。窓 SS も開時 overlay を合成 |
| wire            | 既存 `screenshot` に任意 `automationId` / `runtimeId`。省略時は窓全体。protocol major は上げない                                                                                        |
| Scenario        | 既存 `screenshot` に任意 `automationId`。`path` 必須のまま                                                                                                                              |
| MCP             | 既存 `graft_screenshot` に任意 `automationId`。path 省略時 temp のまま                                                                                                                  |
| 空 AutomationId | Fluent のみ（`GetByControlType` 等 → `runtimeId`）。Scenario/MCP には controlType を足さない                                                                                            |
| E2E             | Fluent: `SampleButton` / Popup 子 / Hover → TipHost・TipSection・ToolTip ノード・窓 SS。Scenario: `SampleButton` + path。空交差は Instrumentation |
| 次              | Avalonia                                                                                                                                                                                |

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_phase35.md` を追加
- [x] `project.md` / `AGENTS.md` / `task_phase34.md` / `graft-core.md` / `competitive-gap.md` 更新

---

## Batch 1 — wire + キャプチャ + ツリー

- [x] `screenshot` params: 任意 `automationId` / `runtimeId`
- [x] 窓内: 同一 UI スレッドで窓 RTB → bounds 交差クリップ（Content 原点と Window RTB の座標差をエージェント側で吸収）
- [x] 窓外: 開いた Popup ルート RTB → クリップ。対象外 Visual は `element.notActionable`
- [x] 開時 ToolTip をオーナーの子ノードとして合流（閉時は載せない）
- [x] 空交差 / 非キャプチャ Visual → `element.notActionable`
- [x] Instrumentation テスト（空交差を含む）

---

## Batch 2 — Fluent / Scenario / MCP + E2E

- [x] `ElementQuery.ScreenshotAsync`（出現待ち → screenshot）
- [x] Scenario / MCP 任意 `automationId`
- [x] Sample Fluent 3 本 + Scenario SampleButton
- [x] 完了チェック。次: Avalonia

---

## Phase 35 完了チェック

- [x] `GetBy…().ScreenshotAsync()` で要素 PNG（meta+bytes）が取れる
- [x] 窓内クリップが窓 SS より小さい（SampleButton）
- [x] 開いた Popup 上の要素が撮れる
- [x] 開いた ToolTip がツリーに現れ、ホストと合成した PNG が撮れる
- [x] 空交差が `element.notActionable`
- [x] Scenario / MCP が任意 `automationId` で追従する
- [x] Sample E2E が緑
- [x] P03 / BitBlt / Avalonia は **含めない**。窓 SS は開時 overlay を合成する

---

## 進め方メモ

- 設計矛盾時は本ファイル / `project.md` 優先
- **次:** Avalonia（P02 完了後。Inspector は任意）
