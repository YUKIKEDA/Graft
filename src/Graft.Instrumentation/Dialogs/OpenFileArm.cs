namespace Graft.Instrumentation.Dialogs;

/// <summary>
/// One-shot OpenFile dialog arm state for the in-process agent (test seam).
/// </summary>
public static class OpenFileArm
{
    private static readonly object Gate = new();
    private static ArmKind _kind = ArmKind.None;
    private static string? _path;

    /// <summary>
    /// Arms the next <see cref="OpenFileArm"/> consumption to return <paramref name="path"/> (OK).
    /// </summary>
    /// <param name="path">File path to return.</param>
    public static void ArmPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        lock (Gate)
        {
            _kind = ArmKind.Ok;
            _path = path;
        }
    }

    /// <summary>
    /// Arms the next consumption to return cancel (<see langword="null"/> path).
    /// </summary>
    public static void ArmCancel()
    {
        lock (Gate)
        {
            _kind = ArmKind.Cancel;
            _path = null;
        }
    }

    /// <summary>
    /// Clears any pending arm without consuming (tests).
    /// </summary>
    public static void Reset()
    {
        lock (Gate)
        {
            _kind = ArmKind.None;
            _path = null;
        }
    }

    /// <summary>
    /// Tries to consume a pending arm (one-shot).
    /// </summary>
    /// <param name="path">OK path when armed with a path; otherwise <see langword="null"/>.</param>
    /// <param name="canceled">True when armed for cancel.</param>
    /// <returns>True when an arm was consumed.</returns>
    public static bool TryConsume(out string? path, out bool canceled)
    {
        lock (Gate)
        {
            switch (_kind)
            {
                case ArmKind.Ok:
                    path = _path;
                    canceled = false;
                    _kind = ArmKind.None;
                    _path = null;
                    return true;
                case ArmKind.Cancel:
                    path = null;
                    canceled = true;
                    _kind = ArmKind.None;
                    _path = null;
                    return true;
                default:
                    path = null;
                    canceled = false;
                    return false;
            }
        }
    }

    private enum ArmKind
    {
        None,
        Ok,
        Cancel,
    }
}
