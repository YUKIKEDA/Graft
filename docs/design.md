English | [日本語](design.ja.md)

# Graft — development overview for a WPF GUI test tool

## 1. Purpose

Build a GUI (end-to-end) test tool for WPF applications.
The stance matches Playwright (web) and TestComplete (a commercial multi-framework tool),
specialized to WPF.

The target is limited to **WPF applications you develop and whose source you control**
(black-box tests of an existing third-party exe are out of scope. See section 3).

**Initial runtime scope:** WPF on .NET 8 or later. .NET Framework WPF waits until there is real demand (see section 10).

## 2. Survey of existing tools and why Graft is different

### Limits of existing OSS tools

FlaUI, WinAppDriver, TestStack.White, and Appium's Windows driver
**only wrap the standard Windows UI Automation (UIA) COM API**.
That has structural limits:

- UIA needs cross-process COM calls, so walking the tree and reading properties is slow
- UIA is an accessibility API, so custom controls often lack action patterns (`InvokePattern` and similar)
- Detecting state changes falls back to coarse events or polling

These tools use only UIA (a public API), so security software does not normally treat them as suspicious.

### What TestComplete's advantage actually is

The survey found that TestComplete's advantage on .NET/WPF apps comes from a dedicated plugin that
**reaches inside the application process**, separate from UIA
(the Open Applications mechanism).
That is the same idea as the in-process access this project aims at.

### Conclusion: where Graft differs

| Tool                                                      | Access                                                   | Scope                                                  |
| --------------------------------------------------------- | -------------------------------------------------------- | ------------------------------------------------------ |
| FlaUI / WinAppDriver / TestStack.White / Appium (Windows) | UIA (COM) wrapper only                                   | General Windows                                        |
| TestComplete                                              | Per-framework in-process access (closed implementation)  | General plus many frameworks, commercial and expensive |
| **Graft (this project)**                                  | In-process access (embedded in the app under test first) | **WPF only, your own apps, aimed at OSS**              |

No OSS tool implements in-process access, so limiting the scope to WPF and to apps you own
keeps the implementation small while aiming at TestComplete-level accuracy.

## 3. Architecture

### Why the approach changed

The first idea was process injection (start the target exe suspended and inject an agent DLL with
`CreateRemoteThread` + `LoadLibrary`). That was dropped, for the reasons below, in favor of
**embedding the agent in the app before it ships**.

**Problems with process injection**

- `CreateRemoteThread` plus `LoadLibrary` is a typical malware injection technique, so AV/EDR behavior detection is likely to flag it. A signature only softens the SmartScreen warning. It does not stop behavior-based EDR
- A new OSS project has no accumulated trust, so it is especially likely to be flagged
- Matching architecture (x86/x64/ARM64) and handling an uninitialized CLR added uncertainty and complexity

**Why embedding first is better**

- The app itself references a test NuGet package and opens its own pipe server at startup, so nothing enters the process from outside
- There is no structural opening for AV/EDR process-injection detection (you do not have to explain the technique and ask people to allow it)
- The native layer (a C++ injector and bootstrap DLL) goes away. The implementation is C# only. The build pipeline and the maintenance cost drop

**Tradeoff (an accepted constraint)**

- It cannot test a target whose source you do not control (a third-party app, a black-box test). That is the same stance as Playwright and Cypress testing your own web app, and it matches specializing Graft to WPF
- A mechanism that keeps the agent out of production builds is required (below)

### Flow

1. The app references a per-framework package (`Graft.Instrumentation.Wpf` or
   `Graft.Instrumentation.Avalonia`). The shared core `Graft.Instrumentation` is an indirect dependency
2. At startup (`OnStartup` or the same place) it calls `Agent.Start()`
   (the API exists only in a `GRAFT_TEST` compile. Details below)
3. Only when `GRAFT_ENABLE=1` is set at run time does the agent open a named pipe server on `GRAFT_PIPE_NAME`
4. The controller (`Graft.Core`) connects to the pipe, handshakes with `GRAFT_CONNECT_TOKEN`, and starts sending commands
5. By default the target process exits when the test finishes (session reuse is opt-in)

```
[controller: Graft.Core / future Graft.McpServer]
   │ named pipe
   │  4-byte length prefix + JSON envelope
   │  (binary is a following frame)
   ▼
[agent inside the app: Graft.Instrumentation.* (.NET 8+)]
   ├─ visual tree walker
   ├─ IElementAdapter (Wpf / Avalonia are separate packages)
   ├─ SendInput P/Invoke (input injection)
   └─ named pipe server (same-user ACL)
```

The process-injection layer (injector, bootstrap DLL) is unnecessary, so it was removed from the architecture diagram.

### Keeping the agent out of production builds (decided)

If startup code for the agent is left in a release build, a production app exposes a named pipe that can run commands. That is a serious security hole.
The source of truth is the following (`#if DEBUG` alone, or a run-time flag alone, is not the policy).

- **Compile time:** outside the dedicated symbol `GRAFT_TEST`, the `Agent.Start` API itself is removed
- **Analyzer:** the only check is whether the preprocessor symbol `GRAFT_TEST` is defined (no Configuration name, and no relaxation of an `#if` wrap). A reference to `Agent.Start` in a compile that lacks the symbol is a build error.
  The analyzer is imported automatically through `Graft.Instrumentation.Wpf` / `.Avalonia`
- **Run time:** the pipe stays down unless `GRAFT_ENABLE=1`
- **Reference:** the NuGet `Graft.props` / `Graft.targets` are the main path.
  The enablement source of truth is the property `GraftTest=true` (`/p:GraftTest=true` or the csproj).
  Samples also offer a convenience configuration named `GraftTest` (the targets set `GraftTest=true`).
  There is no automatic tie to the Debug configuration. `GRAFT_TEST` is added to `DefineConstants`.
  The required analyzer rule is `GRAFT001` (`Agent.Start` outside `GRAFT_TEST` → error). Further rules come later.
  The README carries a copy-and-paste example. A permanent reference is allowed, but the compile, analyzer, and run-time gates above are the minimum.
  `PrivateAssets` alone is not the source of truth for keeping the agent out of production

### Connection and process lifetime (decided)

