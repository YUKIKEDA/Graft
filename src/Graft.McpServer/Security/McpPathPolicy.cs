using Graft.Core;
using Graft.Core.Scenario;
using Graft.Protocol;

namespace Graft.McpServer.Security;

/// <summary>
/// Confines file-system paths received from MCP callers (LLMs, semi-trusted) to allowed roots.
/// </summary>
/// <remarks>
/// Applies to app paths that are launched, Scenario files that are read, screenshot destinations
/// that are written, and file / folder paths armed into the app's dialogs. By default the only
/// allowed root is the server's current working directory (MCP clients start servers in the
/// workspace). Configure with <see cref="AllowedRootsEnvironmentVariable"/> (list separated by
/// <see cref="Path.PathSeparator"/>) or disable with <see cref="AllowAnyPathEnvironmentVariable"/>=1.
/// Relative paths resolve against the current working directory, like the rest of Graft.Core.
/// </remarks>
public static class McpPathPolicy
{
    /// <summary>
    /// Environment variable listing allowed root directories (separated by <see cref="Path.PathSeparator"/>).
    /// </summary>
    public const string AllowedRootsEnvironmentVariable = "GRAFT_MCP_ALLOWED_ROOTS";

    /// <summary>
    /// Environment variable that disables path confinement when set to <c>1</c>.
    /// </summary>
    public const string AllowAnyPathEnvironmentVariable = "GRAFT_MCP_ALLOW_ANY_PATH";

    private static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Returns the allowed roots currently in effect (full paths); empty when confinement is disabled.
    /// </summary>
    /// <returns>Allowed root directories.</returns>
    public static IReadOnlyList<string> GetAllowedRoots()
    {
        if (Environment.GetEnvironmentVariable(AllowAnyPathEnvironmentVariable) == "1")
        {
            return Array.Empty<string>();
        }

        var configured = Environment.GetEnvironmentVariable(AllowedRootsEnvironmentVariable);
        var roots = string.IsNullOrWhiteSpace(configured)
            ? [Environment.CurrentDirectory]
            : configured.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return roots.Select(r => Path.TrimEndingDirectorySeparator(Path.GetFullPath(r))).ToArray();
    }

    /// <summary>
    /// Resolves <paramref name="path"/> to a full path and verifies it lies under an allowed root.
    /// </summary>
    /// <param name="path">Caller-supplied path (absolute or relative to the current directory).</param>
    /// <param name="purpose">Short label for the error message (e.g. <c>appPath</c>).</param>
    /// <returns>The normalized full path.</returns>
    /// <exception cref="GraftException">The path is empty or outside every allowed root (<c>action.failed</c>).</exception>
    public static string EnsureAllowed(string? path, string purpose)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, $"{purpose} must be non-empty.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, $"{purpose} is not a valid path: {ex.Message}");
        }

        if (Environment.GetEnvironmentVariable(AllowAnyPathEnvironmentVariable) == "1")
        {
            return fullPath;
        }

        var roots = GetAllowedRoots();
        if (roots.Any(root => IsUnder(fullPath, root)))
        {
            return fullPath;
        }

        throw new GraftException(
            GraftErrorCodes.ActionFailed,
            $"{purpose} '{fullPath}' is outside the allowed roots ({string.Join(", ", roots)}). "
                + $"Set {AllowedRootsEnvironmentVariable} on the MCP server to allow more directories."
        );
    }

    /// <summary>
    /// Verifies every path a Scenario would touch (launch app, screenshots, armed dialog paths).
    /// </summary>
    /// <param name="document">Parsed Scenario.</param>
    /// <param name="appPathOverride">
    /// App path override from the tool call; when set, <c>launch.appPath</c> is not used and not checked.
    /// </param>
    /// <exception cref="GraftException">A path is outside the allowed roots.</exception>
    public static void EnsureScenarioAllowed(ScenarioDocument document, string? appPathOverride)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (var operation in document.Operations)
        {
            switch (operation)
            {
                case LaunchOperation launch when string.IsNullOrWhiteSpace(appPathOverride):
                    _ = EnsureAllowed(launch.AppPath, "launch.appPath");
                    break;
                case ScreenshotOperation screenshot:
                    _ = EnsureAllowed(screenshot.Path, "screenshot.path");
                    break;
                case ArmOpenFileOperation armOpenFile:
                    _ = EnsureAllowed(armOpenFile.Path, "armOpenFile.path");
                    break;
                case ArmSaveFileOperation armSaveFile:
                    _ = EnsureAllowed(armSaveFile.Path, "armSaveFile.path");
                    break;
                case ArmOpenFolderOperation armOpenFolder:
                    _ = EnsureAllowed(armOpenFolder.Path, "armOpenFolder.path");
                    break;
            }
        }
    }

    private static bool IsUnder(string fullPath, string root)
    {
        if (string.Equals(Path.TrimEndingDirectorySeparator(fullPath), root, PathComparison))
        {
            return true;
        }

        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, PathComparison);
    }
}
