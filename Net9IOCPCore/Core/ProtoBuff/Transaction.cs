using System;
using System.Collections.Generic;
using System.Linq;

namespace Net9IOCPCore.Core.ProtoBuff;

/// <summary>
/// 여러 청크로 분할된 트랜잭션을 조립하는 클래스
/// </summary>
public class Transaction
{
    private readonly Dictionary<short, Payload> _payloads;
    private short _transactionID = 0;
    private short _count = 0;
    private int _payLoadsSize = 0;
    private short _expectedTotalChunks = -1;
    private readonly object _lock = new();

    public Transaction()
    {
        _payloads = [];
    }

    /// <summary>
    /// 트랜잭션 ID
    /// </summary>
    public short TransactionID
    {
        get => _transactionID;
        set => _transactionID = value;
    }

    /// <summary>
    /// 현재까지 추가된 청크 수
    /// </summary>
    public int Count
    {
        get { lock (_lock) { return _payloads.Count; } }
    }

    /// <summary>
    /// 현재까지 누적된 페이로드 바이트 총량
    /// </summary>
    public int PayloadsSize
    {
        get { lock (_lock) { return _payLoadsSize; } }
    }

    /// <summary>
    /// 전체 청크 수가 알려져 있는지 여부
    /// </summary>
    public bool HasExpectedTotalChunks
    {
        get { lock (_lock) { return _expectedTotalChunks >= 0; } }
    }

    /// <summary>
    /// 모든 청크가 모였는지 여부
    /// </summary>
    public bool IsComplete
    {
        get
        {
            lock (_lock)
            {
                if (_expectedTotalChunks < 0) return false;
                return _payloads.Count == _expectedTotalChunks;
            }
        }
    }

    /// <summary>
    /// 청크 추가 (레거시 - 인덱스 자동 증가 방식)
    /// </summary>
    public void Add(Payload payload)
    {
        lock (_lock)
        {
            _payLoadsSize += payload.Length;
            _payloads.Add(_count, payload);
            IncrementCount();
        }
    }

    private short IncrementCount()
    {
        lock (_lock)
        {
            if (_count == short.MaxValue)
                throw new InvalidOperationException("Transaction ID overflow.");
            
            _count++;
            return _count;
        }
    }

    /// <summary>
    /// 프레임 단위로 전달된 청크를 추가합니다.
    /// </summary>
    public bool TryAddChunk(short transactionId, short chunkIndex, short totalChunks, Payload payload)
    {
        lock (_lock)
        {
            if (_transactionID == 0)
            {
                _transactionID = transactionId;
            }
            else if (_transactionID != transactionId)
            {
                return false;
            }

            if (_expectedTotalChunks < 0)
            {
                _expectedTotalChunks = totalChunks;
            }
            else if (_expectedTotalChunks != totalChunks)
            {
                return false;
            }

            if (_payloads.ContainsKey(chunkIndex))
            {
                return false;
            }

            _payloads.Add(chunkIndex, payload);
            _payLoadsSize += payload.Length;
            return true;
        }
    }

    /// <summary>
    /// 모든 청크를 인덱스 순서로 이어붙인 바이트 배열을 반환합니다.
    /// </summary>
    public byte[] GetAssembledBytes()
    {
        lock (_lock)
        {
            if (!IsComplete)
                throw new InvalidOperationException("Transaction is not complete.");

            var ordered = _payloads.OrderBy(kv => kv.Key).Select(kv => kv.Value.ToArray()).ToList();
            int total = ordered.Sum(b => b.Length);
            var result = new byte[total];
            int pos = 0;
            foreach (var part in ordered)
            {
                Buffer.BlockCopy(part, 0, result, pos, part.Length);
                pos += part.Length;
            }
            return result;
        }
    }

    /// <summary>
    /// 조립된 바이트로 Payload 객체를 생성하여 반환합니다.
    /// </summary>
    public Payload GetAssembledPayload()
    {
        var bytes = GetAssembledBytes();
        return Payload.FromArray(bytes);
    }

    /// <summary>
    /// 내부 상태 초기화 및 메모리 해제
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _payloads.Clear();
            _transactionID = 0;
            _count = 0;
            _payLoadsSize = 0;
            _expectedTotalChunks = -1;
        }
    }
}
