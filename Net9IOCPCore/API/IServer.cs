using System;
using System.Threading.Tasks;

namespace Net9IOCPCore.API;

/// <summary>
/// 서버 인터페이스
/// </summary>
public interface IServer : IDisposable
{
    /// <summary>
    /// 서버를 비동기로 시작합니다.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 서버를 정지합니다.
    /// </summary>
    void Stop();

    /// <summary>
    /// 클라이언트 연결 이벤트
    /// </summary>
    event Action<IServer, ISession>? ClientConnected;
}