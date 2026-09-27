# WPF UI Gallery — Graft E2E（最深部）

受け入れ条件（要約）: gitignore の `tests/wpfui`（lepoco/wpfui）内 Gallery にローカル Instrumentation を載せ、`tests/WpfUi.Gallery.Graft.Tests` で **全 UI を Graft 公開 API の最深部まで** E2E する。全テスト **Timeline Always** ＋主要操作の窓/要素 Screenshot。製品非目標は Skip＋理由で追跡。  
参照: [graft-core.md](./graft-core.md)、[competitive-gap.md](./competitive-gap.md)、Sample 正本 `SampleTodoApp.Tests` / 機能マトリクス `SampleWpfApp.Tests`。  
含めない: 第三者 exe ブラックボックス、実 OS コモンダイアログ UIA、WebView/Monaco **内部**の完全自動操作、画像 diff、Avalonia、ホスト `ci.yml` での本スイート実行。

---

## 合意済み契約（grill）

| 項目                | 決定                                                                                                                         |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| 対象アプリ          | **Wpf.Ui.Gallery**（`tests/wpfui`）                                                                                          |
| Instrumentation     | **ローカルパッチ**（gitignore のまま・Upstream に載せない）                                                                  |
| 参照注入            | `tests/wpfui` 直下 **Directory.Build.props** で `Graft.props` / ProjectReference / `Graft.targets` を条件付き注入            |
| 起動フック          | Sample 同一: `#if GRAFT_TEST` → `WpfGraft.Use()` / `Agent.Start()`、終了時 `Stop()`                                          |
| 深さ                | **最深部（C）** — SampleWpfApp 級の操作・Expect・Arm*・MessageBox・マウス/キー等                                             |
| セレクタ            | **ハイブリッド** — FlaUI 既存 AutomationId 優先。不足は `{Page}_{Role}`。表示専用は Name / ControlType / 相対                |
| 非目標・未実装      | **Skip + 理由**（competitive-gap / 本ファイル）。無理埋めしない                                                              |
| テスト配置          | **`tests/WpfUi.Gallery.Graft.Tests`**（Graft リポジトリで追跡）                                                              |
| プロセス            | **カテゴリ単位**で共有セッション。TitleBar Close 等は破壊的専用コレクション                                                  |
| 証跡                | **全テスト Timeline Always** ＋主要操作で窓/要素 SS。Dispose 後に成果物 Assert                                               |
| 追加 Window         | 開く・フォーカス・TitleBar・閉じる・SS まで最深。**Monaco 内部は Skip**                                                      |
| Launch              | `Application.LaunchAsync(Gallery csproj)`、`Configuration=GraftTest`、Timeline `%TEMP%\graft-wpfui-gallery-timeline\{leaf}\` |
| Harmony             | Gallery は net10 → **Lib.Harmony 2.4.2+**（2.3.x は CoreCLR 10 で `PlatformNotSupportedException`）                          |
| ソリューション / CI | **`Graft.slnx` に載せる**。ホスト `ci.yml` からは除外。self-hosted **`ui.yml` のみ**（wpfui があるランナー）                 |
| ビルド              | **Debug + GraftTest**（Layout / TreeList は `#if DEBUG`）                                                                    |
| シナリオ SoT        | **本ファイル**（下記一覧）                                                                                                   |
| 実装順              | 基盤 → Shell → ナビ順カテゴリ（FlaUI 対照は Shell / Dialogs に内包）                                                         |

---

## シナリオ一覧（SoT）

### 共通（全テスト）

- Launch → Handshake → Timeline Always 開始
- 操作前後で窓 SS（必要なら要素クリップ SS）
- Dispose 後に Timeline 成果物（`index.html` 等）存在を Assert
- 破壊的（Close）は専用コレクション

### Shell / Session

