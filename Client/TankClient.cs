using System;
using System.Collections.Generic;
using System.Threading;
using Nettention.Proud;

namespace TankGame
{
    // 간단한 탱크 정보를 저장하는 클래스
    public class TankInfo
    {
        public int ClientId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Direction { get; set; }
        public int TankType { get; set; }
        public float CurrentHealth { get; set; } // 추가: 현재 체력
        public float MaxHealth { get; set; } // 추가: 최대 체력
        public bool IsDestroyed { get; set; } // 추가: 파괴 상태

        public TankInfo(int clientId, float posX = 0, float posY = 0, float direction = 0, int tankType = 0, float maxHealth = 100f)
        {
            ClientId = clientId;
            PosX = posX;
            PosY = posY;
            Direction = direction;
            TankType = tankType;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth; // 초기 체력은 최대 체력으로 설정
            IsDestroyed = false;
        }
    }

    // 간단한 탱크 게임 클라이언트
    class TankClient
    {
        // 동기화 객체 (멀티스레드 보호용)
        private object syncObj = new object();

        // RMI Stub 및 Proxy 인스턴스
        private Tank.Stub tankStub = new Tank.Stub();
        private Tank.Proxy tankProxy = new Tank.Proxy();

        // 네트워크 클라이언트 인스턴스
        private NetClient netClient;

        // 다른 탱크들의 정보
        private Dictionary<int, TankInfo> otherTanks = new Dictionary<int, TankInfo>();

        // 로컬 탱크 정보
        private TankInfo localTank;
        
        // 연결 상태
        private bool isConnected = false;
        
        // 종료 플래그
        private bool isRunning = true;
        
        // 마지막으로 P2P 그룹에 참가한 그룹 ID
        private HostID lastJoinedP2PGroupID = HostID.HostID_None;

        private bool isAutoMove = false;
        private Thread autoMoveThread;
        private Random random = new Random();

        // 초기화 함수
        private void Initialize()
        {
            // 네트워크 클라이언트 생성
            netClient = new NetClient();
            
            // 로컬 탱크 초기화
            localTank = new TankInfo(0); // 임시 ID, 서버 연결 후 업데이트됨
            
            // 스텁 초기화
            InitializeStub();
            
            // 프록시와 스텁을 클라이언트에 연결
            netClient.AttachProxy(tankProxy);
            netClient.AttachStub(tankStub);
            
            // 서버 연결 완료 핸들러
            netClient.JoinServerCompleteHandler = OnJoinServerComplete;
            
            // 서버 연결 해제 핸들러
            netClient.LeaveServerHandler = OnLeaveServer;
            
            // P2P 멤버 참가 핸들러
            netClient.P2PMemberJoinHandler = OnP2PMemberJoin;
            
            // P2P 멤버 퇴장 핸들러
            netClient.P2PMemberLeaveHandler = OnP2PMemberLeave;
        }

        // 스텁 초기화
        private void InitializeStub()
        {
            // 위치 업데이트 처리
            tankStub.OnTankPositionUpdated = (remote, rmiContext, clientId, posX, posY, direction) =>
            {
                lock (syncObj)
                {
                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // 자신의 탱크 정보 업데이트
                        localTank.PosX = posX;
                        localTank.PosY = posY;
                        localTank.Direction = direction;
                    }
                    else
                    {
                        // 다른 탱크 정보 업데이트
                        if (otherTanks.ContainsKey(clientId))
                        {
                            otherTanks[clientId].PosX = posX;
                            otherTanks[clientId].PosY = posY;
                            otherTanks[clientId].Direction = direction;
                            
                            // 다른 탱크의 위치 변경 정보를 콘솔에 출력
                            Console.WriteLine($">>> OTHER TANK {clientId} MOVED TO ({posX},{posY}) dir={direction}");
                        }
                    }
                }
                return true;
            };

