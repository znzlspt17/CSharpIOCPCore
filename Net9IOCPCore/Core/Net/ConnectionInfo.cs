using System;

namespace Net9IOCPCore.Core.Net;

/// <summary>
/// 외부에 노출되는 연결 식별 정보
/// </summary>
public readonly record struct ConnectionInfo(nuint Key);