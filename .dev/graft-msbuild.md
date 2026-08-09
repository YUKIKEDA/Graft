# Graft MSBuild（`GraftTest=true`）

正本は `Graft.Instrumentation.Wpf` 同梱の `build/Graft.props` / `build/Graft.targets`。  
NuGet 参照時は `buildTransitive/Graft.Instrumentation.Wpf.{props,targets}` として自動 import される。

## 有効化

| 方法 | 例 |
| ---- | -- |
| プロパティ（正本） | `dotnet build -p:GraftTest=true` または csproj に `<GraftTest>true</GraftTest>` |
| Configuration（サンプル便利構成） | `dotnet build -c GraftTest`（targets が `GraftTest=true` を立てる） |

Debug 構成への自動紐づけはしない。記号は `GRAFT_TEST`。

## ProjectReference（リポジトリ内）

NuGet の自動 import は効かないため、アプリ csproj で明示 Import する:

```xml
<Import Project="...\src\Graft.Instrumentation.Wpf\build\Graft.props" />
<!-- ... ProjectReference to Graft.Instrumentation.Wpf ... -->
<Import Project="...\src\Graft.Instrumentation.Wpf\build\Graft.targets" />
```

## 確認

```powershell
dotnet build tests/sample-apps/SampleWpfApp -p:GraftTest=true
dotnet build tests/sample-apps/SampleWpfApp -c GraftTest
```
