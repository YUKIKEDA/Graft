using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Graft.Core.Diagnostics;
using Graft.Core.Selectors;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

public sealed class FailureReportTests
{
    /// <summary>
    /// FailureReport JSON round-trips the Phase 2 minimum fields.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Report with step expectName, expected/actual, timedOut true, selector automationId
    ///
    /// Steps:
    /// - Serialize then Deserialize via FailureReportJson
    ///
    /// Expected:
    /// - All minimum fields match the original
    /// </remarks>
    [Fact]
    public void Serialize_ThenDeserialize_PreservesMinimumFields()
    {
        var original = new FailureReport
        {
            Step = FailureSteps.ExpectName,
            Expected = "Clicked 1",
            Actual = "Ready",
            TimedOut = true,
            Selector = new FailureReportSelector { AutomationId = "StatusText" },
        };

        var json = FailureReportJson.Serialize(original);
        var decoded = FailureReportJson.Deserialize(json);

        Assert.Equal(FailureSteps.ExpectName, decoded.Step);
        Assert.Equal("Clicked 1", decoded.Expected);
        Assert.Equal("Ready", decoded.Actual);
        Assert.True(decoded.TimedOut);
        Assert.Equal("StatusText", decoded.Selector.AutomationId);
        Assert.Null(decoded.Selector.Name);
    }

    /// <summary>
    /// Null optional fields are omitted from JSON and FromSelector copies criteria.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Report with only step, timedOut false, selector from Selector.ByAutomationId
    ///
    /// Steps:
    /// - Serialize; parse as JsonDocument; FromSelector on a composite Selector
    ///
    /// Expected:
    /// - expected/actual keys absent; FromSelector mirrors AutomationId, Name, and Nth
    /// </remarks>
    [Fact]
    public void Serialize_OmitsNullOptionals_AndFromSelectorCopiesCriteria()
    {
        var report = new FailureReport
        {
            Step = FailureSteps.Invoke,
            TimedOut = false,
            Selector = FailureReportSelector.FromSelector(Selector.ByAutomationId("SampleButton")),
        };

        using var doc = JsonDocument.Parse(FailureReportJson.Serialize(report));
        var root = doc.RootElement;
        Assert.Equal("invoke", root.GetProperty("step").GetString());
        Assert.False(root.GetProperty("timedOut").GetBoolean());
        Assert.False(root.TryGetProperty("expected", out _));
        Assert.False(root.TryGetProperty("actual", out _));
        Assert.Equal("SampleButton", root.GetProperty("selector").GetProperty("automationId").GetString());

        var fromComposite = FailureReportSelector.FromSelector(
            new Selector
            {
                AutomationId = "Box",
                Name = "Hello",
                ControlType = "TextBox",
                Nth = 2,
            }
        );
        Assert.Equal("Box", fromComposite.AutomationId);
        Assert.Equal("Hello", fromComposite.Name);
        Assert.Equal("TextBox", fromComposite.ControlType);
        Assert.Equal(2, fromComposite.Nth);
        Assert.Equal(2, fromComposite.ToSelector().Nth);
    }

    /// <summary>
    /// Nth survives a failure-report JSON round-trip and comes back on the live selector.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Selector with ControlType Button and Nth 2
    ///
    /// Steps:
    /// - FromSelector, serialize the report, deserialize, ToSelector
    ///
    /// Expected:
    /// - JSON selector.nth is 2
    /// - ToSelector().Nth is 2
    /// </remarks>
    [Fact]
    public void Serialize_ThenDeserialize_PreservesNth()
    {
        var report = new FailureReport
        {
            Step = FailureSteps.Invoke,
            TimedOut = true,
            Selector = FailureReportSelector.FromSelector(new Selector { ControlType = "Button", Nth = 2 }),
        };

        using var doc = JsonDocument.Parse(FailureReportJson.Serialize(report));
        Assert.Equal(2, doc.RootElement.GetProperty("selector").GetProperty("nth").GetInt32());

        var decoded = FailureReportJson.Deserialize(FailureReportJson.Serialize(report));
        Assert.Equal(2, decoded.Selector.Nth);
        Assert.Equal(2, decoded.Selector.ToSelector().Nth);
    }

