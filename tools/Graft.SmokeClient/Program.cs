using Graft.Core;
using Graft.Protocol;
using Graft.Protocol.Messages;
using Graft.SmokeClient;

if (!CliOptions.TryParse(args, out var options, out var parseError) || options is null)
{
    Console.Error.WriteLine(parseError);
    Console.Error.WriteLine();
    Console.Error.WriteLine(CliOptions.Usage);
    return 1;
}

try
{
    return await RunAsync(options).ConfigureAwait(false);
}
catch (GraftException ex)
{
    Console.Error.WriteLine($"{ex.Code}: {ex.Message}");
    return 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine($"{GraftErrorCodes.ActionTimeout}: Timed out.");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"{GraftErrorCodes.ActionFailed}: {ex.Message}");
    return 1;
}

static async Task<int> RunAsync(CliOptions options)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSec));
    var cancellationToken = cts.Token;
    var timeout = TimeSpan.FromSeconds(options.TimeoutSec);

    if (options.Mode == SmokeMode.Launch)
    {
        var appPath = options.AppPath ?? SampleAppPath.ResolveDefault();
        var pipeName = string.IsNullOrWhiteSpace(options.PipeName) ? "graft-smoke-" + Guid.NewGuid().ToString("N") : options.PipeName!;

        Console.WriteLine($"Launching {appPath}");
        Console.WriteLine($"Pipe={pipeName}");

        await using var session = await Application
            .LaunchAsync(
                new LaunchOptions
                {
                    AppPath = appPath,
                    PipeName = pipeName,
                    Token = options.Token,
                    Timeout = timeout,
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        session.WaitOptions = new WaitOptions { ActionTimeout = timeout, ExpectTimeout = timeout };
        await RunOnSessionAsync(session, options, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    var connectPipe = options.PipeName ?? throw new GraftException(GraftErrorCodes.ActionFailed, "connect requires --pipe-name.");
    await using var connection = await Application.ConnectAsync(connectPipe, options.Token, timeout, cancellationToken).ConfigureAwait(false);
    await RunOnConnectionAsync(connection, options, cancellationToken).ConfigureAwait(false);
    return 0;
}

static async Task RunOnSessionAsync(GraftSession session, CliOptions options, CancellationToken cancellationToken)
{
    var button = await session
        .GetByAutomationId(TreeSearch.SampleButtonAutomationId)
        .ExpectVisibleAsync(true, cancellationToken)
        .ConfigureAwait(false);
    PrintSampleButton(button);

    var shot = await session.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
    var screenshotPath = ResolveScreenshotPath(options.ScreenshotOut);
    await shot.SaveAsync(screenshotPath, cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Screenshot format={shot.Format} size={shot.Width}x{shot.Height} bytes={shot.PngBytes.Length} path={screenshotPath}");

    await session.GetByAutomationId(TreeSearch.SampleButtonAutomationId).InvokeAsync(cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Invoked {TreeSearch.SampleButtonAutomationId}");

    var status = await session
        .GetByAutomationId(TreeSearch.StatusTextAutomationId)
        .ExpectNameAsync("Clicked 1", cancellationToken)
        .ConfigureAwait(false);
    Console.WriteLine($"StatusText name={status.Name}");
}

static async Task RunOnConnectionAsync(AgentConnection connection, CliOptions options, CancellationToken cancellationToken)
{
    var button = await WaitForSampleButtonAsync(connection, cancellationToken).ConfigureAwait(false);
    PrintSampleButton(button);

    var (meta, pngBytes) = await connection.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
    var screenshotPath = ResolveScreenshotPath(options.ScreenshotOut);
    await File.WriteAllBytesAsync(screenshotPath, pngBytes, cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Screenshot format={meta.Format} size={meta.Width}x{meta.Height} bytes={meta.ByteLength} path={screenshotPath}");

    await connection.InvokeAsync(TreeSearch.SampleButtonAutomationId, cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Invoked {TreeSearch.SampleButtonAutomationId}");

    var status = await WaitForStatusTextAsync(connection, "Clicked 1", cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"StatusText name={status.Name}");
}

static string ResolveScreenshotPath(string? screenshotOut)
{
    if (!string.IsNullOrWhiteSpace(screenshotOut))
    {
        var full = Path.GetFullPath(screenshotOut);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return full;
    }

    return Path.Combine(Path.GetTempPath(), $"graft-smoke-{Guid.NewGuid():N}.png");
}

static async Task<TreeNode> WaitForSampleButtonAsync(AgentConnection connection, CancellationToken cancellationToken)
{
    GraftException? last = null;
    for (var attempt = 0; attempt < 50; attempt++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var tree = await connection.GetTreeAsync(cancellationToken).ConfigureAwait(false);
            var button = TreeSearch.FindByAutomationId(tree.Root, TreeSearch.SampleButtonAutomationId);
            if (button is not null)
            {
                return button;
            }

            last = new GraftException(GraftErrorCodes.ElementNotFound, $"Element '{TreeSearch.SampleButtonAutomationId}' was not in the tree yet.");
        }
        catch (GraftException ex) when (ex.Code is GraftErrorCodes.ActionFailed or GraftErrorCodes.ElementNotFound)
        {
            last = ex;
        }

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }

    throw last ?? new GraftException(GraftErrorCodes.ElementNotFound, $"Element '{TreeSearch.SampleButtonAutomationId}' not found.");
}

static async Task<TreeNode> WaitForStatusTextAsync(AgentConnection connection, string expectedName, CancellationToken cancellationToken)
{
    GraftException? last = null;
    for (var attempt = 0; attempt < 50; attempt++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var tree = await connection.GetTreeAsync(cancellationToken).ConfigureAwait(false);
            var status = TreeSearch.FindByAutomationId(tree.Root, TreeSearch.StatusTextAutomationId);
            if (status is not null && string.Equals(status.Name, expectedName, StringComparison.Ordinal))
            {
                return status;
            }

            if (status is null)
            {
                last = new GraftException(GraftErrorCodes.ElementNotFound, $"Element '{TreeSearch.StatusTextAutomationId}' was not in the tree yet.");
            }
            else
            {
                last = new GraftException(GraftErrorCodes.ExpectFailed, $"StatusText name was '{status.Name}', expected '{expectedName}'.");
            }
        }
        catch (GraftException ex) when (IsRetryableStatusError(ex.Code))
        {
            last = ex;
        }

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }

    throw last ?? new GraftException(GraftErrorCodes.ExpectFailed, $"StatusText did not become '{expectedName}'.");
}

static bool IsRetryableStatusError(string code) =>
    code is GraftErrorCodes.ActionFailed or GraftErrorCodes.ElementNotFound or GraftErrorCodes.ExpectFailed;

static void PrintSampleButton(TreeNode button)
{
    Console.WriteLine(
        $"SampleButton name={button.Name} bounds=(x={button.Bounds.X}, y={button.Bounds.Y}, width={button.Bounds.Width}, height={button.Bounds.Height})"
    );
}
