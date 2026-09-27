English | [日本語](CONTRIBUTING.ja.md)

# Contributing

Thanks for contributing to Graft. The design source is [`docs/design.md`](docs/design.md). Agent notes are in [`AGENTS.md`](AGENTS.md).

## Requirements

- Windows (an interactive desktop session)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Do not lock the screen for UI / SendInput tests

## Build and test

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx

# Hosted CI equivalent (no launched app, no pack)
./build.ps1

# Full solution. SendInput flakes when apps launch in parallel, so -m:1 is required
dotnet test Graft.slnx -m:1
```

## Issues

Open every Issue from a template (blank Issues are disabled). Each template adds one label.

Labels use the form `<axis>:<value>` (for example `type:bug`). The only axis today is `type`. A new axis, such as `priority:` or `area:`, uses the same form.

| Template   | Label           | Use for                                               | Commit / PR type         |
| ---------- | --------------- | ----------------------------------------------------- | ------------------------ |
| `bug`      | `type:bug`      | A defect in behavior that is already specified        | `fix`                    |
| `feat`     | `type:feat`     | A new capability callers can use                      | `feat`                   |
| `refactor` | `type:refactor` | Internal restructuring with no change callers can see | `refactor`               |
| `test`     | `type:test`     | Test-only work: coverage, structure, helpers          | `test`                   |
| `design`   | `type:docs`     | A `docs/design.md` change only (no implementation)    | `docs`                   |
| `task`     | `type:chore`    | Tooling, conventions, CI, or other documentation      | `chore` / `build` / `ci` |

- A `task` Issue that only changes documentation switches its label to `type:docs`
- A tracking Issue that groups several Issues uses the label of its sub-issues and links them as sub-issues

## Pull requests

1. Conventional Commits title (`type(scope): subject`, subject in English)
2. Keep the headings in [`.github/pull_request_template.md`](.github/pull_request_template.md)
3. Leave the tree formatted (`dotnet csharpier format .`)
4. XML docs on public API under `src/`
5. New Fact / Theory methods have `summary` + `remarks` (Preconditions / Steps / Expected)
6. The GitHub-hosted **CI** workflow is green
7. In `## Related`, put `Closes #N` on its own line. Open an Issue before the branch (`type/<issue-number>-<slug>`)

## CI

| Workflow                                 | Runner                 | What it does                                                                                                                       |
| ---------------------------------------- | ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| [`ci.yml`](.github/workflows/ci.yml)     | `windows-latest`       | Format, build, pack, and tests that do not launch an app (including PRs)                                                           |
| [`pack.yml`](.github/workflows/pack.yml) | `windows-latest`       | On a `v*` tag, pack the `GraftTest` configuration and push to NuGet.org when the `NUGET_API_KEY` secret is set                     |
| [`ui.yml`](.github/workflows/ui.yml)     | self-hosted (optional) | `dotnet test Graft.slnx -m:1`. Push to `main` or manual. **Does not run on a fork PR** (self-hosted safety on a public repository) |

Graft does not provide a virtual display. Hosted Windows runners do not gate E2E that needs SendInput or a foreground window ([`docs/design.md`](docs/design.md) Q27).

### Turn on the self-hosted UI job

1. On a Windows user who is logged on interactively, start a [self-hosted runner](https://docs.github.com/en/actions/hosting-your-own-runners) as a **user process, not a service** (Session 0 cannot do this)
2. Add the labels `windows` and `interactive` (`self-hosted` is automatic)
3. Turn off the screen lock and the screensaver
4. Set the repository Actions variable `GRAFT_ENABLE_UI_CI` to `true`

When the variable is absent, `ui.yml` skips. `workflow_dispatch` needs the same runner.

## NuGet

Published packages share the `Version` in `src/Directory.Build.props` (currently 0.1.0). Pushing a tag such as `v0.1.0` (`v` plus that version) makes [pack.yml](.github/workflows/pack.yml) pack with `Configuration=GraftTest` and push when the repository secret `NUGET_API_KEY` (a nuget.org API key) is set. A tag push without the secret stops after pack.

Local check:

```powershell
dotnet pack src/Graft.Core/Graft.Core.csproj -c GraftTest -o artifacts
```
