English | [日本語](README.ja.md)

# Graft — In-process UI testing for WPF & AvaloniaUI

[![CI](https://github.com/YUKIKEDA/Graft/actions/workflows/ci.yml/badge.svg)](https://github.com/YUKIKEDA/Graft/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

In-process GUI end-to-end tests for WPF and AvaloniaUI.

Graft embeds an agent in the app under test and reads the visual tree directly. It does not walk UI Automation (UIA) over COM the way FlaUI does. The goal is TestComplete-style Open Applications accuracy, limited to apps you own.

> **Status:** WPF on .NET 8+ works. The Avalonia adapter is not implemented. Packages are **0.1.0** (pre-1.0; the public API may break). Publish to NuGet.org happens on a `v*` tag. Until the first tag is pushed, a `ProjectReference` in this repository works too.

## Why in-process

| Tool                                                      | Access                                   | Target                                     |
| --------------------------------------------------------- | ---------------------------------------- | ------------------------------------------ |
| FlaUI / WinAppDriver / TestStack.White / Appium (Windows) | UIA (COM) wrapper                        | General Windows                            |
| TestComplete                                              | Per-framework in-process access (closed) | Commercial, many frameworks                |
| **Graft**                                                 | Embedded in the app under test           | **Your WPF / Avalonia apps, aimed at OSS** |

Black-box tests of a third-party exe are out of scope. The stance is the same as Playwright testing your own web app.

Graft does not inject a process (`CreateRemoteThread` + `LoadLibrary`). The app references the test package and opens a named pipe at startup.

## Requirements

- Windows (an interactive desktop session)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Do not lock the screen during UI tests (Session 0 and an off-screen-only mode are not supported)

## Try it in this repository

```powershell
dotnet tool restore
dotnet build Graft.slnx

# Canonical usage example (story E2E)
dotnet test tests/sample-apps/SampleTodoApp.Tests

# Control coverage and phase regression
dotnet test tests/sample-apps/SampleWpfApp.Tests

# Full solution. SendInput flakes when apps launch in parallel, so -m:1 is required
dotnet test Graft.slnx -m:1
```

Samples:

| Project                                 | Role                                                              |
| --------------------------------------- | ----------------------------------------------------------------- |
| `tests/sample-apps/SampleTodoApp`       | Real MVVM/DI/themed app (canonical embed side)                    |
| `tests/sample-apps/SampleTodoApp.Tests` | Story E2E with the fluent API (**canonical way to write a test**) |
| `tests/sample-apps/SampleWpfApp.Tests`  | Feature matrix                                                    |

The API notes are in [`docs/graft-core.md`](docs/graft-core.md).

## Embed Graft in your app

Two sides:

| Side           | Reference                   | What it does                                       |
| -------------- | --------------------------- | -------------------------------------------------- |
| App under test | `Graft.Instrumentation.Wpf` | Starts the agent only when `GRAFT_TEST` is defined |
| E2E project    | `Graft.Core` only           | Drives the app with `Application.LaunchAsync`      |

Published packages share **one version** (pair `Graft.Core` 0.1.0 with `Graft.Instrumentation.Wpf` 0.1.0). Wire compatibility is the integer `ProtocolVersion.Current`, separate from the NuGet version. A mismatch throws `protocol.versionMismatch`. The message includes both package versions and both protocol versions.

| Package                          | Who references it                                                                                                               |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Graft.Instrumentation.Wpf`      | The app under test. Brings `Graft.Instrumentation` and `Graft.Protocol`. The GRAFT001 DLL is packed under `analyzers/dotnet/cs` |
| `Graft.Instrumentation`          | A dependency of the WPF package. A direct reference still includes GRAFT001                                                     |
| `Graft.Instrumentation.Analyzer` | Packed on its own. Apps get it through the packages above                                                                       |
| `Graft.Core`                     | Tests. `Graft.Protocol` comes as a dependency                                                                                   |
| `Graft.TestUtilities`            | Tests (optional). Shared xUnit fixture                                                                                          |

```powershell
dotnet add package Graft.Instrumentation.Wpf --version 0.1.0
dotnet add package Graft.Core --version 0.1.0
```

NuGet `Graft.Instrumentation` and `Graft.Instrumentation.Wpf` are packed in the `GraftTest` configuration, so the DLL contains the agent and the WPF patches. The caller's `#if GRAFT_TEST` and GRAFT001 still follow the consuming project's symbol. A `ProjectReference` in this repository does not put the agent in Debug or Release output. References that need the agent use `Configuration=GraftTest`.

### 1. App under test (WPF)

A NuGet `PackageReference` imports props and targets. A direct reference in this repository looks like this:

```xml
<Import Project="path\to\src\Graft.Instrumentation.Wpf\build\Graft.props" />

<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="path\to\src\Graft.Instrumentation.Wpf\Graft.Instrumentation.Wpf.csproj" />
</ItemGroup>

<Import Project="path\to\src\Graft.Instrumentation.Wpf\build\Graft.targets" />
```

Startup (`App.xaml.cs` or the same place):

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

#if GRAFT_TEST
    Graft.Instrumentation.Wpf.WpfGraft.Use();
    Graft.Instrumentation.Agent.Start();
#endif
}

protected override void OnExit(ExitEventArgs e)
{
#if GRAFT_TEST
    Graft.Instrumentation.Agent.Stop();
#endif
    base.OnExit(e);
}
```

For a type the built-in map does not know, such as a commercial control, register an action with `WpfControlActions`. A handler that returns `true` finishes the action. `false` continues with native, then Peer, then SendInput. The most specific registered type wins. Graft does not ship Telerik, DevExpress, or Syncfusion implementations.

```csharp
#if GRAFT_TEST
Graft.Instrumentation.Wpf.WpfControlActions.RegisterInvoke<MyVendorGrid>(grid =>
{
    grid.CommitEdit();
    return true;
});
#endif
```

Enable it:

```powershell
dotnet build -p:GraftTest=true
# or the sample convenience configuration
dotnet build -c GraftTest
```

`GRAFT_TEST` is defined on the app and on `Graft.Instrumentation` / `Graft.Instrumentation.Wpf` only when `GraftTest=true` (or `-c GraftTest`). Debug and Release references do not include `Agent.Start`, the pipe server, or the WPF Harmony patches. Referencing `Agent.Start` in a compile that lacks `GRAFT_TEST` is analyzer error **GRAFT001**. There is no automatic tie to the Debug configuration.

At run time the pipe stays down unless `GRAFT_ENABLE=1`. `Application.LaunchAsync` sets that variable, including the pipe name and token.

### 2. Test side

The test project references only `Graft.Core`.

```csharp
using Graft.Core;

await using var app = await Application.LaunchAsync(
    new LaunchOptions
    {
        AppPath = @"path\to\YourApp.csproj",
        Configuration = "GraftTest",
        Timeout = TimeSpan.FromSeconds(60),
    }
);

await app.GetByAutomationId("SampleButton").InvokeAsync();
await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
```

A `.csproj` in `AppPath` launches the equivalent of `dotnet run -c GraftTest`. An exe path works too.

## Common actions

```csharp
// Find
app.GetByAutomationId("SaveButton");
app.GetByName("OK");
app.GetByControlType("Button");

// Act
await app.GetByAutomationId("NameBox").SetValueAsync("hello");
await app.GetByAutomationId("AgreeCheck").ToggleAsync();
await app.GetByAutomationId("ItemList").SelectAsync(0);
await app.GetByAutomationId("ItemList").SelectAsync("表示名");
await app.GetByAutomationId("FileMenu").SelectMenuAsync("id1/id2/leaf");

// A modal. A plain InvokeAsync that calls ShowDialog can hang
var detail = await app.GetByAutomationId("AddButton").InvokeOpeningWindowAsync();

// OS dialogs stay the app's normal API. The test arms them
await app.ArmOpenFileAsync(@"C:\data\import.json");
_ = await app.GetByAutomationId("ImportButton")
    .InvokeOpeningWindowAsync(waitForNewWindow: false);

// Expect
await app.GetByAutomationId("StatusText").ExpectNameAsync("Saved");
await app.GetByAutomationId("Row").WaitForAsync();
await app.WaitForWindowAsync(automationId: "Main");
```

Default timeouts are 5 seconds before an action, 10 seconds for Expect, and 30 seconds for launch plus handshake (`LaunchOptions` / `WaitOptions` override them).

On failure, `GraftException.Report` carries the step, expected value, selector, recent actions, tree, and a screenshot reference. When a previous action succeeded, it also carries `treeDiff` (added, removed, and changed). Set `GraftSession.IncludeTreeDiff = false` to omit it. If selector resolution fails, Graft retries once only when a trusted alternative exists.

Use `SoftAssert` to collect several Expect results in one run. `Check` stores only `GraftException`. Disposing the scope throws `expect.failed` once. The aggregate report's `failures` holds each `FailureReport`. Outside `Check`, the first failure still stops the test.

```csharp
await using (var soft = app.SoftAssert())
{
    await soft.Check(app.GetByAutomationId("StatusText").ExpectNameAsync("Saved"));
    await soft.Check(app.GetByAutomationId("CountText").ExpectNameAsync("1"));
}
```

`SetValueAsync` and `SendKeysAsync` do not wait between characters. Use `TypeHumanAsync` when a debounce must run during typing. It sends one Unicode scalar at a time and waits only between scalars. The wait is off the UI thread, so a `TextChanged` timer can run between characters. It does not clear existing text.

```csharp
await app.GetByAutomationId("SearchBox").TypeHumanAsync("tokyo", TimeSpan.FromMilliseconds(40));
```

### Action timeline (optional)

```csharp
Timeline = new TimelineOptions
{
    OutputDirectory = timelineDir,
    Retention = TimelineRetention.Always, // or OnFailure
}
```

After dispose, Graft writes `index.html` and `frames/*.png`.

## Architecture

```
[Graft.Core / Graft.McpServer]
   │  named pipe (same-user ACL)
   │  4-byte length prefix + JSON
   ▼
[inside the app: Graft.Instrumentation.Wpf]
   ├─ visual tree walker
   ├─ native API → Peer → SendInput
   └─ pipe server (only when GRAFT_ENABLE=1)
```

Three gates keep the agent out of production:

1. **Compile time:** the `Agent.Start` API does not exist outside `GRAFT_TEST`
2. **Analyzer:** a reference without `GRAFT_TEST` is GRAFT001 (error)
3. **Run time:** the pipe stays down unless `GRAFT_ENABLE=1`

### Known limits

- **Under WebView2 / `HwndHost`:** the walker follows the WPF visual tree, so an `HwndHost` such as `WebView2` is visible only as the host element. DOM and native child windows do not appear in `getTree` and cannot be driven with `GetByAutomationId`.
- **SendInput actions:** they inject input into a real desktop, so they need an interactive session and flake when several apps launch in parallel. A run that crosses test projects needs `-m:1`. Inside one test project, a `Graft.TestUtilities` collection can serialize the tests (below).

Shape for a test project that references `Graft.Core` and `Graft.TestUtilities`. Put the `CollectionDefinition` in the test assembly.

```csharp
public sealed class TodoAppFixture : Graft.TestUtilities.GraftAppFixture
{
    protected override Graft.Core.LaunchOptions CreateLaunchOptions() => new()
    {
        AppPath = @"path\to\App.csproj",
        Configuration = "GraftTest",
    };
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TodoAppCollection : ICollectionFixture<TodoAppFixture>
{
    public const string Name = "TodoApp";
}

[Collection(TodoAppCollection.Name)]
public sealed class TodoTests
{
    private readonly Graft.Core.GraftSession _session;

    public TodoTests(TodoAppFixture fixture) => _session = fixture.Session;
}
```

## Other entry points

The same internal action model also backs:

| Entry         | Where                                                | Use                                                                                      |
| ------------- | ---------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| Scenario JSON | `ScenarioJson.ParseFile` → `ScenarioRunner.RunAsync` | Declarative scenario. Contract: [`docs/scenario.schema.json`](docs/scenario.schema.json) |
| MCP           | `src/Graft.McpServer` (stdio)                        | For an LLM or agent. Atomic tools such as `graft_launch`                                 |

The MCP server treats the caller (the LLM) as half-trusted. File paths it receives (the `appPath` to launch, a scenario to read, a screenshot destination, a dialog arm path) must stay **under an allowed root**. The default root is the server's working directory. Add roots with `GRAFT_MCP_ALLOWED_ROOTS` (separated by `Path.PathSeparator`). Set `GRAFT_MCP_ALLOW_ANY_PATH=1` on the server to turn the limit off.

## Repository layout

```
src/
  Graft.Instrumentation/         shared agent (pipe, input, contracts)
  Graft.Instrumentation.Wpf/     WPF adapter + Graft.props/targets
  Graft.Instrumentation.Analyzer GRAFT001
  Graft.Protocol/                shared wire and tree schema
  Graft.Core/                    launch, selectors, Wait/Expect, scenario
  Graft.TestUtilities/           shared xUnit app fixture
  Graft.McpServer/               MCP host (stdio)
tests/sample-apps/               SampleTodoApp / SampleWpfApp
tools/Graft.SmokeClient/         manual Handshake + GetTree check
```

The design source is [`docs/design.md`](docs/design.md). The WPF gap matrix is [`docs/competitive-gap.md`](docs/competitive-gap.md).

## Development

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet build Graft.slnx
dotnet test Graft.slnx -m:1
```

- Formatter: CSharpier
- Commits: Conventional Commits (`type(scope): subject`, subject in English)
- Public API under `src/` needs XML docs
- Test Fact / Theory methods need `summary` + `remarks` (Preconditions / Steps / Expected)
- How to contribute: [CONTRIBUTING.md](CONTRIBUTING.md)

### CI

The **CI** workflow (`windows-latest`) checks format, builds, and runs tests that do not launch an app. Full-solution E2E that uses SendInput needs an interactive Windows session, so the **UI** workflow runs on `main` only when a self-hosted runner is available (it does not run on a fork PR). Graft does not provide a virtual display. Details are in [CONTRIBUTING.md](CONTRIBUTING.md#ci).

## License

[MIT](LICENSE)

Report vulnerabilities through [SECURITY.md](SECURITY.md).
