using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Net9IOCPCore.Core.ProtoBuff;

namespace Net9IOCPCore.Core.Net;

public class ConnectionContext : IDisposable
{
    private GCHandle _gcHandle;
    private (byte[] Buffer, int Offset, int Count)? _currentSend;

    private readonly Socket _socket;
    private readonly SocketAsyncEventArgs _recvArgs;
    private readonly SocketAsyncEventArgs _sendArgs;
    
    private readonly byte[] _recvBuffer;
    private readonly ConcurrentQueue<(byte[] Buffer, int Offset, int Count)> _sendQueue = new();
    private readonly ArrayPool<byte> _pool;
    private readonly object _sendLock = new object();

    private readonly FrameParser _frameParser = new FrameParser();
    
    public Socket Socket => _socket;
    public UIntPtr Key => (UIntPtr)GCHandle.ToIntPtr(_gcHandle);

    public event Action<ConnectionContext, ReadOnlyMemory<byte>>? Received;
    public event Action<ConnectionContext, Exception?>? Disconnected;
    public event Action<ConnectionContext, FrameParser.FrameHeader, Payload>? ReceivedFrame;

    public ConnectionContext(Socket socket, int bufferSize = 1024, ArrayPool<byte>? pool = null)
    {
        _socket = socket;
        _pool = pool ?? ArrayPool<byte>.Shared;

        _gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        GcHandle = _gcHandle;

        _recvBuffer = _pool.Rent(bufferSize);
        
        _recvArgs = new SocketAsyncEventArgs();
        _recvArgs.SetBuffer(_recvBuffer, 0, _recvBuffer.Length);
        _recvArgs.UserToken = this; 
        _recvArgs.Completed += OnReceiveCompleted;

        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.UserToken = this;
        _sendArgs.Completed += OnSendCompleted;

        _frameParser.FrameReceived += (header, payload) =>
        {
            ReceivedFrame?.Invoke(this, header, payload);
        };
        _frameParser.ParseError += (ex) =>
        {
            Debug.WriteLine($"FrameParser error: {ex}");
        };
    }
    
    public GCHandle GcHandle { get; internal set; }

    private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success)
        {
            Close(new SocketException((int)e.SocketError));
            return;
        }

        if (e.BytesTransferred == 0)
        {
            Close(null);
            return;
        }

        var mem = new ReadOnlyMemory<byte>(e.Buffer!, e.Offset, e.BytesTransferred);
        Received?.Invoke(this, mem);

        try
        {
            _frameParser.Process(mem);
        }
        catch (Exception ex)
        {
            Close(ex);
            return;
        }

        e.SetBuffer(0, _recvBuffer.Length);
        try
        {
            Receive();
        }
        catch (Exception ex)
        {
            Close(ex);
        }
    }

    public void Receive()
    {
        try
        {
            if (!_socket.ReceiveAsync(_recvArgs))
            {
                OnReceiveCompleted(this, _recvArgs);
            }
        }
        catch (SocketException ex)
        {
            Close(ex);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> data)
    {
        var buf = _pool.Rent(data.Length);
        data.Span.CopyTo(buf.AsSpan(0, data.Length));
        _sendQueue.Enqueue((buf, 0, data.Length));

        StartSend();

        return ValueTask.CompletedTask;
    }

    private void StartSend()
    {
        lock (_sendLock)
        {
            if (_currentSend != null) 
                return;
            if (!_sendQueue.TryDequeue(out var item))
                return;
            
            _currentSend = item;
            var (buffer, offset, count) = item;
            _sendArgs.SetBuffer(buffer, offset, count);

            try
            {
                if (!_socket.SendAsync(_sendArgs))
                {
                    OnSendCompleted(this, _sendArgs);
                }
            }
            catch (SocketException ex)
            {
                try { _pool.Return(buffer); } catch { }
                _currentSend = null;
                Close(ex);
            }
        }
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
    {
        lock (_sendLock)
        {
            if (e.SocketError != SocketError.Success)
            {
                if (_currentSend != null)
                {
                    try { _pool.Return(_currentSend.Value.Buffer); } catch { }
                    _currentSend = null;
                }
                Close(new SocketException((int)e.SocketError));
                return;
            }

            if (e.BytesTransferred == 0)
            {
                Close(null);
                return;
            }

            if (_currentSend == null)
            {
                Debug.WriteLine("OnSendCompleted: current send is null");
                return;
            }

            var sent = e.BytesTransferred;
            var (buffer, offset, count) = _currentSend.Value;
            var remain = count - sent;

            if (remain > 0)
            {
                offset += sent;
                _currentSend = (buffer, offset, remain);
                _sendArgs.SetBuffer(buffer, offset, remain);

                try
                {
                    if (!_socket.SendAsync(_sendArgs))
                    {
                        OnSendCompleted(this, _sendArgs);
                    }
                }
                catch (SocketException ex)
                {
                    try { _pool.Return(buffer); } catch { }
                    _currentSend = null;
                    Close(ex);
                }
                return;
            }

            try { _pool.Return(buffer); } catch { }
            _currentSend = null;

            if (_sendQueue.Count > 0)
                StartSend();
        }
    }

    internal void Close(Exception? ex)
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException ex2)
        {
            Debug.WriteLine(ex2.Message);
        }
        catch (ObjectDisposedException) { }

        Disconnected?.Invoke(this, ex);
        Dispose();
    }

    public void Dispose()
    {
        try
        {
            _recvArgs.Dispose();
            _sendArgs.Dispose();
            _socket.Dispose();
        }
        catch { }

        if (_gcHandle.IsAllocated)
            _gcHandle.Free();

        try
        {
            if (_recvBuffer != null)
                _pool.Return(_recvBuffer);
        }
        catch { }

        if (_currentSend != null)
        {
            try { _pool.Return(_currentSend.Value.Buffer); } catch { }
            _currentSend = null;
        }
        
        while (_sendQueue.TryDequeue(out var item))
        {
            try { _pool.Return(item.Buffer); } catch { }
        }
    }
}
