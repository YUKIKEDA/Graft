# Graft — 競合シナリオ対照表（WPF）

Avalonia 再開前に、FlaUI / WinAppDriver / TestStack.White / Appium(Windows) がカバーする **自社デスクトップ E2E シナリオ** と Graft 現状を突き合わせる SoT。  
Playwright DX（codegen / trace / video）や TestComplete Object Spy 全体は比較軸に含めない（任意・後追い可）。  
参照: [project.md](./project.md) Q125〜、[task_phase23.md](./task_phase23.md)、利用面: [graft-core.md](./graft-core.md)。

**ゲート:** 本表の **Must** がすべて実装＋ Sample E2E 緑になるまで **Avalonia 禁止**。

---

## 凡例

| 記号       | 意味                                     |
| ---------- | ---------------------------------------- |
| Graft OK   | 代表シナリオを Sample / 契約でカバー済み |
| Graft PART | 一部のみ／ワークアラウンド頼み／穴あり   |
| Graft NO   | 未実装または意図的未対応                 |
| **Must**   | Avalonia 再開前に完了必須（**確定**）    |
| **任意**   | あると良い。ゲート外                     |
| **非目標** | 設計上やらない／別製品領域               |
| **Done**   | 既に十分（Must 完了扱い）                |

競合列は「そのクラスのツールで普通に書くシナリオか」の目安（機能名の完全一致ではない）。

---

## 確定 Must（レビュー結果）

提示リストをベースに、K05 / V06 / W12 / A08 / P02 は任意。**X04 は Must（`-m:1` 運用で Done）**。**D06（操作タイムライン）を Must に追加**（単一 FW 完成度優先。Avalonia はその後）。

`F02 F04 F05` · `M03–M08` · `K03 K04` · `V03 V05` · `T03 T04` · `L04 L05 L06` · `E04` · `H02 H03` · `U02 U03 U04` · `G06–G10` · `C01–C06` · `W06–W11` · `A04–A07` · `X04` · `D06`

Inspector（F08）は自社アプリ + `getTree` 前提では使い所が薄いため **任意**（ゲート外・ロードマップ必須ではない）。

---

## 1. セッション / 起動

| ID  | シナリオ                             | 競合 | Graft | 優先   | 仮Phase | メモ                                        |
| --- | ------------------------------------ | ---- | ----- | ------ | ------- | ------------------------------------------- |
| S01 | アプリ起動して接続し操作開始         | Yes  | OK    | Done   | —       | `Application.LaunchAsync`                   |
| S02 | 既存プロセスへ接続                   | Yes  | PART  | 任意   | —       | `ConnectAsync` はあるが第一級ドキュメント外 |
| S03 | テスト終了でプロセス終了             | Yes  | OK    | Done   | —       | セッション Dispose                          |
| S04 | セッション再利用（プロセス使い回し） | PART | PART  | 任意   | —       | オプトイン方針のみ                          |
| S05 | 第三者 exe のブラックボックス操作    | Yes  | NO    | 非目標 | —       | 事前組み込み前提                            |

---

## 2. 探索 / セレクタ

| ID  | シナリオ                                | 競合 | Graft | 優先   | 仮Phase | メモ                         |
| --- | --------------------------------------- | ---- | ----- | ------ | ------- | ---------------------------- |
| F01 | AutomationId で一意特定                 | Yes  | OK    | Done   | —       | ハード一致                   |
| F02 | Name / ControlType で特定               | Yes  | OK    | Done   | 27      | `GetByName` / `GetByControlType` |
| F03 | 祖先近傍（near）で曖昧さ低減            | PART | PART  | 任意   | —       | `NearAutomationId`               |
| F04 | 表示テキスト / 項目キーでリスト項目特定 | Yes  | OK    | Done   | 27      | `SelectAsync(key)`               |
| F05 | 相対セレクタ（子・兄弟・nth）           | Yes  | OK    | Done   | 27      | `Child` / `Sibling` / `Nth`      |
| F06 | 失敗時の自己修復（代替セレクタ）        | PART | PART  | 任意   | —       | Phase 4。ファジーは非目標    |
| F07 | ファジー／編集距離マッチ                | PART | NO    | 非目標 | —       |                              |
| F08 | Inspector / Spy で Id 採取              | Yes  | NO    | 任意   | —       | 探索補助。getTree で代替可   |

