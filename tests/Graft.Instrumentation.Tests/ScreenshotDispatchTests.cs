using System.IO.Pipes;
using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class ScreenshotDispatchTests : IDisposable
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly string _pipeName = "graft-ss-" + Guid.NewGuid().ToString("N");

    public ScreenshotDispatchTests()
    {
        ClearGraftEnvironment();
        Agent.Stop();
        AgentServices.Reset();
    }

    public void Dispose()
    {
        Agent.Stop();
        AgentServices.Reset();
        ClearGraftEnvironment();
    }

    /// <summary>
    /// screenshot without a registered provider returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; AgentServices.ScreenshotProvider is null
    ///
    /// Steps:
    /// - Handshake then screenshot
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task Screenshot_WithoutProvider_ReturnsActionFailed()
    {
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var (response, raw) = await SendScreenshotAsync(client);
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
        Assert.Null(raw);
    }

    /// <summary>
    /// screenshot returns JSON meta then a raw PNG frame after handshake.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IScreenshotProvider registered (PNG signature payload)
    ///
    /// Steps:
    /// - Handshake then screenshot
    /// - Read JSON response then the follow-up binary frame
    ///
    /// Expected:
    /// - ok=true with format=png and matching byteLength
    /// - raw frame starts with the PNG signature
    /// </remarks>
    [Fact]
    public async Task Screenshot_WithFakeProvider_ReturnsMetaAndPngFrame()
    {
        var png = BuildMinimalPngBytes();
        AgentServices.RegisterScreenshotProvider(new FakeScreenshotProvider(png));
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var (response, raw) = await SendScreenshotAsync(client);
        Assert.True(response.Ok, response.Error?.Message);
        Assert.True(response.Result.HasValue);

        var meta = response.Result.Value.Deserialize<ScreenshotResult>(JsonMessageCodec.Options);
        Assert.NotNull(meta);
        Assert.Equal("png", meta.Format);
        Assert.Equal(16, meta.Width);
        Assert.Equal(12, meta.Height);
        Assert.Equal(png.Length, meta.ByteLength);

        Assert.NotNull(raw);
        Assert.Equal(png.Length, raw.Length);
        Assert.True(raw.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature));
    }

    private void StartAgent()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, _pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, "secret");
        Agent.Start();
    }

    private static async Task<NamedPipeClientStream> ConnectAsync(string pipeName)
    {
        var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await client.ConnectAsync(200).ConfigureAwait(false);
                return client;
            }
            catch (Exception ex)
                when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        await client.DisposeAsync().ConfigureAwait(false);
        throw new TimeoutException($"Could not connect to pipe '{pipeName}'.", last);
    }

    private static async Task<ResponseMessage> SendHandshakeAsync(Stream stream)
    {
        using var paramsDoc = JsonDocument.Parse("""{"token":"secret"}""");
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "1",
            Method = ProtocolMethods.Handshake,
            Params = paramsDoc.RootElement.Clone(),
        };

        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        return await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
    }

    private static async Task<(ResponseMessage Response, byte[]? Raw)> SendScreenshotAsync(
        Stream stream
    )
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.Screenshot,
        };

        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        var response = await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
        if (!response.Ok)
        {
            return (response, null);
        }

        var raw = await FrameIO.ReadAsync(stream).ConfigureAwait(false);
        return (response, raw);
    }

    private static byte[] BuildMinimalPngBytes()
    {
        // Signature + filler (not a full valid PNG file; wire tests only check the signature).
        var bytes = new byte[32];
        PngSignature.CopyTo(bytes, 0);
        return bytes;
    }

    private static void ClearGraftEnvironment()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, null);
    }

    private sealed class FakeScreenshotProvider : IScreenshotProvider
    {
        private readonly byte[] _png;

        public FakeScreenshotProvider(byte[] png) => _png = png;

        public ScreenshotCapture Capture(ScreenshotOptions options) =>
            new()
            {
                Meta = new ScreenshotResult
                {
                    Format = "png",
                    Width = 16,
                    Height = 12,
                    ByteLength = _png.Length,
                },
                PngBytes = _png,
            };
    }
}
