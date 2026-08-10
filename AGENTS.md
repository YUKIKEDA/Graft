# Graft — Agent notes

In-process UI testing for WPF & AvaloniaUI. Design source of truth: `.dev/project.md`. M0/M1/M2: `.dev/task_m0.md` / `task_m1.md` / `task_m2.md`. Phase 2–16: `.dev/task_phase2.md` … `task_phase16.md`.

**Consumer usage example:** `tests/sample-apps/SampleWpfApp.Tests` (see `.dev/graft-core.md`).

## Tooling (must follow)

| Concern | Choice | Details |
| -------- | ------ | ------- |
| Commits | Conventional Commits | `.cursor/rules/conventional-commits.mdc` (`alwaysApply`) |
| Pull requests | GitHub template + rules | `.github/pull_request_template.md`, `.cursor/rules/pull-requests.mdc` |
| Shell | PowerShell (Windows) | `.cursor/rules/powershell-shell.mdc` (`alwaysApply`); skill: `.cursor/skills/powershell-git/` |
| Formatter | CSharpier | `.config/dotnet-tools.json`, `.csharpierrc.json`, format on save via `.vscode/` |
| Linter | StyleCop.Analyzers | `Directory.Build.props`, `stylecop.json`, `.editorconfig` (warnings for now) |
| XML docs | Required on `src/**` public API | Warning via StyleCop; `GenerateDocumentationFile` in `src/Directory.Build.props` |
| Test docs | Required on Fact/Theory methods | `.cursor/rules/testing.mdc` — `summary` + `remarks` (Preconditions/Steps/Expected); no Analyzer yet |

## Quick commands

```bash
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx
dotnet test tests/sample-apps/SampleWpfApp.Tests
# Full solution: SendInput UI tests can flake under cross-assembly parallel launches.
# Prefer -m:1, or run UI projects sequentially. See .dev/project.md §9 (SendInput memo).
dotnet test Graft.slnx -m:1
```

## Commit style (summary)

- `type(optional-scope): subject`
- Types: `feat|fix|docs|style|refactor|test|chore|build|ci`
- Scope: English, recommended not required; examples only (not a closed list)
- Subject: Japanese OK; type/scope stay English
- Breaking: `!` and/or `BREAKING CHANGE:` footer

## C# style (summary)

- Format with CSharpier only; do not hand-warp layout against it
- StyleCop is warning-level overall
- **Public API in `src/`** must have XML docs (`summary` / `param` / `returns` as needed) — warning for now, escalate later
- **Tests (`tests/**`):** every Fact/Theory needs `summary` + `remarks` with `Preconditions` / `Steps` / `Expected` (English headings, Japanese body OK). Theory: one remarks block per method. See `.cursor/rules/testing.mdc`
- `tools/`, sample-apps: XML docs not required
- Escalate StyleCop / docs to errors later after the codebase settles

## Branches

- Prefer small batch branches (e.g. `m0/batch-1-sample-ui`, `chore/dev-tooling`)
- Solution file: `Graft.slnx` (classic `Graft.sln` is gitignored)

## Pull requests

- Title: Conventional Commits (`type(scope): subject`)
- Body: follow `.github/pull_request_template.md` (English headings; Japanese bullets OK)
- Agent rule: `.cursor/rules/pull-requests.mdc`

## Shell (Windows / PowerShell)

- Agent shell is PowerShell — **no bash heredoc** (`cat <<'EOF'`)
- Quote git upstream as `'@{u}'` (bare `@{u}` is a PowerShell hashtable)
- Multi-line commit/PR body: PowerShell here-string `@"..."@`
- Details: `.cursor/rules/powershell-shell.mdc` / skill `powershell-git`
