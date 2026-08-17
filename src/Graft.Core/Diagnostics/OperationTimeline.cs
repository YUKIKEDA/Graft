using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Collects post-operation PNG frames and finalizes an HTML timeline viewer.
/// </summary>
internal sealed class OperationTimeline
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly TimelineOptions _options;
    private readonly Func<CancellationToken, Task<byte[]>> _capturePng;
    private readonly object _gate = new();
    private readonly List<TimelineFrame> _frames = [];
    private bool _failed;
    private bool _finalized;
    private string? _indexPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationTimeline"/> class.
    /// </summary>
    /// <param name="options">Output and retention options.</param>
    /// <param name="capturePng">Captures PNG bytes of the current target window.</param>
    public OperationTimeline(TimelineOptions options, Func<CancellationToken, Task<byte[]>> capturePng)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);
        ArgumentNullException.ThrowIfNull(capturePng);
        _options = options;
        _capturePng = capturePng;
    }

    /// <summary>
    /// Gets the path to <c>index.html</c> after a successful finalize; otherwise null.
    /// </summary>
    public string? IndexPath
    {
        get
        {
            lock (_gate)
            {
                return _indexPath;
            }
        }
    }

    /// <summary>
    /// Marks that a Graft failure occurred (for <see cref="TimelineRetention.OnFailure"/>).
    /// </summary>
    public void MarkFailed()
    {
        lock (_gate)
        {
            _failed = true;
        }
    }

    /// <summary>
    /// Captures one frame after a successful operation (best-effort; never throws).
    /// </summary>
    /// <param name="action">FailureSteps / action id.</param>
    /// <param name="detail">Optional detail (automation id, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="pngBytes">
    /// Optional PNG to store as the frame. When omitted, <c>capturePng</c> (target window) is used.
    /// Pass the bytes from an element-clip screenshot so the frame matches the clip label.
    /// </param>
    /// <returns>A task that completes when the frame is stored or skipped.</returns>
    public async Task CaptureAfterAsync(string action, string? detail, CancellationToken cancellationToken = default, byte[]? pngBytes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        lock (_gate)
        {
            if (_finalized)
            {
                return;
            }
        }

        try
        {
            var png = pngBytes is { Length: > 0 } ? pngBytes : await _capturePng(cancellationToken).ConfigureAwait(false);
            if (png is null || png.Length == 0)
            {
                return;
            }

            var label = string.IsNullOrWhiteSpace(detail) ? action : $"{action}:{detail}";
            Directory.CreateDirectory(FramesDirectory);

            int index;
            string fileName;
            lock (_gate)
            {
                if (_finalized)
                {
                    return;
                }

                index = _frames.Count + 1;
                fileName = index.ToString("D4", CultureInfo.InvariantCulture) + ".png";
                _frames.Add(
                    new TimelineFrame
                    {
                        Index = index,
                        FileName = fileName,
                        Label = label,
                        Action = action,
                        Detail = detail,
                    }
                );
            }

            var path = Path.Combine(FramesDirectory, fileName);
            await File.WriteAllBytesAsync(path, png, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Timeline must not fail the driving operation.
        }
    }

    /// <summary>
    /// Writes HTML/JSON when retention allows; otherwise deletes the output directory.
    /// </summary>
    /// <returns>Path to <c>index.html</c>, or null when discarded / empty.</returns>
    public string? FinalizeArtifacts()
    {
        lock (_gate)
        {
            if (_finalized)
            {
                return _indexPath;
            }

            _finalized = true;

            var keep = _options.Retention == TimelineRetention.Always || (_options.Retention == TimelineRetention.OnFailure && _failed);

            if (!keep || _frames.Count == 0)
            {
                TryDeleteOutput();
                _indexPath = null;
                return null;
            }

            Directory.CreateDirectory(_options.OutputDirectory);
            var manifestPath = Path.Combine(_options.OutputDirectory, "timeline.json");
            var indexPath = Path.Combine(_options.OutputDirectory, "index.html");
            var delay = _options.FrameDelayMilliseconds;
            if (delay <= 0)
            {
                delay = TimelineOptions.DefaultFrameDelayMilliseconds;
            }

            var manifest = new TimelineManifest { FrameDelayMilliseconds = delay, Frames = _frames.ToArray() };

            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), Encoding.UTF8);
            File.WriteAllText(indexPath, TimelineHtml.Build(manifest), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _indexPath = indexPath;
            return _indexPath;
        }
    }

    private string FramesDirectory => Path.Combine(_options.OutputDirectory, "frames");

    private void TryDeleteOutput()
    {
        try
        {
            if (Directory.Exists(_options.OutputDirectory))
            {
                Directory.Delete(_options.OutputDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort discard.
        }
    }

    private sealed class TimelineFrame
    {
        public int Index { get; init; }

        public string FileName { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public string Action { get; init; } = string.Empty;

        public string? Detail { get; init; }
    }

    private sealed class TimelineManifest
    {
        public int FrameDelayMilliseconds { get; init; }

        public TimelineFrame[] Frames { get; init; } = [];
    }

    private static class TimelineHtml
    {
        public static string Build(TimelineManifest manifest)
        {
            var framesJson = JsonSerializer.Serialize(manifest.Frames, JsonOptions);
            var delay = manifest.FrameDelayMilliseconds.ToString(CultureInfo.InvariantCulture);
            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8" />
                  <title>Graft operation timeline</title>
                  <style>
                    body { font-family: Segoe UI, sans-serif; margin: 16px; background: #111; color: #eee; }
                    img { max-width: 100%; border: 1px solid #444; background: #222; }
                    .label { font-size: 18px; margin: 8px 0; min-height: 1.4em; }
                    .meta { color: #aaa; margin-bottom: 12px; }
                    .controls button, .controls select { margin-right: 8px; padding: 6px 10px; }
                  </style>
                </head>
                <body>
                  <h1>Graft operation timeline</h1>
                  <div class="meta" id="meta"></div>
                  <div class="label" id="label"></div>
                  <div class="controls">
                    <button type="button" id="prev">Prev</button>
                    <button type="button" id="play">Play</button>
                    <button type="button" id="next">Next</button>
                    <label>Speed
                      <select id="speed">
                        <option value="0.5">0.5x</option>
                        <option value="1" selected>1x</option>
                        <option value="2">2x</option>
                      </select>
                    </label>
                  </div>
                  <p><img id="frame" alt="timeline frame" /></p>
                  <script>
                    const frames = {{framesJson}};
                    const baseDelay = {{delay}};
                    let i = 0;
                    let timer = null;
                    const img = document.getElementById('frame');
                    const label = document.getElementById('label');
                    const meta = document.getElementById('meta');
                    const speedEl = document.getElementById('speed');

                    function show(idx) {
                      if (!frames.length) return;
                      i = (idx + frames.length) % frames.length;
                      const f = frames[i];
                      img.src = 'frames/' + f.fileName;
                      label.textContent = f.label;
                      meta.textContent = 'Frame ' + (i + 1) + ' / ' + frames.length;
                    }

                    function stop() {
                      if (timer) { clearTimeout(timer); timer = null; }
                      document.getElementById('play').textContent = 'Play';
                    }

                    function schedule() {
                      stop();
                      document.getElementById('play').textContent = 'Pause';
                      const factor = parseFloat(speedEl.value) || 1;
                      timer = setTimeout(() => {
                        show(i + 1);
                        schedule();
                      }, Math.max(50, baseDelay / factor));
                    }

                    document.getElementById('prev').onclick = () => { stop(); show(i - 1); };
                    document.getElementById('next').onclick = () => { stop(); show(i + 1); };
                    document.getElementById('play').onclick = () => {
                      if (timer) { stop(); } else { schedule(); }
                    };
                    speedEl.onchange = () => { if (timer) schedule(); };
                    show(0);
                    if (frames.length) schedule();
                  </script>
                </body>
                </html>
                """;
        }
    }
}
