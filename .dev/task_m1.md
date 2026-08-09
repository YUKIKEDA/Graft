# M1 タスク分解

受け入れ条件（要約）: M0 に加え、(1) ウィンドウ PNG スクショ（JSON メタ + 後続 raw フレーム）、(2) `invoke` で `SampleButton` クリック → `StatusText` が変化、(3) `Graft.props` / `Graft.targets`（`GraftTest=true` → `GRAFT_TEST`）。  
`setValue` は M1 期間に追加可（受け入れの必須線ではない）。Core Launch / xUnit は **M2**。  
参照: [project.md](./project.md) section 7（M1）。前マイルストーン: [task_m0.md](./task_m0.md)。

レビュー負荷を抑えるため **Batch 単位**で進める。1 Batch = 小さめの差分。

---

## Batch 0 — `Graft.props` / `Graft.targets`（ブランチ: `m1/batch-0-props-targets`）

- [x] `Graft.Instrumentation.Wpf` に `build/Graft.props` / `build/Graft.targets` を同梱（NuGet は `buildTransitive/Graft.Instrumentation.Wpf.*`）
- [x] 有効化の正本: プロパティ `GraftTest=true`（`/p:GraftTest=true` または csproj）→ `DefineConstants` に `GRAFT_TEST`
- [x] Debug 構成への自動紐づけはしない（project.md Q55）
- [x] SampleWpfApp のローカル Define をやめ、明示 Import + Configuration=`GraftTest`→`GraftTest=true` に寄せる
- [x] 利用例: [graft-msbuild.md](./graft-msbuild.md)

**完了条件:** Sample を `-p:GraftTest=true` または `-c GraftTest` でビルドすると `GRAFT_TEST` が付き、Agent API が使える。  
**確認:** `dotnet build tests/sample-apps/SampleWpfApp -p:GraftTest=true`  
**レビューポイント:** MSBuild 断片・Sample の移行だけ。プロトコル変更なし。  
**次:** レビュー OK なら Batch 1（Screenshot）へ。

---

## Batch 1 — Screenshot（プロトコル + WPF 取得）

- [ ] wire method: `screenshot`（camelCase、既存 `handshake` / `getTree` に合わせる）
- [ ] 成功時: JSON 応答（メタ: 例 `format=png`, `width`, `height`, `byteLength` 等）の **直後** に raw バイナリフレーム（PNG バイト）（project.md Q17/Q18）
- [ ] デフォルト対象: メインウィンドウ全体。JPEG / 要素クロップは API 口だけ先に空けても実装は後回し可
- [ ] UI ディスパッチャへマーシャリングして取得（WPF: `RenderTargetBitmap` 等）
- [ ] 単体 / STA テストで「メタ + raw が読める・PNG シグネチャがある」を確認

**完了条件:** Handshake 後 `screenshot` で PNG raw が取れる。  
**確認:** `dotnet test tests/Graft.Instrumentation.Wpf.Tests`（または Screenshot 専用テスト）

---

## Batch 2 — 要素解決（invoke の前提）

- [ ] `invoke` / 将来の `setValue` が共有するセレクタ解決（当面: `automationId` 必須。`runtimeId` は任意）
- [ ] 見つからない → `element.notFound`、同点複数は将来 `element.ambiguous`（M1 は automationId 一意前提でよい）
- [ ] GetTree と同一の Visual Tree 走査口を再利用（二重実装を避ける）
- [ ] テスト: SampleButton / 存在しない ID

**完了条件:** automationId で要素を解決できる（内部 API または薄い wire でも可。公開 method は Batch 3 で `invoke` に載せてもよい）。  
**確認:** Instrumentation.Wpf.Tests

---

## Batch 3 — `invoke`（ボタンクリック）

- [ ] wire method: `invoke`、params にセレクタ（例 `{ "automationId": "SampleButton" }`）
- [ ] WPF Button: ネイティブ（`IInvokeProvider` / `RaiseEvent` 等）優先。失敗時 Peer → SendInput は口だけ or 最小フォールバック
- [ ] ディスパッチャへマーシャリング。操作パイプラインは直列（既存パイプループで十分）
- [ ] actionable でない場合は `element.notActionable`（簡易: `IsEnabled` / `IsVisible`）
- [ ] テスト: invoke 後に `StatusText` の name（または Text）が `Clicked 1` になることを GetTree で確認

**完了条件:** Handshake → invoke(SampleButton) → GetTree で StatusText 変化が検証できる。  
**確認:** `dotnet test tests/Graft.Instrumentation.Wpf.Tests`

---

## Batch 4 — SmokeClient M1 受け入れパス

- [ ] Launch 経路を拡張: Handshake →（任意）Screenshot → invoke(SampleButton) → GetTree で StatusText 確認
- [ ] Screenshot をファイルに保存するオプション（例 `--screenshot-out <path>`）。デフォルトは一時ファイル or 検証のみでも可
- [ ] 成功時 exit 0、失敗時は安定 `GraftErrorCodes` を stderr に出して非 0
- [ ] Connect でも同じ操作列が使えること（起動だけ Launch 固有）

**完了条件:** SmokeClient Launch 一本で「スクショ取得 + クリックで TextBlock 変化」を再現できる。  
**確認:** `dotnet run --project tools/Graft.SmokeClient -- launch`（M1 フラグ or デフォルトで invoke まで実行）

---

## Batch 5 — `setValue`（M1 期間・推奨）

- [ ] wire method: `setValue`、TextBox（`SampleTextBox`）へ文字列置換
- [ ] ネイティブ代入優先、失敗時クリア + SendInput（project.md Q51）。append / typeHuman は後付け
- [ ] テスト: setValue → GetTree（または再読取）で値が一致
- [ ] SmokeClient に任意サブコマンド or Launch オプションで 1 往復追加（必須ではない）

**完了条件:** SampleTextBox に値をセットして読める。  
**確認:** Wpf.Tests /（任意）SmokeClient

---

## M1 完了チェック（全 Batch 後）

- [ ] `GraftTest=true` / props・targets で `GRAFT_TEST` が付く
- [ ] Handshake 後 `screenshot` でウィンドウ PNG（メタ+raw）が取れる
- [ ] `invoke` で SampleButton クリック → StatusText が変化する
- [ ] SmokeClient Launch で上記を再現できる
- [ ] Core Launch / xUnit / Avalonia / Scenario / MCP は **含めない**（M2 以降）
- [ ] （推奨）`setValue` が動く

---

## 進め方メモ

- 実装は **Batch 0 → 1 → …** の順。次 Batch に進む前にレビューしやすいサイズで止める
- 設計の正本は `project.md`。矛盾したら project.md を優先して task を直す
- M0 のパイプ・Handshake・GetTree・Analyzer は前提として壊さない
- wire method 名は camelCase 継続（`screenshot`, `invoke`, `setValue`）
- セレクタの本格スコアリングは M2（Core）。M1 は `automationId`（+ 任意 `runtimeId`）で足りる

---

## 未決で Batch 中に固定してよいこと（小さめ）

| 項目 | 仮決め（実装時に task / コードで確定） |
| ---- | -------------------------------------- |
| screenshot メタ JSON フィールド | `format`, `width`, `height`, `byteLength` を最小セット |
| invoke / setValue の params | `{ "automationId": "..." }` を第一選択。`runtimeId` は任意 |
| SmokeClient M1 既定動作 | Launch が GetTree に加え invoke（+ 任意 screenshot）まで行う |
| props の配置 | `Graft.Instrumentation.Wpf` の `buildTransitive` / `build` に同梱（Avalonia は後で同様） |
