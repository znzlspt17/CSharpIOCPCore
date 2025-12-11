using System;

namespace Net9IOCPCore.API;

/// <summary>
/// 전역적으로 고유한 식별자(GUID) 생성 유틸리티
/// </summary>
public static class UUIDGenerator
{
    /// <summary>
    /// 새로운 GUID를 생성합니다.
    /// </summary>
    public static Guid Get() => Guid.NewGuid();
}
