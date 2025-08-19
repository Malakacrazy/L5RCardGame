using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using Microsoft.AspNetCore.SignalR.Client;
using MessagePack;

namespace L5RGame.Network
{
    /// <summary>
    /// Dual-channel network manager implementing Mirror Networking for game traffic 
    /// and SignalR for meta services, optimized for mobile Unity card game.
    /// </summary>
    public class NetworkManager : Mirror.NetworkManager
    {
        [Header("Network Configuration")]
        [SerializeField] private NetworkConfig config;
        [SerializeField] private string signalRHubUrl = "wss://api.l5rgame.com/gamehub";
        [SerializeField] private bool enableDebugLogging = true;
        
        [Header("Mobile Optimization")]
        [SerializeField] private bool adaptiveQuality = true;
        [SerializeField] private float batteryOptimization = 0.8f;
        [SerializeField] private int maxReconnectAttempts = 5;
        
        // Core Components
        private GameNetworkHandler gameHandler;
        private MetaNetworkHandler metaHandler;
        private BandwidthManager bandwidthManager;
        private ConnectionManager connectionManager;
        private MessageBatcher messageBatcher;
        
        // Network State
        private NetworkState currentState = NetworkState.Disconnected;
        private float lastNetworkUpdate;
        private int reconnectAttempts;
        
        // Events
        public event Action<NetworkState> OnNetworkStateChanged;
        public event Action<GameMessage> OnGameMessageReceived;
        public event Action<MetaMessage> OnMetaMessageReceived;
        public event Action<NetworkError> OnNetworkError;
        
        #region Unity Lifecycle
        
        public override void Awake()
        {
            base.Awake();
            InitializeNetworkManager();
        }
        
        public override void Start()
        {
            base.Start();
            StartNetworkSystems();
        }
        