---

## 3. マウス / ポインタ

| ID  | シナリオ                           | 競合 | Graft | 優先 | 仮Phase | メモ                      |
| --- | ---------------------------------- | ---- | ----- | ---- | ------- | ------------------------- |
| M01 | クリック（ボタン Invoke）          | Yes  | OK    | Done | —       | native → Peer → SendInput |
| M02 | 右クリック → ContextMenu（1 段）   | Yes  | OK    | Done | —       | Phase 16                  |
| M03 | ContextMenu サブメニュー           | Yes  | OK    | Done | 26      | `SelectMenuAsync`         |
| M04 | ダブルクリック                     | Yes  | OK    | Done | 25      | `DoubleClickAsync`        |
| M05 | Hover / MouseEnter 副作用          | Yes  | OK    | Done | 25      | ToolTip 待ちは C03        |
| M06 | Drag and Drop                      | Yes  | OK    | Done | 25      | 要素→要素のみ             |
| M07 | 座標クリック（要素外・オフセット） | Yes  | OK    | Done | 25      | クリック点相対 DIP        |
| M08 | マウスホイール                     | Yes  | OK    | Done | 25      | `WheelAsync`              |

---

## 4. キーボード

| ID  | シナリオ                            | 競合 | Graft | 優先 | 仮Phase | メモ                       |
| --- | ----------------------------------- | ---- | ----- | ---- | ------- | -------------------------- |
| K01 | リテラル入力（SendKeys）            | Yes  | OK    | Done | —       |                            |
| K02 | Chord（Ctrl+A 等）                  | Yes  | OK    | Done | —       | `PressAsync` / `pressKeys` |
| K03 | Tab / フォーカス移動の検証          | Yes  | OK    | Done | 29a     | `ExpectFocusedAsync`       |
| K04 | 特殊キー網羅（F1–F12, Win, NumPad） | Yes  | PART  | Done | 29a     | F1–F12 + NumPad。**Win 除外** |
| K05 | typeHuman（遅延付き人間風）         | PART | NO    | 任意 | —       |                            |

---

## 5. テキスト / 値

| ID  | シナリオ               | 競合 | Graft | 優先 | 仮Phase | メモ                 |
| --- | ---------------------- | ---- | ----- | ---- | ------- | -------------------- |
| V01 | TextBox 置換 setValue  | Yes  | OK    | Done | —       |                      |
| V02 | クリアして再入力       | Yes  | OK    | Done | —       |                      |
| V03 | PasswordBox 入力       | Yes  | OK    | Done | 29a     | Set のみ（Get に載せない） |
| V04 | Slider / 数値レンジ    | Yes  | OK    | Done | —       |                      |
| V05 | RichTextBox / 書式付き | PART | PART  | Done | 29a     | **平文のみ**（書式なし） |
| V06 | クリップボード経由貼付 | Yes  | NO    | 任意 | —       |                      |

---

## 6. トグル / チェック

| ID  | シナリオ                 | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | ------------------------ | ---- | ----- | ---- | ------- | ---- |
| T01 | CheckBox トグル          | Yes  | OK    | Done | —       |      |
| T02 | ExpectChecked            | Yes  | OK    | Done | —       |      |
| T03 | RadioButton グループ選択 | Yes  | OK    | Done | 29a     | Toggle + ExpectChecked |
| T04 | ToggleButton             | Yes  | OK    | Done | 29a     | Toggle + ExpectChecked |

---

## 7. リスト / 選択

