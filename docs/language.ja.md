[English](language.md) | 日本語

# 言語方針

nuget.org から Graft を入れる人には、日本語を読まない人がいる。このページは、リポジトリのどこをどの言語で書くかと、日本語訳を英語の正本にどう揃えるかを決める。

常時適用のエージェントルールは [`.cursor/rules/language.mdc`](../.cursor/rules/language.mdc)。英語の書き方は [`.cursor/skills/english-writing/SKILL.md`](../.cursor/skills/english-writing/SKILL.md)。

## ルール

**リポジトリの言語は英語である。** コミットするものは英語で書く。例外は、下の対訳と、このページの例外だけである。

| 領域                                                                              | 言語                  |
| --------------------------------------------------------------------------------- | --------------------- |
| コード: 識別子、XML ドキュメント、行コメント、例外メッセージ                      | 英語                  |
| テスト: メソッド名、remarks、コメント、アサーションとスキップのメッセージ         | 英語                  |
| `docs/`（`*.ja.md` を除く）、`AGENTS.md`、`SECURITY.md`                           | 英語                  |
| `.cursor/rules/`、`.cursor/skills/`、`.github/`（テンプレート、workflow）         | 英語                  |
| コミットメッセージ、PR のタイトルと本文、Issue のタイトルと本文、レビューコメント | 英語                  |
| 対訳文書（下）                                                                    | 英語の正本 + 日本語訳 |

チャットやメンテナとの会話は、どの言語でもよい。コミットするものと GitHub に出すものは、この表に従う。

## 対訳する文書

よく開く文書には、英語の正本の隣に日本語訳を置く。

| 英語（正本）                            | 日本語訳                                      |
| --------------------------------------- | --------------------------------------------- |
| [`README.md`](../README.md)             | [`README.ja.md`](../README.ja.md)             |
| [`CONTRIBUTING.md`](../CONTRIBUTING.md) | [`CONTRIBUTING.ja.md`](../CONTRIBUTING.ja.md) |
| [`docs/design.md`](design.md)           | [`docs/design.ja.md`](design.ja.md)           |
| [`docs/roadmap.md`](roadmap.md)         | [`docs/roadmap.ja.md`](roadmap.ja.md)         |
| [`docs/conventions.md`](conventions.md) | [`docs/conventions.ja.md`](conventions.ja.md) |
| [`docs/language.md`](language.md)       | [`docs/language.ja.md`](language.ja.md)       |

- 日本語ファイルは、同じフォルダで拡張子の前に `.ja` を付けた名前にする
- どちらも先頭に言語の切り替え行を置く。英語側は `English | [日本語](X.ja.md)`、日本語側は `[English](X.md) | 日本語`
- **正本は英語である。** 食い違ったら英語が勝ち、直すのは日本語側である
- 見出しの順序は同じにする。片方の節が、もう片方で見つかるようにするためである。コードブロック、識別子の表、リンクは両方で同じにする
- NuGet パッケージが使うのは `README.md`（英語）だけである

### 対訳を増やすとき

上の表に足すのは、多くの人が開く文書だけである。同じ PR で `docs/language.md` と `docs/language.ja.md` の両方に行を足す。それ以外の文書は英語だけである。`docs/graft-core.md`、`docs/competitive-gap.md`、`docs/graft-msbuild.md` は英語だけである。

## 訳を揃える

- **両方を同じ PR で変える。** 対訳の英語を変える PR は、日本語側も変える。逆も同じである
- 日本語側を書けないときは、次の両方を満たす場合だけ省略してよい
  - PR 本文の `## Summary` にその旨を書く
  - フォローの Issue を `docs: sync <file>.ja.md` というタイトルで開き、`## Related` からリンクする
  日本語側がリリースをまたいで遅れたままにしない
- 日本語ファイルだけの変更（訳を良くする）は、意味を変えてはならない。意味を変えるなら、先に英語を変える
- PR のチェックリストにこの項目がある。レビューでは、両方変わったかを見る

## 日本語訳の書き方

- 語ではなく意味を訳す。常体で、自然な日本語にする
- 識別子、API 名、ファイルパス、コードはそのまま英語で残す
- 同梱スキル [`.agents/skills/natural-japanese`](../.agents/skills/natural-japanese/SKILL.md) で文言を確認できる
- 用語の対応表は [`.cursor/skills/english-writing/SKILL.md`](../.cursor/skills/english-writing/SKILL.md) の末尾にある

## 例外

日本語を書いてよいのは、次だけである。

- 上の表の `*.ja.md`
- `.agents/skills/natural-japanese/`（第三者のスキル。`skills-lock.json` で管理し、手で編集しない）
- 執筆スキルの用語表と、この方針の中の例
- テスト対象アプリが表示する、サンプルアプリの UI 文字列、XAML、フィクスチャ JSON
- OS の表示と一致する Win32 ダイアログのアクセシブル名（例: `ファイル名(N):`）
- それ以外で、わざと非 ASCII にするテストデータ。理由を隣のコメントに書く
