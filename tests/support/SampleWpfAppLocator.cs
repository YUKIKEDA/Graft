namespace Graft.TestSupport;

/// <summary>
/// Resolves SampleWpfApp.csproj from the repo layout.
/// </summary>
internal static class SampleWpfAppLocator
{
    /// <summary>
    /// Returns the full path to SampleWpfApp.csproj.
    /// </summary>
    /// <returns>The project file path.</returns>
    /// <exception cref="InvalidOperationException">The project file is not under the test output directory.</exception>
    public static string ResolveProjectPath()
    {
        // SampleWpfApp.Tests output sits next to the app project.
        var sibling = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SampleWpfApp", "SampleWpfApp.csproj"));
        if (File.Exists(sibling))
        {
            return sibling;
        }

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

    /// <summary>
    /// Returns the repository root that contains the sample app.
    /// </summary>
    /// <returns>The repository root path.</returns>
    public static string ResolveRepoRoot()
    {
        var project = new FileInfo(ResolveProjectPath());
        return project.Directory!.Parent!.Parent!.Parent!.FullName;
    }
}
