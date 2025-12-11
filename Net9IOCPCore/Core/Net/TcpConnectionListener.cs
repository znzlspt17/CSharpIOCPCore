using Net9IOCPCore.Core.IOCP;
using Net9IOCPCore.Core.IOCP.Impl;
using Net9IOCPCore.Core.Event.Impl;
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Net9IOCPCore.Core.Net;

/// <summary>
/// 연결 수락/수신/단절 이벤트를 중계하는 래퍼
/// </summary>
public sealed class TcpConnectionListener : IDisposable
{
    private readonly IPEndPoint _endpoint;
    private readonly Socket _listenSocket;
    private readonly iHandle _iocp;
    private readonly SimpleEventListenerImpl _listener;
    private bool _started;
    private bool _disposed;

    public event Action<ConnectionInfo>? Accepted;
    public event Action<ConnectionInfo, ReadOnlyMemory<byte>>? Received;
    public event Action<ConnectionInfo>? Disconnected;

    public TcpConnectionListener(IPEndPoint endpoint, int backlog = 100)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };
        _iocp = new HandleImpl((uint)Environment.ProcessorCount);
        _listener = new SimpleEventListenerImpl(_endpoint, _iocp, _listenSocket);

        _listener.Accepted += ctx => Accepted?.Invoke(new ConnectionInfo(ctx.Key));
        _listener.Received += (ctx, data) => Received?.Invoke(new ConnectionInfo(ctx.Key), data);
        _listener.Disconnected += ctx => Disconnected?.Invoke(new ConnectionInfo(ctx.Key));
    }

    public void Start()
    {
        if (_started) return;
        _listener.Start();
        _started = true;
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;

        try
        {
            _listener.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping listener: {ex}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();

        try
        {
            _listenSocket?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disposing listen socket: {ex}");
        }

        try
        {
            _iocp?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disposing IOCP: {ex}");
        }

        try
        {
            _listener?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disposing listener: {ex}");
        }

        GC.SuppressFinalize(this);
    }
}