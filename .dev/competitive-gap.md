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

提示リストをベースに、K05 / V06 / W12 / A08 / P02 は任意。**X04 は Must**。

`F02 F04 F05` · `M03–M08` · `K03 K04` · `V03 V05` · `T03 T04` · `L04 L05 L06` · `E04` · `H02 H03` · `U02 U03 U04` · `G06–G10` · `C01–C06` · `W06–W11` · `A04–A07` · `X04`

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
| F02 | Name / ControlType で特定               | Yes  | PART  | Must   | 27      | E2E 正本を Id 以外にも広げる |
| F03 | 祖先近傍（near）で曖昧さ低減            | PART | PART  | 任意   | —       | `NearAutomationId`           |
| F04 | 表示テキスト / 項目キーでリスト項目特定 | Yes  | NO    | Must   | 27      |                              |
| F05 | 相対セレクタ（子・兄弟・nth）           | Yes  | NO    | Must   | 27      |                              |
| F06 | 失敗時の自己修復（代替セレクタ）        | PART | PART  | 任意   | —       | Phase 4。ファジーは非目標    |
| F07 | ファジー／編集距離マッチ                | PART | NO    | 非目標 | —       |                              |
| F08 | Inspector / Spy で Id 採取              | Yes  | NO    | 任意   | —       | 探索補助。getTree で代替可   |

---

## 3. マウス / ポインタ

| ID  | シナリオ                           | 競合 | Graft | 優先 | 仮Phase | メモ                      |
| --- | ---------------------------------- | ---- | ----- | ---- | ------- | ------------------------- |
| M01 | クリック（ボタン Invoke）          | Yes  | OK    | Done | —       | native → Peer → SendInput |
| M02 | 右クリック → ContextMenu（1 段）   | Yes  | OK    | Done | —       | Phase 16                  |
| M03 | ContextMenu サブメニュー           | Yes  | NO    | Must | 26      |                           |
| M04 | ダブルクリック                     | Yes  | NO    | Must | 25      |                           |
| M05 | Hover / MouseEnter 副作用          | Yes  | NO    | Must | 25      |                           |
| M06 | Drag and Drop                      | Yes  | NO    | Must | 25      |                           |
| M07 | 座標クリック（要素外・オフセット） | Yes  | NO    | Must | 25      |                           |
| M08 | マウスホイール                     | Yes  | NO    | Must | 25      |                           |

---

## 4. キーボード

| ID  | シナリオ                            | 競合 | Graft | 優先 | 仮Phase | メモ                       |
| --- | ----------------------------------- | ---- | ----- | ---- | ------- | -------------------------- |
| K01 | リテラル入力（SendKeys）            | Yes  | OK    | Done | —       |                            |
| K02 | Chord（Ctrl+A 等）                  | Yes  | OK    | Done | —       | `PressAsync` / `pressKeys` |
| K03 | Tab / フォーカス移動の検証          | Yes  | PART  | Must | 29      | ExpectFocus 等             |
| K04 | 特殊キー網羅（F1–F12, Win, NumPad） | Yes  | PART  | Must | 29      |                            |
| K05 | typeHuman（遅延付き人間風）         | PART | NO    | 任意 | —       |                            |

---

## 5. テキスト / 値

| ID  | シナリオ               | 競合 | Graft | 優先 | 仮Phase | メモ                 |
| --- | ---------------------- | ---- | ----- | ---- | ------- | -------------------- |
| V01 | TextBox 置換 setValue  | Yes  | OK    | Done | —       |                      |
| V02 | クリアして再入力       | Yes  | OK    | Done | —       |                      |
| V03 | PasswordBox 入力       | Yes  | NO    | Must | 29      |                      |
| V04 | Slider / 数値レンジ    | Yes  | OK    | Done | —       |                      |
| V05 | RichTextBox / 書式付き | PART | NO    | Must | 29      | 範囲は実装時に契約化 |
| V06 | クリップボード経由貼付 | Yes  | NO    | 任意 | —       |                      |

---

## 6. トグル / チェック

| ID  | シナリオ                 | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | ------------------------ | ---- | ----- | ---- | ------- | ---- |
| T01 | CheckBox トグル          | Yes  | OK    | Done | —       |      |
| T02 | ExpectChecked            | Yes  | OK    | Done | —       |      |
| T03 | RadioButton グループ選択 | Yes  | PART  | Must | 29      |      |
| T04 | ToggleButton             | Yes  | PART  | Must | 29      |      |

---

## 7. リスト / 選択

| ID  | シナリオ                          | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | --------------------------------- | ---- | ----- | ---- | ------- | ---- |
| L01 | ListBox 単一選択（index）         | Yes  | OK    | Done | —       |      |
| L02 | ListBox 複数選択（置換）          | Yes  | OK    | Done | —       |      |
| L03 | ComboBox 項目選択（index）        | Yes  | OK    | Done | —       |      |
| L04 | ComboBox ドロップダウン開閉の明示 | Yes  | PART  | Must | 29      |      |
| L05 | 表示名・キーで選択                | Yes  | NO    | Must | 27      |      |
| L06 | ListView / GridView               | Yes  | PART  | Must | 29      |      |
| L07 | 仮想化リストの scroll+select      | Yes  | OK    | Done | —       |      |