- 起動・接続・メイン窓タイトル（`WPF UI Gallery`）
- TitleBar: Minimize / Restore / Maximize（Close は専用）
- ナビ展開・カテゴリ親クリック
- AutoSuggest（`NavigationAutoSuggestBox`）で Settings / 任意ページへ到達
- Footer Settings 到達
- テーマ切替（Settings）→ 状態 Expect + SS
- トレイメニューがあれば Invokable まで（無ければ Skip 理由）

### Home / ハブ

- Dashboard 到達・代表操作（あれば）
- All samples 到達・一覧から葉ページへ
- 各カテゴリ親ハブページ到達

### Design guidance

- Typography: 表示・スクロール・代表テキスト Expect + SS
- Icons: 検索/選択（UI にあれば）・ホバー + SS
- Colors: 表示・スクロール + SS

### Basic Input

各ページで Invoke / Toggle / Select / SetValue / キーボード / Focus / Expect* / SS:

- Anchor / Button / DropDownButton（メニュー深さ） / HyperlinkButton
- ToggleButton / ToggleSwitch / CheckBox（三態があれば）
- ComboBox（単一・キー選択） / RadioButton
- RatingControl / ThumbRate / SplitButton（主＋ドロップ）
- Slider（値セット・Expect）

### Collections

- DataGrid: スクロール・行選択・SelectMany・列キー・セル R/W（可能な範囲）・ExpectSelected
- ListBox / ListView: 単一・複数選択・キー選択
- TreeView: 展開/折りたたみ・選択・状態 Expect
- TreeList（Debug）: 同上相当

### Date & time

- Calendar / CalendarDatePicker / DatePicker / TimePicker: 開く・値変更・Expect・SS

### Dialogs & flyouts

- Snackbar: 表示トリガー・消失待ち/Expect
- ContentDialog: Show → 入力 → 結果ボタン → 閉じたことの Expect（FlaUI IntegrationTests 対照含む）
- Flyout: 開閉・外側クリック相当
- MessageBox: 標準 / WPF UI カスタム → Graft MessageBox 経路でボタン結果まで

### Layout（Debug）

- Expander 開閉
- CardControl / CardAction: Invoke・状態

### Media

- Image / Canvas: 表示 bounds・クリック/ホイール（可なら）+ SS
- WebView / WebBrowser: ホスト到達・外枠操作・SS まで。**内部 DOM は Skip（理由付き）**

### Navigation

- BreadcrumbBar: 項目選択・ナビ結果
- NavigationView（ページ内サンプル）: 項目切替
- Menu: 任意深さ `SelectMenuAsync`
- Multilevel navigation: 複数段遷移 + SamplePage 到達
- TabControl: タブ切替・Expect
- TabView: ナビ未掲載なら AllControls 経由。不可なら Skip 明示

### Status & info

- InfoBadge / InfoBar（閉じる等）
- ProgressBar / ProgressRing: 値・可視 Expect
- ToolTip: Hover → Tip 出現待ち + SS

### Text

- AutoSuggestBox: 入力・候補選択
- NumberBox: SetValue / スピン相当
- PasswordBox: Set のみ（Get に載せない）
- RichTextBox: 平文 Set/Expect（書式は製品境界）
- TextBox: SetValue・Ctrl+A・Delete・空 Expect・Focus
- Label / TextBlock: Name/可視 Expect

### System

- Clipboard: Gallery UI 上のコピー/取得操作（**V06 クリップボード貼付 API は Skip**）
- FilePicker: `ArmOpenFile` / Cancel / `ArmSaveFile` / Cancel（**実 OS ダイアログ UIA は Skip**）

### Windows

- Sandbox / Editor / Monaco: 開く・フォーカス切替・TitleBar・閉じる・SS/Timeline
- Monaco **内部編集は Skip**

### 横断ストーリー（カテゴリ横断・別プロセス可）

- 起動 → AutoSuggest で複数ページ巡回 → Settings テーマ → ContentDialog → FilePicker Arm* → Window 開閉 → Timeline 成果物検証

