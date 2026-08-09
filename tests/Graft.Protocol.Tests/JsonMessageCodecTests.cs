using System.Text.Json;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Protocol.Tests;

public sealed class JsonMessageCodecTests
{
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

    [Fact]
    public void ProtocolVersion_Current_IsOne()
    {
        Assert.Equal(1, ProtocolVersion.Current);
    }
}