---

## 8. ツリー / 開閉

| ID  | シナリオ                   | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | -------------------------- | ---- | ----- | ---- | ------- | ---- |
| E01 | TreeView 展開/折りたたみ   | Yes  | OK    | Done | —       |      |
| E02 | ExpectExpanded / Selected  | Yes  | OK    | Done | —       |      |
| E03 | Expander                   | Yes  | OK    | Done | —       |      |
| E04 | 深いツリーをパス指定で辿る | Yes  | NO    | Must | 27      |      |

---

## 9. タブ / ホスト切替

| ID  | シナリオ                       | 競合 | Graft | 優先 | 仮Phase | メモ |
| --- | ------------------------------ | ---- | ----- | ---- | ------- | ---- |
| H01 | TabControl 選択                | Yes  | OK    | Done | —       |      |
| H02 | Frame / NavigationWindow 遷移  | Yes  | NO    | Must | —       | Phase 24 は Frame なし。需要後 |
| H03 | カスタム「ページ」差し替え待ち | Yes  | OK    | Done | 24      | Visibility パネル |

---

## 10. メニュー

| ID  | シナリオ                     | 競合 | Graft | 優先 | 仮Phase | メモ       |
| --- | ---------------------------- | ---- | ----- | ---- | ------- | ---------- |
| U01 | Menu バー トップ + 1 段サブ  | Yes  | OK    | Done | —       |            |
| U02 | Menu 任意深さ / パス DSL     | Yes  | NO    | Must | 26      |            |
| U03 | ContextMenu サブメニュー     | Yes  | NO    | Must | 26      | M03 と同束 |
| U04 | 無効メニュー項目の明示エラー | Yes  | PART  | Must | 26      |            |

---

## 11. DataGrid / 表

| ID  | シナリオ                            | 競合 | Graft | 優先 | 仮Phase | メモ                 |
| --- | ----------------------------------- | ---- | ----- | ---- | ------- | -------------------- |
| G01 | 行選択（単一）                      | Yes  | OK    | Done | —       |                      |
| G02 | 行複数選択                          | Yes  | OK    | Done | —       |                      |
| G03 | セル Text 読み書き                  | Yes  | OK    | Done | —       |                      |
| G04 | セル CheckBox                       | Yes  | OK    | Done | —       |                      |
| G05 | 列キー（Header）                    | Yes  | OK    | Done | —       |                      |
| G06 | Template 列                         | Yes  | NO    | Must | 28      |                      |
| G07 | セル選択ユニット                    | Yes  | NO    | Must | 28      |                      |
| G08 | ソート後の行特定                    | Yes  | NO    | Must | 28      |                      |
| G09 | フィルタ / 列リサイズ / 並び替え UI | Yes  | NO    | Must | 28      | 範囲は実装時に契約化 |
| G10 | 新規行追加 / 行削除操作             | Yes  | NO    | Must | 28      |                      |

---

## 12. その他コントロール

| ID  | シナリオ                           | 競合 | Graft | 優先 | 仮Phase | メモ       |
| --- | ---------------------------------- | ---- | ----- | ---- | ------- | ---------- |
| C01 | DatePicker / Calendar              | Yes  | NO    | Must | 29      |            |
| C02 | ProgressBar 値の読み取り・完了待ち | Yes  | OK    | Done | 24      | `ExpectValue` |
| C03 | ToolTip 表示待ち                   | Yes  | NO    | Must | 29      | hover 依存 |
| C04 | ToolBar / StatusBar 項目操作       | Yes  | PART  | Must | 29      |            |
| C05 | Popup / Flyout                     | Yes  | PART  | Must | 29      |            |
| C06 | Hyperlink / カスタムクリック可能   | Yes  | PART  | Must | 29      |            |

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

---

## 17. Scenario / MCP / テスト DX

| ID  | シナリオ             | 競合 | Graft | 優先 | 仮Phase | メモ                    |
| --- | -------------------- | ---- | ----- | ---- | ------- | ----------------------- |
| X01 | 宣言的 Scenario JSON | Yes  | OK    | Done | —       |                         |
| X02 | MCP 原子ツール       | Yes  | OK    | Done | —       |                         |
| X03 | Codegen / レコーダー | Yes  | NO    | 任意 | —       |                         |
| X04 | 並列 E2E の安定実行  | Yes  | PART  | Must | 31      | mutex 等。`-m:1` は暫定 |

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
| 29      | コントロール / キー穴          | V03, V05, L04, L06, T03, T04, C01, C03–C06, K03, K04 |
| 31      | SendInput 並列対策             | X04                                                  |

（P02 要素クリップは任意のため Phase 番号なし）

---

## レビューチェックリスト

- [x] Must を行単位で確定した
- [x] K05 / V06 / W12 / A08 / P02 は任意、X04 は Must
- [x] Inspector は任意（ゲート外）
- [x] Avalonia 再開ゲート（Must 全緑）に合意
- [x] Phase 24 受け入れ線は `task_phase24.md` で固定（以降も各 `task_phaseN.md`）