| ID  | シナリオ                          | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | --------------------------------- | ---- | ----- | ---- | ------- | ---- |
| L01 | ListBox 単一選択（index）         | Yes  | OK    | Done | —       |      |
| L02 | ListBox 複数選択（置換）          | Yes  | OK    | Done | —       |      |
| L03 | ComboBox 項目選択（index）        | Yes  | OK    | Done | —       |      |
| L04 | ComboBox ドロップダウン開閉の明示 | Yes  | OK    | Done | 29b     | Expand/Collapse + `IsDropDownOpen` |
| L05 | 表示名・キーで選択                | Yes  | OK    | Done | 27      | `SelectAsync(key)` |
| L06 | ListView / GridView               | Yes  | OK    | Done | 29b     | 行=ListBox API / セル Read のみ |
| L07 | 仮想化リストの scroll+select      | Yes  | OK    | Done | —       |      |

---

## 8. ツリー / 開閉

| ID  | シナリオ                   | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | -------------------------- | ---- | ----- | ---- | ------- | ---- |
| E01 | TreeView 展開/折りたたみ   | Yes  | OK    | Done | —       |      |
| E02 | ExpectExpanded / Selected  | Yes  | OK    | Done | —       |      |
| E03 | Expander                   | Yes  | OK    | Done | —       |      |
| E04 | 深いツリーをパス指定で辿る | Yes  | OK    | Done | 27      | `SelectTreeAsync` |

---

## 9. タブ / ホスト切替

| ID  | シナリオ                       | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | ------------------------------ | ---- | ----- | ---- | ------- | ---- |
| H01 | TabControl 選択                | Yes  | OK    | Done | —       |      |
| H02 | Frame / NavigationWindow 遷移  | Yes  | OK    | Done | 32      | **Frame のみ**（専用 DSL なし・既存 WaitFor/Expect）。NavigationWindow は本 Must 外 |
| H03 | カスタム「ページ」差し替え待ち | Yes  | OK    | Done | 24      | Visibility パネル |

---

## 10. メニュー

| ID  | シナリオ                     | 競合 | Graft | 優先 | 仮Phase | メモ       |
| --- | ---------------------------- | ---- | ----- | ---- | ------- | ---------- |
| U01 | Menu バー トップ + 1 段サブ  | Yes  | OK    | Done | —       |            |
| U02 | Menu 任意深さ / パス DSL     | Yes  | OK    | Done | 26      | `SelectMenuAsync` path |
| U03 | ContextMenu サブメニュー     | Yes  | OK    | Done | 26      | M03 と同束             |
| U04 | 無効メニュー項目の明示エラー | Yes  | OK    | Done | 26      | `element.notActionable` |

---

## 11. DataGrid / 表

| ID  | シナリオ                            | 競合 | Graft | 優先 | 仮Phase | メモ                 |
| --- | ----------------------------------- | ---- | ----- | ---- | ------- | -------------------- |
| G01 | 行選択（単一）                      | Yes  | OK    | Done | —       |                      |
| G02 | 行複数選択                          | Yes  | OK    | Done | —       |                      |
| G03 | セル Text 読み書き                  | Yes  | OK    | Done | —       |                      |
| G04 | セル CheckBox                       | Yes  | OK    | Done | —       |                      |
| G05 | 列キー（Header）                    | Yes  | OK    | Done | —       |                      |
| G06 | Template 列                         | Yes  | OK    | Done | 28      | Get 表示テキスト / Set=TextBox\|CheckBox |
| G07 | セル選択ユニット                    | Yes  | OK    | Done | 28      | `SelectCellAsync` 単一                   |
| G08 | ソート後の行特定                    | Yes  | OK    | Done | 28      | `SelectRowAsync(columnKey,value)`        |
| G09 | フィルタ / 列リサイズ / 並び替え UI | Yes  | PART  | Done | 28      | **ソート UI のみ**（ヘッダークリック）   |
| G10 | 新規行追加 / 行削除操作             | Yes  | OK    | Done | 28      | `AddRowAsync` / `DeleteSelectedRowsAsync` |

---

## 12. その他コントロール