- The runner generates the pipe name and passes it as `GRAFT_PIPE_NAME` (avoids collisions when tests run in parallel)
- The pipe ACL is the same user only. After connect, the handshake is the protocol version plus `GRAFT_CONNECT_TOKEN`
- **One client at a time.** Extra connections are refused. Reconnect plus handshake after a disconnect is allowed
- The public main path is **Launch** (Core sets the environment variables and starts the process). `Connect(pipeName, token)` exists as a low-level API and is not a first-class documented step
- The default is launch then exit. Session reuse is an opt-in tied to the fixture lifetime

### Semantic tree and selectors (decided)

- The visual tree is normalized to a shared JSON schema (`IElementAdapter`)
- The public selector is a **scored composite key** (the highest score at or above a threshold. A tie is `element.ambiguous`).
  Current weights (`SelectorWeights`): automationId=100, name=60, controlType=60, near path=20, threshold=60.
  automationId / name / controlType are a **hard gate** when specified (a mismatch scores 0). Points are added only on a match.
  So `GetByName` / `GetByControlType` alone can reach the threshold (Phase 27 F02 raised the initial provisional weights name=40 / controlType=15).
  Ambiguity when used alone is "more than one candidate at or above the threshold → `element.ambiguous`"
  A shorthand API also offers the automationId-then-name equivalent
- The in-session `runtimeId` is the ordinal from one `getTree` walk. Tests and the wire use the public selector
- An element action sends the public selector on the existing params as optional fields: `automationId`, `name`, `controlType`, `nearAutomationId`, `nth`. A params object that sets only `automationId` stays valid. Drag's other element uses `toAutomationId`, `toName`, `toControlType`, `toNearAutomationId`, and `toNth`
- Core polls `getTree` for the wait, the per-call heal, and the failure report, then sends that call's healed selector. The agent does not heal. It resolves the live tree once, in `getTree` order, with the same weights and hard gates, and acts on that element. An element with no automation id can be acted on. `child` and `sibling` are not sent. Screenshot uses these same fields. `ProtocolVersion.Current` stays 1
- Scoring lives in `Graft.Protocol`. Callers keep writing `Graft.Core.Selectors.Selector`, which maps onto those wire fields. The code change is #172
- Phase 1 required nodes: `runtimeId`, `controlType`, `name`, `automationId`, `bounds`,
  `enabled`, `visible`, `focused`, `children`
  (pattern support, current value, and selector candidates were added early but were not Phase 1 exit criteria)
- The external source of truth for `bounds` is **logical coordinates (DIP) in the target window's client area**.
  Physical and screen conversion stays inside the agent (diagnostic and Inspector extension fields may expose it)
- GetTree has a default cap (depth 25 / 2,000 nodes). Past the cap it truncates and sets `truncated: true`.
  depth / maxNodes / a selector origin can be specified. Diagnostics and Inspector use expanded (50 / 10,000)
- Descendants of an `HwndHost` (including `WebView2`) are outside the WPF visual tree, so they are not walked.
  `getTree` shows the host element only. Internal DOM and native child windows are not `GetByAutomationId` targets.
  Merging DOM into Graft's tree through CDP is deferred ([#93](https://github.com/YUKIKEDA/Graft/issues/93))
- A virtualized list's default is the realized visual tree. `ScrollIntoView` / a realize API are separate
- Windows: the API and schema are multi-window from the start (`windowId` / switch target).
  Phase 1 implementation may start from the main window. **The implementation was finished in Phase 7** (details: Q72 onward)
- Tree diff starts **on the Core side only** (the agent returns a capped full tree).
  `treeDiff` on a failure report is diagnostic (`added` / `removed` / `changed`. `changed.fields` names the changed attributes. `before` / `after` are snapshots without children).
  The baseline is the getTree used by the previous successful action. It is omitted when there is no baseline, or when `GraftSession.IncludeTreeDiff == false`. Switching the target window drops the baseline.
  Identity is a unique `automationId` in the tree. When it is missing or duplicated, the path from the parent is used (a nameless node is `#ControlType@siblingIndex`). `runtimeId` is not used in the diff. Bounds within 0.01 DIP count as equal. JSON Patch is deferred
- Soft assert: `Check` on `GraftSession.SoftAssert()` stores `GraftException`, and `DisposeAsync` throws `expect.failed` once. The aggregate report's `failures` holds each `FailureReport`. Expect outside `Check` still fails immediately. Scenario / MCP have no soft step

### Input, waits, and threads (decided)

- Logical actions are normalized to shared pattern names. Inside the adapter they are tried in the order **native API → Peer/Provider → SendInput**
- Common types have a map (Button→invoke, TextBox→setValue, and so on). An unknown type uses a Peer pattern when one exists, otherwise SendInput. There is no whitelist.
  Callers register `invoke` / `setValue` / `toggle` per control type with `WpfControlActions`.
  The registration closest to the runtime type wins. A handler that returns `true` finishes the action. `false` continues with the built-in order above.
  Disabled or hidden elements are not passed to a handler. Telerik, DevExpress, and Syncfusion implementations are not shipped
- Phase 1 exit criteria for logical actions: `invoke` / `setValue`. Then `toggle` and key input.
  `scrollIntoView` / `select` / `expand` and `collapse` are **Phase 5** (details: Q66).
  Tree `selected` / `expanded` and state Expect are **Phase 6** (details: Q67 onward)
- `setValue`: prefer a native assignment (replace). On failure, clear and SendInput.
  `append` is later. `typeHuman` is `TypeHumanAsync(text, delay)` (wire `typeHuman`, `delayMs`).
  It SendInputs one Unicode scalar at a time. The gap is waited on the agent's request thread so UI debounce can run between characters.
  Existing text is not cleared. `SetValueAsync` / `SendKeysAsync` stay immediate
- SendInput click point: the Peer's clickable point, otherwise the center of the bounds. An offset is optional
- DPI and coordinate conversion are centralized in the agent. Callers see only logical coordinates
- Actions that walk the tree, use a pattern, or take a screenshot are **marshalled to the UI dispatcher and waited on synchronously**.
  The action pipeline is serial
- An action waits by default until it is actionable (visible, enabled, and hittable)
- Business expectations belong on an Expect step. Events first, otherwise polling
- Default timeouts: 5s before an action / 10s for Expect / 30s for launch plus handshake (Options can override)
- Wait / Expect live **on the Core side**. The agent sticks to atomic state reads and actions

### Wire protocol (decided)

