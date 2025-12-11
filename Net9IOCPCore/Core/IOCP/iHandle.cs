using Microsoft.Win32.SafeHandles;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Net9IOCPCore.Core.IOCP;

/// <summary>
/// IOCP 핸들 인터페이스
/// </summary>
public interface iHandle : IDisposable
{
    SafeFileHandle Handle { get; }
    void RegisterHandleToIocp(IntPtr handle, UIntPtr key);
    bool TryDequeueCompletion(out uint bytes, out UIntPtr key, out IntPtr overlapped, uint timeout);
    Task<(bool Success, uint Bytes, UIntPtr Key, IntPtr Overlapped)> DequeueCompletionAsync(CancellationToken cancellationToken);
    bool PostQueuedCompletionStatus(uint bytes, UIntPtr key, IntPtr overlapped);
    void Stop();
}
