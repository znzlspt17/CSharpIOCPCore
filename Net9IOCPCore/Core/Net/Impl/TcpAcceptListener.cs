using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Net9IOCPCore.Core.ProtoBuff;

namespace Net9IOCPCore.Core.Net.Impl;

/// <summary>
/// TCP 연결 수락 및 관리를 담당하는 리스너
/// </summary>
public class TcpAcceptListener : IDisposable
{
    private readonly IPEndPoint _endpoint;
    private Socket? _listenSocket;
    private readonly SocketAsyncEventArgs _acceptArgs;
    private readonly Dictionary<nint, ConnectionContext> _connections = [];

    public event Action<ConnectionContext>? ClientConnected;
    public event Action<ConnectionContext, ReadOnlyMemory<byte>>? RawReceived;
    public event Action<ConnectionContext, FrameParser.FrameHeader, Payload>? ReceivedFrame;
    public event Action<ConnectionContext, Exception?>? ClientDisconnected;

    public TcpAcceptListener(IPEndPoint endpoint)
    {
        _endpoint = endpoint;
        _acceptArgs = new SocketAsyncEventArgs();
        _acceptArgs.Completed += AcceptCompleted;
    }

    public void Start()
    {
        _listenSocket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        _listenSocket.Bind(_endpoint);
        _listenSocket.Listen(backlog: 100);

        _acceptArgs.AcceptSocket = null;
        TryAccept();
    }

    private void TryAccept()
    {
        if (_listenSocket == null) return;

        try
        {
            bool pending = _listenSocket.AcceptAsync(_acceptArgs);
            if (!pending)
            {
                ProcessAccept(_acceptArgs);
            }
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"Accept error: {ex}");
        }
    }

    private void AcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        ProcessAccept(e);
    }

    private void ProcessAccept(SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success || e.AcceptSocket == null)
        {
            try
            {
                e.AcceptSocket = null;
                TryAccept();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Accept retry error: {ex}");
            }
            return;
        }

        Socket client = e.AcceptSocket;
        try
        {
            var ctx = new ConnectionContext(client);

            var key = (nint)GCHandle.ToIntPtr(ctx.GcHandle);
            _connections[key] = ctx;

            ctx.Received += (c, data) => RawReceived?.Invoke(c, data);
            ctx.ReceivedFrame += (c, header, payload) => ReceivedFrame?.Invoke(c, header, payload);
            ctx.Disconnected += (c, ex) =>
            {
                var k = (nint)GCHandle.ToIntPtr(c.GcHandle);
                _connections.Remove(k);
                ClientDisconnected?.Invoke(c, ex);
            };

            ClientConnected?.Invoke(ctx);
            ctx.Receive();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating ConnectionContext: {ex}");
            try { client.Close(); } catch { }
        }
        finally
        {
            e.AcceptSocket = null;
            TryAccept();
        }
    }

    public void Stop()
    {
        try
        {
            _listenSocket?.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Stop listen socket error: {ex}");
        }

        var list = _connections.Values.ToArray();
        foreach (var ctx in list)
        {
            try
            {
                ctx.Close(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing client: {ex}");
            }
        }
        _connections.Clear();
    }

    public void Dispose()
    {
        Stop();
        
        try 
        { 
            _acceptArgs.Dispose(); 
        } 
        catch { }
        
        try
        {
            if (_listenSocket != null)
            {
                try { _listenSocket.Close(); } catch { }
                _listenSocket = null;
            }
        }
        catch { }
    }
}