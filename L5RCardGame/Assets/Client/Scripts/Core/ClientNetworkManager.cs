using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace L5RGame.Client
{
    /// <summary>
    /// Client-side network manager implementing dual-channel networking:
    /// - Mirror Networking for real-time game traffic
    /// - SignalR for meta services (matchmaking, chat, etc.)
    /// Optimized for mobile Unity card game clients.
    /// </summary>
    public class ClientNetworkManager : MonoBehaviour
    {
        [Header("Network Configuration")]
        [SerializeField] private ClientNetworkConfig config;
        [SerializeField] private string gameServerUrl = "ws://localhost:7777";
        [SerializeField] private string metaServerUrl = "wss://api.l5rgame.com/gamehub";
        [SerializeField] private bool enableDebugLogging = true;
        
        [Header("Mobile Optimization")]
        [SerializeField] private bool adaptiveQuality = true;
        [SerializeField] private float batteryOptimizationFactor = 0.8f;
        [SerializeField] private int maxReconnectAttempts = 5;
        [SerializeField] private float reconnectDelay = 2f;
        
        // Core Components
        private Mirror.NetworkManager mirrorManager;
        private HubConnection metaConnection;
        private ClientMessageHandler messageHandler;
        private ClientBandwidthManager bandwidthManager;
        private ClientConnectionManager connectionManager;
        private GameStateManager gameStateManager;
        
        // Authentication
        private string currentGameToken;
        private string currentMetaToken;
        private string playerId;
        
        // Network State
        private ClientNetworkState currentState = ClientNetworkState.Disconnected;
        private float lastNetworkUpdate;
        private int reconnectAttempts;
        private bool isApplicationPaused;
        
        // Message Queues
        private ConcurrentQueue<GameActionMessage> outgoingGameMessages = new ConcurrentQueue<GameActionMessage>();
        private ConcurrentQueue<MetaActionMessage> outgoingMetaMessages = new ConcurrentQueue<MetaActionMessage>();
        
        // Events
        public event Action<ClientNetworkState> OnNetworkStateChanged;
        public event Action<GameStateUpdate> OnGameStateReceived;
        public event Action<MatchFoundMessage> OnMatchFound;
        public event Action<PlayerJoinedMessage> OnPlayerJoined;
        public event Action<PlayerLeftMessage> OnPlayerLeft;
        public event Action<ChatMessage> OnChatMessageReceived;
        public event Action<LeaderboardUpdate> OnLeaderboardUpdated;
        public event Action<FriendStatusUpdate> OnFriendStatusChanged;
        public event Action<NetworkError> OnNetworkError;
        
        #region Unity Lifecycle
        
        void Awake()
        {
            InitializeClientNetworkManager();
        }
        
        void Start()
        {
            StartClientSystems();
        }
        
        void Update()
        {
            UpdateClientSystems();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            HandleApplicationPause(pauseStatus);
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            HandleApplicationFocus(hasFocus);
        }
        
        void OnDestroy()
        {
            Cleanup();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeClientNetworkManager()
        {
            // Initialize configuration
            if (config == null)
                config = CreateDefaultConfig();
            
            // Initialize components
            messageHandler = new ClientMessageHandler();
            bandwidthManager = new ClientBandwidthManager(adaptiveQuality);
            connectionManager = new ClientConnectionManager(maxReconnectAttempts, reconnectDelay);
            gameStateManager = new GameStateManager();
            
            // Setup Mirror networking
            SetupMirrorClient();
            
            // Setup SignalR
            SetupSignalRClient();
            
            LogClient("Client NetworkManager initialized");
        }
        
        private void SetupMirrorClient()
        {
            // Find or create Mirror NetworkManager
            mirrorManager = FindObjectOfType<Mirror.NetworkManager>();
            if (mirrorManager == null)
            {
                var go = new GameObject("Mirror NetworkManager");
                mirrorManager = go.AddComponent<Mirror.NetworkManager>();
                DontDestroyOnLoad(go);
            }
            
            // Configure for client-only operation
            mirrorManager.networkAddress = ExtractAddress(gameServerUrl);
            
            // Setup event handlers
            SetupMirrorEventHandlers();
        }
        
        private void SetupSignalRClient()
        {
            metaConnection = new HubConnectionBuilder()
                .WithUrl(metaServerUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
                .Build();
            
            // Setup event handlers
            SetupSignalREventHandlers();
        }
        
        private void SetupMirrorEventHandlers()
        {
            // Add client event handlers here
            NetworkClient.OnConnectedEvent += OnGameNetworkConnected;
            NetworkClient.OnDisconnectedEvent += OnGameNetworkDisconnected;
            NetworkClient.OnErrorEvent += OnGameNetworkError;
        }
        
        private void SetupSignalREventHandlers()
        {
            metaConnection.Closed += OnMetaNetworkDisconnected;
            metaConnection.Reconnected += OnMetaNetworkReconnected;
            
            // Register message handlers
            RegisterMetaMessageHandlers();
        }
        
        private void RegisterMetaMessageHandlers()
        {
            metaConnection.On<MatchFoundMessage>("MatchFound", (message) => OnMatchFound?.Invoke(message));
            metaConnection.On<PlayerJoinedMessage>("PlayerJoined", (message) => OnPlayerJoined?.Invoke(message));
            metaConnection.On<PlayerLeftMessage>("PlayerLeft", (message) => OnPlayerLeft?.Invoke(message));
            metaConnection.On<ChatMessage>("ChatMessage", (message) => OnChatMessageReceived?.Invoke(message));
            metaConnection.On<LeaderboardUpdate>("LeaderboardUpdate", (message) => OnLeaderboardUpdated?.Invoke(message));
            metaConnection.On<FriendStatusUpdate>("FriendStatusUpdate", (message) => OnFriendStatusChanged?.Invoke(message));
        }
        
        #endregion
        
        #region Connection Management
        
        /// <summary>
        /// Authenticate and connect to both networks
        /// </summary>
        public async Task<bool> ConnectAsync(string gameToken, string metaToken, string playerIdParam)
        {
            try
            {
                SetNetworkState(ClientNetworkState.Connecting);
                
                // Store authentication data
                currentGameToken = gameToken;
                currentMetaToken = metaToken;
                playerId = playerIdParam;
                
                // Connect to meta network first (for lobby/matchmaking)
                var metaConnected = await ConnectToMetaNetworkAsync();
                if (!metaConnected)
                {
                    LogError("Failed to connect to meta network");
                    SetNetworkState(ClientNetworkState.Failed);
                    return false;
                }
                
                SetNetworkState(ClientNetworkState.MetaConnected);
                LogClient("Successfully connected to networks");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Connection failed: {ex.Message}");
                SetNetworkState(ClientNetworkState.Failed);
                return false;
            }
        }
        
        /// <summary>
        /// Connect to game network (called when joining a match)
        /// </summary>
        public async Task<bool> ConnectToGameAsync(string gameServerAddress = null)
        {
            try
            {
                if (gameServerAddress != null)
                {
                    mirrorManager.networkAddress = ExtractAddress(gameServerAddress);
                }
                
                // Set authentication token for game connection
                NetworkClient.Send(new AuthTokenMessage { Token = currentGameToken });
                
                // Connect to game network
                mirrorManager.StartClient();
                
                // Wait for connection with timeout
                var connectionTask = WaitForGameConnection();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(config.ConnectionTimeout));
                
                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    LogError("Game connection timeout");
                    mirrorManager.StopClient();
                    return false;
                }
                
                var connected = await connectionTask;
                if (connected)
                {
                    SetNetworkState(ClientNetworkState.FullyConnected);
                    LogClient("Connected to game network");
                }
                
                return connected;
            }
            catch (Exception ex)
            {
                LogError($"Game connection failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ConnectToMetaNetworkAsync()
        {
            try
            {
                // Set authentication header
                if (metaConnection.Headers == null)
                    metaConnection.Headers = new Dictionary<string, string>();
                
                metaConnection.Headers["Authorization"] = $"Bearer {currentMetaToken}";
                
                await metaConnection.StartAsync();
                
                // Identify the client
                await metaConnection.InvokeAsync("IdentifyClient", playerId);
                
                return metaConnection.State == HubConnectionState.Connected;
            }
            catch (Exception ex)
            {
                LogError($"Meta network connection failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> WaitForGameConnection()
        {
            while (!NetworkClient.isConnected)
            {
                await Task.Yield();
                if (!NetworkClient.active)
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Disconnect from all networks
        /// </summary>
        public async Task DisconnectAsync()
        {
            SetNetworkState(ClientNetworkState.Disconnecting);
            
            // Disconnect from game network
            if (NetworkClient.isConnected)
            {
                mirrorManager.StopClient();
            }
            
            // Disconnect from meta network
            if (metaConnection.State == HubConnectionState.Connected)
            {
                await metaConnection.StopAsync();
            }
            
            SetNetworkState(ClientNetworkState.Disconnected);
            LogClient("Disconnected from all networks");
        }
        
        #endregion
        
        #region Game Network Events
        
        private void OnGameNetworkConnected()
        {
            LogClient("Game network connected");
            
            // Register game message handlers
            NetworkClient.RegisterHandler<GameStateUpdateMessage>(OnGameStateUpdateReceived);
            NetworkClient.RegisterHandler<PlayerActionMessage>(OnPlayerActionReceived);
            NetworkClient.RegisterHandler<GameEventMessage>(OnGameEventReceived);
            
            connectionManager.OnGameConnected();
        }
        
        private void OnGameNetworkDisconnected()
        {
            LogClient("Game network disconnected");
            
            if (currentState == ClientNetworkState.FullyConnected)
            {
                SetNetworkState(ClientNetworkState.MetaConnected);
                HandleGameDisconnection();
            }
            
            connectionManager.OnGameDisconnected();
        }
        
        private void OnGameNetworkError(Exception error)
        {
            LogError($"Game network error: {error.Message}");
            OnNetworkError?.Invoke(new NetworkError(NetworkChannel.Game, error));
        }
        
        #endregion
        
        #region Meta Network Events
        
        private async Task OnMetaNetworkDisconnected(Exception error)
        {
            LogClient($"Meta network disconnected: {error?.Message ?? "Unknown"}");
            
            if (currentState != ClientNetworkState.Disconnecting)
            {
                SetNetworkState(ClientNetworkState.GameConnected);
                HandleMetaDisconnection();
            }
        }
        
        private async Task OnMetaNetworkReconnected(string connectionId)
        {
            LogClient($"Meta network reconnected: {connectionId}");
            
            // Re-identify client after reconnection
            try
            {
                await metaConnection.InvokeAsync("IdentifyClient", playerId);
            }
            catch (Exception ex)
            {
                LogError($"Failed to re-identify after reconnection: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Message Handling
        
        /// <summary>
        /// Send game action to server
        /// </summary>
        public void SendGameAction(GameAction action)
        {
            if (!NetworkClient.isConnected)
            {
                LogWarning("Cannot send game action: not connected to game network");
                return;
            }
            
            var message = new GameActionMessage
            {
                Action = action,
                PlayerId = playerId,
                Timestamp = NetworkTime.time
            };
            
            // Add to queue for batching
            outgoingGameMessages.Enqueue(message);
        }
        
        /// <summary>
        /// Send meta action (chat, friend request, etc.)
        /// </summary>
        public async Task<bool> SendMetaActionAsync(string method, params object[] args)
        {
            if (metaConnection.State != HubConnectionState.Connected)
            {
                LogWarning($"Cannot send meta action {method}: not connected to meta network");
                return false;
            }
            
            try
            {
                await metaConnection.InvokeAsync(method, args);
                LogClient($"Sent meta action: {method}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to send meta action {method}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Request matchmaking
        /// </summary>
        public async Task<bool> RequestMatchmakingAsync(MatchmakingRequest request)
        {
            return await SendMetaActionAsync("RequestMatchmaking", request);
        }
        
        /// <summary>
        /// Send chat message
        /// </summary>
        public async Task<bool> SendChatMessageAsync(string message, string channelId = null)
        {
            var chatMsg = new ChatMessage
            {
                SenderId = playerId,
                Content = message,
                ChannelId = channelId ?? "general",
                Timestamp = DateTime.UtcNow
            };
            
            return await SendMetaActionAsync("SendChatMessage", chatMsg);
        }
        
        #endregion
        
        #region Message Processing
        
        private void OnGameStateUpdateReceived(GameStateUpdateMessage message)
        {
            gameStateManager.UpdateState(message.GameState);
            OnGameStateReceived?.Invoke(message.GameState);
        }
        
        private void OnPlayerActionReceived(PlayerActionMessage message)
        {
            // Process player action
            messageHandler.ProcessPlayerAction(message);
        }
        
        private void OnGameEventReceived(GameEventMessage message)
        {
            // Process game event
            messageHandler.ProcessGameEvent(message);
        }
        
        #endregion
        
        #region Client Updates
        
        private void StartClientSystems()
        {
            messageHandler.Initialize();
            bandwidthManager.Initialize();
            connectionManager.Initialize();
            gameStateManager.Initialize();
        }
        
        private void UpdateClientSystems()
        {
            if (isApplicationPaused)
                return;
                
            var deltaTime = Time.time - lastNetworkUpdate;
            lastNetworkUpdate = Time.time;
            
            // Update components
            messageHandler?.Update(deltaTime);
            bandwidthManager?.Update(deltaTime);
            connectionManager?.Update(deltaTime);
            gameStateManager?.Update(deltaTime);
            
            // Process message queues
            ProcessOutgoingMessages();
            
            // Monitor connection health
            MonitorConnectionHealth();
            
            // Apply adaptive quality
            if (adaptiveQuality)
            {
                ApplyAdaptiveQuality();
            }
        }
        
        private void ProcessOutgoingMessages()
        {
            // Process game messages
            var gameMessageCount = 0;
            while (outgoingGameMessages.TryDequeue(out var gameMessage) && gameMessageCount < config.MaxMessagesPerFrame)
            {
                NetworkClient.Send(gameMessage);
                gameMessageCount++;
            }
            
            // Process meta messages (already handled async)
        }
        
        private void MonitorConnectionHealth()
        {
            var gameConnected = NetworkClient.isConnected;
            var metaConnected = metaConnection.State == HubConnectionState.Connected;
            
            switch (currentState)
            {
                case ClientNetworkState.FullyConnected:
                    if (!gameConnected && !metaConnected)
                        HandleFullDisconnection();
                    else if (!gameConnected)
                        SetNetworkState(ClientNetworkState.MetaConnected);
                    else if (!metaConnected)
                        SetNetworkState(ClientNetworkState.GameConnected);
                    break;
                    
                case ClientNetworkState.GameConnected:
                    if (!gameConnected)
                        SetNetworkState(ClientNetworkState.Disconnected);
                    else if (metaConnected)
                        SetNetworkState(ClientNetworkState.FullyConnected);
                    break;
                    
                case ClientNetworkState.MetaConnected:
                    if (!metaConnected)
                        SetNetworkState(ClientNetworkState.Disconnected);
                    else if (gameConnected)
                        SetNetworkState(ClientNetworkState.FullyConnected);
                    break;
            }
        }
        
        private void ApplyAdaptiveQuality()
        {
            var quality = bandwidthManager.CurrentQuality;
            
            // Adjust message batching based on quality
            config.MaxMessagesPerFrame = quality switch
            {
                NetworkQuality.High => 10,
                NetworkQuality.Medium => 5,
                NetworkQuality.Low => 2,
                _ => 5
            };
        }
        
        #endregion
        
        #region Disconnection Handling
        
        private void HandleFullDisconnection()
        {
            SetNetworkState(ClientNetworkState.Reconnecting);
            _ = AttemptReconnectionAsync();
        }
        
        private void HandleGameDisconnection()
        {
            // Try to reconnect to game network only
            _ = AttemptGameReconnectionAsync();
        }
        
        private void HandleMetaDisconnection()
        {
            // SignalR has automatic reconnection, so just monitor
            LogClient("Meta network will attempt automatic reconnection");
        }
        
        /// <summary>
        /// Attempt full reconnection
        /// </summary>
        public async Task<bool> AttemptReconnectionAsync()
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                LogError("Maximum reconnection attempts reached");
                SetNetworkState(ClientNetworkState.Failed);
                return false;
            }
            
            reconnectAttempts++;
            LogClient($"Attempting reconnection #{reconnectAttempts}");
            
            try
            {
                // Wait with exponential backoff
                var delay = Mathf.Pow(2, reconnectAttempts) * reconnectDelay;
                await Task.Delay(TimeSpan.FromSeconds(delay));
                
                // Try to reconnect
                return await ConnectAsync(currentGameToken, currentMetaToken, playerId);
            }
            catch (Exception ex)
            {
                LogError($"Reconnection attempt failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Attempt game network reconnection only
        /// </summary>
        private async Task<bool> AttemptGameReconnectionAsync()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelay));
                return await ConnectToGameAsync();
            }
            catch (Exception ex)
            {
                LogError($"Game reconnection failed: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Mobile Optimizations
        
        private void HandleApplicationPause(bool pauseStatus)
        {
            isApplicationPaused = pauseStatus;
            
            if (pauseStatus)
            {
                // Reduce activity for battery optimization
                config.MaxMessagesPerFrame = (int)(config.MaxMessagesPerFrame * batteryOptimizationFactor);
                LogClient("Network activity reduced for battery optimization");
            }
            else
            {
                // Restore normal activity
                ApplyAdaptiveQuality();
                LogClient("Network activity restored");
            }
        }
        
        private void HandleApplicationFocus(bool hasFocus)
        {
            if (hasFocus && currentState == ClientNetworkState.Disconnected)
            {
                // Try to reconnect when app regains focus
                _ = AttemptReconnectionAsync();
            }
        }
        
        #endregion
        
        #region State Management
        
        private void SetNetworkState(ClientNetworkState newState)
        {
            if (currentState != newState)
            {
                var oldState = currentState;
                currentState = newState;
                
                LogClient($"Network state changed: {oldState} -> {newState}");
                OnNetworkStateChanged?.Invoke(newState);
                
                // Reset reconnect attempts on successful connection
                if (newState == ClientNetworkState.FullyConnected || newState == ClientNetworkState.MetaConnected)
                {
                    reconnectAttempts = 0;
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get current network statistics
        /// </summary>
        public ClientNetworkStatistics GetNetworkStatistics()
        {
            return new ClientNetworkStatistics
            {
                State = currentState,
                GameConnected = NetworkClient.isConnected,
                MetaConnected = metaConnection.State == HubConnectionState.Connected,
                Ping = NetworkTime.rtt,
                Quality = bandwidthManager.CurrentQuality,
                ReconnectAttempts = reconnectAttempts,
                UploadRate = bandwidthManager.UploadRate,
                DownloadRate = bandwidthManager.DownloadRate
            };
        }
        
        /// <summary>
        /// Check if specific feature is available
        /// </summary>
        public bool IsFeatureAvailable(NetworkFeature feature)
        {
            return feature switch
            {
                NetworkFeature.GameNetwork => NetworkClient.isConnected,
                NetworkFeature.MetaNetwork => metaConnection.State == HubConnectionState.Connected,
                NetworkFeature.Matchmaking => metaConnection.State == HubConnectionState.Connected,
                NetworkFeature.Chat => metaConnection.state == HubConnectionState.Connected,
                NetworkFeature.Leaderboards => metaConnection.State == HubConnectionState.Connected,
                NetworkFeature.Friends => metaConnection.State == HubConnectionState.Connected,
                _ => false
            };
        }
        
        /// <summary>
        /// Get current game state
        /// </summary>
        public GameState GetCurrentGameState()
        {
            return gameStateManager.CurrentState;
        }
        
        /// <summary>
        /// Force network quality setting
        /// </summary>
        public void SetNetworkQuality(NetworkQuality quality)
        {
            adaptiveQuality = false;
            bandwidthManager.SetQuality(quality);
            ApplyAdaptiveQuality();
        }
        
        #endregion
        
        #region Utility Methods
        
        private string ExtractAddress(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return url;
            }
        }
        
        private ClientNetworkConfig CreateDefaultConfig()
        {
            return new ClientNetworkConfig
            {
                ConnectionTimeout = 10f,
                ResponseTimeout = 5f,
                MaxMessagesPerFrame = 5,
                EnableCompression = true,
                EnableBatching = true
            };
        }
        
        #endregion
        
        #region Cleanup
        
        private async void Cleanup()
        {
            if (currentState != ClientNetworkState.Disconnected)
            {
                await DisconnectAsync();
            }
            
            // Cleanup event handlers
            NetworkClient.OnConnectedEvent -= OnGameNetworkConnected;
            NetworkClient.OnDisconnectedEvent -= OnGameNetworkDisconnected;
            NetworkClient.OnErrorEvent -= OnGameNetworkError;
        }
        
        #endregion
        
        #region Logging
        
        private void LogClient(string message)
        {
            if (enableDebugLogging)
                Debug.Log($"🌐 ClientNetworkManager: {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"⚠️ ClientNetworkManager: {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"❌ ClientNetworkManager: {message}");
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Client-specific network configuration
    /// </summary>
    [System.Serializable]
    public class ClientNetworkConfig
    {
        [Header("Connection Settings")]
        public float ConnectionTimeout = 10f;
        public float ResponseTimeout = 5f;
        public int MaxMessagesPerFrame = 5;
        
        [Header("Optimization")]
        public bool EnableCompression = true;
        public bool EnableBatching = true;
        public bool EnablePrediction = true;
    }
    
    /// <summary>
    /// Client network state
    /// </summary>
    public enum ClientNetworkState
    {
        Disconnected,
        Connecting,
        MetaConnected,      // Only meta network connected
        GameConnected,      // Only game network connected
        FullyConnected,     // Both networks connected
        Reconnecting,
        Disconnecting,
        Failed
    }
    
    /// <summary>
    /// Network feature enumeration
    /// </summary>
    public enum NetworkFeature
    {
        GameNetwork,
        MetaNetwork,
        Matchmaking,
        Chat,
        Leaderboards,
        Friends
    }
    
    /// <summary>
    /// Network quality enumeration
    /// </summary>
    public enum NetworkQuality
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// Network channel enumeration
    /// </summary>
    public enum NetworkChannel
    {
        Game,
        Meta
    }
    
    /// <summary>
    /// Client network statistics
    /// </summary>
    public class ClientNetworkStatistics
    {
        public ClientNetworkState State;
        public bool GameConnected;
        public bool MetaConnected;
        public double Ping;
        public NetworkQuality Quality;
        public int ReconnectAttempts;
        public float UploadRate;
        public float DownloadRate;
    }
    
    /// <summary>
    /// Network error class
    /// </summary>
    public class NetworkError
    {
        public NetworkChannel Channel { get; set; }
        public Exception Exception { get; set; }
        public string Message => Exception?.Message ?? "Unknown error";
        
        public NetworkError(NetworkChannel channel, Exception exception)
        {
            Channel = channel;
            Exception = exception;
        }
    }
    
    /// <summary>
    /// Authentication token message for game connection
    /// </summary>
    public struct AuthTokenMessage : NetworkMessage
    {
        public string Token;
    }
    
    /// <summary>
    /// Game action message
    /// </summary>
    public struct GameActionMessage : NetworkMessage
    {
        public GameAction Action;
        public string PlayerId;
        public double Timestamp;
    }
    
    /// <summary>
    /// Game state update message
    /// </summary>
    public struct GameStateUpdateMessage : NetworkMessage
    {
        public GameStateUpdate GameState;
    }
    
    /// <summary>
    /// Player action message
    /// </summary>
    public struct PlayerActionMessage : NetworkMessage
    {
        public string PlayerId;
        public PlayerAction Action;
        public double Timestamp;
    }
    
    /// <summary>
    /// Game event message
    /// </summary>
    public struct GameEventMessage : NetworkMessage
    {
        public GameEvent Event;
        public double Timestamp;
    }
    
    /// <summary>
    /// Meta action message
    /// </summary>
    public class MetaActionMessage
    {
        public string Method { get; set; }
        public object[] Arguments { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Match found message
    /// </summary>
    public class MatchFoundMessage
    {
        public string MatchId { get; set; }
        public string GameServerUrl { get; set; }
        public List<PlayerInfo> Players { get; set; }
    }
    
    /// <summary>
    /// Player joined message
    /// </summary>
    public class PlayerJoinedMessage
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public PlayerInfo PlayerInfo { get; set; }
    }
    
    /// <summary>
    /// Player left message
    /// </summary>
    public class PlayerLeftMessage
    {
        public string PlayerId { get; set; }
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Chat message
    /// </summary>
    public class ChatMessage
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string ChannelId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Leaderboard update
    /// </summary>
    public class LeaderboardUpdate
    {
        public string LeaderboardId { get; set; }
        public List<LeaderboardEntry> Entries { get; set; }
    }
    
    /// <summary>
    /// Leaderboard entry
    /// </summary>
    public class LeaderboardEntry
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
    }
    
    /// <summary>
    /// Friend status update
    /// </summary>
    public class FriendStatusUpdate
    {
        public string FriendId { get; set; }
        public string Status { get; set; }
        public bool IsOnline { get; set; }
    }
    
    /// <summary>
    /// Matchmaking request
    /// </summary>
    public class MatchmakingRequest
    {
        public string PlayerId { get; set; }
        public string GameMode { get; set; }
        public int SkillLevel { get; set; }
        public Dictionary<string, object> Preferences { get; set; }
    }
    
    #endregion
}

// Additional classes that need to be defined for the system to work properly

namespace L5RCardGame.Client
{
    /// <summary>
    /// Placeholder classes for missing dependencies
    /// These should be implemented based on your specific game requirements
    /// </summary>
    
    public class ClientMessageHandler
    {
        public void Initialize() { }
        public void Update(float deltaTime) { }
        public void ProcessPlayerAction(PlayerActionMessage message) { }
        public void ProcessGameEvent(GameEventMessage message) { }
    }
    
    public class ClientBandwidthManager
    {
        public NetworkQuality CurrentQuality { get; private set; } = NetworkQuality.Medium;
        public float UploadRate { get; private set; } = 0f;
        public float DownloadRate { get; private set; } = 0f;
        
        public ClientBandwidthManager(bool adaptiveQuality) { }
        
        public void Initialize() { }
        public void Update(float deltaTime) { }
        public void SetQuality(NetworkQuality quality) 
        { 
            CurrentQuality = quality; 
        }
    }
    
    public class ClientConnectionManager
    {
        public ClientConnectionManager(int maxReconnectAttempts, float reconnectDelay) { }
        
        public void Initialize() { }
        public void Update(float deltaTime) { }
        public void OnGameConnected() { }
        public void OnGameDisconnected() { }
    }
    
    public class GameStateManager
    {
        public GameState CurrentState { get; private set; }
        
        public void Initialize() { }
        public void Update(float deltaTime) { }
        public void UpdateState(GameStateUpdate update) 
        {
            // Implementation depends on your game state structure
        }
    }
    
    /// <summary>
    /// Game-specific data structures
    /// These should be implemented based on your L5R card game requirements
    /// </summary>
    
    public class GameAction
    {
        public string ActionType { get; set; }
        public string TargetId { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
    
    public class PlayerAction
    {
        public string ActionType { get; set; }
        public string PlayerId { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
    
    public class GameEvent
    {
        public string EventType { get; set; }
        public string SourceId { get; set; }
        public Dictionary<string, object> EventData { get; set; }
    }
    
    public class GameState
    {
        public string GameId { get; set; }
        public string CurrentPhase { get; set; }
        public string ActivePlayerId { get; set; }
        public Dictionary<string, object> StateData { get; set; }
    }
    
    public class GameStateUpdate
    {
        public string UpdateType { get; set; }
        public GameState NewState { get; set; }
        public Dictionary<string, object> Changes { get; set; }
    }
    
    public class PlayerInfo
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Avatar { get; set; }
        public int Level { get; set; }
        public Dictionary<string, object> Stats { get; set; }
    }
}