using System.Diagnostics;
using Graft.Protocol;

namespace Graft.Core;

/// <summary>
/// Starts an instrumented app process with Graft environment variables.
/// </summary>
internal static class AppProcessLauncher
{
    private const string EnableEnv = "GRAFT_ENABLE";
    private const string PipeNameEnv = "GRAFT_PIPE_NAME";
    private const string ConnectTokenEnv = "GRAFT_CONNECT_TOKEN";

    public static Process Start(
        string appPath,
        string pipeName,
        string token,
        string configuration,
        IReadOnlyDictionary<string, string>? extraEnvironment = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);

        if (!File.Exists(appPath))
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, $"App path not found: {appPath}");
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

        if (extraEnvironment is not null)
        {
            foreach (var (key, value) in extraEnvironment)
            {
                if (string.IsNullOrWhiteSpace(key) || value is null)
                {
                    continue;
                }

                psi.Environment[key] = value;
            }
        }

        if (appPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            psi.FileName = "dotnet";
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add(appPath);
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(configuration);
        }
        else
        {
            psi.FileName = appPath;
        }

        var process = Process.Start(psi) ?? throw new GraftException(GraftErrorCodes.ActionFailed, "Failed to start application process.");

        // Drain stdout/stderr so the child cannot block on full pipes.
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        return process;
    }
}