- Framing: 4-byte length prefix + body (JSON for now)
- Envelope: request `{ v, id, method, params }` / response `{ v, id, ok, result|error }`
- Protocol version `v` is an integer (initially `1`). Handshake requires an exact match. A mismatch fails. Version negotiation is deferred
- `error`: `{ code, message, details? }`. Initially documented codes:
  `handshake.rejected`, `protocol.versionMismatch`, `element.notFound`, `element.ambiguous`,
  `element.notActionable`, `action.timeout`, `action.failed`, `window.notFound`,
  `pipe.disconnected`, `agent.notEnabled`, `expect.failed`, `selector.invalid`
  (Expect codes issued by Core use the same vocabulary). A diagnostic report is not attached on every call
- Initially synchronous, serial, and single-client. Server push notifications are deferred
- Binary such as a screenshot: a raw binary frame immediately after the JSON meta frame
- MessagePack: add a measurement hook and evaluate against a provisional threshold (GetTree p95 50ms / body 512KB).
  Do not switch immediately. Screenshots are already a raw frame, so they are out of that evaluation. Multi-language bindings and gRPC are outside v1

## 4. Technology stack

| Layer                                        | Language                              | Notes                                                                                 |
| -------------------------------------------- | ------------------------------------- | ------------------------------------------------------------------------------------- |
| Agent (visual tree walk, input, pipe server) | C# (.NET 8 or later)                  | `Graft.Instrumentation` plus a per-framework package                                  |
| Controller (SDK for a test runner)           | C#                                    | The action model is framework-independent. Samples and TestUtilities start from xUnit |
| Protocol                                     | Named pipe + length prefix + JSON     | Local only. MessagePack is considered after measurement                               |
| Input injection                              | C# calls `SendInput` through P/Invoke | Fallback when a pattern fails                                                         |

**The native (C++) layer is gone.** The whole project can be implemented in C# only.

## 5. Proposed layout (updated)

```
Graft/
├── src/
│   ├── Graft.Instrumentation/            # shared core (pipe, Agent, Input, shared contracts)
│   │   ├── Pipe/
│   │   ├── Input/
│   │   └── Agent.cs
│   │
│   ├── Graft.Instrumentation.Wpf/        # WPF adapter (imports the analyzer)
│   │   └── WpfElementAdapter.cs
│   │
│   ├── Graft.Instrumentation.Avalonia/   # Avalonia adapter (imports the analyzer)
│   │   └── AvaloniaElementAdapter.cs
│   │
│   ├── Graft.Instrumentation.Analyzer/   # errors on Agent.Start outside GRAFT_TEST
│   │
│   ├── Graft.Protocol/                   # shared schema (wire, tree, and similar)
│   │
│   ├── Graft.Core/                       # action model, fluent API, scenario (JSON), waits, self-heal
│   │   ├── Selectors/
│   │   ├── Elements/
│   │   ├── Scenario/                     # declarative JSON (the exchange format)
│   │   └── Application.cs                # Launch / low-level Connect
│   │
│   ├── Graft.McpServer/                  # Phase 3: MCP host (a thin wrapper over Core)
│   │
│   └── Graft.TestUtilities/              # xUnit first. NUnit/MSTest later
│
├── tools/
│   ├── Graft.SmokeClient/                # M0: manual Handshake + GetTree console
│   └── Graft.Inspector/                  # FlaUInspect equivalent (future)
│
├── tests/
│   ├── Graft.Instrumentation.Tests/
│   ├── Graft.Core.Tests/
│   └── sample-apps/
│       ├── SampleWpfApp/
│       └── SampleAvaloniaApp/
│
├── Graft.slnx
├── Directory.Build.props
└── README.md
```

### Layout notes (updated)

- The native layer (`native/`) was dropped. The project is one `.slnx` (do not commit a `.sln`)
- Instrumentation is split into **a shared core plus a per-framework package** (so dependencies do not leak)
- The analyzer is imported automatically through the Wpf/Avalonia packages
- `Graft.props` / `Graft.targets` ship in the NuGet package as build help for callers (`GraftTest=true` entry)
- `Graft.Protocol` is the shared schema referenced by both the agent and the controller
- Fluent, Scenario (JSON), and MCP all compile to the same internal action model (none of them is the only source of truth)
- Scenario starts inside `Graft.Core`. Only MCP is split into `Graft.McpServer`
- Selector self-heal is **on the Core side**. Instrumentation returns the current tree and hints
- `Graft.TestUtilities` is for xUnit. `GraftAppFixture` launches and disposes per collection. The serializing `[CollectionDefinition(DisableParallelization = true)]` lives in the test assembly (the attribute is not inherited). SendInput across assemblies still uses `dotnet test -m:1`
- NuGet publishes every `Graft.*` package at the same version (first is 0.1.0. Before 1.0 the public API may break). The wire-compatibility source of truth is `ProtocolVersion.Current` (exact integer match). Published Instrumentation / WPF are packed with `Configuration=GraftTest` so the DLL contains the agent. GRAFT001 goes in `analyzers/dotnet/cs` of the same nupkg (a ProjectReference dependency would drop analyzers from the nuspec). Publish is a `v*` tag and NuGet.org

## 6. Product name

**Graft** (a graft, as in joining a branch to a tree). The name was adopted because the technique embeds code in the app and connects to its inside
(it was first named for the nuance of process "injection", and it was kept after the switch to embedding first
because the core idea, connecting to the target, is the same).
The word alone does not say "GUI test tool", so README, package description, and CLI help
always carry a tagline such as
"Graft — In-process UI testing for WPF".

## 7. Recommended build order (updated)

### Milestone acceptance

| ID     | Acceptance (this is "done")                                                                                                                                                                                                                                                                          | Not included / later                                        |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| **M0** | SampleWpfApp (.NET 8) + Instrumentation.Wpf. No Start API outside `GRAFT_TEST` / `GRAFT001` error. Pipe starts behind the environment-variable gate. `tools/Graft.SmokeClient` launches the sample and reads SampleButton name/bounds through Handshake + GetTree (a manual Connect is also allowed) | props/targets convenience, screenshot, invoke, Core Launch  |
| **M1** | M0 plus a window PNG screenshot (meta+raw) plus a button click confirmed with `invoke` (TextBlock changes). `Graft.props`/`targets` (`GraftTest=true`)                                                                                                                                               | setValue may be added during M1. Core Launch / xUnit are M2 |
| **M2** | M1 plus one green xUnit test that launches through `Graft.Core` (the main path does not need SmokeClient)                                                                                                                                                                                            | Avalonia, Scenario, MCP, self-heal                          |

