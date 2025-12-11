using System;
using System.Threading.Tasks;

namespace Net9IOCPCore.API;

/// <summary>
/// 세션 인터페이스
/// </summary>
public interface ISession : IDisposable
{
    /// <summary>
    /// 세션의 고유 식별자
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// 원시 바이트를 전송합니다.
    /// </summary>
    Task SendRawAsync(byte[] data);

    /// <summary>
    /// 연결을 종료합니다.
    /// </summary>
    void Close();

    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    event Action<ISession, Message>? Received;

    /// <summary>
    /// 연결 종료 알림 이벤트
    /// </summary>
    event Action<ISession, Exception?>? Disconnected;
}