using System;

namespace Net9IOCPCore.Core.Event;

/// <summary>
/// 이벤트 리스너 인터페이스
/// </summary>
public interface IEventListener : IDisposable
{
    void Start();
    void Stop();
}