### 明示 Skip（テストで理由付き・ギャップ追跡）

- WebView / WebBrowser 内部、Monaco 内部
- 実 OS コモンダイアログ UIA、デスクトップ全体 SS、動画/trace、ファジーセレクタ、画像 diff
- V06 クリップボード貼付 API、トースト専用 API、Win キー 等（製品非目標/任意）

---

## Batch 0 — タスク文書

- [x] 本ファイル `.dev/task_wpfui-gallery-e2e.md` を追加
- [x] `AGENTS.md` / `project.md` に一行リンク

---

## Batch 1 — Gallery ローカルパッチ + Launch ハーネス

- [x] `tests/wpfui/Directory.Build.props`（Graft props / ProjectReference / targets）
- [x] Gallery `App` に `GRAFT_TEST` フック（Sample 同一）
- [x] シェル／操作対象へハイブリッド AutomationId（FlaUI 既存優先）
- [x] `tests/WpfUi.Gallery.Graft.Tests` 雛形 + Gallery ロケータ + カテゴリ Collection
- [x] Timeline Always Launch（`GraftTest`）+ 成果物 Assert ヘルパ
- [x] `Graft.slnx` 登録（ホスト CI 除外は workflow 側で維持）
- [x] Lib.Harmony **2.4.2**（Gallery net10）
- [x] Shell smoke E2E 緑（Launch + Settings）
- [x] `ui.yml`: net10 SDK + wpfui 無し時は Gallery テスト除外

---

## Batch 2 — Shell

- [x] 起動 / タイトル / ナビ / AutoSuggest / Settings / テーマ
- [x] TitleBar（Close は破壊的専用）
- [x] FlaUI 対照: Navigation / TitleBar / Window 相当（ShellE2E + smoke）

---

## Batch 3〜 — カテゴリ（ナビ順）

- [x] Design guidance
- [x] Basic Input
- [x] Collections
- [x] Date & time
- [x] Dialogs & flyouts（ContentDialog FlaUI 対照含む）
- [x] Layout（Debug）
- [x] Media（内部 Skip 明示）
- [x] Navigation
- [x] Status & info
- [x] Text
- [x] System（Arm*）
- [x] Windows（外側最深・Monaco 内部 Skip）
- [x] 横断ストーリー 1 本

---

## 完了チェック

- [x] Gallery（Debug + GraftTest）が Launch できる
- [x] シナリオ SoT の各項目が Fact/Theory または明示 Skip になっている
- [x] 全実行テストが Timeline 成果物を残し、Assert している
- [x] ホスト `ci.yml` が本スイートで赤くならない
- [ ] self-hosted `ui.yml` で回せる（ランナーに `tests/wpfui` がある前提）

---

## 進め方メモ

- 設計矛盾時は本ファイル優先。Graft 製品境界は [competitive-gap.md](./competitive-gap.md)
- Gallery パッチは **コミットしない**（`tests/wpfui/` gitignore）
- テストコード・本タスク文書は Graft リポジトリで追跡
- 正本実行: `dotnet test tests/WpfUi.Gallery.Graft.Tests -m:1`（または `Graft.slnx -m:1` の UI 順）
- ローカル検証 (2026-09-05): **合格 48 / スキップ 17 / 失敗 0**（約 5 分）
- **ピットフォール:** IE `WebBrowser` が `https://wpfui.lepo.co` を開くと「スクリプト エラー」モーダルでセッションが止まる → ローカル Gallery は **常に** `about:blank`（`#if GRAFT_TEST` は WPF 一時コンパイルで落ちることがあるので使わない）
- **ナビ:** parent は `ExpandAsync`（tree の `expanded` は n/a でも可）、leaf は `DoubleClickAsync`
- **次:** self-hosted `ui.yml` でフル E2E 実行（ランナーに `tests/wpfui`）
