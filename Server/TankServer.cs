using System;
using System.Collections.Generic;
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
        public int TankType { get; set; } // 추가: 탱크 타입 정보
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

    class TankServer
    {
        // RMI Stub 및 Proxy 인스턴스
        private Tank.Stub tankStub = new Tank.Stub();
        private Tank.Proxy tankProxy = new Tank.Proxy();

        // P2P 그룹 ID
        private HostID gameP2PGroupID = HostID.HostID_None;

        // 연결된 탱크들의 정보
        private Dictionary<HostID, TankInfo> tanks = new Dictionary<HostID, TankInfo>();

        // 네트워크 서버 인스턴스
        private NetServer server;

        // 초기화 함수
        private void Initialize()
        {
            // 서버 객체 생성
            server = new NetServer();

            // 스텁 함수 구현
            InitializeStub();

            // 스텁과 프록시를 서버에 연결
            server.AttachStub(tankStub);
            server.AttachProxy(tankProxy);

            // 클라이언트 접속 이벤트 핸들러
            server.ClientJoinHandler = OnClientJoin;

            // 클라이언트 접속 종료 이벤트 핸들러
            server.ClientLeaveHandler = OnClientLeave;

            // 서버 시작 설정
            var serverParam = new StartServerParameter();
            serverParam.protocolVersion = new Nettention.Proud.Guid(Vars.m_Version);
            serverParam.tcpPorts.Add(Vars.ServerPort);
        }

        // 스텁 함수 초기화
        private void InitializeStub()
        {
            // 위치 이동 요청 처리
            tankStub.SendMove = (remote, rmiContext, posX, posY, direction) =>
            {
                Console.WriteLine($"SendMove from client {remote}: pos=({posX},{posY}), direction={direction}");
                
                // 탱크 정보 업데이트
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].PosX = posX;
                    tanks[remote].PosY = posY;
                    tanks[remote].Direction = direction;

                    // 모든 클라이언트에게 업데이트된 위치 전송
                    // 각 클라이언트에게 개별적으로 메시지 전송
                    foreach (var clientID in tanks.Keys)
                    {
                        // 움직인 클라이언트 자신에게는 보내지 않음
                        if (clientID != remote)
                        {
                            tankProxy.OnTankPositionUpdated(clientID, RmiContext.ReliableSend, 
                                (int)remote, posX, posY, direction);
                        }
                    }
                }
                return true;
            };

            // 발사 요청 처리
            tankStub.SendFire = (remote, rmiContext, shooterId, direction, launchForce, fireX, fireY, fireZ) =>
            {
                Console.WriteLine($"========== SendFire 수신 ==========");
                Console.WriteLine($"From client {remote}: shooterId={shooterId}, direction={direction}, launchForce={launchForce}");
                Console.WriteLine($"Fire position: ({fireX}, {fireY}, {fireZ})");
                Console.WriteLine($"RmiContext: {rmiContext}");
                
                // 해당 클라이언트의 탱크 정보 가져오기
                if (tanks.ContainsKey(remote))
                {
                    var tank = tanks[remote];
                    Console.WriteLine($"Tank found: Position=({tank.PosX},{tank.PosY}), Direction={tank.Direction}");
                    
                    // 모든 클라이언트에게 총알 발사 정보 전송
                    // 각 클라이언트에게 개별적으로 메시지 전송
                    int recipientCount = 0;
                    foreach (var clientID in tanks.Keys)
                    {
                        // 발사한 클라이언트 자신에게는 보내지 않음
                        if (clientID != remote)
                        {
                            Console.WriteLine($"Sending OnSpawnBullet to client {clientID}");
                            tankProxy.OnSpawnBullet(clientID, RmiContext.ReliableSend,
                                (int)remote, shooterId, tank.PosX, tank.PosY, direction, 
                                launchForce, fireX, fireY, fireZ);
                            recipientCount++;
                        }
                    }
                    Console.WriteLine($"OnSpawnBullet sent to {recipientCount} clients");
                }
                else
                {
                    Console.WriteLine($"Error: Tank not found for client {remote}");
                }
                Console.WriteLine($"========== SendFire 처리 완료 ==========");
                return true;
            };

            // 탱크 타입 요청 처리
            tankStub.SendTankType = (remote, rmiContext, tankType) =>
            {
                Console.WriteLine($"========== SendTankType 수신 ==========");
                Console.WriteLine($"From client {remote}: tankType={tankType}");
                
                // 해당 클라이언트의 탱크 정보 업데이트
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].TankType = tankType;
                    Console.WriteLine($"Tank type updated for client {remote}: Type={tankType}");
                    
                    // 모든 다른 클라이언트에게 이 클라이언트의 탱크 타입 알림
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // OnPlayerJoined 메시지를 통해 탱크 타입 정보 전송
                            Console.WriteLine($"Notifying client {clientID} about tank type of client {remote}");
                            tankProxy.OnPlayerJoined(clientID, RmiContext.ReliableSend,
                                (int)remote, tanks[remote].PosX, tanks[remote].PosY, tankType);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Tank not found for client {remote}");
                }
                Console.WriteLine($"========== SendTankType 처리 완료 ==========");
                return true;
            };

            // P2P 메시지 (필요시 구현)
            tankStub.P2PMessage = (remote, rmiContext, message) =>
            {
                Console.WriteLine($"P2PMessage from client {remote}: {message}");
                return true;
            };
            
            // 체력 업데이트 처리 (클라이언트에서 서버로)
            tankStub.SendTankHealthUpdated = (remote, rmiContext, currentHealth, maxHealth) =>
            {
                Console.WriteLine($"========== SendTankHealthUpdated 수신 ==========");
                Console.WriteLine($"From client {remote}: currentHealth={currentHealth}, maxHealth={maxHealth}");
                
                // 해당 클라이언트의 탱크 정보 업데이트
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].CurrentHealth = currentHealth;
                    tanks[remote].MaxHealth = maxHealth;
                    tanks[remote].IsDestroyed = (currentHealth <= 0);
                    
                    Console.WriteLine($"Tank health updated for client {remote}: {currentHealth}/{maxHealth}");
                    
                    // 모든 다른 클라이언트에게 이 클라이언트의 체력 정보 전송
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // UpdateTankHealth 메시지를 통해 체력 정보 전송
                            Console.WriteLine($"Notifying client {clientID} about health of client {remote}");
                            tankProxy.OnTankHealthUpdated(clientID, RmiContext.ReliableSend,
                                (int)remote, currentHealth, maxHealth);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Tank not found for client {remote}");
                }
                Console.WriteLine($"========== SendTankHealthUpdated 처리 완료 ==========");
                return true;
            };
            
            // 탱크 파괴 이벤트 처리 (클라이언트에서 서버로)
            tankStub.SendTankDestroyed = (remote, rmiContext, destroyedById) =>
            {
                Console.WriteLine($"========== SendTankDestroyed 수신 ==========");
                Console.WriteLine($"From client {remote}: destroyedById={destroyedById}");
                
                // 해당 클라이언트의 탱크 정보 업데이트
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].IsDestroyed = true;
                    tanks[remote].CurrentHealth = 0;
                    
                    string destroyedByText = destroyedById > 0 ? $"by tank {destroyedById}" : "by environment";
                    Console.WriteLine($"Tank destroyed for client {remote}: {destroyedByText}");
                    
                    // 모든 다른 클라이언트에게 이 클라이언트의 파괴 정보 전송
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // OnTankDestroyed 메시지를 통해 파괴 정보 전송
                            Console.WriteLine($"Notifying client {clientID} about destruction of client {remote}");
                            tankProxy.OnTankDestroyed(clientID, RmiContext.ReliableSend,
                                (int)remote, destroyedById);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Tank not found for client {remote}");
                }
                Console.WriteLine($"========== SendTankDestroyed 처리 완료 ==========");
                return true;
            };
            
            // 탱크 생성/리스폰 메시지 처리 (클라이언트에서 서버로)
            tankStub.SendTankSpawned = (remote, rmiContext, posX, posY, direction, tankType, initialHealth) =>
            {
                Console.WriteLine($"========== SendTankSpawned 수신 ==========");
                Console.WriteLine($"From client {remote}: position=({posX},{posY}), direction={direction}, tankType={tankType}, health={initialHealth}");
                
                // 해당 클라이언트의 탱크 정보 업데이트
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].PosX = posX;
                    tanks[remote].PosY = posY;
                    tanks[remote].Direction = direction;
                    tanks[remote].TankType = tankType;
                    tanks[remote].CurrentHealth = initialHealth;
                    tanks[remote].MaxHealth = initialHealth; // 최대 체력도 업데이트
                    tanks[remote].IsDestroyed = false;
                    
                    Console.WriteLine($"Tank spawned for client {remote} at ({posX},{posY})");
                    
                    // 모든 다른 클라이언트에게 이 클라이언트의 생성/리스폰 정보 전송
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // OnTankSpawned 메시지를 통해 생성/리스폰 정보 전송
                            Console.WriteLine($"Notifying client {clientID} about spawn of client {remote}");
                            tankProxy.OnTankSpawned(clientID, RmiContext.ReliableSend,
                                (int)remote, posX, posY, direction, tankType, initialHealth);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Tank not found for client {remote}");
                }
                Console.WriteLine($"========== SendTankSpawned 처리 완료 ==========");
                return true;
            };
        }

        // 클라이언트 접속 처리
        private void OnClientJoin(NetClientInfo clientInfo)
        {
            Console.WriteLine($"Client {clientInfo.hostID} connected");
            
            // 랜덤 위치 생성 (예: 0~100 범위)
            Random random = new Random();
            float posX = (float)(random.NextDouble() * 100);
            float posY = (float)(random.NextDouble() * 100);
            
            // 초기 탱크 타입은 -1로 설정 (선택 안함)
            int defaultTankType = -1;
            
            // 초기 체력 설정
            float defaultMaxHealth = 100.0f;
            float defaultHealth = defaultMaxHealth;
            
            // 탱크 정보 저장
            TankInfo newTank = new TankInfo((int)clientInfo.hostID, posX, posY, 0, defaultTankType, defaultMaxHealth);
            tanks.Add(clientInfo.hostID, newTank);
            
            Console.WriteLine($"New tank created for client {clientInfo.hostID} with tank type {defaultTankType} and health {defaultHealth}/{defaultMaxHealth}");
            
            // 새 클라이언트에게 기존 탱크 정보 전송
            foreach (var tank in tanks)
            {
                if (tank.Key != clientInfo.hostID)  // 본인 제외
                {
                    // 기존 플레이어 정보 전송
                    tankProxy.OnPlayerJoined(clientInfo.hostID, RmiContext.ReliableSend,
                        (int)tank.Key, tank.Value.PosX, tank.Value.PosY, tank.Value.TankType);
                    
                    // 기존 플레이어 체력 정보 전송
                    tankProxy.OnTankHealthUpdated(clientInfo.hostID, RmiContext.ReliableSend,
                        (int)tank.Key, tank.Value.CurrentHealth, tank.Value.MaxHealth);
                    
                    // 기존 플레이어가 파괴 상태라면 파괴 정보도 전송
                    if (tank.Value.IsDestroyed)
                    {
                        tankProxy.OnTankDestroyed(clientInfo.hostID, RmiContext.ReliableSend,
                            (int)tank.Key, 0); // 파괴자 ID 정보가 없으므로 0(환경)으로 설정
                    }
                    
                    Console.WriteLine($"Sending existing player info to new client: ID={tank.Key}, Type={tank.Value.TankType}, Health={tank.Value.CurrentHealth}/{tank.Value.MaxHealth}");
                }
            }
            
            // 모든 클라이언트에게 새 플레이어 참가 알림
            foreach (var existingClientID in tanks.Keys)
            {
                // 새로 참가한 클라이언트 자신에게는 보내지 않음
                if (existingClientID != clientInfo.hostID)
                {
                    // 새 플레이어 정보 전송
                    tankProxy.OnPlayerJoined(existingClientID, RmiContext.ReliableSend,
                        (int)clientInfo.hostID, posX, posY, defaultTankType);
                    
                    // 새 플레이어 체력 정보 전송
                    tankProxy.OnTankHealthUpdated(existingClientID, RmiContext.ReliableSend,
                        (int)clientInfo.hostID, defaultHealth, defaultMaxHealth);
                    
                    Console.WriteLine($"Notifying existing client {existingClientID} about new player: ID={clientInfo.hostID}, Type={defaultTankType}, Health={defaultHealth}/{defaultMaxHealth}");
                }
            }
            
            // P2P 그룹 업데이트 (필요시)
            UpdateP2PGroup();
        }

        // 클라이언트 접속 종료 처리
        private void OnClientLeave(NetClientInfo clientInfo, ErrorInfo errorInfo, ByteArray comment)
        {
            Console.WriteLine($"Client {clientInfo.hostID} disconnected: {errorInfo.comment}");
            
            // 탱크 정보 제거
            if (tanks.ContainsKey(clientInfo.hostID))
            {
                tanks.Remove(clientInfo.hostID);
            }
            
            // 모든 클라이언트에게 플레이어 퇴장 알림
            // 각 클라이언트에게 개별적으로 메시지 전송
            foreach (var remainingClientID in tanks.Keys)
            {
                tankProxy.OnPlayerLeft(remainingClientID, RmiContext.ReliableSend, (int)clientInfo.hostID);
            }
            
            // P2P 그룹 업데이트 (필요시)
            UpdateP2PGroup();
        }

        // P2P 그룹 업데이트 (필요시 사용)
        private void UpdateP2PGroup()
        {
            // 기존 그룹 제거
            if (gameP2PGroupID != HostID.HostID_None)
            {
                server.DestroyP2PGroup(gameP2PGroupID);
            }
            
            // 새 그룹 생성 (2명 이상일 때)
            if (tanks.Count >= 2)
            {
                HostID[] clients = new HostID[tanks.Count];
                int index = 0;
                foreach (var tank in tanks)
                {
                    clients[index++] = tank.Key;
                }
                
                gameP2PGroupID = server.CreateP2PGroup(clients, new ByteArray());
                Console.WriteLine($"P2P group created with {tanks.Count} members");
            }
            else
            {
                gameP2PGroupID = HostID.HostID_None;
            }
        }

        // 서버 시작
        public void Start()
        {
            Initialize();
            
            try
            {
                // 서버 시작 파라미터 설정
                var serverParam = new StartServerParameter();
                serverParam.protocolVersion = new Nettention.Proud.Guid(Vars.m_Version);
                serverParam.tcpPorts.Add(Vars.ServerPort);
                
                // 서버 시작
                server.Start(serverParam);
                Console.WriteLine($"Tank Server started on port {Vars.ServerPort}");
                
                // 커맨드 처리
                ProcessCommands();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server start failed: {ex.Message}");
            }
        }

        // 커맨드 처리 루프
        private void ProcessCommands()
        {
            Console.WriteLine("Server is running. Commands:");
            Console.WriteLine("status: Show connected clients");
            Console.WriteLine("health [id]: Show tank health (all or specific ID)");
            Console.WriteLine("damage id amount: Apply damage to a tank");
            Console.WriteLine("heal id amount: Heal a tank");
            Console.WriteLine("respawn id x y: Respawn a tank at position (x,y)");
            Console.WriteLine("q: Quit server");
            
            string input;
            while (true)
            {
                input = Console.ReadLine();
                
                if (input == "q")
                {
                    break;
                }
                else if (input == "status")
                {
                    PrintConnectedClients();
                }
                else if (input.StartsWith("health"))
                {
                    ShowTankHealth(input);
                }
                else if (input.StartsWith("damage "))
                {
                    ApplyDamageToTank(input);
                }
                else if (input.StartsWith("heal "))
                {
                    HealTank(input);
                }
                else if (input.StartsWith("respawn "))
                {
                    RespawnTank(input);
                }
            }
            
            // 서버 종료
            server.Stop();
            Console.WriteLine("Server stopped");
        }
        
        // 연결된 클라이언트 정보 출력
        private void PrintConnectedClients()
        {
            Console.WriteLine("========== Connected Clients ==========");
            Console.WriteLine($"Total: {tanks.Count} clients");
            
            foreach (var tank in tanks)
            {
                string healthStatus = tank.Value.IsDestroyed ? "DESTROYED" : $"{tank.Value.CurrentHealth}/{tank.Value.MaxHealth}";
                Console.WriteLine($"Client ID: {tank.Key}, Position: ({tank.Value.PosX},{tank.Value.PosY}), TankType: {tank.Value.TankType}, Health: {healthStatus}");
            }
            
            Console.WriteLine("=======================================");
        }
        
        // 탱크 체력 정보 출력
        private void ShowTankHealth(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
            {
                // 모든 탱크의 체력 정보 출력
                Console.WriteLine("========== Tank Health Status ==========");
                foreach (var tank in tanks)
                {
                    string healthStatus = tank.Value.IsDestroyed ? "DESTROYED" : $"{tank.Value.CurrentHealth}/{tank.Value.MaxHealth}";
                    Console.WriteLine($"Tank {tank.Key}: {healthStatus}");
                }
                Console.WriteLine("=======================================");
            }
            else
            {
                // 특정 탱크의 체력 정보 출력
                try
                {
                    int targetId = int.Parse(parts[1]);
                    // 적절한 HostID 찾기
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        string healthStatus = tank.IsDestroyed ? "DESTROYED" : $"{tank.CurrentHealth}/{tank.MaxHealth}";
                        Console.WriteLine($"Tank {targetId} health: {healthStatus}");
                    }
                    else
                    {
                        Console.WriteLine($"Tank with ID {targetId} not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid tank ID format: {ex.Message}");
                }
            }
        }
        
        // 탱크에 데미지 적용
        private void ApplyDamageToTank(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length >= 3)
            {
                try
                {
                    int targetId = int.Parse(parts[1]);
                    float damageAmount = float.Parse(parts[2]);
                    // 적절한 HostID 찾기
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        
                        // 이미 파괴된 탱크라면 처리하지 않음
                        if (tank.IsDestroyed)
                        {
                            Console.WriteLine($"Tank {targetId} is already destroyed");
                            return;
                        }
                        
                        // 체력 감소
                        tank.CurrentHealth = Math.Max(0, tank.CurrentHealth - damageAmount);
                        
                        // 파괴 여부 확인
                        bool wasDestroyed = tank.CurrentHealth <= 0;
                        tank.IsDestroyed = wasDestroyed;
                        
                        // 클라이언트에게 체력 업데이트 전송
                        foreach (var clientID in tanks.Keys)
                        {
                            tankProxy.OnTankHealthUpdated(clientID, RmiContext.ReliableSend,
                                (int)targetHostId, tank.CurrentHealth, tank.MaxHealth);
                            
                            // 파괴된 경우 파괴 이벤트도 전송
                            if (wasDestroyed)
                            {
                                tankProxy.OnTankDestroyed(clientID, RmiContext.ReliableSend,
                                    (int)targetHostId, 0); // 서버에 의한 파괴는 0으로 표시
                            }
                        }
                        
                        string result = wasDestroyed ? "DESTROYED" : $"{tank.CurrentHealth}/{tank.MaxHealth}";
                        Console.WriteLine($"Applied {damageAmount} damage to tank {targetId}. New health: {result}");
                    }
                    else
                    {
                        Console.WriteLine($"Tank with ID {targetId} not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid parameters: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid parameters. Format: damage id amount");
            }
        }
        
        // 탱크 치유
        private void HealTank(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length >= 3)
            {
                try
                {
                    int targetId = int.Parse(parts[1]);
                    float healAmount = float.Parse(parts[2]);
                    // 적절한 HostID 찾기
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        
                        // 파괴된 탱크는 치유할 수 없음
                        if (tank.IsDestroyed)
                        {
                            Console.WriteLine($"Cannot heal tank {targetId}. It is destroyed");
                            return;
                        }
                        
                        // 체력 증가 (최대 체력을 초과하지 않도록)
                        tank.CurrentHealth = Math.Min(tank.MaxHealth, tank.CurrentHealth + healAmount);
                        
                        // 클라이언트에게 체력 업데이트 전송
                        foreach (var clientID in tanks.Keys)
                        {
                            tankProxy.OnTankHealthUpdated(clientID, RmiContext.ReliableSend,
                                (int)targetHostId, tank.CurrentHealth, tank.MaxHealth);
                        }
                        
                        Console.WriteLine($"Healed tank {targetId} for {healAmount}. New health: {tank.CurrentHealth}/{tank.MaxHealth}");
                    }
                    else
                    {
                        Console.WriteLine($"Tank with ID {targetId} not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid parameters: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid parameters. Format: heal id amount");
            }
        }
        
        // 탱크 리스폰
        private void RespawnTank(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length >= 4)
            {
                try
                {
                    int targetId = int.Parse(parts[1]);
                    float posX = float.Parse(parts[2]);
                    float posY = float.Parse(parts[3]);
                    // 적절한 HostID 찾기
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        
                        // 탱크 정보 업데이트
                        tank.PosX = posX;
                        tank.PosY = posY;
                        tank.CurrentHealth = tank.MaxHealth; // 체력 회복
                        tank.IsDestroyed = false; // 파괴 상태 해제
                        
                        // 클라이언트에게 리스폰 정보 전송
                        foreach (var clientID in tanks.Keys)
                        {
                            tankProxy.OnTankSpawned(clientID, RmiContext.ReliableSend,
                                (int)targetHostId, posX, posY, tank.Direction, tank.TankType, tank.MaxHealth);
                        }
                        
                        Console.WriteLine($"Respawned tank {targetId} at position ({posX},{posY}) with full health");
                    }
                    else
                    {
                        Console.WriteLine($"Tank with ID {targetId} not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid parameters: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid parameters. Format: respawn id x y");
            }
        }
        
        // 클라이언트 ID로 HostID 찾기
        private HostID FindHostIDById(int clientId)
        {
            foreach (var tank in tanks)
            {
                if ((int)tank.Key == clientId)
                {
                    return tank.Key;
                }
            }
            return HostID.HostID_None; // 찾지 못한 경우
        }

        // 메인 함수
        static void Main(string[] args)
        {
            TankServer tankServer = new TankServer();
            tankServer.Start();
        }
    }
} 