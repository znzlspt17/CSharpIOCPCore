using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Net9IOCPCore.Core.Util.NativeMethod;

namespace Net9IOCPCore.Core.IOCP.Impl;

/// <summary>
/// IOCP(I/O Completion Port) 핸들 구현
/// </summary>
public sealed class HandleImpl : iHandle
{
    private SafeFileHandle? _handle;
    private bool _disposed;
    private CancellationTokenSource _forceCancelationToken = new();

    public SafeFileHandle Handle
    {
        get
        {
            if (_handle is null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Handle is null or invalid");
            }
            return _handle;
        }
    }

    public HandleImpl(uint thread)
    {
        _handle = CreateIOCP(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, thread);

        if (_handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create IOCP");
        }
    }

    public void RegisterHandleToIocp(IntPtr handle, UIntPtr key)
    {
        var result = CreateIOCP(handle, _handle!.DangerousGetHandle(), key, 0);
        if (result.IsInvalid)
        {
            throw new InvalidOperationException("Failed to register handle to IOCP");
        }
    }

    public bool TryDequeueCompletion(out uint bytes, out UIntPtr key, out IntPtr overlapped, uint timeout = 0x00000000)
    {
        return GetQueuedCompletionStatus(_handle!, out bytes, out key, out overlapped, timeout);
    }

    public Task<(bool Success, uint Bytes, UIntPtr Key, IntPtr Overlapped)> DequeueCompletionAsync(CancellationToken cancellationToken = default)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _forceCancelationToken.Token);
        return Task.Run(() =>
        {
            try
            {
                while (true)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();

                    bool result = GetQueuedCompletionStatus(_handle!, out uint bytes, out UIntPtr key, out IntPtr overlapped, 1000);
                    if (result || overlapped != IntPtr.Zero)
                    {
                        return (result, bytes, key, overlapped);
                    }
                }
            }
            finally
            {
                linkedCts.Dispose();
            }
        }, linkedCts.Token);
    }

    public async Task RunCompletionLoopAsync(Func<uint, UIntPtr, IntPtr, Task> onComplete, CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _forceCancelationToken.Token);
        while (!linkedCts.IsCancellationRequested)
        {
            var (success, bytes, key, overlapped) = await DequeueCompletionAsync(linkedCts.Token);
            if (linkedCts.IsCancellationRequested)
            {
                break;
            }
            if (success)
            {
                await onComplete(bytes, key, overlapped);
            }
        }
    }

    public bool PostQueuedCompletionStatus(uint bytes, UIntPtr key, IntPtr overlapped)
    {
        return Core.Util.NativeMethod.PostQueuedCompletionStatus(_handle!, bytes, key, overlapped);
    }

    public void Stop()
    {
        try
        {
            _forceCancelationToken?.Cancel();
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _forceCancelationToken?.Cancel();
            _forceCancelationToken?.Dispose();
            _forceCancelationToken = null!;
        }
        catch { }

        try
        {
            _handle?.Dispose();
            _handle = null;
        }
        catch { }

        GC.SuppressFinalize(this);
    }
}
