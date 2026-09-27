# Graft MSBuild (`GraftTest=true`)

The source of truth is `build/Graft.props` and `build/Graft.targets` shipped with `Graft.Instrumentation.Wpf`.
A NuGet reference imports them automatically as `buildTransitive/Graft.Instrumentation.Wpf.{props,targets}`.

## Enable

| Method                             | Example                                                                          |
| ---------------------------------- | -------------------------------------------------------------------------------- |
| Property (source of truth)         | `dotnet build -p:GraftTest=true`, or `<GraftTest>true</GraftTest>` in the csproj |
| Configuration (sample convenience) | `dotnet build -c GraftTest` (the targets set `GraftTest=true`)                   |

There is no automatic tie to the Debug configuration. The symbol is `GRAFT_TEST`.

`Graft.Instrumentation` and `Graft.Instrumentation.Wpf` themselves include the agent, the pipe server, and the WPF patches only when `GraftTest=true` or `Configuration=GraftTest`. Debug and Release output omit them. A test in this repository that needs the agent sets `AdditionalProperties="Configuration=GraftTest"` on the reference so it does not mix with the solution's Debug output.

## ProjectReference (inside this repository)

NuGet auto-import does not apply, so the app csproj imports them explicitly:

```xml
<Import Project="...\src\Graft.Instrumentation.Wpf\build\Graft.props" />
<!-- ... ProjectReference to Graft.Instrumentation.Wpf ... -->
<Import Project="...\src\Graft.Instrumentation.Wpf\build\Graft.targets" />
```

## Check

```powershell
dotnet build tests/sample-apps/SampleWpfApp -p:GraftTest=true
dotnet build tests/sample-apps/SampleWpfApp -c GraftTest
```
