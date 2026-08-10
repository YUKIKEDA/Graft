# Graft — WPF/AvaloniaUI向けGUIテスト自動化ツール 開発概要

## 1. プロジェクトの目的

WPFおよびAvaloniaUIアプリケーション向けのGUI（E2E）テスト自動化ツールを開発する。
Playwright（Web）やTestComplete（マルチフレームワーク対応の商用ツール）に相当する立ち位置を、
WPF/AvaloniaUIに特化することで実現する。

対象は**自社で開発しソースコードを管理できるWPF/Avaloniaアプリケーション**に限定する
（サードパーティ製の既存exeに対するブラックボックステストは対象外。詳細は3節参照）。

**初期ランタイムスコープ:** .NET 8 以降の WPF / Avalonia のみ。.NET Framework 製 WPF は需要が固まってから検討する（10節参照）。

## 2. 既存ツールの調査結果と差別化の根拠

### 既存OSSツールの限界
FlaUI、WinAppDriver、TestStack.White、AppiumのWindows driverは、いずれも
**Windows標準のUI Automation（UIA）COM APIをラップしているだけ**の実装である。
これには構造的な限界がある：

- UIAはcrossプロセスのCOM呼び出しが必要なため、ツリー走査やプロパティ取得が遅い
- UIAはアクセシビリティAPIとして設計されており、カスタムコントロールでは
  操作用のパターン（InvokePattern等）が未実装なことが多い
- 状態変化の検知が粗いイベントかポーリングに頼らざるを得ない

これらのツールはUIA（正規の公開API）のみを使っているため、セキュリティソフトに
不審な挙動として検知されることは基本的にない。

### TestCompleteの優位性の正体
調査の結果、商用ツールTestCompleteが.NET/WPFアプリに対して持つ優位性は、
UIAとは別の「アプリケーションプロセス内部に直接アクセスする専用プラグイン」
（Open Applications機構）によるものであることが判明した。
これはまさに本プロジェクトが目指す「in-process直接アクセス」と同じ設計思想である。

### 結論：差別化ポイント
| ツール                                                   | アクセス方式                                           | 対象範囲                                          |
| -------------------------------------------------------- | ------------------------------------------------------ | ------------------------------------------------- |
| FlaUI / WinAppDriver / TestStack.White / Appium(Windows) | UIA(COM)ラップのみ                                     | 汎用Windows全般                                   |
| TestComplete                                             | フレームワーク別のin-process直接アクセス（非公開実装） | 汎用+多数フレームワーク、商用・高額               |
| **Graft（本プロジェクト）**                              | in-process直接アクセス（対象アプリへの事前組み込み）   | **WPF/AvaloniaUIに特化、自社アプリ限定、OSS想定** |

OSSの中でin-process直接アクセスを実装しているものは存在しないため、
スコープをWPF/AvaloniaUIの2フレームワーク・自社アプリに絞ることで、実装をシンプルに保ちながら
TestComplete相当の精度を狙う、という位置づけ。

## 3. アーキテクチャ方針

### 【重要】方式変更の経緯

当初はプロセス注入（対象exeをサスペンド状態で起動し、`CreateRemoteThread`+`LoadLibrary`で
エージェントDLLを注入する方式）を検討していたが、以下の理由により
**対象アプリへの事前組み込み方式**に変更した。

**プロセス注入方式の問題点**
- `CreateRemoteThread`/`LoadLibrary`の組み合わせは、マルウェアの典型的な
  プロセスインジェクション手法と同一であり、AV/EDR製品のふるまい検知に
  引っかかりやすい。署名で軽減できるのはSmartScreenの警告表示のみで、
  EDRの挙動ベース検知には無力
- OSSプロジェクトの立ち上げ初期は「信頼の蓄積」がないため、特に検知されやすい
- アーキテクチャ（x86/x64/ARM64）の一致問題、CLR未初期化状態への対応など、
  実装上の不確実性・複雑性も高かった

**事前組み込み方式のメリット**
- 対象アプリ自身がテスト用のNuGetパッケージを参照し、起動時に自分でパイプサーバーを
  立ち上げる方式のため、プロセス注入という「外部から入り込む」操作が一切発生しない
- AV/EDRのプロセスインジェクション検知に引っかかる余地が構造的になくなる
  （セキュリティ上の懸念を「説明して回避してもらう」必要がなくなる）
- ネイティブ層（C++によるインジェクター・ブートストラップDLL）が丸ごと不要になり、
  実装がC#のみで完結する。ビルドパイプライン・保守コストが大幅に下がる

**トレードオフ（許容する制約）**
- ソースコードを管理できない対象（サードパーティ製アプリ、ブラックボックステスト）
  には使えない。これはPlaywright/Cypressが「自分たちのWebアプリを対象にする」のと
  同じ立ち位置であり、Graftの「WPF/AvaloniaUIに特化する」という方向性とも矛盾しない
- 本番ビルドへの誤混入を防ぐ仕組みが必須（後述）

### 処理フロー

1. 対象アプリがフレームワーク別パッケージ（`Graft.Instrumentation.Wpf` または
   `Graft.Instrumentation.Avalonia`）を参照する（共有コア `Graft.Instrumentation` は間接依存）
2. アプリ起動時（`OnStartup` 等）に `Agent.Start()` を呼び出す
   （`GRAFT_TEST` コンパイル時のみ API が存在。詳細は下記）