            // 총알 발사 처리
            tankStub.OnSpawnBullet = (remote, rmiContext, clientId, shooterId, posX, posY, direction, launchForce, fireX, fireY, fireZ) =>
            {
                lock (syncObj)
                {
                    // 총알 발사 정보 출력
                    Console.WriteLine($"OnSpawnBullet: Tank {clientId} fired from ({posX},{posY}) dir={direction}, force={launchForce}");
                    Console.WriteLine($"Fire position: ({fireX}, {fireY}, {fireZ}), shooter ID={shooterId}");
                    
                    // 추가 로직이 필요하면 여기에 구현
                }
                return true;
            };

            // 플레이어 참가 처리
            tankStub.OnPlayerJoined = (remote, rmiContext, clientId, posX, posY, tankType) =>
            {
                lock (syncObj)
                {
                    // 새 탱크 정보 저장 (자신이 아닌 경우)
                    if (clientId != (int)netClient.GetLocalHostID())
                    {
                        TankInfo newTank = new TankInfo(clientId, posX, posY, 0, tankType);
                        otherTanks[clientId] = newTank;
                        
                        Console.WriteLine($"OnPlayerJoined: Tank {clientId} joined at ({posX},{posY}) with tank type {tankType}");
                    }
                    else
                    {
                        // 자신의 초기 위치 설정
                        localTank.ClientId = clientId;
                        localTank.PosX = posX;
                        localTank.PosY = posY;
                        localTank.TankType = tankType;
                        
                        Console.WriteLine($"My tank spawned at ({posX},{posY}) with tank type {tankType}");
                    }
                }
                return true;
            };

            // 플레이어 퇴장 처리
            tankStub.OnPlayerLeft = (remote, rmiContext, clientId) =>
            {
                lock (syncObj)
                {
                    // 퇴장한 탱크 정보 제거
                    if (otherTanks.ContainsKey(clientId))
                    {
                        otherTanks.Remove(clientId);
                        Console.WriteLine($"OnPlayerLeft: Tank {clientId} left the game");
                    }
                }
                return true;
            };

            // P2P 메시지 처리
            tankStub.P2PMessage = (remote, rmiContext, message) =>
            {
                lock (syncObj)
                {
                    // P2P 메시지 출력
                    Console.WriteLine($"P2PMessage from {remote}: {message}");
                }
                return true;
            };

            // 탱크 타입 요청 처리
            tankStub.SendTankType = (remote, rmiContext, tankType) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine($"SendTankType from client {remote}: tank type {tankType}");
                    
