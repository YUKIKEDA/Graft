# Security Policy

Graft のエージェントは対象アプリ内で名前付きパイプを開きます。本番ビルドへの誤混入はセキュリティホールになるため、有効化は次の 3 段です。

1. コンパイル時: `GRAFT_TEST` 外では `Agent.Start` API 自体が存在しない
2. Analyzer: `GRAFT_TEST` 未定義での参照は **GRAFT001**（Error）
3. 実行時: `GRAFT_ENABLE=1` が無い限りパイプを立てない

`Application.LaunchAsync` がパイプ名・トークンを含む環境変数を付与します。テスト以外で `GRAFT_ENABLE` を立てないでください。

## 報告

脆弱性は **GitHub Security Advisories**（[Report a vulnerability](https://github.com/YUKIKEDA/Graft/security/advisories/new)）へお願いします。公開 Issue には、パイプ認証回避や本番混入の再現手順を書かないでください。

対応目安（ベストエフォート）:

- 受領確認: 数日以内
- 修正方針: 重大度に応じて
