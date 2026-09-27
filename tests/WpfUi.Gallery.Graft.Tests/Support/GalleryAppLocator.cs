namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Locates the gitignored WPF UI Gallery project under <c>tests/wpfui</c>.
/// </summary>
internal static class GalleryAppLocator
{
    private const string RelativeProjectPath = "tests/wpfui/src/Wpf.Ui.Gallery/Wpf.Ui.Gallery.csproj";

    /// <summary>
    /// Gets a value indicating whether the Gallery csproj is present on disk.
    /// </summary>
    public static bool IsAvailable => TryResolveProjectPath() is not null;

    /// <summary>
    /// Resolves the absolute path to <c>Wpf.Ui.Gallery.csproj</c>.
    /// </summary>
    /// <returns>Absolute csproj path.</returns>
    /// <exception cref="InvalidOperationException">Gallery is missing (clone into tests/wpfui).</exception>
    public static string ResolveProjectPath() =>
        TryResolveProjectPath()
        ?? throw new InvalidOperationException(
            "Could not locate tests/wpfui/src/Wpf.Ui.Gallery/Wpf.Ui.Gallery.csproj. "
                + "Clone https://github.com/lepoco/wpfui into tests/wpfui and apply the local Graft patch."
        );

    /// <summary>
    /// Tries to resolve the Gallery csproj without throwing.
    /// </summary>
    /// <returns>Absolute path, or null when missing.</returns>
    public static string? TryResolveProjectPath()
    {
        var fromOutput = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "wpfui", "src", "Wpf.Ui.Gallery", "Wpf.Ui.Gallery.csproj")
        );
        if (File.Exists(fromOutput))
        {
            return fromOutput;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, RelativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
