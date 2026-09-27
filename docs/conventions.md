English | [日本語](conventions.ja.md)

# Conventions

Coding details that agents also enforce live in [`.cursor/rules/csharp-tooling.mdc`](../.cursor/rules/csharp-tooling.mdc) and [`.cursor/rules/testing.mdc`](../.cursor/rules/testing.mdc). Product behavior lives in [`docs/design.md`](design.md).

## Layout

- Solution file: **`Graft.slnx`**. Do not commit `Graft.sln` (it is gitignored)
- Libraries stay split under `src/`: `Graft.Core`, `Graft.Protocol`, `Graft.Instrumentation`, `Graft.Instrumentation.Wpf`, `Graft.Instrumentation.Analyzer`, `Graft.McpServer`, `Graft.TestUtilities`. Do not collapse them into one project
- Tests stay under `tests/`. Sample apps stay under `tests/sample-apps/`
- `tools/Graft.SmokeClient` stays as the manual diagnostic client
- Lasting decisions go in `docs/`. `.dev/` is local scratch and is gitignored

## Targets and analyzers

- Libraries default to `net8.0`. WPF projects use `net8.0-windows`
- Nullable is enabled. `TreatWarningsAsErrors` is false. StyleCop stays at warning severity
- Format with CSharpier (`dotnet csharpier`). The local tool is pinned in `.config/dotnet-tools.json`
- Public API under `src/` needs XML docs (`summary`, and `param` / `returns` when they apply). `tools/` and sample apps do not
- Every Fact and Theory has `summary` and `remarks` with the headings Preconditions, Steps, and Expected, in English. One remarks block per Theory method

## Verification

- Windows, before a PR that can affect the build or tests: `./build.ps1` (tool restore, CSharpier check, build `Graft.slnx`, unit tests that do not launch an app). Pack is not part of this script
- Launched-app and SendInput tests: `dotnet test Graft.slnx -m:1`. They are not part of `./build.ps1`
- Hosted CI is `.github/workflows/ci.yml`. Interactive UI CI is `.github/workflows/ui.yml` and stays opt-in

## Language in samples

Sample-app UI strings, XAML, and fixture JSON may stay Japanese. They are the app under test. Win32 dialog names stay as the OS shows them. Explain Japanese test data with an English comment. See [`docs/language.md`](language.md).
