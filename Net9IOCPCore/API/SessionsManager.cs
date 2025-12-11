using System;
using System.Collections.Concurrent;


namespace Net9IOCPCore.API;

/// <summary>
/// 세션 관리자
/// </summary>
public sealed class SessionsManager
{
    private readonly ConcurrentDictionary<Guid, Session> _session = new();
    
    public int Count => _session.Count;
    
    public bool TryAdd(Guid id, Session session)
    {
        return _session.TryAdd(id, session);
    }

    public bool TryRemove(Guid id, out Session? session)
    {
        return _session.TryRemove(id, out session);
    }

    public bool TryGet(Guid id, out Session? session)
    {
        return _session.TryGetValue(id, out session);
    }

    public IEnumerable<Session> GetAll()
    {
        return _session.Values.ToList();
    }

    public void Clear()
    {
        _session.Clear();
    }
}
