namespace Graft.Instrumentation.Dialogs;

/// <summary>
/// One-shot WPF MessageBox arm state for the in-process agent (test seam).
/// </summary>
public static class MessageBoxArm
{
    private static readonly object Gate = new();
    private static readonly HashSet<string> AllowedResults = new(StringComparer.OrdinalIgnoreCase) { "None", "OK", "Cancel", "Yes", "No" };

    private static string? _result;

    /// <summary>
    /// Arms the next <c>MessageBox.Show</c> to return <paramref name="result"/> (one-shot).
    /// </summary>
    /// <param name="result">
    /// Result name matching <c>MessageBoxResult</c>: <c>None</c>, <c>OK</c>, <c>Cancel</c>,
    /// <c>Yes</c>, or <c>No</c>.
    /// </param>
    public static void ArmResult(string result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        if (!AllowedResults.Contains(result))
        {
            throw new ArgumentException("result must be one of: None, OK, Cancel, Yes, No.", nameof(result));
        }

        lock (Gate)
        {
            // Canonical casing for MessageBoxResult.ToString() parity.
            _result = AllowedResults.First(r => string.Equals(r, result, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Clears any pending arm without consuming (tests).
    /// </summary>
    public static void Reset()
    {
        lock (Gate)
        {
            _result = null;
        }
    }

    /// <summary>
    /// Tries to consume a pending arm (one-shot).
    /// </summary>
    /// <param name="result">Armed result name when consumed.</param>
    /// <returns>True when an arm was consumed.</returns>
    public static bool TryConsume(out string? result)
    {
        lock (Gate)
        {
            if (_result is null)
            {
                result = null;
                return false;
            }

            result = _result;
            _result = null;
            return true;
        }
    }
}
