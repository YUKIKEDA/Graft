namespace Graft.Core.Diagnostics;

/// <summary>
/// Ring buffer of recent Core operations attached to failure reports.
/// </summary>
public sealed class OperationLog
{
    /// <summary>
    /// Default capacity for the ring buffer.
    /// </summary>
    public const int DefaultCapacity = 32;

    private readonly object _gate = new();
    private readonly Queue<OperationLogEntry> _entries;
    private readonly int _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationLog"/> class.
    /// </summary>
    /// <param name="capacity">Maximum entries retained (must be positive).</param>
    public OperationLog(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be positive."
            );
        }

        _capacity = capacity;
        _entries = new Queue<OperationLogEntry>(capacity);
    }

    /// <summary>
    /// Appends an operation, dropping the oldest entry when full.
    /// </summary>
    /// <param name="action">Action id (see <see cref="FailureSteps"/>).</param>
    /// <param name="detail">Optional short detail.</param>
    public void Record(string action, string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        var entry = new OperationLogEntry
        {
            At = DateTimeOffset.UtcNow,
            Action = action,
            Detail = detail,
        };

        lock (_gate)
        {
            if (_entries.Count >= _capacity)
            {
                _ = _entries.Dequeue();
            }

            _entries.Enqueue(entry);
        }
    }

    /// <summary>
    /// Returns a snapshot of recent operations (oldest first).
    /// </summary>
    /// <returns>Copied entries.</returns>
    public IReadOnlyList<OperationLogEntry> Snapshot()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }
}
