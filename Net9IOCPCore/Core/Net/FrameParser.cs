using System;
using System.Collections.Generic;
using Net9IOCPCore.Core.ProtoBuff;

namespace Net9IOCPCore.Core.Net;

/// <summary>
/// 프레임 포맷:
/// [Length:int32 (헤더 + payload bytes)][TransactionId:int16][ChunkIndex:int16][TotalChunks:int16][payload bytes...]
/// Length는 헤더(6) + payloadBytes 길이
/// </summary>
public class FrameParser
{
    private readonly List<byte> _buffer = new();

    public record FrameHeader(short TransactionId, short ChunkIndex, short TotalChunks);

    public event Action<FrameHeader, Payload>? FrameReceived;
    public event Action<Exception>? ParseError;

    public void Process(ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0) return;
        
        foreach (var b in data.Span)
        {
            _buffer.Add(b);
        }

        try
        {
            int pos = 0;
            var bufferArray = _buffer.ToArray();

            while (true)
            {
                if (bufferArray.Length - pos < 4) break;

                int length = BitConverter.ToInt32(bufferArray, pos);
                pos += 4;

                if (length <= 0)
                    throw new InvalidOperationException("Invalid frame length.");

                if (bufferArray.Length - pos < length) 
                {
                    pos -= 4;
                    break;
                }

                if (length < 6)
                    throw new InvalidOperationException("Frame length too small for header.");

                short transactionId = BitConverter.ToInt16(bufferArray, pos);
                short chunkIndex = BitConverter.ToInt16(bufferArray, pos + 2);
                short totalChunks = BitConverter.ToInt16(bufferArray, pos + 4);
                pos += 6;

                int payloadBytesLen = length - 6;
                byte[] payloadBytes = new byte[payloadBytesLen];
                if (payloadBytesLen > 0)
                {
                    Array.Copy(bufferArray, pos, payloadBytes, 0, payloadBytesLen);
                    pos += payloadBytesLen;
                }

                var header = new FrameHeader(transactionId, chunkIndex, totalChunks);
                var payload = Payload.FromArray(payloadBytes);
                FrameReceived?.Invoke(header, payload);

                if (_buffer.Count - pos <= 0) break;
            }
            
            if (pos > 0)
            {
                _buffer.RemoveRange(0, pos);
            }
        }
        catch (Exception ex)
        {
            ParseError?.Invoke(ex);
            _buffer.Clear();
        }
    }
}