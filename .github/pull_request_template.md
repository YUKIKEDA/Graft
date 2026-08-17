## Summary

<!-- What changed and why (Japanese OK). Keep it short. -->

-

## Related

<!-- Issue / M0 Batch / docs. Use N/A if none. -->

- Batch / task: <!-- e.g. M0 Batch 1 in `.dev/task_m0.md` -->
- Issue: <!-- e.g. #123 or N/A -->
- Docs: <!-- e.g. `.dev/project.md` / N/A -->

## Test plan

- [ ]
- [ ]

## Risk / Rollback

<!-- What could break, and how to revert. Use N/A if low risk. -->

- Risk:
- Rollback:

## Checklist

- [ ] PR title follows Conventional Commits (`type(scope): subject`)
- [ ] `dotnet build Graft.slnx` succeeds
- [ ] CSharpier applied to touched C# (format on save or `dotnet csharpier format`)
- [ ] No unintentional new StyleCop warnings in touched files
- [ ] New/changed tests include `summary` + `remarks` (Preconditions/Steps/Expected) — or N/A
- [ ] If M0 work: linked the relevant Batch in **Related** / `task_m0.md` updated if needed
- [ ] Hosted CI (`.github/workflows/ci.yml`) is green
- [ ] Docs updated when behavior or workflow changed (`AGENTS.md`, `.dev/*`, rules) — or N/A
