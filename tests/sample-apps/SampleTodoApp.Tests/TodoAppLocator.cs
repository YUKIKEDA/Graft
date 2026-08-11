namespace SampleTodoApp.Tests;

internal static class TodoAppLocator
{
    public static string ResolveProjectPath()
    {
        var sibling = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "SampleTodoApp",
                "SampleTodoApp.csproj"
            )
        );
        if (File.Exists(sibling))
        {
            return sibling;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "tests",
                "sample-apps",
                "SampleTodoApp",
                "SampleTodoApp.csproj"
            );
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate SampleTodoApp.csproj.");
    }
}
