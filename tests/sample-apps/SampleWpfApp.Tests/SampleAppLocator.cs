namespace SampleWpfApp.Tests;

/// <summary>
/// Resolves the sibling SampleWpfApp project path for Launch.
/// </summary>
internal static class SampleAppLocator
{
    public static string ResolveProjectPath()
    {
        // Prefer relative-to-this-file layout when running from repo builds.
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

        throw new InvalidOperationException("Could not locate SampleWpfApp.csproj. Keep SampleWpfApp.Tests next to SampleWpfApp.");
    }
}
