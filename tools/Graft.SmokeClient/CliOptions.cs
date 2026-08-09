namespace Graft.SmokeClient;

internal enum SmokeMode
{
    Launch,
    Connect,
}

internal sealed class CliOptions
{
    public required SmokeMode Mode { get; init; }

    public string? AppPath { get; init; }

    public string? PipeName { get; init; }

    public string Token { get; init; } = "graft-smoke-token";

    public int TimeoutSec { get; init; } = 30;

    public static bool TryParse(string[] args, out CliOptions? options, out string? error)
    {
        options = null;
        error = null;

        if (args.Length == 0)
        {
            error = "Missing mode. Use: launch | connect";
            return false;
        }

        SmokeMode mode;
        if (string.Equals(args[0], "launch", StringComparison.OrdinalIgnoreCase))
        {
            mode = SmokeMode.Launch;
        }
        else if (string.Equals(args[0], "connect", StringComparison.OrdinalIgnoreCase))
        {
            mode = SmokeMode.Connect;
        }
        else
        {
            error = $"Unknown mode '{args[0]}'. Use: launch | connect";
            return false;
        }

        string? app = null;
        string? pipeName = null;
        var token = "graft-smoke-token";
        var timeoutSec = 30;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Unexpected argument '{arg}'.";
                return false;
            }

            static bool ReadValue(
                string[] a,
                ref int index,
                out string value,
                out string? readError
            )
            {
                if (index + 1 >= a.Length)
                {
                    value = string.Empty;
                    readError = $"Missing value for '{a[index]}'.";
                    return false;
                }

                index++;
                value = a[index];
                readError = null;
                return true;
            }

            switch (arg)
            {
                case "--app":
                    if (!ReadValue(args, ref i, out var appValue, out error))
                    {
                        return false;
                    }

                    app = appValue;
                    break;
                case "--pipe-name":
                    if (!ReadValue(args, ref i, out var pipeValue, out error))
                    {
                        return false;
                    }

                    pipeName = pipeValue;
                    break;
                case "--token":
                    if (!ReadValue(args, ref i, out var tokenValue, out error))
                    {
                        return false;
                    }

                    token = tokenValue;
                    break;
                case "--timeout-sec":
                    if (!ReadValue(args, ref i, out var timeoutValue, out error))
                    {
                        return false;
                    }

                    if (!int.TryParse(timeoutValue, out timeoutSec) || timeoutSec <= 0)
                    {
                        error = "--timeout-sec must be a positive integer.";
                        return false;
                    }

                    break;
                default:
                    error = $"Unknown option '{arg}'.";
                    return false;
            }
        }

        if (mode == SmokeMode.Connect && string.IsNullOrWhiteSpace(pipeName))
        {
            error = "connect requires --pipe-name.";
            return false;
        }

        options = new CliOptions
        {
            Mode = mode,
            AppPath = app,
            PipeName = pipeName,
            Token = token,
            TimeoutSec = timeoutSec,
        };
        return true;
    }

    public static string Usage { get; } =
        """
            Graft.SmokeClient — M0 Handshake + GetTree smoke tool

            Usage:
              Graft.SmokeClient launch [--app <csproj|exe>] [--pipe-name <name>] [--token <secret>] [--timeout-sec <n>]
              Graft.SmokeClient connect --pipe-name <name> [--token <secret>] [--timeout-sec <n>]

            Launch is the M0 demo path: starts SampleWpfApp (GraftTest) with GRAFT_* env vars,
            then Handshake + GetTree and prints SampleButton name/bounds.
            """;
}
