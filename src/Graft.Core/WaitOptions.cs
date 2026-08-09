namespace Graft.Core;

/// <summary>
/// Timeouts for Core-side wait / expect polling.
/// </summary>
public sealed class WaitOptions
{
    /// <summary>
    /// Gets the default pre-action wait timeout (5 seconds).
    /// </summary>
    public static TimeSpan DefaultActionTimeout { get; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the default expect timeout (10 seconds).
    /// </summary>
    public static TimeSpan DefaultExpectTimeout { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the default poll interval (100 ms).
    /// </summary>
    public static TimeSpan DefaultPollInterval { get; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets the timeout used before actions such as invoke.
    /// </summary>
    public TimeSpan ActionTimeout { get; init; } = DefaultActionTimeout;

    /// <summary>
    /// Gets the timeout used for expect / assert polling.
    /// </summary>
    public TimeSpan ExpectTimeout { get; init; } = DefaultExpectTimeout;

    /// <summary>
    /// Gets the delay between getTree polls.
    /// </summary>
    public TimeSpan PollInterval { get; init; } = DefaultPollInterval;
}
