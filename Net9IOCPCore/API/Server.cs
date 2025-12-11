using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Net9IOCPCore.Core.Net.Impl;
using Net9IOCPCore.Core.ProtoBuff;
using Net9IOCPCore.Core.Net;

namespace Net9IOCPCore.API;

public class Server : IServer
{
    private readonly TcpAcceptListener _server;
    private readonly SessionsManager _sessions = new SessionsManager();
    public event Action<IServer, ISession>? ClientConnected;

    public Server(IPEndPoint endpoint)
    {
        _server = new TcpAcceptListener(endpoint);
        _server.ClientConnected += OnClientConnected;
        _server.ReceivedFrame += (context, header, payload) => { /* 내부에서 세션이 처리 */ };
        _server.RawReceived += (context, bytes) => { /* 필요시 노출 */ };
        _server.ClientDisconnected += (context, exception) => { /* 세션에서 처리 */ };
    }

    private void OnClientConnected(ConnectionContext ctx)
    {
        var id = UUIDGenerator.Get();
        var session = new Session(ctx, id);
        _sessions.TryAdd(id, session);


        // 세션 종료 시 정리
        session.Disconnected += (s, ex) =>
        {
            _sessions.TryRemove(id, out _);
        };

        ClientConnected?.Invoke(this, session);
    }

    public Task StartAsync()
    {
        _server.Start();
        return Task.CompletedTask;
    }

    public void Stop()
    {
        _server.Stop();
        foreach (var session in _sessions.GetAll())
        {
            try { session.Dispose(); } catch { }
        }
        _sessions.Clear();
    }

    public void Dispose()
    {
        Stop();
        try { _server.Dispose(); } catch { }
    }
}