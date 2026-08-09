# Graft — Agent notes

In-process UI testing for WPF & AvaloniaUI. Design source of truth: `.dev/project.md`. M0 breakdown: `.dev/task_m0.md`.

## Tooling (must follow)

| Concern | Choice | Details |
| -------- | ------ | ------- |
| Commits | Conventional Commits | `.cursor/rules/conventional-commits.mdc` (`alwaysApply`) |
| Formatter | CSharpier | `.config/dotnet-tools.json`, `.csharpierrc.json`, format on save via `.vscode/` |
| Linter | StyleCop.Analyzers | `Directory.Build.props`, `stylecop.json`, `.editorconfig` (warnings for now) |

## Quick commands

```bash
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx
```

## Commit style (summary)

- `type(optional-scope): subject`
- Types: `feat|fix|docs|style|refactor|test|chore|build|ci`
- Scope: English, recommended not required; examples only (not a closed list)
- Subject: Japanese OK; type/scope stay English
- Breaking: `!` and/or `BREAKING CHANGE:` footer

## C# style (summary)

- Format with CSharpier only; do not hand-warp layout against it
- StyleCop is warning-level; XML docs not required yet
- Escalate StyleCop to errors later after the codebase settles

## Branches

- Prefer small batch branches (e.g. `m0/batch-1-sample-ui`, `chore/dev-tooling`)
- Solution file: `Graft.slnx` (classic `Graft.sln` is gitignored)