**SampleWpfApp (at M0):** a Button (`AutomationId=SampleButton`) + a TextBox + a TextBlock that changes on click.

**First repository slice:** the M0 projects + a `Directory.Build.props` base + a tests folder.
Do not create empty skeletons for Core / Avalonia / McpServer and similar.

### Work order

1. **M0:** Directory.Build.props → SampleWpfApp → Protocol + Instrumentation(+Wpf) → Analyzer (`GRAFT001`) → SmokeClient (Launch is the canonical demo)
2. **M1:** Screenshot + `invoke` + props/targets. Then `setValue` / `toggle` / keys
3. **M2:** Core Launch, waits, scored selectors, error codes + one xUnit test
4. Phase 2 onward (failure diagnosis, Scenario JSON, MCP, self-heal)

## 8. Letting an LLM write, fix, and check tests

### Background

End-to-end tests are generally brittle and expensive to maintain.
This project assumes an LLM can write tests, fix them when they fail, and read the results.
The aim is to cut the cost of hand-editing selectors and test code on every UI change.

### Features to add

1. **Screenshots**
   - Capture the target window in-process and return it on the pipe
   - Default: a whole-window PNG. JPEG/quality and an element crop are optional APIs

2. **A structured failure report**
   - Required minimum: failed step, expected, actual, whether it timed out, target selector
   - Attached by default: the last N action log entries, the tree at failure (a diff is allowed), a screenshot reference
   - Self-heal candidates and detailed environment info stay optional until Phase 4

3. **A declarative test format**
   - The exchange format is **JSON** (the contract is a JSON Schema). YAML is deferred
   - The fluent API, Scenario, and MCP all compile to the same internal action model
   - The implementation lives in `Graft.Core` (not a separate `Graft.Scenario` project at the start)

4. **An MCP-style conversational interface**
   - A thin wrapper that exposes Phase 1–2 (`Graft.McpServer`)

5. **Self-healing selectors**
   - The logic is in Core. Instrumentation only returns the current tree and stable hints

### Relationship to Avalonia Headless

Avalonia's official Headless Testing Platform is for **control and layout tests** with the window and rendering replaced.
It complements Graft's real-process E2E.
Graft does not provide or integrate a headless backend. Support for an app running headless can be added later if there is demand.

### CI and the runtime environment

An interactive Windows session is the basic assumption.
Graft does not provide a virtual display. A pure Session 0 / off-screen-only mode is out of the initial scope.

GitHub Actions:

- Host (required gate): `.github/workflows/ci.yml` (`windows-latest`). CSharpier, build, and tests that do not launch an app.
- Full-solution E2E: `.github/workflows/ui.yml`. Self-hosted (interactive logon, screen not locked, labels `windows` + `interactive`). Repository variable `GRAFT_ENABLE_UI_CI=true` enables `main` push and manual runs. It does not run on a fork PR because the repository is public. Steps are in `CONTRIBUTING.md`.

### Implementation phases and priority

| Phase      | Contents                                                     | Role                                                                                                                     |
| ---------- | ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------ |
| Phase 1    | Instrumentation itself + screenshot + atomic action commands | Foundation. Wait/Expect are Core                                                                                         |
| Phase 2    | Structured failure diagnosis + declarative JSON scenarios    | The core an LLM can use. The action model is gathered in Core                                                            |
| Phase 3    | `Graft.McpServer`                                            | A thin public layer over Phase 1–2                                                                                       |
| Phase 4    | Self-healing selectors                                       | Accuracy is refined on the Core side                                                                                     |
| Phase 5    | Remaining WPF actions (scroll / select / expand)             | Fill action holes, including virtualization                                                                              |
| Phase 6    | Tree state (`selected` / `expanded`) + Expect                | Diagnosis and LLM. State checks for Phase 5 actions                                                                      |
| Phase 7    | Windows and modals (list/switch/wait/open)                   | WPF coverage. The window side of the competitive gap                                                                     |
| Phase 8    | DataGrid row-centered MVP + `checked`                        | Complex host UI. Cell read/write is the next phase                                                                       |
| Phase 9    | DataGrid cell read/write (Text column MVP)                   | Host plus (row, col). OS dialogs are next                                                                                |
| Phase 10   | OpenFile dialog seam (policy + MVP)                          | Arm + Harmony `CommonItemDialog.RunDialog`                                                                               |
| Phase 11   | SaveFile dialog seam (same shape as OpenFile)                | Arm + the same RunDialog patch (Save only)                                                                               |
| Phase 12   | OpenFolder dialog seam (same shape)                          | Arm + the same RunDialog (`FolderName`)                                                                                  |
| Phase 13   | MessageBox seam (runtime MVP)                                | Arm + Harmony `MessageBox.Show`                                                                                          |
| Phase 14   | Key chord / special keys (`pressKeys`)                       | `PressAsync`. Avalonia moves later                                                                                       |
| Phase 15   | Public Screenshot (Session / Scenario / MCP)                 | Promote the existing wire. Element clip is not included                                                                  |
| Phase 16   | Right-click + ContextMenu / MenuItem                         | `RightClickAsync` + the open menu on the tree                                                                            |
| Phase 17   | TabControl selection (`select` extended)                     | Existing `SelectAsync(index)`. Slider and similar are next                                                               |
| Phase 18   | Slider value (`setValue` extended)                           | Invariant double → `Slider.Value`. Multi-select is next                                                                  |
| Phase 19   | ListBox multi-select (`selectMany`)                          | Replace semantics. DataGrid multi-row is not included                                                                    |
| Phase 20   | Menu bar (existing `invoke`)                                 | Top level plus one submenu. The open submenu is on the tree                                                              |
| Phase 21   | DataGrid column key + CheckBox column                        | Header `columnKey`. Multi-row select is next                                                                             |
| Phase 22   | DataGrid multi-row select (`selectMany` extended)            | Extended + FullRow. Next is the gap review                                                                               |
| Phase 23   | Competitive scenario matrix (WPF Must review)                | Docs only. Must fixed. Avalonia is after Must                                                                            |
| Phase 24   | Waits / Expect / screen changes and progress                 | Stronger Wait/Expect + value + a progress sample                                                                         |
| Phase 25   | Advanced mouse                                               | dbl/hover/drag/clickAt/wheel (SendInput)                                                                                 |
| Phase 26   | Menu depth                                                   | `SelectMenuAsync` / ContextMenu submenu / U04                                                                            |
| Phase 27   | Find, paths, and key selection                               | GetByName/relative/Select key/SelectTree                                                                                 |
| Phase 28   | Remaining DataGrid                                           | Template/cell select/row key/sort/row CRUD                                                                               |
| Phase 29a  | Input, toggle, and key holes                                 | V03/V05/T03/T04/K03/K04                                                                                                  |
| Phase 29b  | List and other UI holes                                      | L04/L06/C01/C03–C06                                                                                                      |
| Phase 31   | SendInput parallelism                                        | X04. Done with the canonical `-m:1` (mutex dropped)                                                                      |
| Phase 32   | Frame navigation (H02)                                       | Frame only. No dedicated DSL                                                                                             |
| Phase 33   | Action timeline (D06)                                        | PNG sequence + HTML. Must. No GIF/FFmpeg                                                                                 |
| Phase 34   | SampleTodoApp (canonical caller guide)                       | MVVM/DI/theme + real JSON E2E                                                                                            |
| Phase 35   | Element-clipped screenshot (P02)                             | Must. Window clip + popup RTB + ToolTip node. An open overlay is composited on the host                                  |
| (parallel) | Deepest WPF UI Gallery E2E                                   | Local `tests/wpfui` + `WpfUi.Gallery.Graft.Tests` |