3. 実行時に `GRAFT_ENABLE=1` がある場合のみ、エージェントが
   `GRAFT_PIPE_NAME` で名前付きパイプサーバーを起動する
4. コントローラー（`Graft.Core`）がパイプに接続し、`GRAFT_CONNECT_TOKEN` でハンドシェイク後、
   コマンド送受信を開始する
5. デフォルトではテスト完了後に対象プロセスを終了（セッション再利用はオプトイン）

```
[コントローラー: Graft.Core / 将来 Graft.McpServer]
   │ 名前付きパイプ
   │  4byte長さプレフィックス + JSONエンベロープ
   │  （バイナリは後続フレーム）
   ▼
[対象アプリ内エージェント: Graft.Instrumentation.* (.NET 8+)]
   ├─ Visual Tree Walker
   ├─ IElementAdapter（Wpf / Avalonia は別パッケージ）
   ├─ SendInput P/Invoke（入力インジェクション）
   └─ 名前付きパイプサーバー（同一ユーザー ACL）
```

プロセス注入層（インジェクター、ブートストラップDLL）は不要となったため、
アーキテクチャ図から完全に削除された。

### 本番ビルドへの誤混入防止（決定済み）

エージェントの起動コードが誤ってリリースビルドに残ると、本番アプリ上に
コマンド実行可能な名前付きパイプが立ってしまうため、重大なセキュリティホールになる。
対策の正本は次のとおり（`#if DEBUG` 単体や実行時フラグのみは採用しない）。

- **コンパイル時:** 専用シンボル `GRAFT_TEST` 外では `Agent.Start` API 自体を消す
- **Analyzer:** 判定正本はプリプロセッサ記号 `GRAFT_TEST` の有無のみ（Configuration 名や
  `#if` 囲み緩和は使わない）。未定義コンパイルでの `Agent.Start` 参照はビルドエラー。
  Analyzer は `Graft.Instrumentation.Wpf` / `.Avalonia` 経由で自動導入する
- **実行時:** `GRAFT_ENABLE=1` が無い限りパイプを立てない
- **参照:** NuGet 同梱の `Graft.props` / `Graft.targets` を本線とする。
  有効化の正本はプロパティ `GraftTest=true`（`/p:GraftTest=true` または csproj）。
  サンプルに Configuration=`GraftTest` 便利構成も用意（targets が GraftTest=true を立てる）。
  Debug 構成への自動紐づけはしない。`GRAFT_TEST` を DefineConstants に追加。
  Analyzer 必須ルールは `GRAFT001`（GRAFT_TEST 外の Agent.Start → Error）。追加ルールは後追い。
  README にコピー＆ペースト例も載せる。常時参照も許容するが、上記コンパイル/Analyzer/実行時が最低ライン。
  `PrivateAssets` 単独は混入防止の正本にしない

### 接続・プロセス寿命（決定済み）

- パイプ名はランナーが生成し `GRAFT_PIPE_NAME` で渡す（並列テスト時の衝突回避）
- パイプ ACL は同一ユーザーのみ。接続後にプロトコル版＋`GRAFT_CONNECT_TOKEN` でハンドシェイク
- **同時接続は単一クライアントのみ**。追加接続は拒否。切断後の再接続＋Handshake は許可
- 公開主経路は **Launch**（Core が環境変数を付与して起動）。`Connect(pipeName, token)` は
  低レベル API として用意し、ドキュメントの第一級にはしない
- デフォルトは起動→終了。セッション再利用は Fixture 寿命へのオプトイン

### セマンティックツリー・セレクタ（決定済み）

- Visual Tree を共通 JSON スキーマへ正規化（`IElementAdapter`）
- 公開セレクタは **スコアリング方式の複合キー**（閾値以上の最高点。同点は `element.ambiguous`）。
  初期仮重み: automationId=100, name=40, controlType=15, 近傍パス=20, 閾値=60（チューニング前提）。
  ショートハンド API として automationId→name 相当も用意
- セッション内 `runtimeId` は内部ハンドル。テスト記述の正本にはしない
- Phase 1 必須ノード: `runtimeId`, `controlType`, `name`, `automationId`, `bounds`,
  `enabled`, `visible`, `focused`, `children`
  （パターン可否・現在値・セレクタ候補は早期追加するが Phase 1 完了条件外）
- `bounds` の外部正本は **対象ウィンドウクライアント領域の論理座標（DIP）**。
  物理/スクリーン変換はエージェント内部のみ（診断・Inspector の拡張フィールドには可）
- GetTree はデフォルト上限あり（深さ 25 / ノード 2,000）。超過時は切り詰め＋`truncated: true`。
  depth/maxNodes/セレクタ起点を指定可能。診断・Inspector 用に expanded（50 / 10,000）
- 仮想化リストは実現済み Visual Tree がデフォルト。`ScrollIntoView` / 実現 API を別途提供
- ウィンドウ: API・スキーマは最初からマルチウィンドウ（`windowId` / 対象切替）。
  Phase 1 実装はメインウィンドウからでよい。**実装完遂は Phase 7**（詳細は Q72〜 / `task_phase7.md`）
- ツリー差分は初期 **Core 側のみ**（エージェントは上限付き完全ツリー）。
  デフォルト出力は診断向け差分（追加/削除/変更＋要素スナップショット）。JSON Patch は後回し

