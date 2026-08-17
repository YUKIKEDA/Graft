using System.Diagnostics;

namespace Graft.SmokeClient;

internal static class SampleLauncher
{
    private const string EnableEnv = "GRAFT_ENABLE";
    private const string PipeNameEnv = "GRAFT_PIPE_NAME";
    private const string ConnectTokenEnv = "GRAFT_CONNECT_TOKEN";

    public static string ResolveDefaultAppPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "sample-apps", "SampleWpfApp", "SampleWpfApp.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Also handle running from repo root / tools output without walking past drive root.
            dir = dir.Parent;
        }

        throw new SmokeException(Graft.Protocol.GraftErrorCodes.ActionFailed, "Could not locate SampleWpfApp.csproj. Pass --app <path>.");
    }

    public static Process Start(string appPath, string pipeName, string token)
    {
        if (!File.Exists(appPath))
        {
            throw new SmokeException(Graft.Protocol.GraftErrorCodes.ActionFailed, $"App path not found: {appPath}");
        }

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
        };

        psi.Environment[EnableEnv] = "1";
        psi.Environment[PipeNameEnv] = pipeName;
        psi.Environment[ConnectTokenEnv] = token;

        if (appPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            psi.FileName = "dotnet";
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add(appPath);
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("GraftTest");
        }
        else
        {
            psi.FileName = appPath;
        }

        var process = Process.Start(psi) ?? throw new SmokeException(Graft.Protocol.GraftErrorCodes.ActionFailed, "Failed to start sample process.");

        // Drain stdout/stderr so the child cannot block on full pipes.
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        return process;
    }
}
