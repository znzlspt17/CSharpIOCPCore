using Net9IOCPCore.Core.IOCP;
using Net9IOCPCore.Core.Net;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Net9IOCPCore.Core.Event.Impl;

internal class SimpleEventListenerImpl : IEventListener
{
    private readonly Socket _socket;
    private readonly SocketAsyncEventArgs _eventArgs;
    private readonly iHandle _iocp;
    private readonly ConcurrentDictionary<nint, ConnectionContext> _connections = new();
    private readonly IPEndPoint _address;

    public event Action<ConnectionContext>? Accepted;
    public event Action<ConnectionContext, ReadOnlyMemory<byte>>? Received;
    public event Action<ConnectionContext>? Disconnected;

    public SimpleEventListenerImpl(IPEndPoint address, iHandle iocp, Socket socket)
    {
        _address = address;
        _iocp = iocp;
        _socket = socket;
        _eventArgs = new SocketAsyncEventArgs();
        _eventArgs.Completed += OnAcceptCompleted;
    }

    public void Start()
    {
        _socket.Bind(_address);
        _socket.Listen(100);

        _eventArgs.AcceptSocket = null;
        try
        {
            if (!_socket.AcceptAsync(_eventArgs))
            {
                ProcessAccept(_eventArgs);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public void Stop()
    {
        try
        {
            _socket.Close();
        }
        catch (SocketException ex)
        {
            Debug.WriteLine(ex);
        }

        try
        {
            var contexts = _connections.Values.ToArray();
            foreach (var ctx in contexts)
            {
                try
                {
                    ctx.Close(null);
                }
                catch (Exception inner)
                {
                    Debug.WriteLine(inner);
                }
            }
            _connections.Clear();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        try
        {
            _iocp.PostQueuedCompletionStatus(0, UIntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        try
        {
            _iocp.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        ProcessAccept(e);
    }

    private void ProcessAccept(SocketAsyncEventArgs e)
    {
        while (true)
        {
            if (e.SocketError != SocketError.Success || e.AcceptSocket == null)
            {
                try
                {
                    e.AcceptSocket = null;
                    if (_socket.AcceptAsync(e))
                        return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return;
                }
                continue;
            }

            var clientSocket = e.AcceptSocket;
            var context = new ConnectionContext(clientSocket);
            var keyPtr = GCHandle.ToIntPtr(context.GcHandle);
            _connections[(nint)keyPtr] = context;
            _iocp.RegisterHandleToIocp(clientSocket.Handle, (UIntPtr)keyPtr);

            context.Received += (c, data) => Received?.Invoke(c, data);
            context.Disconnected += (c, exception) =>
            {
                var key = GCHandle.ToIntPtr(c.GcHandle);
                _connections.TryRemove((nint)key, out _);
                Disconnected?.Invoke(c);
            };

            Accepted?.Invoke(context);
            context.Receive();

            e.AcceptSocket = null;
            try
            {
                if (_socket.AcceptAsync(e))
                    return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return;
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _eventArgs.Dispose();
    }
}