| ID  | シナリオ                           | 競合 | Graft | 優先 | 仮Phase | メモ       |
| --- | ---------------------------------- | ---- | ----- | ---- | ------- | ---------- |
| C01 | DatePicker / Calendar              | Yes  | OK    | Done | 29b     | SelectedDate `yyyy-MM-dd`（Calendar UI なし） |
| C02 | ProgressBar 値の読み取り・完了待ち | Yes  | OK    | Done | 24      | `ExpectValue` |
| C03 | ToolTip 表示待ち                   | Yes  | OK    | Done | 29b     | `ExpectToolTipAsync` |
| C04 | ToolBar / StatusBar 項目操作       | Yes  | OK    | Done | 29b     | 専用 API なし（Sample） |
| C05 | Popup / Flyout                     | Yes  | OK    | Done | 29b     | 開時 Child ツリー合流 |
| C06 | Hyperlink / カスタムクリック可能   | Yes  | OK    | Done | 29b     | TextBlock 内 Hyperlink + Click |

---

## 13. ウィンドウ / ダイアログ / 遷移 / 待ち

| ID  | シナリオ                               | 競合 | Graft | 優先   | 仮Phase | メモ        |
| --- | -------------------------------------- | ---- | ----- | ------ | ------- | ----------- |
| W01 | 子ウィンドウ list / switch / wait      | Yes  | OK    | Done   | —       |             |
| W02 | モーダル ShowDialog 開封               | Yes  | OK    | Done   | —       |             |
| W03 | Open/Save/Folder ダイアログ（シーム）  | Yes  | OK    | Done   | —       |             |
| W04 | MessageBox（シーム）                   | Yes  | OK    | Done   | —       |             |
| W05 | 実 OS コモンダイアログを UIA 操作      | Yes  | NO    | 非目標 | —       |             |
| W06 | 要素の出現待ち（汎用）                 | Yes  | OK    | Done   | 24      | `WaitForAsync` |
| W07 | 要素の消失待ち                         | Yes  | OK    | Done   | 24      | `ExpectGoneAsync` |
| W08 | 窓の消失待ち                           | Yes  | OK    | Done   | 24      | `WaitForWindowClosedAsync` |
| W09 | 進捗ダイアログ → 完了 → 次画面         | Yes  | OK    | Done   | 24      | Sample `ProgressWindow` |
| W10 | 同一窓内画面遷移の安定検証             | Yes  | OK    | Done   | 24      | `NextScreenPanel` |
| W11 | 非同期 UI（Dispatcher 遅延）の自動待機 | Yes  | PART  | Done   | 24      | 専用 API なし（ポーリング） |
| W12 | トースト / 一時通知                    | PART | NO    | 任意   | —       |             |

---

## 14. スクリーンショット / ビジュアル

| ID  | シナリオ                | 競合 | Graft | 優先   | 仮Phase | メモ |
| --- | ----------------------- | ---- | ----- | ------ | ------- | ---- |
| P01 | ウィンドウ全体 PNG      | Yes  | OK    | Done   | —       |      |
| P02 | 要素クリップ Screenshot | Yes  | NO    | 任意   | —       |      |
| P03 | 画像 expect / diff      | Yes  | NO    | 任意   | —       |      |
| P04 | デスクトップ全体        | PART | NO    | 非目標 | —       |      |
| P05 | 動画 / trace 記録       | Yes  | NO    | 非目標 | —       |      |

---

## 15. アサーション / Expect

| ID  | シナリオ                            | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | ----------------------------------- | ---- | ----- | ---- | ------- | ---- |
| A01 | ExpectName                          | Yes  | OK    | Done | —       |      |
| A02 | ExpectSelected / Expanded / Checked | Yes  | OK    | Done | —       |      |
| A03 | ExpectCellText                      | Yes  | OK    | Done | —       |      |
| A04 | ExpectEnabled / Disabled            | Yes  | OK   | Done | 24      |      |
| A05 | ExpectVisible / Hidden              | Yes  | OK   | Done | 24      |      |
| A06 | テキスト部分一致 / Regex            | Yes  | OK   | Done | 24      | Contains / Matches |
| A07 | ExpectValue（Slider 等を tree で）  | PART | OK   | Done | 24      | `TreeNode.value` |
| A08 | ソフトアサート（失敗を貯める）      | PART | NO    | 任意 | —       |      |

