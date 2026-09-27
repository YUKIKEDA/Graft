namespace Graft.Instrumentation.Dialogs;

#if GRAFT_TEST

/// <summary>
/// One-shot file or folder dialog arm for the in-process agent (test seam).
/// </summary>
/// <remarks>
/// <see cref="OpenFile"/>, <see cref="SaveFile"/>, and <see cref="OpenFolder"/> do not share state.
/// The wire methods stay separate.
/// </remarks>
public sealed class DialogArm
{
    private readonly object _gate = new();
    private ArmKind _kind = ArmKind.None;
    private string? _path;

    private DialogArm() { }

    /// <summary>
    /// Gets the open-file arm.
    /// </summary>
    public static DialogArm OpenFile { get; } = new();

    /// <summary>
    /// Gets the save-file arm.
    /// </summary>
    public static DialogArm SaveFile { get; } = new();

    /// <summary>
    /// Gets the open-folder arm.
    /// </summary>
    public static DialogArm OpenFolder { get; } = new();

    /// <summary>
    /// Arms the next consumption to return <paramref name="path"/> (OK).
    /// </summary>
    /// <param name="path">Path to return.</param>
    public void ArmPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        lock (_gate)
        {
            _kind = ArmKind.Ok;
            _path = path;
        }
    }

    /// <summary>
    /// Arms the next consumption to return cancel (<see langword="null"/> path).
    /// </summary>
    public void ArmCancel()
    {
        lock (_gate)
        {
            _kind = ArmKind.Cancel;
            _path = null;
        }
    }

    /// <summary>
    /// Clears any pending arm without consuming (tests).
    /// </summary>
    public void Reset()
    {
        lock (_gate)
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
    public bool TryConsume(out string? path, out bool canceled)
    {
        lock (_gate)
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

#endif
