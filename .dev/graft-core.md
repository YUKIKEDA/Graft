# Graft.Core — コントローラ利用メモ（M2）

主経路は **Launch**（SmokeClient は診断用）。対象アプリは `GraftTest=true` / `-c GraftTest` で `GRAFT_TEST` 付きビルドであること。

```csharp
await using var session = await Application.LaunchAsync(
    new LaunchOptions
    {
        AppPath = @"path\to\YourApp.csproj", // or .exe
        // Timeout 既定 30s（起動+Handshake）
    }
);

await session.GetByAutomationId("SampleButton").InvokeAsync();
await session.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
```

- `ConnectAsync` は既起動エージェント向けの低レベル API
- Wait / Expect のタイムアウトは `session.WaitOptions`（アクション 5s / Expect 10s 既定）
- セレクタは Core 側スコアリング（`Selector` / `TreeSelector`）。ショートハンドは `ByAutomationId`

受け入れ確認:

```bash
dotnet test tests/Graft.Core.Tests --filter M2Acceptance
```
