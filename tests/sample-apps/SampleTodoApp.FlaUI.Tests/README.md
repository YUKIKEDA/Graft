# SampleTodoApp.FlaUI.Tests

Comparison harness: the same SampleTodoApp story as [`../SampleTodoApp.Tests`](../SampleTodoApp.Tests), driven with **FlaUI.UIA3** instead of Graft.

## How to run

Interactive desktop required (file dialogs). Not part of the default CI gate.

```powershell
dotnet build tests/sample-apps/SampleTodoApp/SampleTodoApp.csproj -c Debug
dotnet test tests/sample-apps/SampleTodoApp.FlaUI.Tests
```

## Graft vs FlaUI (this story)

| Concern         | Graft (`SampleTodoApp.Tests`)               | FlaUI (this project)                                                             |
| --------------- | ------------------------------------------- | -------------------------------------------------------------------------------- |
| Launch          | `Application.LaunchAsync` + GraftTest Agent | Debug `SampleTodoApp.exe` (no Agent)                                             |
| Data directory  | Settings UI + `ArmOpenFolder`               | Pre-write `%LocalAppData%\GraftSampleTodo\settings.json`                         |
| Import / Export | `ArmOpenFile` / `ArmSaveFile`               | Real Win32 `#32770` under Main (Open: ValuePattern; Save: keyboard path + Enter) |
| Timeline        | `LaunchOptions.Timeline`                    | N/A                                                                              |
| API             | `GetByAutomationId` Fluent                  | `FindFirstDescendant` + patterns                                                 |

Canonical consumer docs remain [`.dev/graft-core.md`](../../../.dev/graft-core.md) → `SampleTodoApp.Tests`.