        void Update()
        {
            UpdateNetworkSystems();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            HandleApplicationPause(pauseStatus);
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            HandleApplicationFocus(hasFocus);
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeNetworkManager()
        {
            // Initialize configuration
            if (config == null)
                config = CreateDefaultConfig();
            
            // Initialize components
            gameHandler = new GameNetworkHandler(this);
            metaHandler = new MetaNetworkHandler(signalRHubUrl);
            bandwidthManager = new BandwidthManager(adaptiveQuality);
            connectionManager = new ConnectionManager(maxReconnectAttempts);
            messageBatcher = new MessageBatcher(config);
            
            // Configure Mirror settings
            ConfigureMirrorNetworking();
            
            LogNetwork("NetworkManager initialized with dual-channel architecture");
        }
        
        private void ConfigureMirrorNetworking()
        {
            // WebSocket transport configuration
            var transport = GetComponent<TelepathyTransport>();
            if (transport != null)
            {
                transport.port = config.GamePort;
                transport.MaxMessageSize = config.MaxMessageSize;
            }
            
            // Network settings
            sendRate = config.SendRate;
            serverTickRate = config.TickRate;
            
            // Timeout settings
            networkAddress = config.ServerAddress;
        }
        
        private NetworkConfig CreateDefaultConfig()
        {
            return new NetworkConfig
            {
                ServerAddress = "localhost",
                GamePort = 7777,
                MaxMessageSize = 16384,
                SendRate = 10,
                TickRate = 10,
                ConnectionTimeout = 10f,
                ResponseTimeout = 5f,
                KeepAliveInterval = 1f
            };
        }
        
        #endregion
        
        #region Network Connection Management
        
        /// <summary>
        /// Connect to both game and meta networks
        /// </summary>
        public async Task<bool> ConnectToNetwork(string gameToken, string metaToken)
        {
            try
            {
                SetNetworkState(NetworkState.Connecting);
                
                // Connect to game network (Mirror)
                var gameConnected = await ConnectToGameNetwork(gameToken);
                if (!gameConnected)
                {
                    SetNetworkState(NetworkState.Failed);
                    return false;
                }
                
                // Connect to meta network (SignalR)
                var metaConnected = await ConnectToMetaNetwork(metaToken);
                if (!metaConnected)
                {
                    LogWarning("Meta network connection failed, continuing with game only");
                }
                
                SetNetworkState(NetworkState.Connected);
                reconnectAttempts = 0;
                
                LogNetwork("Successfully connected to dual-channel network");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Network connection failed: {ex.Message}");
                SetNetworkState(NetworkState.Failed);
                return false;
            }
        }
        
        private async Task<bool> ConnectToGameNetwork(string gameToken)
        {
            try
            {
                // Set authentication token
                gameHandler.SetAuthToken(gameToken);
                
                // Start Mirror client
                StartClient();
                
                // Wait for connection with timeout
                var startTime = Time.time;
                while (!NetworkClient.isConnected && Time.time - startTime < config.ConnectionTimeout)
                {
                    await Task.Yield();
                }
                
                return NetworkClient.isConnected;
            }
            catch (Exception ex)
            {
                LogError($"Game network connection failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ConnectToMetaNetwork(string metaToken)
        {
            try
            {
                return await metaHandler.ConnectAsync(metaToken);
            }
            catch (Exception ex)
            {
                LogError($"Meta network connection failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from both networks
        /// </summary>
        public async Task DisconnectFromNetwork()
        {
            SetNetworkState(NetworkState.Disconnecting);
            
            // Disconnect from game network
            if (NetworkClient.isConnected)
            {
                StopClient();
            }
            
            // Disconnect from meta network
            await metaHandler.DisconnectAsync();
            
            SetNetworkState(NetworkState.Disconnected);
            LogNetwork("Disconnected from dual-channel network");
        }
        
        #endregion
        
        #region Mirror Network Callbacks
        
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            gameHandler.OnConnected();
            LogNetwork("Connected to game network");
        }
        
        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            gameHandler.OnDisconnected();
            HandleGameDisconnection();
            LogNetwork("Disconnected from game network");
        }
        
        public override void OnClientError(Exception exception)
        {
            base.OnClientError(exception);
            LogError($"Game network error: {exception.Message}");
            OnNetworkError?.Invoke(new NetworkError(NetworkChannel.Game, exception));
        }
        
        #endregion
        
        #region Message Handling
        
        /// <summary>
        /// Send game message through Mirror network
        /// </summary>
        public void SendGameMessage<T>(T message, MessagePriority priority = MessagePriority.Normal) where T : struct, INetworkMessage
        {
            if (!NetworkClient.isConnected)
            {
                LogWarning("Cannot send game message: not connected");
                return;
            }
            
            var wrappedMessage = new GameMessage<T>
            {
                MessageType = typeof(T).Name,
                Priority = priority,
                Timestamp = NetworkTime.time,
                Data = message
            };
            
            if (priority == MessagePriority.Critical)
            {
                // Send immediately
                NetworkClient.Send(wrappedMessage);
                LogNetwork($"Sent critical game message: {typeof(T).Name}");
            }
            else
            {
                // Batch for efficiency
                messageBatcher.QueueGameMessage(wrappedMessage);
            }
        }
        
        /// <summary>
        /// Send meta message through SignalR
        /// </summary>
        public async Task<bool> SendMetaMessage(string method, object data)
        {
            try
            {
                await metaHandler.SendAsync(method, data);
                LogNetwork($"Sent meta message: {method}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to send meta message {method}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Register handler for specific game message type
        /// </summary>
        public void RegisterGameMessageHandler<T>(Action<T> handler) where T : struct, INetworkMessage
        {
            gameHandler.RegisterHandler<T>(handler);
        }
        
        /// <summary>
        /// Register handler for meta messages
        /// </summary>
        public void RegisterMetaMessageHandler(string method, Action<object[]> handler)
        {
            metaHandler.RegisterHandler(method, handler);
        }
        
        #endregion
        
        #region Network Updates
        
        private void StartNetworkSystems()
        {
            gameHandler.Initialize();
            bandwidthManager.Initialize();
            connectionManager.Initialize();
            messageBatcher.Initialize();
        }
        
        private void UpdateNetworkSystems()
        {
            var deltaTime = Time.time - lastNetworkUpdate;
            lastNetworkUpdate = Time.time;
            
            // Update components
            gameHandler?.Update(deltaTime);
            bandwidthManager?.Update(deltaTime);
            connectionManager?.Update(deltaTime);
            messageBatcher?.Update(deltaTime);
            
            // Monitor connection health
            MonitorConnectionHealth();
            
            // Apply adaptive quality
            if (adaptiveQuality)
            {
                ApplyAdaptiveQuality();
            }
        }
        
        private void MonitorConnectionHealth()
        {
            if (currentState == NetworkState.Connected)
            {
                var gameConnected = NetworkClient.isConnected;
                var metaConnected = metaHandler.IsConnected;
                
                if (!gameConnected && !metaConnected)
                {
                    HandleFullDisconnection();
                }
                else if (!gameConnected)
                {
                    HandleGameDisconnection();
                }
                else if (!metaConnected)
                {
                    HandleMetaDisconnection();
                }
            }
        }
        
        private void ApplyAdaptiveQuality()
        {
            var quality = bandwidthManager.CurrentQuality;
            
            // Adjust send rate based on network quality
            switch (quality)
            {
                case NetworkQuality.High:
                    sendRate = config.SendRate;
                    break;
                case NetworkQuality.Medium:
                    sendRate = config.SendRate * 0.75f;
                    break;
                case NetworkQuality.Low:
                    sendRate = config.SendRate * 0.5f;
                    break;
            }
            
            // Update message batching parameters
            messageBatcher.SetQuality(quality);
        }
        
        #endregion
        
        #region Disconnection Handling
        
        private void HandleFullDisconnection()
        {
            SetNetworkState(NetworkState.Reconnecting);
            connectionManager.StartReconnection();
        }
        
        private void HandleGameDisconnection()
        {
            if (currentState == NetworkState.Connected)
            {
                SetNetworkState(NetworkState.GameDisconnected);
                connectionManager.StartGameReconnection();
            }
        }
        
        private void HandleMetaDisconnection()
        {
            if (currentState == NetworkState.Connected)
            {
                SetNetworkState(NetworkState.MetaDisconnected);
                connectionManager.StartMetaReconnection();
            }
        }
        
        /// <summary>
        /// Attempt to reconnect to networks
        /// </summary>
        public async Task<bool> AttemptReconnection()
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                LogError("Maximum reconnection attempts reached");
                SetNetworkState(NetworkState.Failed);
                return false;
            }
            
            reconnectAttempts++;
            LogNetwork($"Attempting reconnection #{reconnectAttempts}");
            
            try
            {
                // Wait with exponential backoff
                var delay = Mathf.Pow(2, reconnectAttempts) * 1000; // ms
                await Task.Delay((int)delay);
                
                // Try to reconnect
                var lastToken = connectionManager.GetLastGameToken();
                var lastMetaToken = connectionManager.GetLastMetaToken();
                
                return await ConnectToNetwork(lastToken, lastMetaToken);
            }
            catch (Exception ex)
            {
                LogError($"Reconnection attempt failed: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Mobile Optimizations
        
        private void HandleApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Reduce network activity when paused
                sendRate *= batteryOptimization;
                messageBatcher.SetPaused(true);
                LogNetwork("Network activity reduced for battery optimization");
            }
            else
            {
                // Restore normal activity
                sendRate = config.SendRate;
                messageBatcher.SetPaused(false);
                LogNetwork("Network activity restored");
            }
        }
        
        private void HandleApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Switch to background mode
                connectionManager.SetBackgroundMode(true);
            }
            else
            {
                // Switch to foreground mode
                connectionManager.SetBackgroundMode(false);
                
                // Check if reconnection is needed
                if (currentState == NetworkState.Disconnected)
                {
                    _ = AttemptReconnection();
                }
            }
        }
        
        #endregion
        
        #region State Management
        
        private void SetNetworkState(NetworkState newState)
        {
            if (currentState != newState)
            {
                var oldState = currentState;
                currentState = newState;
                
                LogNetwork($"Network state changed: {oldState} -> {newState}");
                OnNetworkStateChanged?.Invoke(newState);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get current network statistics
        /// </summary>
        public NetworkStatistics GetNetworkStatistics()
        {
            return new NetworkStatistics
            {
                State = currentState,
                GameConnected = NetworkClient.isConnected,
                MetaConnected = metaHandler.IsConnected,
                Ping = (int)(NetworkTime.rtt * 1000),
                UploadRate = bandwidthManager.UploadRate,
                DownloadRate = bandwidthManager.DownloadRate,
                Quality = bandwidthManager.CurrentQuality,
                MessagesSent = messageBatcher.MessagesSent,
                MessagesReceived = messageBatcher.MessagesReceived
            };
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
        
        /// <summary>
        /// Check if specific network feature is available
        /// </summary>
        public bool IsFeatureAvailable(NetworkFeature feature)
        {
            return feature switch
            {
                NetworkFeature.GameNetwork => NetworkClient.isConnected,
                NetworkFeature.MetaNetwork => metaHandler.IsConnected,
                NetworkFeature.Matchmaking => metaHandler.IsConnected,
                NetworkFeature.Chat => metaHandler.IsConnected,
                NetworkFeature.Leaderboards => metaHandler.IsConnected,
                NetworkFeature.Friends => metaHandler.IsConnected,
                _ => false
            };
        }
        
        #endregion
        
        #region Logging
        
        private void LogNetwork(string message)
        {
            if (enableDebugLogging)
                Debug.Log($"🌐 NetworkManager: {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"⚠️ NetworkManager: {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"❌ NetworkManager: {message}");
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Network configuration settings
    /// </summary>
    [System.Serializable]
    public class NetworkConfig
    {
        [Header("Server Settings")]
        public string ServerAddress = "localhost";
        public int GamePort = 7777;
        public int MetaPort = 443;
        
        [Header("Message Settings")]
        public int MaxMessageSize = 16384; // 16KB
        public float SendRate = 10f; // Hz
        public float TickRate = 10f; // Hz
        
        [Header("Timeout Settings")]
        public float ConnectionTimeout = 10f;
        public float ResponseTimeout = 5f;
        public float KeepAliveInterval = 1f;
        
        [Header("Mobile Optimization")]
        public bool EnableCompression = true;
        public bool EnableBatching = true;
        public float BatteryOptimizationFactor = 0.8f;
    }
    
    /// <summary>
    /// Network state enumeration
    /// </summary>
    public enum NetworkState
    {
        Disconnected,
        Connecting,
        Connected,
        GameDisconnected,
        MetaDisconnected,
        Reconnecting,
        Disconnecting,
        Failed
    }
    
    /// <summary>
    /// Message priority levels
    /// </summary>
    public enum MessagePriority
    {
        Critical = 0,   // Game state changes
        High = 1,       // Player actions
        Normal = 2,     // Animation triggers
        Low = 3         // Visual effects
    }
    
    /// <summary>
    /// Network quality levels
    /// </summary>
    public enum NetworkQuality
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// Network channel types
    /// </summary>
    public enum NetworkChannel
    {
        Game,
        Meta
    }
    
    /// <summary>
    /// Available network features
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
    /// Network error information
    /// </summary>
    public class NetworkError
    {
        public NetworkChannel Channel { get; }
        public Exception Exception { get; }
        public DateTime Timestamp { get; }
        
        public NetworkError(NetworkChannel channel, Exception exception)
        {
            Channel = channel;
            Exception = exception;
            Timestamp = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Network statistics for monitoring
    /// </summary>
    public class NetworkStatistics
    {
        public NetworkState State;
        public bool GameConnected;
        public bool MetaConnected;
        public int Ping;
        public float UploadRate;
        public float DownloadRate;
        public NetworkQuality Quality;
        public int MessagesSent;
        public int MessagesReceived;
    }
    
    /// <summary>
    /// Base interface for network messages
    /// </summary>
    public interface INetworkMessage
    {
        MessagePriority Priority { get; }
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader);
    }
    
    /// <summary>
    /// Game message wrapper
    /// </summary>
    [System.Serializable]
    public struct GameMessage<T> : INetworkMessage where T : struct, INetworkMessage
    {
        public string MessageType;
        public MessagePriority Priority;
        public double Timestamp;
        public T Data;
        
        MessagePriority INetworkMessage.Priority => Priority;
        
        public void Serialize(NetworkWriter writer)
        {
            writer.WriteString(MessageType);
            writer.WriteByte((byte)Priority);
            writer.WriteDouble(Timestamp);
            Data.Serialize(writer);
        }
        
        public void Deserialize(NetworkReader reader)
        {
            MessageType = reader.ReadString();
            Priority = (MessagePriority)reader.ReadByte();
            Timestamp = reader.ReadDouble();
            Data.Deserialize(reader);
        }
    }
    
    /// <summary>
    /// Meta message for SignalR
    /// </summary>
    public class MetaMessage
    {
        public string Method { get; set; }
        public object[] Arguments { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    #endregion
}
