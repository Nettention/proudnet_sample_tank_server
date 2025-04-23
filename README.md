# Tank Game with ProudNet

이 프로젝트는 ProudNet을 사용하여 구현된 간단한 탱크 게임 서버와 클라이언트입니다.

## 프로젝트 구조

- `Common/`: 서버와 클라이언트가 공유하는 공통 코드
  - `Tank.PIDL`: ProudNet RMI 정의 파일
  - `Vars.cs`: 공통 설정 값
  - `CompilePIDL.bat`: PIDL 컴파일 스크립트

- `Server/`: 서버 코드
  - `TankServer.cs`: 메인 서버 코드
  - `TankServer.csproj`: 서버 프로젝트 파일

- `Client/`: 클라이언트 코드
  - `TankClient.cs`: 메인 클라이언트 코드
  - `TankClient.csproj`: 클라이언트 프로젝트 파일

## 빌드 및 실행 방법

### 1. PIDL 컴파일

먼저 PIDL 파일을 컴파일하여 RMI 스텁과 프록시 코드를 생성해야 합니다:

```
cd Common
CompilePIDL.bat
```

### 2. 솔루션 빌드

Visual Studio에서 `TankGame.sln` 솔루션 파일을 열고 빌드합니다.
또는 명령줄에서 다음을 실행합니다:

```
dotnet build TankGame.sln
```

### 3. 실행

1. 먼저 서버를 실행합니다:
```
cd Server/bin/Debug/net472
TankServer.exe
```

2. 그다음 클라이언트를 실행합니다(여러 명의 플레이어는 여러 클라이언트 실행):
```
cd Client/bin/Debug/net472
TankClient.exe
```

## 클라이언트 명령어

클라이언트에서는 다음 명령어를 사용할 수 있습니다:

- `move x y dir`: 탱크 위치를 (x, y)로 이동하고 방향을 dir로 설정
- `fire dir`: dir 방향으로 발사
- `msg text`: 다른 클라이언트에게 P2P 메시지 전송
- `q`: 종료

## 참고

- ProudNet 라이브러리 경로는 `../../ProudNet/lib/net472/`로 설정되어 있습니다. 실제 환경에 맞게 .csproj 파일에서 경로를 수정해주세요.
- 서버는 기본적으로 포트 33334에서 실행됩니다. 필요하면 `Vars.cs`에서 수정 가능합니다. 