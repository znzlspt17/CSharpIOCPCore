# CSharpIOCPCore

C#으로 구현한 IOCP (Input/Output Completion Port) 기반 네트워크 통신 라이브러리입니다. 

## 📋 프로젝트 소개

이 라이브러리는 Windows의 IOCP를 활용하여 고성능 비동기 네트워크 통신을 쉽게 구현할 수 있도록 도와줍니다.  
다수의 클라이언트 연결을 효율적으로 처리할 수 있는 재사용 가능한 컴포넌트를 제공합니다.

## 🚀 주요 기능

- IOCP 기반 비동기 소켓 통신
- 고성능 멀티 클라이언트 처리
- 안정적인 연결 관리
- 효율적인 스레드 풀 활용
- 간편한 API 인터페이스

## 🛠️ 기술 스택

- C# / . NET
- Socket Programming
- IOCP (Input/Output Completion Port)

## 📦 설치

### 직접 빌드
```bash
# 레포지토리 클론
git clone https://github.com/znzlspt17/CSharpIOCPCore.git

# 프로젝트 디렉토리로 이동
cd CSharpIOCPCore

# 빌드
dotnet build
```

## 📖 사용 방법

### 기본 사용 예제

```csharp
// 서버 초기화 예제
using CSharpIOCPCore;

// TODO: 실제 API에 맞게 수정 필요
var server = new IOCPServer();
server.Start(port: 8080);

// 클라이언트 연결 처리
server.OnClientConnected += (client) => {
    Console.WriteLine("Client connected!");
};

// 데이터 수신 처리
server.OnDataReceived += (client, data) => {
    Console.WriteLine($"Received: {data}");
};
```

## 🔧 요구사항

- . NET 6.0 이상
- Windows OS

## 📄 라이선스

이 프로젝트는 개인 학습 및 예제 목적으로 작성되었습니다.  

## 💡 개발 지원

이 프로젝트는 **GitHub Copilot**의 지원을 받아 개발되었습니다. 

---

**Created by znzlspt17**
