---
name: powershell-git
description: >-
  Run git commit, push, and GitHub PR creation safely in PowerShell on Windows.
  Use when committing, pushing, creating PRs with gh, or writing multi-line
  commit/PR bodies in this repo (avoids bash heredoc and @{u} pitfalls).
---

# PowerShell git / gh

## When to use

Any `git commit`, `git push`, or `gh pr create` in this repository on Windows.

## Rules

1. Read `.cursor/rules/powershell-shell.mdc`, `.cursor/rules/conventional-commits.mdc`, and `.cursor/rules/pull-requests.mdc`.
2. Never use bash heredoc (`cat <<'EOF'`).
3. Always quote `'@{u}'` if you need upstream ref.
4. Draft Conventional Commits messages (`type(scope): subject`; Japanese subject OK).
5. PR body must match `.github/pull_request_template.md` section headings.

## Commit sequence

```powershell
git status -sb
git diff
git diff --cached
git log -5 --oneline

git add <paths>
git commit -m @"
type(scope): subject

optional body
"@
git status -sb
```

## Push + PR sequence

```powershell
git push -u origin HEAD

$body = @"
## Summary
- point 1
- point 2

## Related
- Batch / task: N/A
- Issue: N/A
- Docs: N/A

## Test plan
- [ ] check 1
- [ ] check 2

## Risk / Rollback
- Risk: N/A
- Rollback: N/A

## Checklist
- [ ] PR title follows Conventional Commits (`type(scope): subject`)
- [ ] `dotnet build Graft.slnx` succeeds
- [ ] CSharpier applied to touched C# (format on save or `dotnet csharpier format`)
- [ ] Hosted CI (`.github/workflows/ci.yml`) is green
- [ ] No unintentional new StyleCop warnings in touched files
- [ ] If M0 work: linked the relevant Batch in **Related** / `task_m0.md` updated if needed
- [ ] Docs updated when behavior or workflow changed (`AGENTS.md`, `.dev/*`, rules) — or N/A
"@
gh pr create --title "type(scope): subject" --body $body
```

## Do not

- Mix bash and PowerShell in one script block expecting bash semantics
- Pass unquoted `@{u}` to git in PowerShell
- Skip Conventional Commits type prefix
- Use PR section headings other than the template
