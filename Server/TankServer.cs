using System;
using System.Collections.Generic;
using Nettention.Proud;

namespace TankGame
{
    // Simple class to store tank information
    public class TankInfo
    {
        public int ClientId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Direction { get; set; }
        public int TankType { get; set; } // Added: Tank type information
        public float CurrentHealth { get; set; } // Added: Current health
        public float MaxHealth { get; set; } // Added: Maximum health
        public bool IsDestroyed { get; set; } // Added: Destruction status

        public TankInfo(int clientId, float posX = 0, float posY = 0, float direction = 0, int tankType = 0, float maxHealth = 100f)
        {
            ClientId = clientId;
            PosX = posX;
            PosY = posY;
            Direction = direction;
            TankType = tankType;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth; // Initial health is set to maximum health
            IsDestroyed = false;
        }
    }

    class TankServer
    {
        // RMI Stub and Proxy instances
        private Tank.Stub tankStub = new Tank.Stub();
        private Tank.Proxy tankProxy = new Tank.Proxy();

        // P2P group ID
        private HostID gameP2PGroupID = HostID.HostID_None;

        // Information of connected tanks
        private Dictionary<HostID, TankInfo> tanks = new Dictionary<HostID, TankInfo>();

        // Network server instance
        private NetServer server;

        // Initialization function
        private void Initialize()
        {
            // Create server object
            server = new NetServer();

            // Implement stub functions
            InitializeStub();

            // Attach stub and proxy to server
            server.AttachStub(tankStub);
            server.AttachProxy(tankProxy);

            // Client connection event handler
            server.ClientJoinHandler = OnClientJoin;

            // Client disconnection event handler
            server.ClientLeaveHandler = OnClientLeave;

            // Server start settings
            var serverParam = new StartServerParameter();
            serverParam.protocolVersion = new Nettention.Proud.Guid(Vars.m_Version);
            serverParam.tcpPorts.Add(Vars.ServerPort);
        }

        // Initialize stub functions
        private void InitializeStub()
        {
            // Handle position movement request
            tankStub.SendMove = (remote, rmiContext, posX, posY, direction) =>
            {
                Console.WriteLine($"SendMove from client {remote}: pos=({posX},{posY}), direction={direction}");
                
                // Update tank information
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].PosX = posX;
                    tanks[remote].PosY = posY;
                    tanks[remote].Direction = direction;

                    // Send updated position to all clients
                    // Send message to each client individually
                    foreach (var clientID in tanks.Keys)
                    {
                        // Don't send to the client who moved
                        if (clientID != remote)
                        {
                            tankProxy.OnTankPositionUpdated(clientID, RmiContext.ReliableSend, 
                                (int)remote, posX, posY, direction);
                        }
                    }
                }
                return true;
            };

            // Handle fire request
            tankStub.SendFire = (remote, rmiContext, shooterId, direction, launchForce, fireX, fireY, fireZ) =>
            {
                Console.WriteLine($"========== SendFire Received ==========");
                Console.WriteLine($"From client {remote}: shooterId={shooterId}, direction={direction}, launchForce={launchForce}");
                Console.WriteLine($"Fire position: ({fireX}, {fireY}, {fireZ})");
                Console.WriteLine($"RmiContext: {rmiContext}");
                
                // Get tank information of the client
                if (tanks.ContainsKey(remote))
                {
                    var tank = tanks[remote];
                    Console.WriteLine($"Tank found: Position=({tank.PosX},{tank.PosY}), Direction={tank.Direction}");
                    
                    // Send bullet fire information to all clients
                    // Send message to each client individually
                    int recipientCount = 0;
                    foreach (var clientID in tanks.Keys)
                    {
                        // Don't send to the client who fired
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
                Console.WriteLine($"========== SendFire Processing Complete ==========");
                return true;
            };

            // Handle tank type request
            tankStub.SendTankType = (remote, rmiContext, tankType) =>
            {
                Console.WriteLine($"========== SendTankType Received ==========");
                Console.WriteLine($"From client {remote}: tankType={tankType}");
                
                // Update tank information of the client
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].TankType = tankType;
                    Console.WriteLine($"Tank type updated for client {remote}: Type={tankType}");
                    
                    // Notify all other clients about this client's tank type
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // Send tank type information through OnPlayerJoined message
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
                Console.WriteLine($"========== SendTankType Processing Complete ==========");
                return true;
            };

            // P2P message (implement if needed)
            tankStub.P2PMessage = (remote, rmiContext, message) =>
            {
                Console.WriteLine($"P2PMessage from client {remote}: {message}");
                
                // If P2P group exists, relay to all members except the sender
                if (gameP2PGroupID != HostID.HostID_None && !message.StartsWith("P2P_GROUP_INFO:"))
                {
                    // Relay to all clients except the message sender
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            string relayedMessage = $"RELAY_FROM_{remote}:{message}";
                            tankProxy.P2PMessage(clientID, RmiContext.ReliableSend, relayedMessage);
                            Console.WriteLine($"Relayed P2P message to client {clientID}: {relayedMessage}");
                        }
                    }
                }
                
