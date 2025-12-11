using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

using Net9IOCPCore.Core.Event.Impl;
using Net9IOCPCore.Core.IOCP;
using Net9IOCPCore.Core.Net;

using Xunit;

namespace Net9IOCPCore.Tests
{
    // 간단한 페이크 iHandle — 테스트용으로 최소 동작만 구현
    internal class FakeIocp : iHandle
    {
        public SafeFileHandle Handle { get; } = new SafeFileHandle(IntPtr.Zero, false);

        public void Dispose() { }

        public void RegisterHandleToIocp(IntPtr handle, UIntPtr key) { /* nop */ }

        public bool TryDequeueCompletion(out uint bytes, out UIntPtr key, out IntPtr overlapped, uint timeout)
        {
            bytes = 0; key = UIntPtr.Zero; overlapped = IntPtr.Zero;
            return false;
        }

        public Task<(bool Success, uint Bytes, UIntPtr Key, IntPtr Overlapped)> DequeueCompletionAsync(System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult((false, 0u, UIntPtr.Zero, IntPtr.Zero));
        }

        public bool PostQueuedCompletionStatus(uint bytes, UIntPtr key, IntPtr overlapped) => true;

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }

    public class SimpleEventListenerIntegrationTests
    {
        [Fact(Timeout = 5_000)]
        public async Task Start_AcceptsClient_And_ReceivesData()
        {
            var iocp = new FakeIocp();
            using var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);

            var listener = new SimpleEventListenerImpl(endpoint, iocp, listenSocket);

            var acceptedTcs = new TaskCompletionSource<ConnectionContext?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var receivedTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            listener.Accepted += ctx => acceptedTcs.TrySetResult(ctx);
            listener.Received += (ctx, mem) => receivedTcs.TrySetResult(mem.ToArray());

            // Start listener (bind + listen)
            listener.Start();

            // get assigned port from the same socket instance
            var localEp = (IPEndPoint)listenSocket.LocalEndPoint!;
            var port = localEp.Port;

            // connect client
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(IPAddress.Loopback, port);

            // wait for accept
            var accepted = await acceptedTcs.Task;
            Assert.NotNull(accepted);

            // send test bytes from client
            var sendBytes = Encoding.UTF8.GetBytes("hello-simple");
            await client.SendAsync(sendBytes, SocketFlags.None);

            var received = await receivedTcs.Task;
            Assert.Equal(sendBytes, received);

            // cleanup
            accepted!.Close(null);
            listener.Dispose();
        }
    }
}