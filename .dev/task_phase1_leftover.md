# Phase 1 余り — toggle / キー / SendInput

受け入れ条件（要約）: Phase 1 必須線（`invoke` / `setValue`）の上に残っていた **SendInput フォールバック**、**`toggle`**、**キー入力（`sendKeys`）** を載せ、Core Fluent から使えるようにする。末尾で Scenario / MCP に薄い追従を入れる。  
含めない: Avalonia、自己修復（Phase 4）、`typeHuman` / chord DSL（Ctrl+A 等）、`scroll` / `select` / `expand`。  
参照: [project.md](./project.md) Q40–Q41 / Q51–Q52 / Q56。前フェーズ: [task_m1.md](./task_m1.md)。利用メモ: [graft-core.md](./graft-core.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。

---

## Batch 0 — タスク文書（ブランチ: `phase1/batch-0-task-doc`）

- [x] 本ファイル `.dev/task_phase1_leftover.md` を追加

**完了条件:** Batch 分割と完了条件が文書化されている。  
**次:** Batch 1（SendInput 基盤）へ。

---

## Batch 1 — SendInput 基盤（ブランチ: `phase1/batch-1-sendinput`）

- [x] `Graft.Instrumentation` に Win32 `SendInput` ラッパ（マウスクリック / Unicode キー）
- [x] ウィンドウ前面化・フォーカス補助
- [x] 論理座標 → スクリーン座標（WPF `PointToScreen` / DPI）
- [x] テスト: `InputInjectorTests`（空文字 no-op 等）

**完了条件:** エージェント側からクリック / タイプを注入できる基盤がある。  
**次:** Batch 2（invoke / setValue フォールバック）へ。

---

## Batch 2 — invoke / setValue SendInput フォールバック（ブランチ: `phase1/batch-2-fallback`）

- [x] `WpfElementInvoker`: Peer / ButtonBase 失敗後に SendInput クリック
- [x] `WpfElementValueSetter`: ネイティブ / ValuePattern 失敗後にクリア + SendInput タイプ
- [x] Sample にマウス専用コントロール（`SampleMouseTarget`）でフォールバック経路を緑
- [x] 既存 Button / TextBox テストが回帰しない

**完了条件:** パターン非対応要素でも invoke / setValue が SendInput 経由で通る。  
**次:** Batch 3（`toggle`）へ。

---

## Batch 3 — `toggle`（ブランチ: `phase1/batch-3-toggle`）

- [x] wire method `toggle`（params: `automationId`、任意 `runtimeId`）— 状態フリップ
- [x] `IElementToggler` + WPF（`IToggleProvider` / CheckBox）→ だめなら SendInput
- [x] Sample: `SampleCheckBox`
- [x] Core: `ToggleAsync` + `FailureSteps.Toggle`
- [x] テスト: STA + wire + Sample 1 経路

**完了条件:** Core Fluent で CheckBox をトグルできる。  
**次:** Batch 4（`sendKeys`）へ。

---

## Batch 4 — `sendKeys`（ブランチ: `phase1/batch-4-sendkeys`）

- [x] wire method `sendKeys`（params: `automationId`, `text`）— リテラル文字列のみ
- [x] フォーカス後 Unicode SendInput（Batch 1 基盤）
- [x] Core: `SendKeysAsync`
- [x] テスト: SampleTextBox への入力 1 経路

**完了条件:** Core Fluent で `sendKeys` によりテキスト入力できる。  
**次:** Batch 5（Scenario / MCP 追従）へ。

---

## Batch 5 — Scenario + MCP 追従（ブランチ: `phase1/batch-5-scenario-mcp`）

- [x] Scenario schema / parser / runner に `toggle` / `sendKeys`
- [x] MCP: `graft_toggle` / `graft_send_keys`
- [x] docs: `graft-core.md`、本ファイルの完了チェック

**完了条件:** Scenario / MCP から同操作が呼べる。  
**次:** Phase 1 余り完了チェックへ。

---

## Phase 1 余り 完了チェック

- [x] invoke / setValue がパターン失敗時に SendInput へ落ちる
- [x] CheckBox を `toggle` で切り替えられる（Core Fluent）
- [x] `sendKeys` でテキストを打てる（Core Fluent）
- [x] Scenario / MCP から同操作が呼べる
- [x] Avalonia / 自己修復 / 高度キー DSL は **含めない**

---

## 進め方メモ

- 論理操作順は **ネイティブ → Peer/Provider → SendInput**（project.md Q40）
- クリック点: Peer `GetClickablePoint` → bounds 中心（Q52）
- `setValue` フォールバック: クリア + SendInput（Q51）
- MCP / Scenario は Core の薄いラッパー。操作ロジックを複製しない