                return true;
            };
            
            // Handle health update (from client to server)
            tankStub.SendTankHealthUpdated = (remote, rmiContext, currentHealth, maxHealth) =>
            {
                Console.WriteLine($"========== SendTankHealthUpdated Received ==========");
                Console.WriteLine($"From client {remote}: currentHealth={currentHealth}, maxHealth={maxHealth}");
                
                // Update tank information of the client
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].CurrentHealth = currentHealth;
                    tanks[remote].MaxHealth = maxHealth;
                    tanks[remote].IsDestroyed = (currentHealth <= 0);
                    
                    Console.WriteLine($"Tank health updated for client {remote}: {currentHealth}/{maxHealth}");
                    
                    // Send this client's health information to all other clients
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // Send health information through UpdateTankHealth message
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
                Console.WriteLine($"========== SendTankHealthUpdated Processing Complete ==========");
                return true;
            };
            
            // Handle tank destruction event (from client to server)
            tankStub.SendTankDestroyed = (remote, rmiContext, destroyedById) =>
            {
                Console.WriteLine($"========== SendTankDestroyed Received ==========");
                Console.WriteLine($"From client {remote}: destroyedById={destroyedById}");
                
                // Update tank information of the client
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].IsDestroyed = true;
                    tanks[remote].CurrentHealth = 0;
                    
                    string destroyedByText = destroyedById > 0 ? $"by tank {destroyedById}" : "by environment";
                    Console.WriteLine($"Tank destroyed for client {remote}: {destroyedByText}");
                    
                    // Send this client's destruction information to all other clients
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // Send destruction information through OnTankDestroyed message
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
                Console.WriteLine($"========== SendTankDestroyed Processing Complete ==========");
                return true;
            };
            
            // Handle tank creation/respawn message (from client to server)
            tankStub.SendTankSpawned = (remote, rmiContext, posX, posY, direction, tankType, initialHealth) =>
            {
                Console.WriteLine($"========== SendTankSpawned Received ==========");
                Console.WriteLine($"From client {remote}: position=({posX},{posY}), direction={direction}, tankType={tankType}, health={initialHealth}");
                
                // Update tank information of the client
                if (tanks.ContainsKey(remote))
                {
                    tanks[remote].PosX = posX;
                    tanks[remote].PosY = posY;
                    tanks[remote].Direction = direction;
                    tanks[remote].TankType = tankType;
                    tanks[remote].CurrentHealth = initialHealth;
                    tanks[remote].MaxHealth = initialHealth; // Update maximum health as well
                    tanks[remote].IsDestroyed = false;
                    
                    Console.WriteLine($"Tank spawned for client {remote} at ({posX},{posY})");
                    
                    // Send this client's creation/respawn information to all other clients
                    foreach (var clientID in tanks.Keys)
                    {
                        if (clientID != remote)
                        {
                            // Send creation/respawn information through OnTankSpawned message
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
                Console.WriteLine($"========== SendTankSpawned Processing Complete ==========");
                return true;
            };
        }

        // Handle client connection
        private void OnClientJoin(NetClientInfo clientInfo)
        {
            Console.WriteLine($"Client {clientInfo.hostID} connected");
            
            // Generate random position (e.g., 0~100 range)
            Random random = new Random();
            float posX = (float)(random.NextDouble() * 100);
            float posY = (float)(random.NextDouble() * 100);
            
            // Set initial tank type to -1 (not selected)
            int defaultTankType = -1;
            
            // Set initial health
            float defaultMaxHealth = 100.0f;
            float defaultHealth = defaultMaxHealth;
            
            // Store tank information
            TankInfo newTank = new TankInfo((int)clientInfo.hostID, posX, posY, 0, defaultTankType, defaultMaxHealth);
            tanks.Add(clientInfo.hostID, newTank);
            
            Console.WriteLine($"New tank created for client {clientInfo.hostID} with tank type {defaultTankType} and health {defaultHealth}/{defaultMaxHealth}");
            
            // Send existing tank information to new client
            foreach (var tank in tanks)
            {
                if (tank.Key != clientInfo.hostID)  // Exclude self
                {
                    // Send existing player information
                    tankProxy.OnPlayerJoined(clientInfo.hostID, RmiContext.ReliableSend,
                        (int)tank.Key, tank.Value.PosX, tank.Value.PosY, tank.Value.TankType);
                    
                    // Send existing player health information
                    tankProxy.OnTankHealthUpdated(clientInfo.hostID, RmiContext.ReliableSend,
                        (int)tank.Key, tank.Value.CurrentHealth, tank.Value.MaxHealth);
                    
                    // If existing player is destroyed, send destruction information as well
                    if (tank.Value.IsDestroyed)
                    {
                        tankProxy.OnTankDestroyed(clientInfo.hostID, RmiContext.ReliableSend,
                            (int)tank.Key, 0); // Set destroyer ID to 0 (environment) since no destroyer info available
                    }
                    
                    Console.WriteLine($"Sending existing player info to new client: ID={tank.Key}, Type={tank.Value.TankType}, Health={tank.Value.CurrentHealth}/{tank.Value.MaxHealth}");
                }
            }
            
            // Notify all clients about new player join
            foreach (var existingClientID in tanks.Keys)
            {
                // Don't send to the newly joined client
                if (existingClientID != clientInfo.hostID)
                {
                    // Send new player information
                    tankProxy.OnPlayerJoined(existingClientID, RmiContext.ReliableSend,
                        (int)clientInfo.hostID, posX, posY, defaultTankType);
                    
                    // Send new player health information
                    tankProxy.OnTankHealthUpdated(existingClientID, RmiContext.ReliableSend,
                        (int)clientInfo.hostID, defaultHealth, defaultMaxHealth);
                    
                    Console.WriteLine($"Notifying existing client {existingClientID} about new player: ID={clientInfo.hostID}, Type={defaultTankType}, Health={defaultHealth}/{defaultMaxHealth}");
                }
            }
            
            // Update P2P group (if needed)
            UpdateP2PGroup();
        }

        // Handle client disconnection
        private void OnClientLeave(NetClientInfo clientInfo, ErrorInfo errorInfo, ByteArray comment)
        {
            Console.WriteLine($"Client {clientInfo.hostID} disconnected: {errorInfo.comment}");
            
            // Remove tank information
            if (tanks.ContainsKey(clientInfo.hostID))
            {
                tanks.Remove(clientInfo.hostID);
            }
            
            // Notify all clients about player leave
            // Send message to each client individually
            foreach (var remainingClientID in tanks.Keys)
            {
                tankProxy.OnPlayerLeft(remainingClientID, RmiContext.ReliableSend, (int)clientInfo.hostID);
            }
            
            // Update P2P group (if needed)
            UpdateP2PGroup();
        }

        // Update P2P group (use if needed)
        private void UpdateP2PGroup()
        {
            // Remove existing group
            if (gameP2PGroupID != HostID.HostID_None)
            {
                server.DestroyP2PGroup(gameP2PGroupID);
                gameP2PGroupID = HostID.HostID_None;
            }
            
            // Create new group (when 2 or more clients)
            if (tanks.Count >= 2)
            {
                HostID[] clients = new HostID[tanks.Count];
                int index = 0;
                foreach (var tank in tanks)
                {
                    clients[index++] = tank.Key;
                }
                
                gameP2PGroupID = server.CreateP2PGroup(clients, new ByteArray());
                Console.WriteLine($"P2P group created with {tanks.Count} members, Group ID: {gameP2PGroupID}");
                
                // Notify all clients about P2P group ID
                foreach (var clientID in tanks.Keys)
                {
                    // Send group ID information through P2PMessage
                    string groupInfoMsg = $"P2P_GROUP_INFO:{gameP2PGroupID}";
                    tankProxy.P2PMessage(clientID, RmiContext.ReliableSend, groupInfoMsg);
                    Console.WriteLine($"Sent P2P group info to client {clientID}: {groupInfoMsg}");
                }
            }
            else
            {
                Console.WriteLine("Not enough clients to create P2P group (need at least 2)");
                gameP2PGroupID = HostID.HostID_None;
            }
        }

        // Start server
        public void Start()
        {
            Initialize();
            
            try
            {
                // Set server start parameters
                var serverParam = new StartServerParameter();
                serverParam.protocolVersion = new Nettention.Proud.Guid(Vars.m_Version);
                serverParam.tcpPorts.Add(Vars.ServerPort);
                
                // Start server
                server.Start(serverParam);
                Console.WriteLine($"Tank Server started on port {Vars.ServerPort}");
                
                // Process commands
                ProcessCommands();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server start failed: {ex.Message}");
            }
        }

        // Command processing loop
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
            
            // Stop server
            server.Stop();
            Console.WriteLine("Server stopped");
        }
        
        // Print connected client information
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
        
        // Show tank health information
        private void ShowTankHealth(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
            {
                // Show health information of all tanks
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
                // Show health information of specific tank
                try
                {
                    int targetId = int.Parse(parts[1]);
                    // Find appropriate HostID
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
        
        // Apply damage to tank
        private void ApplyDamageToTank(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length >= 3)
            {
                try
                {
                    int targetId = int.Parse(parts[1]);
                    float damageAmount = float.Parse(parts[2]);
                    // Find appropriate HostID
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        
                        // Don't process if tank is already destroyed
                        if (tank.IsDestroyed)
                        {
                            Console.WriteLine($"Tank {targetId} is already destroyed");
                            return;
                        }
                        
                        // Decrease health
                        tank.CurrentHealth = Math.Max(0, tank.CurrentHealth - damageAmount);
                        
                        // Check if destroyed
                        bool wasDestroyed = tank.CurrentHealth <= 0;
                        tank.IsDestroyed = wasDestroyed;
                        
                        // Send health update to clients
                        foreach (var clientID in tanks.Keys)
                        {
                            tankProxy.OnTankHealthUpdated(clientID, RmiContext.ReliableSend,
                                (int)targetHostId, tank.CurrentHealth, tank.MaxHealth);
                            
                            // Send destruction event if destroyed
                            if (wasDestroyed)
                            {
                                tankProxy.OnTankDestroyed(clientID, RmiContext.ReliableSend,
                                    (int)targetHostId, 0); // Server-caused destruction is marked as 0
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
        
        // Heal tank
        private void HealTank(string input)
        {
            string[] parts = input.Split(' ');
            
            if (parts.Length >= 3)
            {
                try
                {
                    int targetId = int.Parse(parts[1]);
                    float healAmount = float.Parse(parts[2]);
                    // Find appropriate HostID
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        
                        // Cannot heal destroyed tank
                        if (tank.IsDestroyed)
                        {
                            Console.WriteLine($"Cannot heal tank {targetId}. It is destroyed");
                            return;
                        }
                        
                        // Increase health (not exceeding maximum health)
                        tank.CurrentHealth = Math.Min(tank.MaxHealth, tank.CurrentHealth + healAmount);
                        
                        // Send health update to clients
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
        
        // Respawn tank
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
                    // Find appropriate HostID
                    HostID targetHostId = FindHostIDById(targetId);
                    
                    if (targetHostId != HostID.HostID_None && tanks.ContainsKey(targetHostId))
                    {
                        var tank = tanks[targetHostId];
                        
                        // Update tank information
                        tank.PosX = posX;
                        tank.PosY = posY;
                        tank.CurrentHealth = tank.MaxHealth; // Restore health
                        tank.IsDestroyed = false; // Remove destruction status
                        
                        // Send respawn information to clients
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
        
        // Find HostID by client ID
        private HostID FindHostIDById(int clientId)
        {
            foreach (var tank in tanks)
            {
                if ((int)tank.Key == clientId)
                {
                    return tank.Key;
                }
            }
            return HostID.HostID_None; // Return if not found
        }

        // Main function
        static void Main(string[] args)
        {
            TankServer tankServer = new TankServer();
            tankServer.Start();
        }
    }
} 