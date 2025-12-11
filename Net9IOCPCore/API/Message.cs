using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Net9IOCPCore.API;

/// <summary>
/// 풀링을 지원하는 재사용 가능한 메시지 객체
/// </summary>
public sealed class Message : IDisposable
{
    public int MessageType;
    public int Length;
    public byte[]? Payload;

    internal Message() { }

    public void Dispose()
    {
        MessagePool.Return(this);
    }

    internal void Reset()
    {
        MessageType = 0;
        Length = 0;
        Payload = null;
    }
}

/// <summary>
/// 스레드 안전한 메시지 풀 (메모리 누수 방지를 위한 크기 제한 포함)
/// </summary>
public static class MessagePool
{
    private static readonly ConcurrentBag<Message> _bag = new();
    private static int _currentCount;
    private const int MaxPoolSize = 1000;

    public static Message Rent()
    {
        if (_bag.TryTake(out var msg))
        {
            Interlocked.Decrement(ref _currentCount);
            return msg;
        }
        return new Message();
    }

    internal static void Return(Message msg)
    {
        msg.Reset();

        // 풀 크기 제한 (메모리 누수 방지)
        if (_currentCount < MaxPoolSize)
        {
            _bag.Add(msg);
            Interlocked.Increment(ref _currentCount);
        }
    }
}