    /// <summary>
    /// HealingCandidates round-trip through FailureReport JSON.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Report with one healing candidate (score, selector, reason)
    ///
    /// Steps:
    /// - Serialize then Deserialize via FailureReportJson
    ///
    /// Expected:
    /// - healingCandidates preserved (score / automationId / reason)
    /// </remarks>
    [Fact]
    public void Serialize_ThenDeserialize_PreservesHealingCandidates()
    {
        var original = new FailureReport
        {
            Step = FailureSteps.Wait,
            TimedOut = true,
            Selector = new FailureReportSelector { AutomationId = "Gone" },
            HealingCandidates =
            [
                new HealingCandidate
                {
                    Score = 75,
                    Selector = new FailureReportSelector
                    {
                        Name = "Click Me",
                        ControlType = "Button",
                        NearAutomationId = "Main",
                    },
                    Reason = "stableIdentity",
                },
            ],
        };

        var decoded = FailureReportJson.Deserialize(FailureReportJson.Serialize(original));
        Assert.NotNull(decoded.HealingCandidates);
        Assert.Single(decoded.HealingCandidates);
        Assert.Equal(75, decoded.HealingCandidates[0].Score);
        Assert.Equal("Click Me", decoded.HealingCandidates[0].Selector.Name);
        Assert.Equal("Button", decoded.HealingCandidates[0].Selector.ControlType);
        Assert.Equal("Main", decoded.HealingCandidates[0].Selector.NearAutomationId);
        Assert.Equal("stableIdentity", decoded.HealingCandidates[0].Reason);
    }

