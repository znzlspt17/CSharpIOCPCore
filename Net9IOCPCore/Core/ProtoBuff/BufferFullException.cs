using System;

namespace Net9IOCPCore.Core.ProtoBuff;

/// <summary>
/// 버퍼가 가득 찼을 때 발생하는 예외
/// </summary>
public class BufferFullException : Exception
{
    public BufferFullException()
    {
    }

    public BufferFullException(string? message) : base(message)
    {
    }

    public BufferFullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}