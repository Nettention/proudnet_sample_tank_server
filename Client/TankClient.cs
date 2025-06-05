using System;
using System.Collections.Generic;
using System.Threading;
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
        public int TankType { get; set; }
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

    // Simple tank game client
    class TankClient
    {
        // Synchronization object (for multi-thread protection)
        private object syncObj = new object();

        // RMI Stub and Proxy instances
        private Tank.Stub tankStub = new Tank.Stub();
        private Tank.Proxy tankProxy = new Tank.Proxy();

        // Network client instance
        private NetClient netClient;

        // Information of other tanks
        private Dictionary<int, TankInfo> otherTanks = new Dictionary<int, TankInfo>();

        // Local tank information
        private TankInfo localTank;

        // Connection status
        private bool isConnected = false;

        // Exit flag
        private bool isRunning = true;

        // Last joined P2P group ID
        private HostID lastJoinedP2PGroupID = HostID.HostID_None;

        // Store other client IDs in P2P group
        private List<HostID> p2pGroupMembers = new List<HostID>();

        // P2P message UI history
        private List<string> messageHistory = new List<string>();
        private const int MAX_MESSAGE_HISTORY = 10;

        private bool isAutoMove = false;
        private Thread autoMoveThread;
        private Random random = new Random();

        // Initialization function
        private void Initialize()
        {
            // Create network client
            netClient = new NetClient();

            // Initialize local tank
            localTank = new TankInfo(0); // Temporary ID, will be updated after server connection

            // Initialize stub
            InitializeStub();

            // Attach proxy and stub to client
            netClient.AttachProxy(tankProxy);
            netClient.AttachStub(tankStub);

            // Server connection complete handler
            netClient.JoinServerCompleteHandler = OnJoinServerComplete;

            // Server disconnection handler
            netClient.LeaveServerHandler = OnLeaveServer;

            // P2P member join handler
            netClient.P2PMemberJoinHandler = OnP2PMemberJoin;

            // P2P member leave handler
            netClient.P2PMemberLeaveHandler = OnP2PMemberLeave;
        }

        // Initialize stub
        private void InitializeStub()
        {
            // Handle position updates
            tankStub.OnTankPositionUpdated = (remote, rmiContext, clientId, posX, posY, direction) =>
            {
                lock (syncObj)
                {
                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // Update own tank information
                        localTank.PosX = posX;
                        localTank.PosY = posY;
                        localTank.Direction = direction;
                    }
                    else
                    {
                        // Update other tank information
                        if (otherTanks.ContainsKey(clientId))
                        {
                            otherTanks[clientId].PosX = posX;
                            otherTanks[clientId].PosY = posY;
                            otherTanks[clientId].Direction = direction;

                            // Output other tank's position change information to console
                            Console.WriteLine($">>> OTHER TANK {clientId} MOVED TO ({posX},{posY}) dir={direction}");
                        }
                    }
                }
                return true;
            };

            // Handle bullet firing
            tankStub.OnSpawnBullet = (remote, rmiContext, clientId, shooterId, posX, posY, direction, launchForce, fireX, fireY, fireZ) =>
            {
                lock (syncObj)
                {
                    // Output bullet firing information
                    Console.WriteLine($"OnSpawnBullet: Tank {clientId} fired from ({posX},{posY}) dir={direction}, force={launchForce}");
                    Console.WriteLine($"Fire position: ({fireX}, {fireY}, {fireZ}), shooter ID={shooterId}");

                    // Implement additional logic here if needed
                }
                return true;
            };

            // Handle player join
            tankStub.OnPlayerJoined = (remote, rmiContext, clientId, posX, posY, tankType) =>
            {
                lock (syncObj)
                {
                    // Store new tank information (if not self)
                    if (clientId != (int)netClient.GetLocalHostID())
                    {
                        TankInfo newTank = new TankInfo(clientId, posX, posY, 0, tankType);
                        otherTanks[clientId] = newTank;

                        Console.WriteLine($"OnPlayerJoined: Tank {clientId} joined at ({posX},{posY}) with tank type {tankType}");
                    }
                    else
                    {
                        // Set own initial position
                        localTank.ClientId = clientId;
                        localTank.PosX = posX;
                        localTank.PosY = posY;
                        localTank.TankType = tankType;

                        Console.WriteLine($"My tank spawned at ({posX},{posY}) with tank type {tankType}");
                    }
                }
                return true;
            };

            // Handle player leave
            tankStub.OnPlayerLeft = (remote, rmiContext, clientId) =>
            {
                lock (syncObj)
                {
                    // Remove left tank information
                    if (otherTanks.ContainsKey(clientId))
                    {
                        otherTanks.Remove(clientId);
                        Console.WriteLine($"OnPlayerLeft: Tank {clientId} left the game");
                    }
                }
                return true;
            };

            // Handle P2P messages
            tankStub.P2PMessage = (remote, rmiContext, message) =>
            {
                lock (syncObj)
                {
                    if (message.StartsWith("P2P_GROUP_INFO:"))
                    {
                        // Receive P2P group information from server
                        string[] parts = message.Split(':');
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                HostID groupID = HostID.HostID_None;
                                lastJoinedP2PGroupID = groupID;
                                Console.WriteLine($"P2P: Received group info, Group ID: {parts[1]}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing P2P group info: {ex.Message}");
                            }
                        }
                    }
                    else if (message.StartsWith("RELAY_FROM_"))
                    {
                        // Handle server relay P2P message
                        int start = message.IndexOf(':');
                        if (start != -1)
                        {
                            string sourceInfo = message.Substring(11, start - 11); // From after "RELAY_FROM_" to before colon
                            string actualMessage = message.Substring(start + 1);

                            string displayMsg = $"P2P Message from Client ID {sourceInfo} (relayed): {actualMessage}";
                            Console.WriteLine(displayMsg);

                            // Add to message history
                            AddToMessageHistory(displayMsg);
                        }
                        else
                        {
                            // Invalid message format
                            Console.WriteLine($"Invalid relayed P2P message format: {message}");
                        }
                    }
                    else
                    {
                        // Direct P2P message received
                        string displayMsg = $"P2P Message from Client ID {remote.ToString()} (direct): {message}";
                        Console.WriteLine(displayMsg);

                        // Add to message history
                        AddToMessageHistory(displayMsg);
                    }
                }
                return true;
            };

            // Handle tank type request
            tankStub.SendTankType = (remote, rmiContext, tankType) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine($"SendTankType from client {remote}: tank type {tankType}");

                    // Additional logic for storing tank type of the client (stored on server)
                    // Client doesn't need to process this information separately
                }
                return true;
            };

            // Handle tank health update
            tankStub.OnTankHealthUpdated = (remote, rmiContext, clientId, currentHealth, maxHealth) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine($"OnTankHealthUpdated: Tank {clientId} health: {currentHealth}/{maxHealth}");

                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // Update own tank health
                        localTank.CurrentHealth = currentHealth;
                        localTank.MaxHealth = maxHealth;
                        Console.WriteLine($"My tank health updated: {currentHealth}/{maxHealth}");
                    }
                    else if (otherTanks.ContainsKey(clientId))
                    {
                        // Update other tank health
                        otherTanks[clientId].CurrentHealth = currentHealth;
                        otherTanks[clientId].MaxHealth = maxHealth;
                    }
                }
                return true;
            };

            // Handle tank destruction event
            tankStub.OnTankDestroyed = (remote, rmiContext, clientId, destroyedById) =>
            {
                lock (syncObj)
                {
                    string destroyedByText = destroyedById > 0 ? $"by tank {destroyedById}" : "by environment";
                    Console.WriteLine($"OnTankDestroyed: Tank {clientId} was destroyed {destroyedByText}");

                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // Own tank was destroyed
                        localTank.IsDestroyed = true;
                        localTank.CurrentHealth = 0;
                        Console.WriteLine($"MY TANK WAS DESTROYED {destroyedByText}!");
                    }
                    else if (otherTanks.ContainsKey(clientId))
                    {
                        // Other tank was destroyed
                        otherTanks[clientId].IsDestroyed = true;
                        otherTanks[clientId].CurrentHealth = 0;
                    }
                }
                return true;
            };

            // Handle tank creation/respawn event
            tankStub.OnTankSpawned = (remote, rmiContext, clientId, posX, posY, direction, tankType, initialHealth) =>
            {
                lock (syncObj)
                {
                    Console.WriteLine($"OnTankSpawned: Tank {clientId} spawned at ({posX},{posY}) with type {tankType} and health {initialHealth}");

                    if (clientId == (int)netClient.GetLocalHostID())
                    {
                        // Own tank creation/respawn
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
                        // Other tank creation/respawn
                        if (otherTanks.ContainsKey(clientId))
                        {
                            // Update existing tank information
                            otherTanks[clientId].PosX = posX;
                            otherTanks[clientId].PosY = posY;
                            otherTanks[clientId].Direction = direction;
                            otherTanks[clientId].TankType = tankType;
                            otherTanks[clientId].CurrentHealth = initialHealth;
                            otherTanks[clientId].IsDestroyed = false;
                        }
                        else
                        {
                            // Create new tank information
                            TankInfo newTank = new TankInfo(clientId, posX, posY, direction, tankType, initialHealth);
                            otherTanks[clientId] = newTank;
                        }
                    }
                }
                return true;
            };
        }

        // Handle server connection complete
        private void OnJoinServerComplete(ErrorInfo info, ByteArray replyFromServer)
        {
            lock (syncObj)
            {
                if (info.errorType == ErrorType.Ok)
                {
                    // Connection successful
                    isConnected = true;

                    // Update own ID
                    localTank.ClientId = (int)netClient.GetLocalHostID();

                    Console.WriteLine($"Connected to server. My ID: {localTank.ClientId}");
                }
                else
                {
                    // Connection failed
                    Console.WriteLine($"Failed to connect to server: {info.comment}");
                    isRunning = false;
                }
            }
        }

        // Handle server disconnection
        private void OnLeaveServer(ErrorInfo errorInfo)
        {
            lock (syncObj)
            {
                Console.WriteLine($"Disconnected from server: {errorInfo.comment}");
                isConnected = false;
                isRunning = false;
            }
        }

        // Handle P2P member join
        private void OnP2PMemberJoin(HostID memberHostID, HostID groupHostID, int memberCount, ByteArray customField)
        {
            lock (syncObj)
            {
                Console.WriteLine($"P2P: Member {memberHostID} joined group {groupHostID}");
                lastJoinedP2PGroupID = groupHostID;

                // Add to member list
                if (!p2pGroupMembers.Contains(memberHostID) && memberHostID != netClient.GetLocalHostID())
                {
                    p2pGroupMembers.Add(memberHostID);
                    Console.WriteLine($"P2P: Added member {memberHostID} to group member list");
                }
            }
        }

        // Handle P2P member leave
        private void OnP2PMemberLeave(HostID memberHostID, HostID groupHostID, int memberCount)
        {
            lock (syncObj)
            {
                Console.WriteLine($"P2P: Member {memberHostID} left group {groupHostID}");

                // Remove from member list
                if (p2pGroupMembers.Contains(memberHostID))
                {
                    p2pGroupMembers.Remove(memberHostID);
                    Console.WriteLine($"P2P: Removed member {memberHostID} from group member list");
                }

                // If no one is in the group, reset group ID
                if (memberCount <= 1)
                {
                    lastJoinedP2PGroupID = HostID.HostID_None;
                    p2pGroupMembers.Clear();
                    Console.WriteLine("P2P: Group is now empty or has only this client");
                }
            }
        }

        // Connect to server
        private void ConnectToServer()
        {
            // Set connection parameters
            NetConnectionParam param = new NetConnectionParam();
            param.protocolVersion.Set(Vars.m_Version);
            param.serverIP = Vars.ServerIP;
            param.serverPort = (ushort)Vars.ServerPort;

            // Attempt server connection
            netClient.Connect(param);
            Console.WriteLine("Connecting to server...");
        }

        // Request tank movement
        private void RequestMove(float posX, float posY, float direction)
        {
            if (isConnected)
            {



                // Send movement request to server
                tankProxy.SendMove(HostID.HostID_Server, RmiContext.ReliableSend,
                    posX, posY, direction);




                // Update local information (may be updated again by server)
                localTank.PosX = posX;
                localTank.PosY = posY;
                localTank.Direction = direction;
            }
        }

        // Request fire
        private void RequestFire(float direction, float launchForce = 25.0f)
        {
            if (isConnected)
            {
                // Set fire position and power (in actual game, these values are calculated from current tank state)
                float fireX = localTank.PosX;
                float fireY = 1.0f; // Slightly above ground
                float fireZ = localTank.PosY; // Note: In 2D game, Z-axis may be mapped to client Y-axis

                // Send fire request to server (including shooter ID, direction, launch force, fire position)
                tankProxy.SendFire(HostID.HostID_Server, RmiContext.ReliableSend,
                    localTank.ClientId, direction, launchForce, fireX, fireY, fireZ);

                Console.WriteLine($"Fire request sent: direction={direction}, force={launchForce}, position=({fireX}, {fireY}, {fireZ})");
            }
        }

        // Send health update request
        private void SendHealthUpdate(float currentHealth, float maxHealth)
        {
            if (isConnected)
            {
                // Send health update request to server
                tankProxy.SendTankHealthUpdated(HostID.HostID_Server, RmiContext.ReliableSend,
                    currentHealth, maxHealth);

                // Update local information
                localTank.CurrentHealth = currentHealth;
                localTank.MaxHealth = maxHealth;

                Console.WriteLine($"Health update sent: {currentHealth}/{maxHealth}");
            }
        }

        // Send tank destruction event
        private void SendTankDestroyed(int destroyedById)
        {
            if (isConnected)
            {
                // Send tank destruction event to server
                tankProxy.SendTankDestroyed(HostID.HostID_Server, RmiContext.ReliableSend,
                    destroyedById);

                // Update local state
                localTank.IsDestroyed = true;
                localTank.CurrentHealth = 0;

                string destroyedByText = destroyedById > 0 ? $"by tank {destroyedById}" : "by environment";
                Console.WriteLine($"Tank destroyed event sent: destroyed {destroyedByText}");
            }
        }

        // Send tank creation/respawn message
        private void SendTankSpawn(float posX, float posY, float direction, int tankType, float initialHealth)
        {
            if (isConnected)
            {
                // Send tank creation/respawn message to server
                tankProxy.SendTankSpawned(HostID.HostID_Server, RmiContext.ReliableSend,
                    posX, posY, direction, tankType, initialHealth);

                // Update local state
                localTank.PosX = posX;
                localTank.PosY = posY;
                localTank.Direction = direction;
                localTank.TankType = tankType;
                localTank.CurrentHealth = initialHealth;
                localTank.IsDestroyed = false;

                Console.WriteLine($"Tank spawn message sent: position=({posX},{posY}), type={tankType}, health={initialHealth}");
            }
        }

        // Send P2P message
        private void SendP2PMessage(string message)
        {
            if (isConnected)
            {
                if (lastJoinedP2PGroupID != HostID.HostID_None)
                {
                    // If P2P group exists, send message directly to group
                    RmiContext context = RmiContext.ReliableSend;
                    context.enableLoopback = false; // Don't send to self

                    tankProxy.P2PMessage(lastJoinedP2PGroupID, context, message);
                    Console.WriteLine($"P2P message sent directly to group {lastJoinedP2PGroupID}: {message}");

                    // Add own message to message history
                    AddToMessageHistory($"Me: {message}");
                }
                else
                {
                    // If no P2P group, send message through server
                    tankProxy.P2PMessage(HostID.HostID_Server, RmiContext.ReliableSend, message);
                    Console.WriteLine($"P2P message sent via server: {message}");

                    // Add own message to message history
                    AddToMessageHistory($"Me (via server): {message}");
                }
            }
            else
            {
                Console.WriteLine("Cannot send P2P message: Not connected to server");
            }
        }

        // Add to message history
        private void AddToMessageHistory(string message)
        {
            messageHistory.Add(message);

            // Remove oldest message if exceeding maximum count
            while (messageHistory.Count > MAX_MESSAGE_HISTORY)
            {
                messageHistory.RemoveAt(0);
            }
        }

        // Show message history
        private void ShowMessageHistory()
        {
            Console.WriteLine("===== Message History =====");
            if (messageHistory.Count == 0)
            {
                Console.WriteLine("No messages yet");
            }
            else
            {
                foreach (var msg in messageHistory)
                {
                    Console.WriteLine(msg);
                }
            }
            Console.WriteLine("==========================");
        }

        // Show P2P status information
        private void ShowP2PStatus()
        {
            lock (syncObj)
            {
                Console.WriteLine("===== P2P Status =====");
                Console.WriteLine($"Connected to server: {isConnected}");
                Console.WriteLine($"P2P Group ID: {(lastJoinedP2PGroupID != HostID.HostID_None ? lastJoinedP2PGroupID.ToString() : "None")}");
                Console.WriteLine($"P2P Group Members: {p2pGroupMembers.Count}");

                if (p2pGroupMembers.Count > 0)
                {
                    Console.WriteLine("Members:");
                    foreach (var member in p2pGroupMembers)
                    {
                        Console.WriteLine($"  - {member}");
                    }
                }

                Console.WriteLine("=====================");
            }
        }

        // Run game loop
        public void Run()
        {
            // Initialize
            Initialize();

            // Connect to server
            ConnectToServer();

            // Thread for network frame processing
            Thread networkThread = new Thread(() =>
            {
                while (isRunning)
                {
                    // Process network periodically
                    netClient.FrameMove();
                    Thread.Sleep(10); // Control CPU usage
                }
            });

            networkThread.IsBackground = true;
            networkThread.Start();

            // Command instructions
            Console.WriteLine("Commands:");
            Console.WriteLine("move x y dir : Move tank to position (x,y) with direction dir");
            Console.WriteLine("fire dir [force] : Fire in direction dir with optional force (default 25.0)");
            Console.WriteLine("tank n       : Select tank type (0-3)");
            Console.WriteLine("health h [max]: Update tank health to h with optional max health");
            Console.WriteLine("destroy [id] : Send tank destroyed event (id of destroyer, default 0 = environment)");
            Console.WriteLine("spawn x y dir type health: Spawn/respawn tank");
            Console.WriteLine("msg text     : Send P2P message to other clients");
            Console.WriteLine("history      : Show message history");
            Console.WriteLine("status       : Show P2P connection status");
            Console.WriteLine("auto         : Toggle auto movement");
            Console.WriteLine("q            : Quit");

            // Main loop
            while (isRunning)
            {
                lock (syncObj)
                {
                    if (isConnected)
                    {
                        // Display current status
                        string healthStatus = localTank.IsDestroyed ? "DESTROYED" : $"{localTank.CurrentHealth}/{localTank.MaxHealth}";
                        Console.WriteLine($"Status: Tank ID={localTank.ClientId}, Position=({localTank.PosX},{localTank.PosY}), Type={localTank.TankType}, Health={healthStatus}");
                    }
                }

                // Handle user input
                string input = Console.ReadLine();

                lock (syncObj)
                {
                    if (!isConnected)
                        continue;

                    // Process commands
                    if (input.StartsWith("move "))
                    {
                        // Handle move command
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
                        // Handle fire command
                        string[] parts = input.Split(' ');
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                float dir = float.Parse(parts[1]);
                                float force = 25.0f; // Default value

                                // Use additional parameter as launch force if available
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
                        // Handle tank type selection
                        string[] parts = input.Split(' ');
                        if (parts.Length == 2)
                        {
                            try
                            {
                                int tankType = int.Parse(parts[1]);
                                if (tankType >= 0 && tankType <= 3)
                                {
                                    // Send tank type request to server
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
                        // Handle health update command
                        string[] parts = input.Split(' ');
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                float health = float.Parse(parts[1]);
                                float maxHealth = localTank.MaxHealth; // Default: current max health

                                // Use max health parameter if available
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
                        // Handle tank destruction command
                        string[] parts = input.Split(' ');
                        int destroyerId = 0; // Default: destroyed by environment

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
                        // Handle tank creation/respawn command
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
                        // Handle P2P message sending
                        string message = input.Substring(4);

                        SendP2PMessage(message);
                    }
                    else if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
                    {
                        // Show message history
                        ShowMessageHistory();
                    }
                    else if (input.Equals("status", StringComparison.OrdinalIgnoreCase))
                    {
                        // Show P2P status information
                        ShowP2PStatus();
                    }
                    else if (input.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        // Toggle auto movement
                        ToggleAutoMove();
                    }
                    else if (input.Equals("q"))
                    {
                        // Handle exit
                        isRunning = false;

                        // Disconnect from server
                        netClient.Disconnect();
                    }
                    else
                    {
                        Console.WriteLine("Unknown command");
                    }
                }
            }

            // Resource cleanup
            if (autoMoveThread != null && autoMoveThread.IsAlive)
            {
                isAutoMove = false;
                autoMoveThread.Join(1000);
            }

            // Close connection (if still connected)
            if (isConnected)
            {
                netClient.Disconnect();
            }
            Console.WriteLine("Client stopped");
        }

        // Toggle auto movement
        private void ToggleAutoMove()
        {
            lock (syncObj)
            {
                isAutoMove = !isAutoMove;

                if (isAutoMove)
                {
                    Console.WriteLine("Auto movement enabled");

                    // Start auto movement thread
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

        // Auto movement thread
        private void AutoMoveThread()
        {
            while (isAutoMove && isConnected && isRunning)
            {
                try
                {
                    // Generate random direction and slight movement
                    lock (syncObj)
                    {
                        float x = localTank.PosX + (float)(random.NextDouble() * 20 - 10);
                        float y = localTank.PosY + (float)(random.NextDouble() * 20 - 10);
                        float dir = (float)(random.NextDouble() * 360);

                        // Limit to valid range
                        x = Math.Max(0, Math.Min(100, x));
                        y = Math.Max(0, Math.Min(100, y));

                        // Request movement
                        RequestMove(x, y, dir);

                        Console.WriteLine($"Auto move to ({x},{y}) dir={dir}");
                    }

                    // Wait 3-5 seconds
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

        // Main function
        static void Main(string[] args)
        {
            TankClient client = new TankClient();
            client.Run();
        }
    }
}