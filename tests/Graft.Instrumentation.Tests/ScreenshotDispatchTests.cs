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
        PipeTestClient.ClearEnvironment();
        Agent.Stop();
        Agent.Reset();
    }

    public void Dispose()
    {
        Agent.Stop();
        Agent.Reset();
        PipeTestClient.ClearEnvironment();
    }

    /// <summary>
    /// screenshot without a registered provider returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; no screenshot provider is registered
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
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var (response, raw) = await PipeTestClient.ExchangeAsync(client, ScreenshotRequest());
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
        Agent.Use(new AgentBackend { ScreenshotProvider = new FakeScreenshotProvider(png) });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var (response, raw) = await PipeTestClient.ExchangeAsync(client, ScreenshotRequest());
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

    private static RequestMessage ScreenshotRequest() =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.Screenshot,
        };

    private static byte[] BuildMinimalPngBytes()
    {
        // Signature plus filler. These tests check the signature, not a decoded image.
        var bytes = new byte[32];
        PngSignature.CopyTo(bytes, 0);
        return bytes;
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
