using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Net9IOCPCore.Core.Net;
using Xunit;

namespace Net9IOCPCore.Tests
{
    public class ConnectionContextTests
    {
        [Fact(Timeout = 5_000)]
        public async Task WriteAsync_SendsDataToRemote()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var localEp = (IPEndPoint)listener.LocalEndpoint;
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // connect client -> listener
                var connectTask = client.ConnectAsync(localEp.Address, localEp.Port);

                var serverSocketTask = listener.AcceptSocketAsync();

                await Task.WhenAll(connectTask, serverSocketTask);

                using var serverSocket = serverSocketTask.Result;
                using var cc = new ConnectionContext(serverSocket);

                // 준비: 클라이언트에서 읽을 버퍼
                var receiveBuffer = new byte[1024];
                var receiveTask = Task.Run(async () =>
                {
                    var total = 0;
                    while (total < 5)
                    {
                        var read = await client.ReceiveAsync(receiveBuffer.AsMemory(total, receiveBuffer.Length - total), SocketFlags.None);
                        if (read == 0) break;
                        total += read;
                    }
                    var result = new byte[total];
                    Array.Copy(receiveBuffer, 0, result, 0, total);
                    return result;
                });

                // act: 서버측 ConnectionContext로 전송
                var payload = Encoding.UTF8.GetBytes("hello");
                await cc.WriteAsync(payload);

                var received = await receiveTask;
                Assert.Equal(payload, received);
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}