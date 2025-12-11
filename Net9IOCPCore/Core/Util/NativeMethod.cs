using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Net9IOCPCore.Core.Util;

/// <summary>
/// Windows Kernel32 IOCP 네이티브 메서드
/// </summary>
internal static class NativeMethod
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern SafeFileHandle CreateIOCP(IntPtr Handle, IntPtr Existing, UIntPtr Key, uint Threads);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetQueuedCompletionStatus(SafeFileHandle Port, out uint Bytes, out UIntPtr Key, out IntPtr lpOverlapped, uint Millisec);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool PostQueuedCompletionStatus(SafeFileHandle CompletionPort, uint dwNumberOfBytesTransferred, UIntPtr dwCompletionKey, IntPtr lpOverlapped);
}
