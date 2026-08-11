using System.Diagnostics;

namespace SampleTodoApp.FlaUI.Tests;

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

    /// <summary>
    /// Builds Debug if needed and returns <c>SampleTodoApp.exe</c> path.
    /// </summary>
    public static string EnsureDebugExe()
    {
        var projectPath = ResolveProjectPath();
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var exe = Path.Combine(projectDir, "bin", "Debug", "net8.0-windows", "SampleTodoApp.exe");
        if (!File.Exists(exe))
        {
            BuildDebug(projectPath);
        }

        if (!File.Exists(exe))
        {
            throw new FileNotFoundException(
                "SampleTodoApp.exe not found after Debug build.",
                exe
            );
        }

        return exe;
    }

    private static void BuildDebug(string projectPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Debug --nologo -v q",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("dotnet build failed to start.");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"dotnet build SampleTodoApp failed (exit {process.ExitCode}).\n{stdout}\n{stderr}"
            );
        }
    }
}