### 入力・待機・スレッド（決定済み）

- 論理操作は共通パターン名に正規化し、アダプタ内で **ネイティブ API → Peer/Provider → SendInput**
  の順で試す
- よくある型は対応表（Button→invoke、TextBox→setValue 等）。未知型は Peer パターン有無を見て汎用処理し、
  だめなら SendInput。ホワイトリスト制限はしない
- Phase 1 完了条件の論理操作: `invoke` / `setValue`。続けて `toggle` とキー入力。
  `scrollIntoView` / `select` / `expand`・`collapse` は **Phase 5**（詳細は Q66 / `task_phase5.md`）。
  ツリー `selected` / `expanded` と状態 Expect は **Phase 6**（詳細は Q67〜 / `task_phase6.md`）
- `setValue`: ネイティブ代入（置き換え）優先。失敗時はクリア＋SendInput。
  `append` / `typeHuman` は後付けオプション
- SendInput クリック点: Peer のクリック可能点 → なければ bounds 中心。オフセットはオプション
- DPI/座標変換はエージェント側で一元化し、外部には論理座標系のみを見せる
- ツリー走査・パターン・スクショを含む操作は **UI ディスパッチャへマーシャリングし同期待機**。
  操作パイプラインは直列
- アクションは actionable（可視・有効・ヒット可能）待ちがデフォルト
- ビジネス期待は Expect ステップが正本。イベント優先、だめならポーリング
- デフォルトタイムアウト: アクション前待ち 5s / Expect 10s / 起動+Handshake 30s（Options で上書き可）
- Wait / Expect は **Core 側**。エージェントは原子的な状態取得と操作に徹する

### ワイヤプロトコル（決定済み）

- フレーミング: 4 byte 長さプレフィックス + ボディ（当面 JSON）
- エンベロープ: 要求 `{ v, id, method, params }` / 応答 `{ v, id, ok, result|error }`
- プロトコル版 `v` は整数（初期 `1`）。Handshake で完全一致必須。不一致は失敗。版交渉は後回し
- `error`: `{ code, message, details? }`。初期文書化コード:
  `handshake.rejected`, `protocol.versionMismatch`, `element.notFound`, `element.ambiguous`,
  `element.notActionable`, `action.timeout`, `action.failed`, `window.notFound`,
  `pipe.disconnected`, `agent.notEnabled`, `expect.failed`, `selector.invalid`
  （Expect 系は Core 発行でも同じ語彙）。診断レポートの常時添付はしない
- 初期は同期・直列・単一クライアント。サーバープッシュ通知は後回し
- スクショ等のバイナリ: JSON メタフレームの直後に raw バイナリフレーム
- MessagePack: 計測フックを入れ、仮閾値（GetTree p95 50ms / 本文 512KB）で評価ゲート。
  即切替はしない。スクショは raw フレーム済みのため対象外。多言語/gRPC は v1 スコープ外

## 4. 技術スタック

| レイヤー                                                  | 言語                                       | 備考                                                                   |
| --------------------------------------------------------- | ------------------------------------------ | ---------------------------------------------------------------------- |
| エージェント（Visual Tree走査・入力実行・パイプサーバー） | C# (.NET 8以降)                            | `Graft.Instrumentation` + フレームワーク別パッケージ                   |
| コントローラー（テストランナー向けSDK）                   | C#                                         | 操作モデルはフレームワーク非依存。サンプル/TestUtilities は xUnit から |
| 通信プロトコル                                            | 名前付きパイプ + 長さプレフィックス + JSON | ローカル専用。MessagePack は計測後に検討                               |
| 入力インジェクション実行                                  | C#からP/Invokeで`SendInput`を直接呼び出し  | パターン失敗時のフォールバック                                         |

**ネイティブ(C++)層は不要になった。** プロジェクト全体をC#のみで実装できる。

## 5. プロジェクト構成案（更新版）

```
Graft/
├── src/
│   ├── Graft.Instrumentation/            # 共有コア（パイプ、Agent、Input、共通契約）
│   │   ├── Pipe/
│   │   ├── Input/
│   │   └── Agent.cs
│   │
│   ├── Graft.Instrumentation.Wpf/        # WPF アダプタ（Analyzer を自動導入）
│   │   └── WpfElementAdapter.cs
│   │
│   ├── Graft.Instrumentation.Avalonia/   # Avalonia アダプタ（Analyzer を自動導入）
│   │   └── AvaloniaElementAdapter.cs
│   │
│   ├── Graft.Instrumentation.Analyzer/   # GRAFT_TEST 外の Agent.Start をエラー化
│   │
│   ├── Graft.Protocol/                   # ワイヤ／ツリー等の共有スキーマ
│   │
│   ├── Graft.Core/                       # 操作モデル、Fluent、Scenario(JSON)、待機、自己修復
│   │   ├── Selectors/
│   │   ├── Elements/
│   │   ├── Scenario/                     # 宣言的 JSON（正本の交換形式）
│   │   └── Application.cs                # Launch / 低レベル Connect
│   │
│   ├── Graft.McpServer/                  # Phase 3: MCP ホスト（Core の薄いラッパー）
│   │
│   └── Graft.TestUtilities/              # まず xUnit。NUnit/MSTest は後追い
│
├── tools/
│   ├── Graft.SmokeClient/                # M0: Handshake + GetTree 手動検証コンソール
│   └── Graft.Inspector/                  # FlaUInspect相当（将来）
│
├── tests/
│   ├── Graft.Instrumentation.Tests/
│   ├── Graft.Core.Tests/
│   └── sample-apps/
│       ├── SampleWpfApp/
│       └── SampleAvaloniaApp/
│
├── Graft.slnx
├── Directory.Build.props
└── README.md
```