---

## 16. 失敗診断 / レポート

| ID  | シナリオ                     | 競合 | Graft | 優先   | 仮Phase | メモ |
| --- | ---------------------------- | ---- | ----- | ------ | ------- | ---- |
| D01 | 構造化 FailureReport         | Yes  | OK    | Done   | —       |      |
| D02 | 失敗時スクショ添付           | Yes  | OK    | Done   | —       |      |
| D03 | 失敗時ツリー添付             | Yes  | OK    | Done   | —       |      |
| D04 | ツリー差分 JSON              | PART | NO    | 任意   | —       |      |
| D05 | シナリオファイル自動書き換え | PART | NO    | 非目標 | —       |      |
| D06 | 操作タイムライン（目視レビュー） | PART | OK    | Done | 33      | PNG 連番 + HTML（速度・操作名字幕）。GIF/FFmpeg/ImageSharp なし。`task_phase33.md` |

---

## 17. Scenario / MCP / テスト DX

| ID  | シナリオ             | 競合 | Graft | 優先 | 仮Phase | メモ                    |
| --- | -------------------- | ---- | ----- | ---- | ------- | ----------------------- |
| X01 | 宣言的 Scenario JSON | Yes  | OK    | Done | —       |                         |
| X02 | MCP 原子ツール       | Yes  | OK    | Done | —       |                         |
| X03 | Codegen / レコーダー | Yes  | NO    | 任意 | —       |                         |
| X04 | 並列 E2E の安定実行  | Yes  | PART  | Done | 31      | 正本 `dotnet test Graft.slnx -m:1`。真の並列安定化は非目標（mutex 見送り） |

---

## 18. プラットフォーム（参考）

| ID  | シナリオ                  | 競合 | Graft | 優先    | 仮Phase | メモ               |
| --- | ------------------------- | ---- | ----- | ------- | ------- | ------------------ |
| Z01 | Avalonia アダプタ         | —    | NO    | 非目標* | —       | *Must 完了後に解禁 |
| Z02 | .NET Framework WPF        | PART | NO    | 非目標  | —       |                    |
| Z03 | Headless 専用バックエンド | PART | NO    | 非目標  | —       |                    |

---

## Must 実装分割（仮 Phase）

| 仮Phase | 束                             | 主な ID                                              |
| ------- | ------------------------------ | ---------------------------------------------------- |
| 23      | 本表 + roadmap（実装なし）     | —                                                    |
| 24      | 待ち / Expect / 画面遷移・進捗 | W06–W11, A04–A07, H03, C02（H02 Frame は除外）        |
| 25      | マウス高度                     | M04–M08                                              |
| 26      | メニュー深さ                   | M03, U02–U04                                         |
| 27      | 探索・パス・キー指定           | F02, F04, F05, L05, E04                              |
| 28      | DataGrid 残り                  | G06–G10                                              |
| 29a     | 入力・トグル・キー穴           | V03, V05, T03, T04, K03, K04                         |
| 29b     | リスト / その他 UI 穴          | L04, L06, C01, C03–C06                               |
| 31      | SendInput 並列対策             | X04（`-m:1` で Done）                                |
| 32      | Frame 遷移                     | H02（Frame のみ）                                    |
| 33      | 操作タイムライン               | D06                                                  |

（P02 要素クリップは任意のため Phase 番号なし）

残 Must（Avalonia 前）: **なし**（H02 / X04 / D06 は Done）。Avalonia 解禁可。

---

## レビューチェックリスト

- [x] Must を行単位で確定した
- [x] K05 / V06 / W12 / A08 / P02 は任意、X04 は Must（運用 Done）
- [x] D06 を Must に追加（単一 FW 完成度優先）
- [x] Inspector は任意（ゲート外）
- [x] Avalonia 再開ゲート（Must 全緑）に合意
- [x] Phase 24 受け入れ線は `task_phase24.md` で固定（以降も各 `task_phaseN.md`）
