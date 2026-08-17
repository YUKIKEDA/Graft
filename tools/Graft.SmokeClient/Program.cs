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
catch (SmokeException ex)
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

    if (options.Mode == SmokeMode.Launch)
    {
        var appPath = options.AppPath ?? SampleLauncher.ResolveDefaultAppPath();
        var pipeName = string.IsNullOrWhiteSpace(options.PipeName) ? "graft-smoke-" + Guid.NewGuid().ToString("N") : options.PipeName!;
        var token = options.Token;

        Console.WriteLine($"Launching {appPath}");
        Console.WriteLine($"Pipe={pipeName}");

        using var process = SampleLauncher.Start(appPath, pipeName, token);
        try
        {
            await using var client = await AgentClient
                .ConnectAsync(pipeName, TimeSpan.FromSeconds(options.TimeoutSec), cancellationToken)
                .ConfigureAwait(false);
            await RunM1ScenarioAsync(client, options, token, cancellationToken).ConfigureAwait(false);
            return 0;
        }
        finally
        {
            TryKill(process);
        }
    }

    // Connect mode
    var connectPipe = options.PipeName ?? throw new SmokeException(GraftErrorCodes.ActionFailed, "connect requires --pipe-name.");

    await using (
        var client = await AgentClient.ConnectAsync(connectPipe, TimeSpan.FromSeconds(options.TimeoutSec), cancellationToken).ConfigureAwait(false)
    )
    {
        await RunM1ScenarioAsync(client, options, options.Token, cancellationToken).ConfigureAwait(false);
        return 0;
    }
}

static async Task RunM1ScenarioAsync(AgentClient client, CliOptions options, string token, CancellationToken cancellationToken)
{
    await client.HandshakeAsync(token, cancellationToken).ConfigureAwait(false);

    var button = await WaitForSampleButtonAsync(client, cancellationToken).ConfigureAwait(false);
    PrintSampleButton(button);

    var (meta, pngBytes) = await client.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
    var screenshotPath = ResolveScreenshotPath(options.ScreenshotOut);
    await File.WriteAllBytesAsync(screenshotPath, pngBytes, cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Screenshot format={meta.Format} size={meta.Width}x{meta.Height} bytes={meta.ByteLength} path={screenshotPath}");

    await client.InvokeAsync(TreeSearch.SampleButtonAutomationId, cancellationToken).ConfigureAwait(false);
    Console.WriteLine($"Invoked {TreeSearch.SampleButtonAutomationId}");

    var status = await WaitForStatusTextAsync(client, "Clicked 1", cancellationToken).ConfigureAwait(false);
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

static async Task<TreeNode> WaitForSampleButtonAsync(AgentClient client, CancellationToken cancellationToken)
{
    SmokeException? last = null;
    for (var attempt = 0; attempt < 50; attempt++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var tree = await client.GetTreeAsync(cancellationToken).ConfigureAwait(false);
            var button = TreeSearch.FindByAutomationId(tree.Root, TreeSearch.SampleButtonAutomationId);
            if (button is not null)
            {
                return button;
            }

            last = new SmokeException(GraftErrorCodes.ElementNotFound, $"Element '{TreeSearch.SampleButtonAutomationId}' was not in the tree yet.");
        }
        catch (SmokeException ex) when (ex.Code is GraftErrorCodes.ActionFailed or GraftErrorCodes.ElementNotFound)
        {
            // MainWindow may not be ready immediately after process start.
            last = ex;
        }

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }

    throw last ?? new SmokeException(GraftErrorCodes.ElementNotFound, $"Element '{TreeSearch.SampleButtonAutomationId}' not found.");
}

static async Task<TreeNode> WaitForStatusTextAsync(AgentClient client, string expectedName, CancellationToken cancellationToken)
{
    SmokeException? last = null;
    for (var attempt = 0; attempt < 50; attempt++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var tree = await client.GetTreeAsync(cancellationToken).ConfigureAwait(false);
            var status = TreeSearch.FindByAutomationId(tree.Root, TreeSearch.StatusTextAutomationId);
            if (status is not null && string.Equals(status.Name, expectedName, StringComparison.Ordinal))
            {
                return status;
            }

            if (status is null)
            {
                last = new SmokeException(GraftErrorCodes.ElementNotFound, $"Element '{TreeSearch.StatusTextAutomationId}' was not in the tree yet.");
            }
            else
            {
                last = new SmokeException(GraftErrorCodes.ExpectFailed, $"StatusText name was '{status.Name}', expected '{expectedName}'.");
            }
        }
        catch (SmokeException ex) when (IsRetryableStatusError(ex.Code))
        {
            last = ex;
        }

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }

    throw last ?? new SmokeException(GraftErrorCodes.ExpectFailed, $"StatusText did not become '{expectedName}'.");
}

static bool IsRetryableStatusError(string code) =>
    code is GraftErrorCodes.ActionFailed or GraftErrorCodes.ElementNotFound or GraftErrorCodes.ExpectFailed;

static void PrintSampleButton(TreeNode button)
{
    Console.WriteLine(
        $"SampleButton name={button.Name} bounds=(x={button.Bounds.X}, y={button.Bounds.Y}, width={button.Bounds.Width}, height={button.Bounds.Height})"
    );
}

static void TryKill(System.Diagnostics.Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            _ = process.WaitForExit(5000);
        }
    }
    catch
    {
        // Best-effort cleanup for Launch mode.
    }
}
