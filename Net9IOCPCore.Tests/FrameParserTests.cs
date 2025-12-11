using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Net9IOCPCore.Core.Net;
using Net9IOCPCore.Core.ProtoBuff;

namespace Net9IOCPCore.Tests;

public class FrameParserTests
{
    [Fact]
    public async Task Process_SingleFrame_RaisesFrameReceivedWithCorrectHeaderAndPayload()
    {
        // arrange
        var payloadBytes = Encoding.UTF8.GetBytes("hello");
        int length = 6 + payloadBytes.Length; // header(6) + payload
        var buffer = new byte[4 + length];
        Array.Copy(BitConverter.GetBytes(length), 0, buffer, 0, 4);
        Array.Copy(BitConverter.GetBytes((short)1), 0, buffer, 4, 2); // TransactionId
        Array.Copy(BitConverter.GetBytes((short)0), 0, buffer, 6, 2); // ChunkIndex
        Array.Copy(BitConverter.GetBytes((short)1), 0, buffer, 8, 2); // TotalChunks
        Array.Copy(payloadBytes, 0, buffer, 10, payloadBytes.Length);

        var parser = new FrameParser();
        var tcs = new TaskCompletionSource<(FrameParser.FrameHeader header, Payload payload)>();

        parser.FrameReceived += (h, p) => tcs.TrySetResult((h, p));
        parser.ParseError += ex => tcs.TrySetException(ex);

        // act
        parser.Process(buffer);

        // assert
        var (header, payload) = await tcs.Task;
        Assert.Equal((short)1, header.TransactionId);
        Assert.Equal((short)0, header.ChunkIndex);
        Assert.Equal((short)1, header.TotalChunks);
        Assert.Equal(payloadBytes, payload.ToArray());
    }
}