## 9. Open questions

- Wording of the `GRAFT001` message, and what to verify when a symbol propagates across multi-targeting
- Concrete MSBuild fragments for `Graft.props` / `Graft.targets` (how they land in the samples)
- Concrete rows of the common-type map (WPF type names)
- Measured tuning of selector weights, and the final fields of the `details` schema
- Item key and display-name forms of scroll/select (the next candidate after index as the source of truth)
- Driving a real OS common dialog through UIA (rejected by policy. A separate discussion if it is ever needed)
- WPF competitive gap (source of truth: [`docs/competitive-gap.md`](competitive-gap.md). Must is fixed → Phase 24+)
- Inspector is optional (an in-house app can often use `getTree` instead)
- The measured-log format used to evaluate MessagePack
- Whether to support .NET Framework WPF (only after demand is clear)
- Multi-language bindings / gRPC (outside v1. Revisit after the action model is stable)
- (Reference, rejected) AV/EDR and code-signing problems of process injection were effectively removed by switching to embedding first
- **Test parallelism and SendInput:** `SampleUiCollection` / `McpUiCollection` serialize only inside an assembly. Running `dotnet test Graft.slnx` in parallel can launch SampleWpfApp from Core, Sample, and MCP at once, and SendInput (click / keys / chord / rightClick) can flake on focus fights (examples: `ello` left after PressKeys, SendKeys that misses, a ContextMenu that never opens so the MenuItem wait times out). **Source of truth: `dotnet test Graft.slnx -m:1`** (or run the UI projects in order). A cross-process mutex prototype was dropped because it could not hold the foreground

## 10. Decision log

Items agreed while grilling the design. If this section and the body disagree, this section and the body that has already been updated win.