### 構成上のポイント（更新版）
- ネイティブ層（`native/`）は廃止。プロジェクト全体が `.sln` 一つで完結する
- Instrumentation は **共有コア + フレームワーク別パッケージ**に分割（依存の汚染を防ぐ）
- Analyzer は Wpf/Avalonia パッケージ経由で自動導入する
- 利用側ビルド支援として `Graft.props` / `Graft.targets` を NuGet 同梱（`GraftTest=true` 入口）
- `Graft.Protocol` はエージェント・コントローラー双方から参照される共有スキーマ
- Fluent / Scenario(JSON) / MCP はいずれも同じ内部操作モデルにコンパイルする（どれも唯一の正本ではない）
- Scenario は初期は `Graft.Core` 内。MCP のみ `Graft.McpServer` に分離
- 自己修復セレクタは **Core 側**。Instrumentation は現在ツリーとヒント提供に徹する

## 6. プロダクト名

**Graft**（接ぎ木）。対象アプリに直接コードを組み込み内部に接続するという技術の本質を表す名称として採用
（当初はプロセス「注入」のニュアンスで命名したが、事前組み込み方式に変更した後も
「対象に接続する」という核心的なコンセプトは共通しているため、名称はそのまま維持する）。
単語自体からは「GUIテスト自動化ツール」であることは伝わりにくいため、
README・パッケージ説明・CLIヘルプ等では必ず
「Graft — In-process UI testing for WPF & AvaloniaUI」のようなタグラインを併記する運用とする。

## 7. 推奨する着手順序（更新版）

### マイルストーン受け入れ条件

| ID     | 受け入れ条件（これで「できた」）                                                                                                                                                                                                                                  | 含めない／後回し                                       |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ |
| **M0** | SampleWpfApp（.NET 8）+ Instrumentation.Wpf。`GRAFT_TEST` 外では Start API 無し／`GRAFT001` Error。環境変数ゲート付きでパイプ起動。`tools/Graft.SmokeClient` が Sample を Launch し Handshake + GetTree で SampleButton の name/bounds 取得（Connect 手起動も可） | props/targets 便利化、Screenshot、invoke、Core Launch  |
| **M1** | M0 ＋ ウィンドウ PNG スクショ（メタ+raw）＋ `invoke` でボタンクリック確認（TextBlock 変化）。`Graft.props`/`targets`（`GraftTest=true`）                                                                                                                          | setValue は M1 期間に追加可。Core Launch / xUnit は M2 |
| **M2** | M1 ＋ `Graft.Core` の Launch 経由 xUnit テスト1本が緑（SmokeClient 無しで主経路）                                                                                                                                                                                 | Avalonia、Scenario、MCP、自己修復                      |

**SampleWpfApp（M0 時点）:** Button（`AutomationId=SampleButton`）+ TextBox + クリックで変わる TextBlock。

**最初に置くリポジトリ範囲:** M0 プロジェクト一式 + `Directory.Build.props` 土台 + tests フォルダ。
Core / Avalonia / McpServer 等の空スケルトンは作らない。

### 作業順

1. **M0:** Directory.Build.props → SampleWpfApp → Protocol + Instrumentation(+Wpf) → Analyzer(`GRAFT001`) → SmokeClient（Launch 正本）
2. **M1:** Screenshot + `invoke` + props/targets。続けて `setValue` / `toggle` / キー
3. **M2:** Core Launch・待機・スコアリングセレクタ・エラーコード + xUnit 1本
4. WPF が安定したら `Graft.Instrumentation.Avalonia`
5. Phase 2 以降（失敗診断、Scenario JSON、MCP、自己修復）

## 8. LLMによるテスト作成・修正・確認への対応

### 背景・目的
E2Eテストは一般に壊れやすく、メンテナンスコストが高いという課題がある。
本プロジェクトでは、テストの作成・失敗時の修正・実行結果の確認をLLMが行えることを
前提にツールを設計する。これにより、UI変更のたびに人力でセレクタやテストコードを
修正するコストを削減することを狙う。

### 追加する機能

1. **スクリーンショット取得機能**
   - in-process で対象ウィンドウを画像化してパイプ返却
   - デフォルト: ウィンドウ全体 PNG。JPEG/品質・要素クロップはオプション API

2. **構造化された失敗診断レポート**
   - 必須最小: 失敗ステップ、期待、実際、タイムアウト有無、対象セレクタ
   - デフォルト添付: 直近 N 操作ログ、失敗時ツリー（差分可）、スクショ参照
   - 自己修復候補・詳細環境情報は Phase 4 まで任意拡張

3. **宣言的テストフォーマット**
   - 交換形式の正本は **JSON**（JSON Schema で契約）。YAML は後回し
   - Fluent API も Scenario も MCP も、同じ内部操作モデルにコンパイルする
   - 実装は `Graft.Core` 内（初期は別プロジェクト `Graft.Scenario` にしない）

