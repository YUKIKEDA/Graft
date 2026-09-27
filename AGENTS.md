# Graft — Agent notes

In-process UI testing for WPF. Design source of truth: `docs/design.md`. Open work: `docs/roadmap.md`. Archived phase notes: `.dev/task_*.md`. WPF gap matrix: `docs/competitive-gap.md`.

**Consumer usage example (canonical):** `tests/sample-apps/SampleTodoApp.Tests` (see `docs/graft-core.md`). Feature matrix: `SampleWpfApp.Tests`. WPF UI Gallery deep E2E (local `tests/wpfui`): `.dev/task_wpfui-gallery-e2e.md`.

## Tooling (must follow)

| Concern       | Choice                          | Details                                                                                                                                                                     |
| ------------- | ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Commits       | Conventional Commits            | `.cursor/rules/conventional-commits.mdc` (`alwaysApply`)                                                                                                                    |
| Pull requests | GitHub template + rules         | `.github/pull_request_template.md`, `.cursor/rules/pull-requests.mdc`                                                                                                       |
| Shell         | PowerShell (Windows)            | `.cursor/rules/powershell-shell.mdc` (`alwaysApply`); skill: `.cursor/skills/powershell-git/`                                                                               |
| Formatter     | CSharpier                       | `.config/dotnet-tools.json`, `.csharpierrc.json`, format on save via `.vscode/`                                                                                             |
| CI            | GitHub Actions                  | `.github/workflows/ci.yml` (hosted: format/build/unit). Full UI: `.github/workflows/ui.yml` self-hosted interactive, opt-in via `GRAFT_ENABLE_UI_CI`. See `CONTRIBUTING.md` |
| Linter        | StyleCop.Analyzers              | `Directory.Build.props`, `stylecop.json`, `.editorconfig` (warnings for now)                                                                                                |
| XML docs      | Required on `src/**` public API | Warning via StyleCop; `GenerateDocumentationFile` in `src/Directory.Build.props`                                                                                            |
| Test docs     | Required on Fact/Theory methods | `.cursor/rules/testing.mdc` — `summary` + `remarks` (Preconditions/Steps/Expected); no Analyzer yet                                                                         |

## Quick commands

```bash
# Hosted CI equivalent (no launched-app E2E, no pack):
./build.ps1

dotnet test tests/sample-apps/SampleWpfApp.Tests
# Full solution: SendInput UI tests flake under cross-assembly parallel launches.
# Required: -m:1 (or run UI projects sequentially). See docs/design.md §9 / .dev/task_phase31.md.
dotnet test Graft.slnx -m:1
```

## Commit style (summary)

- `type(optional-scope): subject`
- Types: `feat|fix|docs|style|refactor|test|chore|build|ci`
- Scope: English, recommended not required; examples only (not a closed list)
- Subject: English; type and scope stay English
- Breaking: `!` and/or `BREAKING CHANGE:` footer

## C# style (summary)

- Format with CSharpier only; do not hand-warp layout against it
- StyleCop is warning-level overall
- **Public API in `src/`** must have XML docs (`summary` / `param` / `returns` as needed) — warning for now, escalate later
- **Tests (`tests/**`):** every Fact/Theory needs `summary` + `remarks` with `Preconditions` / `Steps` / `Expected` in English. Theory: one remarks block per method. See `.cursor/rules/testing.mdc`
- `tools/`, sample-apps: XML docs not required
- Escalate StyleCop / docs to errors later after the codebase settles

## Branches

- `type/<issue-number>-<slug>` after an Issue exists (`.cursor/rules/workflow.mdc`)
- Solution file: `Graft.slnx` (classic `Graft.sln` is gitignored)

## Pull requests

- Title: Conventional Commits (`type(scope): subject`)
- Body: follow `.github/pull_request_template.md` (English). `## Related` includes `Closes #N` on its own line
- Agent rule: `.cursor/rules/pull-requests.mdc`

## Shell (Windows / PowerShell)

- Agent shell is PowerShell — **no bash heredoc** (`cat <<'EOF'`)
- Quote git upstream as `'@{u}'` (bare `@{u}` is a PowerShell hashtable)
- Multi-line commit/PR body: PowerShell here-string `@"..."@`
- Details: `.cursor/rules/powershell-shell.mdc` / skill `powershell-git`