| ID   | Decision                                                                                                                                                                                                                                                                                                  |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1   | Initial runtime is .NET 8+ WPF/Avalonia only. .NET Framework WPF waits for demand                                                                                                                                                                                                                         |
| Q2   | Instrumentation splits into a shared core plus `.Wpf` / `.Avalonia`                                                                                                                                                                                                                                       |
| Q3   | Enablement is `GRAFT_TEST` (compile) + an analyzer error + a run-time opt-in                                                                                                                                                                                                                              |
| Q4   | The run-time opt-in is an environment variable (the runner sets it)                                                                                                                                                                                                                                       |
| Q5   | The runner generates the pipe name and passes `GRAFT_PIPE_NAME`                                                                                                                                                                                                                                           |
| Q6   | Process lifetime defaults to launch then exit. Reuse is opt-in                                                                                                                                                                                                                                            |
| Q7   | The public selector is a composite key. `runtimeId` is an internal handle                                                                                                                                                                                                                                 |
| Q8   | Fluent / Scenario / MCP compile to the same internal action model. There is no single source of truth among them                                                                                                                                                                                          |
| Q9   | The declarative scenario source of truth is JSON. YAML is deferred                                                                                                                                                                                                                                        |
| Q10  | Scenario lives in Core. Only MCP is split into `Graft.McpServer`                                                                                                                                                                                                                                          |
| Q11  | Self-heal is Core. Instrumentation provides the tree and hints                                                                                                                                                                                                                                            |
| Q12  | A conditional reference is recommended. A permanent reference is allowed. Analyzer + symbol + environment variable are the minimum                                                                                                                                                                        |
| Q13  | Multi-window is designed into the API from the start. Phase 1 implementation starts from the main window                                                                                                                                                                                                  |
| Q14  | UI actions are marshalled to the dispatcher, synchronous and serial                                                                                                                                                                                                                                       |
| Q15  | Actionable wait plus Expect as the source of truth. Events first, otherwise polling                                                                                                                                                                                                                       |
| Q16  | Framing is a 4-byte length prefix plus a body                                                                                                                                                                                                                                                             |
| Q17  | The default screenshot is a window PNG. JPEG and crop are optional                                                                                                                                                                                                                                        |
| Q18  | Binary is a raw frame after the JSON meta frame                                                                                                                                                                                                                                                           |
| Q19  | Pipe ACL is the same user. Handshake is version + CONNECT_TOKEN                                                                                                                                                                                                                                           |
| Q20  | Shared envelope `{v,id,method,params}` / `{v,id,ok,result\|error}`                                                                                                                                                                                                                                        |
| Q21  | Outside `GRAFT_TEST`, the Start API is removed and a call is an analyzer error. Do not key off DEBUG                                                                                                                                                                                                      |
| Q22  | Failure diagnosis is a required minimum plus standard attachments by default. Self-heal candidates are optional in Phase 4                                                                                                                                                                                |
| Q23  | Virtualization defaults to the realized tree. Realize and scroll APIs are separate                                                                                                                                                                                                                        |
| Q24  | The Phase 1 agent stops at atomic actions. Wait/Expect are Core                                                                                                                                                                                                                                           |
| Q25  | Phase 1 required tree fields are set B. Pattern, value, and selector candidates are outside the exit criteria                                                                                                                                                                                             |
| Q26  | Default timeouts: action 5s / Expect 10s / launch+handshake 30s                                                                                                                                                                                                                                           |
| Q27  | CI assumes an interactive session. Hosted Actions are format/build/unit (`ci.yml`). Full-solution E2E is self-hosted `ui.yml` (`GRAFT_ENABLE_UI_CI`). Graft does not provide a virtual display                                                                                                            |
| Q28  | Complementary to Avalonia Headless. Graft is real-process E2E. Headless support waits for demand                                                                                                                                                                                                          |
| Q29  | Environment variables: `GRAFT_ENABLE` / `GRAFT_PIPE_NAME` / `GRAFT_CONNECT_TOKEN`                                                                                                                                                                                                                         |
| Q30  | The main path is Launch. `Connect` is a low-level API                                                                                                                                                                                                                                                     |
| Q31  | MessagePack is considered after measurement. Multi-language bindings and gRPC are outside v1                                                                                                                                                                                                              |
| Q32  | Core is framework-independent. TestUtilities and samples start from xUnit                                                                                                                                                                                                                                 |
| Q33  | The analyzer is imported automatically from Instrumentation.Wpf / .Avalonia                                                                                                                                                                                                                               |
| Q34  | Record this decision in section 10 and reflect it in the layout, the build order, and the body                                                                                                                                                                                                            |
| Q35  | The analyzer checks the `GRAFT_TEST` symbol only. No Configuration or `#if` relaxation                                                                                                                                                                                                                    |
| Q36  | `Graft.props`/`targets` are the main path (`GraftTest=true`). The README also has a copy example                                                                                                                                                                                                          |
| Q37  | GetTree has a default cap plus `truncated` plus depth/maxNodes/origin                                                                                                                                                                                                                                     |
| Q38  | Default 25/2,000. Expanded 50/10,000                                                                                                                                                                                                                                                                      |
| Q39  | Tree diff starts on the Core side only. An agent-side diff is deferred                                                                                                                                                                                                                                    |
| Q40  | Logical actions are native → Peer → SendInput                                                                                                                                                                                                                                                             |
| Q41  | Phase 1 exit is `invoke`+`setValue`. Toggle and keys are next. scroll/select/expand are later                                                                                                                                                                                                             |
| Q42  | MessagePack is evaluated with measurement and a provisional threshold. Do not switch immediately                                                                                                                                                                                                          |
| Q43  | Single client. Reconnect plus handshake is allowed                                                                                                                                                                                                                                                        |
| Q44  | External coordinates are logical DIP in the window client area. Conversion stays in the agent                                                                                                                                                                                                             |
| Q45  | Selectors are scored plus a threshold. A tie is ambiguous. Shorthands are separate                                                                                                                                                                                                                        |
| Q46  | `error` is `{code,message,details?}`. Stable codes are documented                                                                                                                                                                                                                                         |
| Q47  | `v` is an integer. Handshake is an exact match. Version negotiation is deferred                                                                                                                                                                                                                           |
| Q48  | Reflect Q35 onward into the body and sections 9 and 10 immediately                                                                                                                                                                                                                                        |
| Q49  | Selector weights: automationId=100, name=60, controlType=60, near path=20, threshold=60 (Phase 27 F02 changed the initial name=40 / controlType=15. name and controlType are hard gates)                                                                                                                  |
| Q50  | Initial stable error-code set (handshake/protocol/element/action/window/pipe/agent/expect/selector)                                                                                                                                                                                                       |
| Q51  | setValue prefers a native replace, then clear+SendInput on failure. append/typeHuman come later                                                                                                                                                                                                           |
| Q52  | A SendInput click uses the Peer point, then the center. An offset is optional                                                                                                                                                                                                                             |
| Q53  | The default tree diff is diagnostic. JSON Patch is deferred                                                                                                                                                                                                                                               |
| Q54  | The only required analyzer rule is GRAFT001 Error. Further rules come later                                                                                                                                                                                                                               |
| Q55  | `GraftTest=true` is the source of truth. `Configuration=GraftTest` is a sample convenience. No Debug tie                                                                                                                                                                                                  |
| Q56  | Common types use a map. Unknown types go Peer then SendInput. No whitelist                                                                                                                                                                                                                                |
| Q57  | After Q49 onward is reflected, tighten the first implementation milestone acceptance                                                                                                                                                                                                                      |
| Q58  | Split milestones into M0/M1/M2                                                                                                                                                                                                                                                                            |
| Q59  | M0 includes GRAFT_TEST + environment variables + the analyzer. props/targets are M1                                                                                                                                                                                                                       |
| Q60  | The M0 manual client is `tools/Graft.SmokeClient`                                                                                                                                                                                                                                                         |
| Q61  | Ask a few more questions tied directly to M0, then close and implement                                                                                                                                                                                                                                    |
| Q62  | SmokeClient supports both Launch and Connect. The M0 demo source of truth is Launch                                                                                                                                                                                                                       |
| Q63  | The sample is a Button + a TextBox + a TextBlock that changes on click                                                                                                                                                                                                                                    |
| Q64  | Start with the M0 set + Directory.Build.props + a tests base. Do not create empty skeletons                                                                                                                                                                                                               |
| Q65  | Implementation starts from gitignore and templates. M0 proceeds batch by batch                                                                                                                                                                                                       |
| Q66  | Phase 5: scrollIntoView / select / expand and collapse.                                                                                                                                                                                                                  |
| Q67  | Phase 6: `selected`/`expanded` on TreeNode as `bool?` (null or omitted when not applicable). Protocol stays v1                                                                                                                                                                                            |
| Q68  | selected is selection hosts only (item nodes). expanded is expand targets (TreeViewItem/Expander). checked is separate                                                                                                                                                                                    |
| Q69  | ExpectSelectedAsync / ExpectExpandedAsync. null is expect.failed. Scenario/MCP follow thinly                                                                                                                                                                                                              |
| Q70  | Phase 6 acceptance is a realized ListBox item plus a TreeViewItem. Combo item Expect is outside the exit criteria                                                                                                                                                                                         |
| Q71  | The next candidate at the end of Phase 6 was Avalonia then Inspector, but Q72 put WPF coverage first                                                                                                                                                                                                      |
| Q72  | Phase 7: multi-window + WPF modals. Avalonia/Inspector come after WPF coverage.                                                                                                                                                                                          |
| Q73  | A window has an in-session `windowId`. List/Switch. Meta: title/automationId/isModal/isActive. The default target switches                                                                                                                                                                                |
| Q74  | Opening ShowDialog is `InvokeOpeningWindow` (BeginInvoke + wait to appear, auto Switch by default). A plain Invoke is unsupported                                                                                                                                                                         |
| Q75  | WaitForWindow takes a title and/or an automationId. A merged tree of every window and an OS-dialog implementation are not included                                                                                                                                                                        |
| Q76  | After Phase 7, next is the OS dialog policy or complex UI. Avalonia then Inspector stay toward the end                                                                                                                                                                                                    |
| Q77  | Phase 8: DataGrid **row-centered MVP** plus `checked` in the last batch of the same phase.                                                                                                                                                                               |
| Q78  | The API is the existing `scrollIntoView` / `select` (host plus index). No new wire. The sample is FullRow+Single only                                                                                                                                                                                     |
| Q79  | The tree is a realized `DataGridRow` plus `selected`. The row has a stable automationId. Cell coordinates, edit, and sort are not included                                                                                                                                                                |
| Q80  | The public surface is a thin E2E of existing scenario steps. There is no DataGrid-specific MCP tool                                                                                                                                                                                                       |
| Q81  | After Phase 8, next is DataGrid **cell read/write**. OS dialogs, Avalonia, and Inspector are later still                                                                                                                                                                                                  |
| Q82  | Phase 9: DataGrid **cell read/write** (Text column).                                                                                                                                                                                                                     |
| Q83  | The address is host plus (rowIndex, columnIndex). API: GetCellText / SetCellValue / ExpectCellText plus the same wire names                                                                                                                                                                               |
| Q84  | Write is BeginEdit → value → CommitEdit. Columns are DataGridTextColumn only. DataGridCell is not on the tree                                                                                                                                                                                             |
| Q85  | The sample stays FullRow+Single with an editable Text column. Scenario/MCP follow thinly                                                                                                                                                                                                                  |
| Q86  | After Phase 9, next is the **OS common-dialog policy**. Column keys and other column kinds come later                                                                                                                                                                                                     |
| Q87  | Phase 10: OpenFile **runtime seam** (policy+MVP). No real OS UIA.                                                                                                                                                                                                       |
| Q88  | The app keeps a plain `OpenFileDialog`. Harmony replaces `CommonItemDialog.RunDialog`. Application code has no Graft API                                                                                                                                                                                  |
| Q89  | Arm ahead of time (single path OK / Cancel, once). Unarmed calls use the real dialog. Open with `waitForNewWindow:false`                                                                                                                                                                                  |
| Q90  | After Phase 10, next is the **SaveFile seam**. Avalonia / Inspector stay behind                                                                                                                                                                                                                           |
| Q91  | Phase 11: SaveFile **runtime seam** (same shape as OpenFile).                                                                                                                                                                                                           |
| Q92  | A plain `SaveFileDialog`. The same `CommonItemDialog.RunDialog` patch. `DialogArm.SaveFile` is independent of `DialogArm.OpenFile`                                                                                                                                                                       |
| Q93  | `ArmSaveFile` / `ArmSaveFileCancel`, once, `waitForNewWindow:false`. Scenario/MCP follow thinly                                                                                                                                                                                                           |
| Q94  | After Phase 11, next is the **Folder seam**. Avalonia / Inspector stay behind                                                                                                                                                                                                                             |
| Q95  | Phase 12: OpenFolder **runtime seam** (same shape as Open/Save).                                                                                                                                                                                                        |
| Q96  | A plain `OpenFolderDialog`. The same `RunDialog` patch. The result is `FolderName`. `DialogArm.OpenFolder` is independent of the file arms                                                                                                                                                               |
| Q97  | `ArmOpenFolder` / `ArmOpenFolderCancel`, once, `waitForNewWindow:false`. Scenario/MCP follow thinly                                                                                                                                                                                                       |
| Q98  | After Phase 12, next is the **MessageBox seam**. Avalonia / Inspector stay behind                                                                                                                                                                                                                         |
| Q99  | Phase 13: MessageBox **runtime seam**.                                                                                                                                                                                                                                  |
| Q100 | A plain `MessageBox.Show`. Harmony replaces the main overloads. Application code has no Graft API                                                                                                                                                                                                         |
| Q101 | `ArmMessageBox(result)` (OK/Cancel/Yes/No/None), once, `waitForNewWindow:false`. Scenario/MCP                                                                                                                                                                                                             |
| Q102 | After Phase 13 the original next step was Avalonia, but filling the WPF competitive gap comes first (Q103)                                                                                                                                                                                                |
| Q103 | Avalonia moves back. Phase 14 is a **key chord**. Then Screenshot → right-click/Menu → … → Avalonia                                                                                                                                                                                                       |
| Q104 | `PressAsync` / wire `pressKeys`. `sendKeys` stays literal. One call is one chord, with focus                                                                                                                                                                                                              |
| Q105 | DSL: `Control`/`Alt`/`Shift` + `A`–`Z`/`0`–`9`/Enter/Tab/Escape/Backspace/Delete/Space/Arrow*                                                                                                                                                                                                             |
| Q106 | Sample E2E: TextBox SetValue → Control+A → Delete → Expect empty.                                                                                                                                                                                                       |
| Q107 | Phase 15 is a **public Screenshot**. The fluent return is a `Screenshot` of meta+bytes plus `SaveAsync`                                                                                                                                                                                                   |
| Q108 | The target is the current target window only. Scenario requires a path. MCP makes the path optional (temp when omitted)                                                                                                                                                                                   |
| Q109 | E2E: fluent PNG signature+size / scenario writes a path. Image diff and element clip are not included.                                                                                                                                                                             |
| Q110 | Phase 16: `RightClickAsync` plus the open ContextMenu on the tree. MenuItem uses the existing `invoke`                                                                                                                                                                                                    |
| Q111 | Implementation is a SendInput right-click plus flush. The caller waits. The menu bar and submenus are not included.                                                                                                                                                                |
| Q112 | Phase 17 is **TabControl** only. Extend the existing `SelectAsync(index)`. ExpectSelected + StatusText.                                                                                                                                                                            |
| Q113 | Scenario uses the existing `select`. No MCP change. Slider / multi-select / header addressing are not included                                                                                                                                                                                            |
| Q114 | Phase 18 is **Slider only**. Existing `SetValueAsync` / `setValue`. InvariantCulture double → `Slider.Value`.                                                                                                                                                                      |
| Q115 | Verification is the StatusText side effect only (no tree `value`). Scenario uses the existing `setValue`. No MCP change. Multi-select is not included                                                                                                                                                     |
| Q116 | Phase 19: ListBox only. New `SelectManyAsync` / wire `selectMany` (replace, empty indexes = clear).                                                                                                                                                                                |
| Q117 | The sample is a separate `SampleMultiList` (Extended). Single is an error. ExpectSelected + StatusText. Scenario/MCP follow thinly                                                                                                                                                                        |
| Q118 | Phase 20: menu bar. Existing `invoke` only. Top level plus one submenu. The open submenu is on the tree.                                                                                                                                                                           |
| Q119 | Sample File→Ping. Scenario uses the existing `invoke`. No MCP change. Arbitrary depth, a path DSL, and a new wire are not included                                                                                                                                                                        |
| Q120 | Phase 21: the column key is the Header (Ordinal). Wire `column` xor `columnKey`. CheckBox is `"True"`/`"False"`.                                                                                                                                                                   |
| Q121 | SampleGrid gains an Active CheckBox column. Scenario/MCP follow thinly. Multi-row select and Template columns are not included                                                                                                                                                                            |
| Q122 | Phase 22: DataGrid multi-row select (`selectMany` extended) — grilling starts                                                                                                                                                                                                                             |
| Q123 | Phase 22: extend the existing `selectMany` to DataGrid rows. Replace, empty clears, Single errors. FullRow only.                                                                                                                                                                   |
| Q124 | The sample is a separate `SampleMultiGrid` (Extended). ExpectSelected + StatusText + empty-clear fluent. Scenario follows thinly. No new MCP tool                                                                                                                                                         |
| Q125 | Before Avalonia, make the competitive scenario matrix the source of truth. FlaUI-class actions and checks. Avalonia is forbidden until Must is done. `docs/competitive-gap.md`                                                                                                   |
| Q126 | Phase 23 is docs only. Must is fixed after the table review. Screen changes and progress (appear/disappear) are Must candidates. Split the implementation across provisional Phase 24+                                                                                                                    |
| Q127 | Must fixed: the proposed id set plus X04. K05/V06/W12/A08/P02 are optional. Inspector is optional. Update `docs/competitive-gap.md`                                                                                                                                                                       |
| Q128 | Phase 24: Expect* extensions + WaitFor/Gone + WaitForWindowClosed + TreeNode.value.                                                                                                                                                                                                |
| Q129 | Sample: progress window → the next panel in the same window. No Frame. Scenario/MCP follow thinly. No dedicated W11 API                                                                                                                                                                                   |
| Q130 | Phase 25: DoubleClick/Hover/Drag (element to element)/ClickAt (DIP)/Wheel. SendInput.                                                                                                                                                                                              |
| Q131 | Hover is a move plus a short dwell. Waiting for a ToolTip is Phase 29b. The sample has one Mouse section. Scenario/MCP follow thinly                                                                                                                                                                      |
| Q132 | Phase 26: `SelectMenuAsync` path DSL (AutomationId/`/`). Wire `selectMenu`. ContextMenu comes after RightClick. U04=`element.notActionable`.                                                                                                                                       |
| Q133 | Phase 27: GetByName/ControlType, Child/Sibling/Nth, SelectAsync(key), SelectTreeAsync.                                                                                                                                                                                             |
| Q134 | Phase 28: Template/SelectCell/SelectRow/ClickColumnHeader/AddRow/DeleteSelectedRows. G09 is the sort UI only.                                                                                                                                                                      |
| Q135 | Phase 29a: Password Set / RichText plain text / Radio and Toggle checked / ExpectFocused / F+NumPad (Win excluded). 29b=L04/L06/C01/C03–C06.                                                                                                                                       |
| Q136 | Phase 29b: DatePicker yyyy-MM-dd / ComboBox Expand / ListView GridView read / ExpectToolTip / ToolBar and StatusBar sample / Popup merged while open / Hyperlink Click.                                                                                                            |
| Q137 | Phase 31: the whole-solution source of truth is `-m:1`. The named-mutex prototype was dropped because it could not hold the foreground.                                                                                                                                            |
| Q138 | Roadmap: H02 → action timeline (D06) → Avalonia. X04 is Done with `-m:1`. Finish one framework first                                                                                                                                                                                                      |
| Q139 | H02: Frame only, no dedicated DSL, sample + WaitFor/Expect. NavigationWindow is outside this Must.                                                                                                                                                                                 |
| Q140 | D06: Core option, Always/OnFailure, Dispose+Save, one frame after each action, PNG+HTML (pace and captions), no image NuGet/FFmpeg. Must.                                                                                                                                          |
| Q141 | Before Avalonia, make SampleTodoApp the canonical caller guide. MVVM+DI+theme+real JSON. R3/ObservableCollections.                                                                                                                                                                 |
| Q142 | `LaunchOptions.Environment` is general (optional). SampleTodo's data directory is UI/OpenFolder (`settings.json`). E2E uses ArmOpenFolder. One story. R3 (CommunityToolkit.Mvvm is not used). No demo seed                                                                                                |
| Q143 | P02 is promoted to Must (Phase 35). Avalonia is blocked again. `element.ScreenshotAsync`. Window RTB intersection clip + popup root RTB. An open ToolTip is a child node. An open overlay is composited onto element and window screenshots. Wire optional automationId/runtimeId. |
| Q144 | Wire element target is the public selector on the existing params (`automationId`, `name`, `controlType`, `nearAutomationId`, `nth`; drag also `toName`, `toControlType`, `toNearAutomationId`, `toNth`). Core polls getTree for the wait, the per-call heal, and the failure report, then sends the healed selector. The agent does not heal: one live walk, getTree order, same weights. No `child`/`sibling`. Screenshot uses the same fields. `runtimeId` stays a walk ordinal, not a wire target. `ProtocolVersion.Current` stays 1. Scoring moves to Protocol; `Selector` stays in Core. Supersedes the screenshot wire target in Q143. Code is #172. |