4. **MCP的な対話インターフェース**
   - Phase 1〜2 の機能を公開する薄いラッパー（`Graft.McpServer`）

5. **自己修復セレクタ**
   - ロジックは Core。Instrumentation は現在ツリーと安定ヒントを返すだけ

### Avalonia Headless との関係

Avalonia 公式の Headless Testing Platform は、ウィンドウ／描画を差し替えた
**コントロール／レイアウト試験**向けであり、Graft の実プロセス E2E とは相補関係にある。
Graft 自身は Headless バックエンドを提供・統合しない。Headless 上アプリへの対応は需要次第で後付け。

### CI / 実行環境

インタラクティブな Windows セッションを基本前提とする。
Graft は仮想ディスプレイを提供しない。GitHub Actions 等向けには
「推奨セルフホスト構成」（インタラクティブログオン、画面ロックしない等）を文書化する。
純 Session 0 / オフスクリーン専用モードは初期対象外。

### 実装フェーズと優先順位

| フェーズ | 内容                                                 | 位置づけ                                   |
| -------- | ---------------------------------------------------- | ------------------------------------------ |
| Phase 1  | Instrumentation 本体 + スクショ + 原子的操作コマンド | 基盤。Wait/Expect は Core                  |
| Phase 2  | 構造化失敗診断 + 宣言的 JSON シナリオ                | LLM が使える中核。操作モデルを Core に集約 |
| Phase 3  | `Graft.McpServer`                                    | Phase 1〜2 の薄い公開層                    |
| Phase 4  | 自己修復セレクタ                                     | Core 側で精度を磨き込む                    |
| Phase 5  | WPF 残アクション（scroll / select / expand）         | 仮想化対応を含む操作面の穴埋め             |
| Phase 6  | ツリー状態（`selected` / `expanded`）+ Expect        | 診断・LLM。Phase 5 操作の状態検証          |
| Phase 7  | ウィンドウ／モーダル（list/switch/wait/開封）        | WPF カバレッジ。競合ギャップの窓面         |
| Phase 8  | DataGrid 行中心 MVP + `checked`                      | 複雑ホスト UI。セル R/W は次フェーズ       |
| Phase 9  | DataGrid セル R/W（Text 列 MVP）                     | ホスト＋(row, col)。OS ダイアログは次      |
| Phase 10 | OpenFile ダイアログ・シーム（方針 + MVP）            | Arm + Harmony CommonItemDialog.RunDialog   |
| Phase 11 | SaveFile ダイアログ・シーム（OpenFile 同型 MVP）     | Arm + 同一 RunDialog パッチ（Save のみ）   |
| Phase 12 | OpenFolder ダイアログ・シーム（同型 MVP）            | Arm + 同一 RunDialog（`FolderName`）       |
| Phase 13 | MessageBox シーム（Runtime MVP）                     | Arm + Harmony `MessageBox.Show`            |
| Phase 14 | キー chord / 特殊キー（`pressKeys`）                 | `PressAsync`。Avalonia は後ろへ            |
| Phase 15 | 公開 Screenshot（Session / Scenario / MCP）          | 既存 wire を第一級化。要素クリップは含めない |
| Phase 16 | 右クリック + ContextMenu / MenuItem                  | `RightClickAsync` + 開いたメニューをツリーに |
| Phase 17 | TabControl 選択（`select` 拡張）                     | 既存 `SelectAsync(index)`。Slider 等は次   |
| Phase 18 | Slider 値設定（`setValue` 拡張）                     | Invariant double → `Slider.Value`。複数選択は次 |
| Phase 19 | ListBox 複数選択（`selectMany`）                     | 置換セマンティクス。DataGrid 複数行は含めない |
| Phase 20 | Menu バー（既存 `invoke`）                           | トップ+1段サブ。開いたサブをツリーに       |
| Phase 21 | DataGrid 列キー + CheckBox 列                        | Header `columnKey`。複数行選択は次         |
| （次）   | DataGrid 複数行選択 → Avalonia                       | WPF ギャップ埋め優先。Inspector は最後寄り |

## 9. 未検討・今後の課題

- `GRAFT001` のメッセージ文言、マルチターゲット時の記号伝播の検証項目
- `Graft.props` / `Graft.targets` の具体 MSBuild 断片（サンプルへの落とし込み）
- よくある型の対応表の具体行（WPF/Avalonia それぞれの型名）
- セレクタ重みの実測チューニング、`details` スキーマのフィールド確定
- 診断向けツリー差分 JSON のフィールド名の確定
- scroll/select の項目キー・表示名指定（index 正本の次候補）
- 実 OS コモンダイアログの UIA 操作（方針上非採用。必要なら別検討）
- DataGrid Template 列 / セル選択
- hover・D&D / DataGrid 複数行選択（Phase 22）
- ContextMenu サブメニュー / Menu 任意深さ
- Avalonia アダプタ → Inspector（最後寄り）
- MessagePack 評価用の実測ログ形式
- .NET Framework WPF 対応の要否（需要が固まってから）
- 多言語バインディング / gRPC（v1 スコープ外。再検討は操作モデル安定後）
- （参考・不採用）プロセス注入方式の AV/EDR・コード署名問題は、事前組み込みへの変更で実質解消
- **テスト並列と SendInput:** `SampleUiCollection` / `McpUiCollection` はアセンブリ内直列化のみ。`dotnet test Graft.slnx` は Core / Sample / MCP が同時に SampleWpfApp を起動し、SendInput（click / keys / chord / rightClick）がフォーカス競合でフレークしうる（症状例: PressKeys 後に `ello` 残存、SendKeys 空振り、ContextMenu が開かず MenuItem 待ちタイムアウト）。暫定: `dotnet test Graft.slnx -m:1` または UI 系プロジェクトを順実行。恒久: アセンブリ横断 mutex / CI ジョブ分割（未着手）

