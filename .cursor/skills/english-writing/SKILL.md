---
name: english-writing
description: >-
  Write Graft comments, XML docs, test names and Preconditions/Steps/Expected remarks,
  exception messages, and docs in English, with the repository's terminology.
  Use when writing or editing XML docs, inline comments, test names or remarks,
  exception messages, README or design wording, when translating between the English
  docs and their *.ja.md translations, or when a human or bot review flags wording.
---

# Writing English in Graft

Read this before writing. When a review corrects wording, add the general rule here in the same change. Do not leave it in chat. Do not repeat a finding that is already written here.

The language policy is [`docs/language.md`](../../../docs/language.md) and [`.cursor/rules/language.mdc`](../../rules/language.mdc).

## Sentence shape

- Plain, direct English. Prefer short sentences and common words
- XML doc elements are full sentences and end with a period (StyleCop SA1629)
- Summaries start with a verb in the third person: "Returns ...", "Throws ...", "Reads ..."
  - Properties: "Gets ..." or "Gets or sets ..." (SA1623)
  - Constructors: "Initializes a new instance of the <see cref="T"/> class." (SA1642)
  - Boolean members: "Gets a value indicating whether ..."
- Write `<see langword="true"/>`, `<see langword="false"/>`, `<see langword="null"/>` for keywords in XML docs
- Refer to types and members with `<see cref="..."/>` and parameters with `<paramref name="..."/>`
- Keep the subject the same across a method name, its summary, and its remarks
- Inline comments explain why, not what the next line does

## Test names and remarks

- Method names: `{Target}_{Scenario}_{Expected}` in PascalCase
- Remarks use the headings Preconditions, Steps, and Expected. See [`.cursor/rules/testing.mdc`](../../rules/testing.mdc)
- Write the remarks in English. Name the actual input and the actual check

## Exception messages

- One sentence. Name the condition that failed
- Use the same terms as the design and the XML docs

## Terminology

Use these words. The left column is the Japanese concept used in the current design notes. The right column is the English to write in XML docs, tests, and the English docs.

| Concept                  | Write          |
| ------------------------ | -------------- |
| 対象アプリ               | app under test |
| 事前組み込み、in-process | in-process     |
| 自動化 ID、AutomationId  | automation id  |
| ビジュアルツリー         | visual tree    |
| 失敗レポート             | failure report |
| ソフトアサーション       | soft assert    |
| ツリー差分               | tree diff      |
| シナリオ                 | scenario       |
| 名前付きパイプ           | named pipe     |

Words to avoid:

| Avoid                                          | Write                                            |
| ---------------------------------------------- | ------------------------------------------------ |
| "fails" for throwing                           | "throws `GraftException`" (or the concrete type) |
| "the UI" when the tree or one element is meant | "visual tree" or "element"                       |

## Writing the Japanese translations

The Japanese `*.ja.md` files use the Japanese column of the terminology table above. Use plain form (常体), natural Japanese, and keep identifiers and code in English. The vendored skill [`.agents/skills/natural-japanese`](../../../.agents/skills/natural-japanese/SKILL.md) can check the wording.

## After a review

1. Before applying a suggested replacement, look for similar sentences elsewhere
2. If the finding is not covered above, add it here as a general rule, not as one sentence
3. Then run `./build.ps1` when the change can affect the build or tests
