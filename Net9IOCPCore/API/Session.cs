using System;
using System.Threading.Tasks;

using Net9IOCPCore.Core.Net;
using Net9IOCPCore.Core.ProtoBuff;

namespace Net9IOCPCore.API;

/// <summary>
/// 클라이언트 연결을 나타내는 세션 클래스
/// </summary>
public sealed class Session : ISession
{
    private readonly ConnectionContext _ctx;
    private bool _disposed;

    public Guid Id { get; }
    
    public event Action<ISession, Message>? Received;
    public event Action<ISession, Exception?>? Disconnected;

    public Session(ConnectionContext ctx, Guid id)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        
        _ctx = ctx;
        Id = id;
        
        _ctx.ReceivedFrame += OnReceivedFrame;
        _ctx.Disconnected += OnDisconnected;
    }

    private void OnReceivedFrame(ConnectionContext c, FrameParser.FrameHeader header, Payload payload)
    {
        var bytes = payload.ToArray();

        var msg = MessagePool.Rent();
        msg.MessageType = header.TransactionId;
        msg.Length = bytes.Length;
        msg.Payload = bytes;

        try
        {
            Received?.Invoke(this, msg);
        }
        finally
        {
            // 이벤트 핸들러가 등록되지 않은 경우 메시지를 즉시 반환
            if (Received == null)
            {
                msg.Dispose();
            }
        }
    }

    private void OnDisconnected(ConnectionContext c, Exception? ex)
    {
        Disconnected?.Invoke(this, ex);
    }

    public Task SendRawAsync(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);        
        return _ctx.WriteAsync(data).AsTask();
    }

    public ConnectionContext GetConnectionContext() => _ctx;

    public void Close()
    {
        _ctx.Close(null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _ctx.ReceivedFrame -= OnReceivedFrame;
        _ctx.Disconnected -= OnDisconnected;
        _ctx.Dispose();
    }
}