## 10. 設計決定ログ

設計詰め（grill）で合意した事項。本文と矛盾する場合は本節および反映済み本文を正とする。

| ID  | 決定                                                                                              |
| --- | ------------------------------------------------------------------------------------------------- |
| Q1  | 初期ランタイムは .NET 8+ の WPF/Avalonia のみ。.NET Framework WPF は需要後                        |
| Q2  | Instrumentation は共有コア + `.Wpf` / `.Avalonia` に分割                                          |
| Q3  | 有効化は `GRAFT_TEST`（コンパイル）+ Analyzer エラー + 実行時オプトイン                           |
| Q4  | 実行時オプトインは環境変数（ランナーが付与）                                                      |
| Q5  | パイプ名はランナー生成 → `GRAFT_PIPE_NAME` で渡す                                                 |
| Q6  | プロセス寿命はデフォルト起動→終了。再利用はオプトイン                                             |
| Q7  | 公開セレクタは複合キー。runtimeId は内部ハンドル                                                  |
| Q8  | Fluent / Scenario / MCP は同じ内部操作モデルへ。唯一の正本は置かない                              |
| Q9  | 宣言的シナリオの正本は JSON。YAML は後回し                                                        |
| Q10 | Scenario は Core 内。MCP のみ `Graft.McpServer` に分離                                            |
| Q11 | 自己修復は Core。Instrumentation はツリーとヒント提供                                             |
| Q12 | 条件付き参照を推奨。常時参照も可。Analyzer + シンボル + 環境変数が最低ライン                      |
| Q13 | マルチウィンドウを API 初期から設計。Phase 1 実装はメインから                                     |
| Q14 | UI 操作はディスパッチャへマーシャリングし同期・直列                                               |
| Q15 | actionable 待ち + Expect 正本。イベント優先、なければポーリング                                   |
| Q16 | フレーミングは 4byte 長さプレフィックス + ボディ                                                  |
| Q17 | スクショデフォルトはウィンドウ PNG。JPEG/クロップはオプション                                     |
| Q18 | バイナリは JSON メタの後続 raw フレーム                                                           |
| Q19 | パイプ ACL は同一ユーザー。版 + CONNECT_TOKEN でハンドシェイク                                    |
| Q20 | 共通エンベロープ `{v,id,method,params}` / `{v,id,ok,result\|error}`                               |
| Q21 | `GRAFT_TEST` 外は Start API 消去 + 呼び出し Analyzer エラー。DEBUG 判定は使わない                 |
| Q22 | 失敗診断は最小必須 + 標準添付デフォルト。自己修復候補は Phase 4 任意                              |
| Q23 | 仮想化は実現済みツリーがデフォルト。実現/スクロール API を別途                                    |
| Q24 | Phase 1 エージェントは原子的操作まで。Wait/Expect は Core                                         |
| Q25 | Phase 1 必須ツリー項目は B セット。パターン/値/セレクタ候補は完了条件外                           |
| Q26 | タイムアウト既定: アクション 5s / Expect 10s / 起動+Handshake 30s                                 |
| Q27 | CI はインタラクティブセッション前提。推奨セルフホスト構成を文書化。Graft は仮想ディスプレイ非提供 |
| Q28 | Avalonia Headless とは相補。Graft は実プロセス E2E。Headless 対応は需要後                         |
| Q29 | 環境変数: `GRAFT_ENABLE` / `GRAFT_PIPE_NAME` / `GRAFT_CONNECT_TOKEN`                              |
| Q30 | 主経路は Launch。`Connect` は低レベル API                                                         |
| Q31 | MessagePack は計測後検討。多言語/gRPC は v1 外                                                    |
| Q32 | Core は FW 非依存。TestUtilities/サンプルは xUnit から                                            |
| Q33 | Analyzer は Instrumentation.Wpf / .Avalonia から自動導入                                          |
| Q34 | 本決定を section 10 に記録し、構成・着手順・本文へ反映                                            |
| Q35 | Analyzer 判定は `GRAFT_TEST` 記号のみ。Configuration / `#if` 緩和は使わない                       |
| Q36 | `Graft.props`/`targets` 本線（`GraftTest=true`）。README にコピー例も                             |
| Q37 | GetTree はデフォルト上限＋`truncated`＋ depth/maxNodes/起点指定                                   |
| Q38 | デフォルト 25/2,000。expanded 50/10,000                                                           |
| Q39 | ツリー差分は初期 Core 側のみ。エージェント差分は後回し                                            |
| Q40 | 論理操作はネイティブ → Peer → SendInput                                                           |
| Q41 | Phase 1 完了は `invoke`+`setValue`。toggle/キー次点。scroll/select/expand は後続                  |
| Q42 | MessagePack は計測＋仮閾値で評価。即切替しない                                                    |
| Q43 | 単一クライアント。再接続＋Handshake 可                                                            |
| Q44 | 外部座標はウィンドウクライアント論理 DIP。変換はエージェント内                                    |
| Q45 | セレクタはスコアリング＋閾値。同点は ambiguous。ショートハンド別途                                |
| Q46 | `error` は `{code,message,details?}`。安定コードを文書化                                          |
| Q47 | `v` は整数。Handshake 完全一致。版交渉は後回し                                                    |
| Q48 | Q35〜 を本文・section 9/10 へ即反映                                                               |
| Q49 | セレクタ仮重み: automationId=100, name=40, controlType=15, 近傍パス=20, 閾値=60                   |
| Q50 | 安定エラーコード初期セット（handshake/protocol/element/action/window/pipe/agent/expect/selector） |
| Q51 | setValue はネイティブ置換優先、失敗時クリア+SendInput。append/typeHuman は後付け                  |
| Q52 | SendInput クリックは Peer 点→中心。オフセットオプション可                                         |
| Q53 | ツリー差分デフォルトは診断向け。JSON Patch は後回し                                               |
| Q54 | Analyzer 必須は GRAFT001 Error のみ。追加ルールは後追い                                           |
| Q55 | GraftTest=true が正本。Configuration=GraftTest はサンプル便利構成。Debug 紐づけなし               |
| Q56 | よくある型は対応表、未知型は Peer→SendInput。ホワイトリスト制限なし                               |
| Q57 | Q49〜反映後、最初の実装マイルストーン受け入れ条件を詰める                                         |
| Q58 | マイルストーンを M0/M1/M2 の三段に分ける                                                          |
| Q59 | M0 に GRAFT_TEST+環境変数+Analyzer。props/targets は M1                                           |
| Q60 | M0 手動クライアントは `tools/Graft.SmokeClient`                                                   |
| Q61 | M0 直結をあと数問してから閉じ、実装へ                                                             |
| Q62 | SmokeClient は Launch と Connect 両方。M0 デモ正本は Launch                                       |
| Q63 | Sample は Button + TextBox + クリックで変わる TextBlock                                           |
| Q64 | 最初は M0 一式 + Directory.Build.props + tests 土台。空スケルトンは作らない                       |
| Q65 | 実装は gitignore + 雛形から。M0 は task_m0.md の Batch 単位で進める                                |
| Q66 | Phase 5: scrollIntoView / select / expand・collapse。詳細は `task_phase5.md`                      |
| Q67 | Phase 6: TreeNode に `selected`/`expanded` を `bool?`（非該当は null/省略）。プロトコル v1 のまま  |
| Q68 | selected は選択系のみ（項目ノード）。expanded は開閉対象（TreeViewItem/Expander）。checked は別途 |
| Q69 | ExpectSelectedAsync / ExpectExpandedAsync。null は expect.failed。Scenario/MCP は薄い追従         |
| Q70 | Phase 6 受け入れは ListBox 実現済み項目 + TreeViewItem。Combo 項目 Expect は完了条件外            |
| Q71 | Phase 6 時点の次候補は Avalonia → Inspector だったが、Q72 で WPF カバレッジを先行に改訂           |
| Q72 | Phase 7: マルチウィンドウ + WPF モーダル。Avalonia/Inspector は WPF カバレッジ後。詳細は `task_phase7.md` |
| Q73 | 窓はセッション内 `windowId`。List/Switch。メタ: title/automationId/isModal/isActive。既定ターゲット切替 |
| Q74 | ShowDialog 開封は `InvokeOpeningWindow`（BeginInvoke+出現待ち、既定自動 Switch）。素の Invoke は非対応 |
| Q75 | WaitForWindow は title および／または automationId。全窓マージツリー・OS ダイアログ実装は含めない   |
| Q76 | Phase 7 の次は OS ダイアログ方針 or 複雑 UI。Avalonia → Inspector は最後寄り                      |
| Q77 | Phase 8: DataGrid **行中心 MVP** + 同一 Phase 最終 Batch で `checked`。詳細は `task_phase8.md`     |
| Q78 | API は既存 `scrollIntoView` / `select`（ホスト＋index）。新 wire なし。Sample は FullRow+Single のみ |
| Q79 | ツリーは実現済み `DataGridRow` + `selected`。行に安定 automationId。セル座標／編集／ソートは含めない |
| Q80 | 公開は既存 Scenario ステップの薄い E2E。DataGrid 専用 MCP は作らない                               |
| Q81 | Phase 8 の次は DataGrid **セル R/W**。OS ダイアログ・Avalonia・Inspector はさらに後               |
| Q82 | Phase 9: DataGrid **セル R/W**（Text 列）。詳細は `task_phase9.md`                                |
| Q83 | 指定はホスト＋(rowIndex, columnIndex)。API: GetCellText / SetCellValue / ExpectCellText + 同名 wire |
| Q84 | 書込は BeginEdit→値→CommitEdit。列は DataGridTextColumn のみ。ツリーに DataGridCell は出さない     |
| Q85 | Sample は FullRow+Single のまま編集可能 Text 列。Scenario/MCP 薄い追従                             |
| Q86 | Phase 9 の次は **OS 共通ダイアログ方針**。列キー／他列種は後続                                     |
| Q87 | Phase 10: OpenFile **Runtime シーム**（方針+MVP）。実 OS UIA はしない。詳細は `task_phase10.md`     |
| Q88 | アプリは素の `OpenFileDialog`。Harmony で `CommonItemDialog.RunDialog` を差し替え。業務コードに Graft API なし |
| Q89 | 事前 Arm（単一パス OK / Cancel、一回限り）。未アームは実ダイアログ。開封は `waitForNewWindow:false`   |
| Q90 | Phase 10 の次は **SaveFile シーム**。Avalonia / Inspector は後ろ                                     |
| Q91 | Phase 11: SaveFile **Runtime シーム**（OpenFile 同型）。詳細は `task_phase11.md`                      |
| Q92 | 素の `SaveFileDialog`。同一 `CommonItemDialog.RunDialog` パッチ。`SaveFileArm` は OpenFile と独立      |
| Q93 | `ArmSaveFile` / `ArmSaveFileCancel`、一回限り、`waitForNewWindow:false`。Scenario/MCP 薄い追従        |
| Q94 | Phase 11 の次は **Folder シーム**。Avalonia / Inspector は後ろ                                        |
| Q95 | Phase 12: OpenFolder **Runtime シーム**（Open/Save 同型）。詳細は `task_phase12.md`                   |
| Q96 | 素の `OpenFolderDialog`。同一 `RunDialog` パッチ。結果は `FolderName`。`OpenFolderArm` は独立         |
| Q97 | `ArmOpenFolder` / `ArmOpenFolderCancel`、一回限り、`waitForNewWindow:false`。Scenario/MCP 薄い追従    |
| Q98 | Phase 12 の次は **MessageBox シーム**。Avalonia / Inspector は後ろ                                    |
| Q99 | Phase 13: MessageBox **Runtime シーム**。詳細は `task_phase13.md`                                      |
| Q100 | 素の `MessageBox.Show`。Harmony で主要オーバーロードを差し替え。業務コードに Graft API なし          |
| Q101 | `ArmMessageBox(result)`（OK/Cancel/Yes/No/None）、一回限り、`waitForNewWindow:false`。Scenario/MCP   |
| Q102 | Phase 13 の次は当初 Avalonia だったが、WPF 競合ギャップ埋めを優先（Q103）                            |
| Q103 | Avalonia を後ろへ。Phase 14 は **キー chord**。次は Screenshot → 右クリック/Menu → … → Avalonia       |
| Q104 | `PressAsync` / wire `pressKeys`。`sendKeys` はリテラルのまま。1 呼び出し = 1 chord、フォーカス付き   |
| Q105 | DSL: `Control`/`Alt`/`Shift` + `A`–`Z`/`0`–`9`/Enter/Tab/Escape/Backspace/Delete/Space/Arrow*      |
| Q106 | Sample E2E: TextBox SetValue → Control+A → Delete → Expect 空。詳細は `task_phase14.md`               |
| Q107 | Phase 15 は **公開 Screenshot**。Fluent 戻りは meta+bytes の `Screenshot` + `SaveAsync`               |
| Q108 | 対象は現在ターゲット窓のみ。Scenario は path 必須。MCP は path 任意（省略時 temp）                    |
| Q109 | E2E: Fluent PNG シグネチャ+size / Scenario path 書き。画像 diff・要素クリップは含めない。`task_phase15.md` |
| Q110 | Phase 16: `RightClickAsync` + 開いた ContextMenu をツリーに載せ MenuItem は既存 `invoke`               |
| Q111 | 実装は SendInput 右クリック + flush。待ちは呼び出し側。Menu バー/サブメニューは含めない。`task_phase16.md` |
| Q112 | Phase 17 は **TabControl** のみ。既存 `SelectAsync(index)` 拡張。ExpectSelected + StatusText。`task_phase17.md` |
| Q113 | Scenario は既存 `select`。MCP 変更なし。Slider / 複数選択 / ヘッダー指定は含めない                              |
| Q114 | Phase 18 は **Slider のみ**。既存 `SetValueAsync` / `setValue`。InvariantCulture double → `Slider.Value`。`task_phase18.md` |
| Q115 | 検証は StatusText 副作用のみ（tree `value` なし）。Scenario 既存 `setValue`。MCP 変更なし。複数選択は含めない   |
| Q116 | Phase 19: ListBox のみ。新 `SelectManyAsync` / wire `selectMany`（置換、空 indexes=クリア）。`task_phase19.md`          |
| Q117 | Sample は別 `SampleMultiList`（Extended）。Single はエラー。ExpectSelected + StatusText。Scenario/MCP 薄い追従          |
| Q118 | Phase 20: Menu バー。既存 `invoke` のみ。トップ+1段サブ。開いたサブをツリーに。`task_phase20.md`                        |
| Q119 | Sample File→Ping。Scenario 既存 `invoke`。MCP 変更なし。任意深さ／パス DSL／新 wire は含めない                          |
| Q120 | Phase 21: 列キーは Header（Ordinal）。wire `column` xor `columnKey`。CheckBox は `"True"`/`"False"`。`task_phase21.md` |
| Q121 | SampleGrid に Active CheckBox 列。Scenario/MCP 薄い追従。複数行選択・Template 列は含めない                                |
| Q122 | Phase 22 予定: DataGrid 複数行選択（`selectMany` 拡張）                                                                  |
