namespace Graft.McpServer.Tests;

/// <summary>
/// Resolves SampleWpfApp.csproj from the repo layout for MCP scenario tests.
/// </summary>
internal static class SampleAppLocator
{
    public static string ResolveProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "sample-apps", "SampleWpfApp", "SampleWpfApp.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate SampleWpfApp.csproj from the test output directory.");
    }
}
