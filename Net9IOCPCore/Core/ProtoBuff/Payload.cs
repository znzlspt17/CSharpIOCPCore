using System;
using System.Collections.Generic;

namespace Net9IOCPCore.Core.ProtoBuff;

public partial class Payload
{
    internal enum PayloadType : byte
    {
        Byte = 0,
        Bool = 1,
        Char = 2,
        Short = 3,
        Int = 4,
        Long = 5,
        Float = 6,
        Double = 7,
        String = 8,
    }

    /// <summary>
    /// 내부 버퍼 최대 크기(바이트). 
    /// 주의: 각 항목에는 타입 태그와 길이 필드가 포함되므로 실제 사용 가능한 데이터는 이보다 작습니다.
    /// </summary>
    public static readonly short MaxBufferSize = 1024;

    private readonly List<byte> _buffer;
    private readonly object _lock = new();

    public Payload()
    {
        _buffer = [];
    }

    /// <summary>
    /// 바이트 배열로부터 Payload를 생성합니다.
    /// </summary>
    public static Payload FromArray(byte[] data)
    {
        var p = new Payload();
        lock (p._lock)
        {
            p._buffer.AddRange(data);
        }
        return p;
    }

    /// <summary>
    /// 특정 헤더/프로토콜 오버헤드를 제외한 실제 사용 가능한 최대 바이트 수를 반환합니다.
    /// </summary>
    public static int GetMaxUsableBytes(int reservedHeaderBytes = 0)
    {
        return Math.Max(0, MaxBufferSize - reservedHeaderBytes);
    }

    public int Length
    {
        get { lock (_lock) { return _buffer.Count; } }
    }

    public int RemainingSpace
    {
        get { lock (_lock) { return MaxBufferSize - _buffer.Count; } }
    }

    public bool IsBufferFull(int requiredBytes = 1)
    {
        lock (_lock)
        {
            return (_buffer.Count + requiredBytes) > MaxBufferSize;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }

    public byte[] ToArray()
    {
        lock (_lock)
        {
            return _buffer.ToArray();
        }
    }
}