    /// <summary>
    /// A session window timeout attaches step, recent operations, the tree, and a temp screenshot.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake agent lists one window titled Sample, returns a getTree root with automationId Main, and a 4-byte screenshot frame
    /// - GraftSession ExpectTimeout is 1 second and PollInterval is 50 milliseconds
    ///
    /// Steps:
    /// - ListWindowsAsync
    /// - WaitForWindowAsync for title Missing
    ///
    /// Expected:
    /// - GraftException action.timeout, step waitForWindow, timedOut true, empty selector
    /// - Recent operations include listWindows, and the tree automationId is Main
    /// - screenshotPath is %TEMP%\graft-fail-&lt;guid&gt;.png and the file exists
    /// </remarks>
    [Fact]
    public async Task WaitForWindow_Timeout_AttachesReport()
    {
        var pipeName = "graft-fail-" + Guid.NewGuid().ToString("N");
        var server = RunFakeAgentAsync(pipeName);
        var connection = await Application.ConnectAsync(pipeName, "secret", TimeSpan.FromSeconds(5));
        var process =
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ping -n 60 127.0.0.1 >NUL",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            ) ?? throw new InvalidOperationException("Failed to start the placeholder process.");
        var session = new GraftSession(process, connection)
        {
            WaitOptions = new WaitOptions { ExpectTimeout = TimeSpan.FromSeconds(1), PollInterval = TimeSpan.FromMilliseconds(50) },
        };
        string? screenshotPath = null;
        try
        {
            await session.ListWindowsAsync();
            var ex = await Assert.ThrowsAsync<GraftException>(() => session.WaitForWindowAsync(title: "Missing"));
            screenshotPath = ex.Report?.ScreenshotPath;

            Assert.Equal(GraftErrorCodes.ActionTimeout, ex.Code);
            Assert.Contains("waiting for window (title='Missing')", ex.Message, StringComparison.Ordinal);
            Assert.NotNull(ex.Report);
            Assert.Equal(FailureSteps.WaitForWindow, ex.Report.Step);
            Assert.True(ex.Report.TimedOut);
            Assert.Null(ex.Report.Selector.AutomationId);
            Assert.Null(ex.Report.Selector.Name);
            Assert.Null(ex.Report.Selector.ControlType);
            Assert.Null(ex.Report.Selector.NearAutomationId);
            Assert.Null(ex.Report.Selector.Nth);
            Assert.NotNull(ex.Report.RecentOperations);
            Assert.Contains(ex.Report.RecentOperations, operation => operation.Action == FailureSteps.ListWindows);
            Assert.NotNull(ex.Report.Tree);
            Assert.Equal("Main", ex.Report.Tree.AutomationId);
            Assert.NotNull(screenshotPath);
            Assert.Equal(
                Path.TrimEndingDirectorySeparator(Path.GetTempPath()),
                Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(screenshotPath)!),
                StringComparer.OrdinalIgnoreCase
            );
            var fileName = Path.GetFileName(screenshotPath);
            Assert.StartsWith("graft-fail-", fileName, StringComparison.Ordinal);
            Assert.EndsWith(".png", fileName, StringComparison.Ordinal);
            Assert.True(File.Exists(screenshotPath));
        }
        finally
        {
            if (screenshotPath is not null)
            {
                File.Delete(screenshotPath);
            }

            await session.DisposeAsync();
            await server.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static async Task RunFakeAgentAsync(string pipeName)
    {
        await using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
        );
        await server.WaitForConnectionAsync();

        try
        {
            var handshake = await JsonMessageCodec.ReadRequestAsync(server);
            await JsonMessageCodec.WriteResponseAsync(
                server,
                new ResponseMessage
                {
                    V = ProtocolVersion.Current,
                    Id = handshake.Id,
                    Ok = true,
                }
            );

            while (true)
            {
                var request = await JsonMessageCodec.ReadRequestAsync(server);
                if (request.Method == ProtocolMethods.ListWindows)
                {
                    await WriteOkAsync(
                        server,
                        request.Id,
                        new ListWindowsResult
                        {
                            Windows =
                            [
                                new WindowInfo
                                {
                                    WindowId = 1,
                                    Title = "Sample",
                                    AutomationId = "Main",
                                },
                            ],
                        }
                    );
                    continue;
                }

                if (request.Method == ProtocolMethods.GetTree)
                {
                    await WriteOkAsync(
                        server,
                        request.Id,
                        new GetTreeResult
                        {
                            Root = new TreeNode
                            {
                                ControlType = "Window",
                                Name = "Sample",
                                AutomationId = "Main",
                                Bounds = new ElementBounds(),
                                Children = [],
                            },
                        }
                    );
                    continue;
                }

                if (request.Method == ProtocolMethods.Screenshot)
                {
                    var png = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
                    await WriteOkAsync(
                        server,
                        request.Id,
                        new ScreenshotResult
                        {
                            Format = "png",
                            Width = 1,
                            Height = 1,
                            ByteLength = png.Length,
                        }
                    );
                    await FrameIO.WriteAsync(server, png);
                    continue;
                }

                await JsonMessageCodec.WriteResponseAsync(
                    server,
                    new ResponseMessage
                    {
                        V = ProtocolVersion.Current,
                        Id = request.Id,
                        Ok = false,
                        Error = new ErrorObject { Code = GraftErrorCodes.ActionFailed, Message = $"Unexpected method '{request.Method}'." },
                    }
                );
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // Client disconnected.
        }
    }

    private static Task WriteOkAsync<T>(Stream stream, string id, T result) =>
        JsonMessageCodec.WriteResponseAsync(
            stream,
            new ResponseMessage
            {
                V = ProtocolVersion.Current,
                Id = id,
                Ok = true,
                Result = JsonSerializer.SerializeToElement(result, JsonMessageCodec.Options),
            }
        );
}
