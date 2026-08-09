using System.Text.Json;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Protocol.Tests;

public sealed class JsonMessageCodecTests
{
    /// <summary>
    /// Request envelopes survive length-prefixed JSON round-trip.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty MemoryStream
    /// - Request with v=Current, id, method getTree, params.depth=25
    ///
    /// Steps:
    /// - WriteRequestAsync, seek to 0, ReadRequestAsync
    ///
    /// Expected:
    /// - Fields and params.depth match the original request
    /// </remarks>
    [Fact]
    public async Task Request_RoundTrip_OverMemoryStream()
    {
        await using var stream = new MemoryStream();
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "1",
            Method = "getTree",
            Params = JsonSerializer.SerializeToElement(new { depth = 25 }),
        };

        await JsonMessageCodec.WriteRequestAsync(stream, request);
        stream.Position = 0;

        var decoded = await JsonMessageCodec.ReadRequestAsync(stream);
        Assert.Equal(ProtocolVersion.Current, decoded.V);
        Assert.Equal("1", decoded.Id);
        Assert.Equal("getTree", decoded.Method);
        Assert.True(decoded.Params.HasValue);
        Assert.Equal(25, decoded.Params.Value.GetProperty("depth").GetInt32());
    }

    /// <summary>
    /// Successful response envelopes round-trip with result and without error.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty MemoryStream
    /// - Ok response with result.truncated=false
    ///
    /// Steps:
    /// - WriteResponseAsync, seek to 0, ReadResponseAsync
    ///
    /// Expected:
    /// - Ok is true, Error is null, result.truncated is false
    /// </remarks>
    [Fact]
    public async Task Response_Ok_RoundTrip_OverMemoryStream()
    {
        await using var stream = new MemoryStream();
        var response = new ResponseMessage
        {
            V = ProtocolVersion.Current,
            Id = "1",
            Ok = true,
            Result = JsonSerializer.SerializeToElement(new { truncated = false }),
        };

        await JsonMessageCodec.WriteResponseAsync(stream, response);
        stream.Position = 0;

        var decoded = await JsonMessageCodec.ReadResponseAsync(stream);
        Assert.True(decoded.Ok);
        Assert.Null(decoded.Error);
        Assert.True(decoded.Result.HasValue);
        Assert.False(decoded.Result.Value.GetProperty("truncated").GetBoolean());
    }

    /// <summary>
    /// Error responses preserve code, message, and details.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty MemoryStream
    /// - Ok=false response with protocol.versionMismatch and details.actual=2
    ///
    /// Steps:
    /// - WriteResponseAsync, seek to 0, ReadResponseAsync
    ///
    /// Expected:
    /// - Ok is false; Error.Code/Message/Details match
    /// </remarks>
    [Fact]
    public async Task Response_Error_RoundTrip_IncludesCodeAndMessage()
    {
        await using var stream = new MemoryStream();
        var response = new ResponseMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Ok = false,
            Error = new ErrorObject
            {
                Code = GraftErrorCodes.ProtocolVersionMismatch,
                Message = "expected v=1",
                Details = JsonSerializer.SerializeToElement(new { actual = 2 }),
            },
        };

        await JsonMessageCodec.WriteResponseAsync(stream, response);
        stream.Position = 0;

        var decoded = await JsonMessageCodec.ReadResponseAsync(stream);
        Assert.False(decoded.Ok);
        Assert.NotNull(decoded.Error);
        Assert.Equal(GraftErrorCodes.ProtocolVersionMismatch, decoded.Error.Code);
        Assert.Equal("expected v=1", decoded.Error.Message);
        Assert.True(decoded.Error.Details.HasValue);
        Assert.Equal(2, decoded.Error.Details.Value.GetProperty("actual").GetInt32());
    }

    /// <summary>
    /// ProtocolVersion.Current is fixed at 1 for v1 wire compatibility.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - None
    ///
    /// Steps:
    /// - Read ProtocolVersion.Current
    ///
    /// Expected:
    /// - Value equals 1
    /// </remarks>
    [Fact]
    public void ProtocolVersion_Current_IsOne()
    {
        Assert.Equal(1, ProtocolVersion.Current);
    }
}