                    // 해당 클라이언트의 탱크 타입 저장을 위한 추가 로직 (서버에서 저장됨)
                    // 클라이언트에서는 이 정보를 받아도 별도로 처리할 필요가 없음
                }
                return true;
            };
            
            // 탱크 체력 업데이트 처리
            tankStub.OnTankHealthUpdated = (remote, rmiContext, clientId, currentHealth, maxHealth) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine($"OnTankHealthUpdated: Tank {clientId} health: {currentHealth}/{maxHealth}");
                    
                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // 자신의 탱크 체력 업데이트
                        localTank.CurrentHealth = currentHealth;
                        localTank.MaxHealth = maxHealth;
                        Console.WriteLine($"My tank health updated: {currentHealth}/{maxHealth}");
                    }
                    else if (otherTanks.ContainsKey(clientId))
                    {
                        // 다른 탱크 체력 업데이트
                        otherTanks[clientId].CurrentHealth = currentHealth;
                        otherTanks[clientId].MaxHealth = maxHealth;
                    }
                }
                return true;
            };
            
            // 탱크 파괴 이벤트 처리
            tankStub.OnTankDestroyed = (remote, rmiContext, clientId, destroyedById) =>
            {
                lock (syncObj)
                {
                    string destroyedByText = destroyedById > 0 ? $"by tank {destroyedById}" : "by environment";
                    Console.WriteLine($"OnTankDestroyed: Tank {clientId} was destroyed {destroyedByText}");
                    
                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // 자신의 탱크가 파괴됨
                        localTank.IsDestroyed = true;
                        localTank.CurrentHealth = 0;
                        Console.WriteLine($"MY TANK WAS DESTROYED {destroyedByText}!");
                    }
                    else if (otherTanks.ContainsKey(clientId))
                    {
                        // 다른 탱크가 파괴됨
                        otherTanks[clientId].IsDestroyed = true;
                        otherTanks[clientId].CurrentHealth = 0;
                    }
                }
                return true;
            };
            
            // 탱크 생성/리스폰 이벤트 처리
            tankStub.OnTankSpawned = (remote, rmiContext, clientId, posX, posY, direction, tankType, initialHealth) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine($"OnTankSpawned: Tank {clientId} spawned at ({posX},{posY}) with type {tankType} and health {initialHealth}");
                    
                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // 자신의 탱크 생성/리스폰
                        localTank.PosX = posX;
                        localTank.PosY = posY;
                        localTank.Direction = direction;
                        localTank.TankType = tankType;
                        localTank.CurrentHealth = initialHealth;
                        localTank.IsDestroyed = false;
                        Console.WriteLine($"MY TANK SPAWNED at ({posX},{posY}) with health {initialHealth}");
                    }
                    else
                    {
                        // 다른 탱크 생성/리스폰
                        if (otherTanks.ContainsKey(clientId))
                        {
                            // 기존 탱크 정보 업데이트
                            otherTanks[clientId].PosX = posX;
                            otherTanks[clientId].PosY = posY;
                            otherTanks[clientId].Direction = direction;
                            otherTanks[clientId].TankType = tankType;
                            otherTanks[clientId].CurrentHealth = initialHealth;
                            otherTanks[clientId].IsDestroyed = false;
                        }
                        else
                        {
                            // 새 탱크 정보 생성
                            TankInfo newTank = new TankInfo(clientId, posX, posY, direction, tankType, initialHealth);
                            otherTanks[clientId] = newTank;
                        }
                    }
                }
                return true;
            };
        }

        // 서버 연결 완료 처리
        private void OnJoinServerComplete(ErrorInfo info, ByteArray replyFromServer)
        {
            lock (syncObj)
            {
                if (info.errorType == ErrorType.Ok)
                {
                    // 연결 성공
                    isConnected = true;
                    
                    // 자신의 ID 업데이트
                    localTank.ClientId = (int)netClient.GetLocalHostID();
                    
                    Console.WriteLine($"Connected to server. My ID: {localTank.ClientId}");
                }
                else
                {
                    // 연결 실패
                    Console.WriteLine($"Failed to connect to server: {info.comment}");
                    isRunning = false;
                }
            }
        }

        // 서버 연결 해제 처리
        private void OnLeaveServer(ErrorInfo errorInfo)
        {
            lock (syncObj)
            {
                Console.WriteLine($"Disconnected from server: {errorInfo.comment}");
                isConnected = false;
                isRunning = false;
            }
        }

        // P2P 멤버 참가 처리
        private void OnP2PMemberJoin(HostID memberHostID, HostID groupHostID, int memberCount, ByteArray customField)
        {
            lock (syncObj)
            {
                Console.WriteLine($"P2P: Member {memberHostID} joined group {groupHostID}");
                lastJoinedP2PGroupID = groupHostID;
            }
        }

        // P2P 멤버 퇴장 처리
        private void OnP2PMemberLeave(HostID memberHostID, HostID groupHostID, int memberCount)
        {
            lock (syncObj)
            {
                Console.WriteLine($"P2P: Member {memberHostID} left group {groupHostID}");
            }
        }

        // 서버 연결
        private void ConnectToServer()
        {
            // 연결 파라미터 설정
            NetConnectionParam param = new NetConnectionParam();
            param.protocolVersion.Set(Vars.m_Version);
            param.serverIP = Vars.ServerIP;
            param.serverPort = (ushort)Vars.ServerPort;
            
            // 서버 연결 시도
            netClient.Connect(param);
            Console.WriteLine("Connecting to server...");
        }

        // 탱크 이동 요청
        private void RequestMove(float posX, float posY, float direction)
        {
            if (isConnected)
            {
                // 서버에 이동 요청
                tankProxy.SendMove(HostID.HostID_Server, RmiContext.ReliableSend, 
                    posX, posY, direction);
                
                // 로컬 정보 업데이트 (서버에서 다시 갱신될 수 있음)
                localTank.PosX = posX;
                localTank.PosY = posY;
                localTank.Direction = direction;
            }
        }

        // 발사 요청
        private void RequestFire(float direction, float launchForce = 25.0f)
        {
            if (isConnected)
            {
                // 발사 위치와 파워 설정 (실제 게임에서는 이 값들이 현재 탱크 상태에서 계산됨)
                float fireX = localTank.PosX;
                float fireY = 1.0f; // 지상에서 약간 높이
                float fireZ = localTank.PosY; // 주의: 2D 게임에서 Z축은 클라이언트 Y축과 매핑될 수 있음
                
                // 서버에 발사 요청 (발사자 ID, 방향, 발사 힘, 발사 위치 포함)
                tankProxy.SendFire(HostID.HostID_Server, RmiContext.ReliableSend, 
                    localTank.ClientId, direction, launchForce, fireX, fireY, fireZ);
                
                Console.WriteLine($"Fire request sent: direction={direction}, force={launchForce}, position=({fireX}, {fireY}, {fireZ})");
            }
        }

        // 체력 업데이트 요청
        private void SendHealthUpdate(float currentHealth, float maxHealth)
        {
            if (isConnected)
            {
                // 서버에 체력 업데이트 요청 전송
                tankProxy.SendTankHealthUpdated(HostID.HostID_Server, RmiContext.ReliableSend, 
                    currentHealth, maxHealth);
                
                // 로컬 정보 업데이트
                localTank.CurrentHealth = currentHealth;
                localTank.MaxHealth = maxHealth;
                
                Console.WriteLine($"Health update sent: {currentHealth}/{maxHealth}");
            }
        }
        
        // 탱크 파괴 이벤트 전송
        private void SendTankDestroyed(int destroyedById)
        {
            if (isConnected)
            {
                // 서버에 탱크 파괴 이벤트 전송
                tankProxy.SendTankDestroyed(HostID.HostID_Server, RmiContext.ReliableSend, 
                    destroyedById);
                
                // 로컬 상태 업데이트
                localTank.IsDestroyed = true;
                localTank.CurrentHealth = 0;
                
                string destroyedByText = destroyedById > 0 ? $"by tank {destroyedById}" : "by environment";
                Console.WriteLine($"Tank destroyed event sent: destroyed {destroyedByText}");
            }
        }
        
        // 탱크 생성/리스폰 메시지 전송
        private void SendTankSpawn(float posX, float posY, float direction, int tankType, float initialHealth)
        {
            if (isConnected)
            {
                // 서버에 탱크 생성/리스폰 메시지 전송
                tankProxy.SendTankSpawned(HostID.HostID_Server, RmiContext.ReliableSend, 
                    posX, posY, direction, tankType, initialHealth);
                
                // 로컬 상태 업데이트
                localTank.PosX = posX;
                localTank.PosY = posY;
                localTank.Direction = direction;
                localTank.TankType = tankType;
                localTank.CurrentHealth = initialHealth;
                localTank.IsDestroyed = false;
                
                Console.WriteLine($"Tank spawn message sent: position=({posX},{posY}), type={tankType}, health={initialHealth}");
            }
        }

        // P2P 메시지 전송
        private void SendP2PMessage(string message)
        {
            if (isConnected && lastJoinedP2PGroupID != HostID.HostID_None)
            {
                // P2P 그룹 멤버에게 메시지 전송
                RmiContext context = RmiContext.ReliableSend;
                context.enableLoopback = false; // 자신에게는 전송하지 않음
                
                tankProxy.P2PMessage(lastJoinedP2PGroupID, context, message);
            }
        }

        // 게임 루프 실행
        public void Run()
        {
            // 초기화
            Initialize();
            
            // 서버 연결
            ConnectToServer();
            
            // 네트워크 프레임 처리를 위한 스레드
            Thread networkThread = new Thread(() =>
            {
                while (isRunning)
                {
                    // 주기적으로 네트워크 처리
                    netClient.FrameMove();
                    Thread.Sleep(10); // CPU 사용률 조절
                }
            });
            
            networkThread.IsBackground = true;
            networkThread.Start();
            
            // 명령어 안내
            Console.WriteLine("Commands:");
            Console.WriteLine("move x y dir : Move tank to position (x,y) with direction dir");
            Console.WriteLine("fire dir [force] : Fire in direction dir with optional force (default 25.0)");
            Console.WriteLine("tank n       : Select tank type (0-3)");
            Console.WriteLine("health h [max]: Update tank health to h with optional max health");
            Console.WriteLine("destroy [id] : Send tank destroyed event (id of destroyer, default 0 = environment)");
            Console.WriteLine("spawn x y dir type health: Spawn/respawn tank");
            Console.WriteLine("msg text     : Send P2P message to other clients");
            Console.WriteLine("auto         : Toggle auto movement");
            Console.WriteLine("q            : Quit");
            
            // 메인 루프
            while (isRunning)
            {
                lock (syncObj)
                {
                    if (isConnected)
                    {
                        // 현재 상태 표시
                        string healthStatus = localTank.IsDestroyed ? "DESTROYED" : $"{localTank.CurrentHealth}/{localTank.MaxHealth}";
                        Console.WriteLine($"Status: Tank ID={localTank.ClientId}, Position=({localTank.PosX},{localTank.PosY}), Type={localTank.TankType}, Health={healthStatus}");
                    }
                }
                
                // 사용자 입력 처리
                string input = Console.ReadLine();
                
                lock (syncObj)
                {
                    if (!isConnected)
                        continue;
                    
                    // 명령어 처리
                    if (input.StartsWith("move "))
                    {
                        // 이동 명령 처리
                        string[] parts = input.Split(' ');
                        if (parts.Length == 4)
                        {
                            try
                            {
                                float x = float.Parse(parts[1]);
                                float y = float.Parse(parts[2]);
                                float dir = float.Parse(parts[3]);
                                
                                RequestMove(x, y, dir);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid parameters. Format: move x y dir");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid parameters. Format: move x y dir");
                        }
                    }
                    else if (input.StartsWith("fire "))
                    {
                        // 발사 명령 처리
                        string[] parts = input.Split(' ');
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                float dir = float.Parse(parts[1]);
                                float force = 25.0f; // 기본값
                                
                                // 추가 파라미터가 있으면 발사 힘으로 사용
                                if (parts.Length >= 3)
                                {
                                    force = float.Parse(parts[2]);
                                }
                                
                                RequestFire(dir, force);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid parameters. Format: fire dir [force]");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid parameters. Format: fire dir [force]");
                        }
                    }
                    else if (input.StartsWith("tank "))
                    {
                        // 탱크 타입 선택 처리
                        string[] parts = input.Split(' ');
                        if (parts.Length == 2)
                        {
                            try
                            {
                                int tankType = int.Parse(parts[1]);
                                if (tankType >= 0 && tankType <= 3)
                                {
                                    // 서버에 탱크 타입 요청 전송
                                    tankProxy.SendTankType(HostID.HostID_Server, RmiContext.ReliableSend, tankType);
                                    Console.WriteLine($"Tank type set to {tankType}");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid tank type. Valid range is 0-3");
                                }
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid parameter. Format: tank n (where n is 0-3)");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid parameter. Format: tank n (where n is 0-3)");
                        }
                    }
                    else if (input.StartsWith("health "))
                    {
                        // 체력 업데이트 명령 처리
                        string[] parts = input.Split(' ');
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                float health = float.Parse(parts[1]);
                                float maxHealth = localTank.MaxHealth; // 기본값: 현재 최대 체력
                                
                                // 최대 체력 파라미터가 있으면 사용
                                if (parts.Length >= 3)
                                {
                                    maxHealth = float.Parse(parts[2]);
                                }
                                
                                SendHealthUpdate(health, maxHealth);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid parameters. Format: health h [maxHealth]");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid parameters. Format: health h [maxHealth]");
                        }
                    }
                    else if (input.StartsWith("destroy"))
                    {
                        // 탱크 파괴 명령 처리
                        string[] parts = input.Split(' ');
                        int destroyerId = 0; // 기본값: 환경에 의한 파괴
                        
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                destroyerId = int.Parse(parts[1]);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid destroyer ID. Using default (0 = environment)");
                            }
                        }
                        
                        SendTankDestroyed(destroyerId);
                    }
                    else if (input.StartsWith("spawn "))
                    {
                        // 탱크 생성/리스폰 명령 처리
                        string[] parts = input.Split(' ');
                        if (parts.Length >= 6)
                        {
                            try
                            {
                                float posX = float.Parse(parts[1]);
                                float posY = float.Parse(parts[2]);
                                float direction = float.Parse(parts[3]);
                                int tankType = int.Parse(parts[4]);
                                float health = float.Parse(parts[5]);
                                
                                SendTankSpawn(posX, posY, direction, tankType, health);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid parameters. Format: spawn x y direction type health");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid parameters. Format: spawn x y direction type health");
                        }
                    }
                    else if (input.StartsWith("msg "))
                    {
                        // P2P 메시지 전송 처리
                        string message = input.Substring(4);
                        
                        SendP2PMessage(message);
                    }
                    else if (input.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        // 자동 이동 토글
                        ToggleAutoMove();
                    }
                    else if (input.Equals("q"))
                    {
                        // 종료 처리
                        isRunning = false;
                        
                        // 서버 연결 해제
                        netClient.Disconnect();
                    }
                    else
                    {
                        Console.WriteLine("Unknown command");
                    }
                }
            }
            
            // 자원 정리
            if (autoMoveThread != null && autoMoveThread.IsAlive)
            {
                isAutoMove = false;
                autoMoveThread.Join(1000);
            }
            
            // 연결 종료 (아직 연결되어 있는 경우)
            if (isConnected)
            {
                netClient.Disconnect();
            }
            Console.WriteLine("Client stopped");
        }

        // 자동 이동 토글
        private void ToggleAutoMove()
        {
            lock (syncObj)
            {
                isAutoMove = !isAutoMove;
                
                if (isAutoMove)
                {
                    Console.WriteLine("Auto movement enabled");
                    
                    // 자동 이동 스레드 시작
                    autoMoveThread = new Thread(AutoMoveThread);
                    autoMoveThread.IsBackground = true;
                    autoMoveThread.Start();
                }
                else
                {
                    Console.WriteLine("Auto movement disabled");
                }
            }
        }
        
        // 자동 이동 스레드
        private void AutoMoveThread()
        {
            while (isAutoMove && isConnected && isRunning)
            {
                try
                {
                    // 랜덤 방향 및 약간의 이동 생성
                    lock (syncObj)
                    {
                        float x = localTank.PosX + (float)(random.NextDouble() * 20 - 10);
                        float y = localTank.PosY + (float)(random.NextDouble() * 20 - 10);
                        float dir = (float)(random.NextDouble() * 360);
                        
                        // 유효한 범위로 제한
                        x = Math.Max(0, Math.Min(100, x));
                        y = Math.Max(0, Math.Min(100, y));
                        
                        // 이동 요청
                        RequestMove(x, y, dir);
                        
                        Console.WriteLine($"Auto move to ({x},{y}) dir={dir}");
                    }
                    
                    // 3-5초 대기
                    int sleepTime = random.Next(3000, 5000);
                    Thread.Sleep(sleepTime);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in auto move: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        // 메인 함수
        static void Main(string[] args)
        {
            TankClient client = new TankClient();
            client.Run();
        }
    }
} 