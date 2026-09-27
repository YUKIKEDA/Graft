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

    /// <summary>
    /// Maximum number of stdout/stderr lines kept for launch diagnostics.
    /// </summary>
    internal const int MaxOutputTailLines = 40;

    public static Process Start(
        string appPath,
        string pipeName,
        string token,
        string configuration,
        IReadOnlyDictionary<string, string>? extraEnvironment = null
    ) => Start(appPath, pipeName, token, configuration, extraEnvironment, out _);

    public static Process Start(
        string appPath,
        string pipeName,
        string token,
        string configuration,
        IReadOnlyDictionary<string, string>? extraEnvironment,
        out OutputTail outputTail
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

        // Kill the app (and anything it spawns) if this controller process dies without disposing the session.
        _ = ChildProcessJob.TryAssign(process);

        // Drain stdout/stderr so the child cannot block on full pipes, keeping the last lines for
        // diagnostics when the app exits before the handshake. Event-based reads never throw into
        // an unobserved task.
        var tail = new OutputTail(MaxOutputTailLines);
        process.OutputDataReceived += (_, e) => tail.Add(e.Data);
        process.ErrorDataReceived += (_, e) => tail.Add(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        outputTail = tail;
        return process;
    }

    /// <summary>
    /// Thread-safe ring buffer of the most recent child-process output lines.
    /// </summary>
    internal sealed class OutputTail
    {
        private readonly Queue<string> _lines = new();
        private readonly int _capacity;

        public OutputTail(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            lock (_lines)
            {
                _lines.Enqueue(line);
                while (_lines.Count > _capacity)
                {
                    _ = _lines.Dequeue();
                }
            }
        }

        public string Snapshot()
        {
            lock (_lines)
            {
                return string.Join(Environment.NewLine, _lines);
            }
        }
    }
}
