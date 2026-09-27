# Local verification gate matching hosted CI (.github/workflows/ci.yml),
# without pack and without launched-app tests.
# Usage: ./build.ps1
# Windows only. Exits non-zero on failure.

$ErrorActionPreference = "Stop"

if ($env:OS -ne "Windows_NT") {
    Write-Error "build.ps1 is Windows-only. Run dotnet tool restore, dotnet csharpier check ., dotnet build Graft.slnx, and the unit-test projects listed in this script."
    exit 1
}

$root = $PSScriptRoot
Set-Location $root

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet csharpier check .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet restore Graft.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build Graft.slnx --no-restore -c Debug
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$tests = @(
    "tests/Graft.Protocol.Tests/Graft.Protocol.Tests.csproj",
    "tests/Graft.TestUtilities.Tests/Graft.TestUtilities.Tests.csproj",
    "tests/Graft.Instrumentation.Tests/Graft.Instrumentation.Tests.csproj",
    "tests/Graft.Instrumentation.Surface.Tests/Graft.Instrumentation.Surface.Tests.csproj",
    "tests/Graft.Instrumentation.Analyzer.Tests/Graft.Instrumentation.Analyzer.Tests.csproj",
    "tests/Graft.Instrumentation.Wpf.Tests/Graft.Instrumentation.Wpf.Tests.csproj",
    "tests/Graft.Core.Tests/Graft.Core.Tests.csproj",
    "tests/Graft.McpServer.Tests/Graft.McpServer.Tests.csproj"
)

foreach ($project in $tests) {
    $filterArgs = @()
    if ($project -match 'Graft\.Core\.Tests|Graft\.McpServer\.Tests') {
        $filterArgs = @("--filter", "Category!=UI")
    }

    dotnet test $project --no-build --no-restore -c Debug @filterArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "build.ps1 completed successfully."
