using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Mirror;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using MongoDB.Driver;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MessagePack;
using Prometheus;

namespace UnityCardGame.Server
{
    /// <summary>
    /// Mirror-based authoritative game server for Unity mobile card game
    /// Handles real-time game logic, state management, and IronPython script execution
    /// </summary>
    public class GameServer : BackgroundService
    {
        private readonly ILogger<GameServer> _logger;
        private readonly IConfiguration _config;
        private readonly IEventStore _eventStore;
        private readonly IRedisCache _stateCache;
        private readonly IScriptEngine _pythonEngine;
        private readonly ITokenService _tokenService;
        private readonly IGameStateCoordinator _stateCoordinator;
        
        // Network configuration
        private readonly NetworkConfig _networkConfig;
        private readonly ConcurrentDictionary<string, GameInstance> _activeGames;
        private readonly ConcurrentDictionary<string, PlayerConnection> _playerConnections;
        
        // Performance metrics
        private static readonly Counter GameActionsProcessed = Metrics
            .CreateCounter("game_actions_total", "Total number of game actions processed");
        private static readonly Histogram ActionProcessingTime = Metrics
            .CreateHistogram("action_processing_seconds", "Time spent processing game actions");
        private static readonly Gauge ActiveGamesCount = Metrics
            .CreateGauge("active_games", "Number of active games");
        private static readonly Gauge ConnectedPlayersCount = Metrics
            .CreateGauge("connected_players", "Number of connected players");

        public GameServer(
            ILogger<GameServer> logger,
            IConfiguration config,
            IEventStore eventStore,
            IRedisCache stateCache,
            IScriptEngine pythonEngine,
            ITokenService tokenService,
            IGameStateCoordinator stateCoordinator)
        {
            _logger = logger;
            _config = config;
            _eventStore = eventStore;
            _stateCache = stateCache;
            _pythonEngine = pythonEngine;
            _tokenService = tokenService;
            _stateCoordinator = stateCoordinator;
            
            _networkConfig = new NetworkConfig();
            _activeGames = new ConcurrentDictionary<string, GameInstance>();
            _playerConnections = new ConcurrentDictionary<string, PlayerConnection>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await StartServerAsync(stoppingToken);
                
                // Keep the server running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await UpdateServerMetrics();
                    await CleanupExpiredGames();
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in game server");
                throw;
            }
            finally
            {
                await StopServerAsync();
            }
        }

        private async Task StartServerAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Mirror Game Server on port {Port}", _networkConfig.GamePort);
            
            // Configure Mirror networking
            ConfigureMirrorServer();
            
            // Initialize transport layer
            var transport = NetworkManager.singleton.GetComponent<SimpleWebTransport>();
            transport.port = (ushort)_networkConfig.GamePort;
            transport.maxMessageSize = _networkConfig.MaxMessageSize;
            
            // Set up connection callbacks
            NetworkServer.OnConnectedEvent += OnPlayerConnected;
            NetworkServer.OnDisconnectedEvent += OnPlayerDisconnected;
            NetworkServer.OnErrorEvent += OnNetworkError;
            
            // Register message handlers
            NetworkServer.RegisterHandler<GameActionMessage>(OnGameActionReceived);
            NetworkServer.RegisterHandler<AuthenticationMessage>(OnAuthenticationReceived);
            NetworkServer.RegisterHandler<JoinGameMessage>(OnJoinGameReceived);
            NetworkServer.RegisterHandler<LeaveGameMessage>(OnLeaveGameReceived);
            
            // Start listening for connections
            NetworkServer.Listen(_networkConfig.MaxConnections);
            
            _logger.LogInformation("Mirror Game Server started successfully");
            await Task.CompletedTask;
        }

        private void ConfigureMirrorServer()
        {
            // Set server-only mode
            NetworkManager.singleton.serverOnly = true;
            
            // Configure tick rate
            NetworkServer.sendRate = _networkConfig.SendRate;
            NetworkTime.fixedDeltaTime = _networkConfig.TickInterval;
            
            // Set timeout values
            NetworkServer.disconnectInactiveTimeout = _networkConfig.ConnectionTimeout;
        }

        private void OnPlayerConnected(NetworkConnectionToClient conn)
        {
            _logger.LogInformation("Player connected: {ConnectionId}", conn.connectionId);
            
            var playerConnection = new PlayerConnection
            {
                ConnectionId = conn.connectionId,
                Connection = conn,
                ConnectedAt = DateTime.UtcNow,
                IsAuthenticated = false
            };
            
            _playerConnections.TryAdd(conn.connectionId.ToString(), playerConnection);
            ConnectedPlayersCount.Inc();
        }

        private void OnPlayerDisconnected(NetworkConnectionToClient conn)
        {
            _logger.LogInformation("Player disconnected: {ConnectionId}", conn.connectionId);
            
            var connectionId = conn.connectionId.ToString();
            if (_playerConnections.TryRemove(connectionId, out var playerConnection))
            {
                // Remove player from any active games
                if (!string.IsNullOrEmpty(playerConnection.CurrentGameId))
                {
                    _ = HandlePlayerLeaveGame(playerConnection.CurrentGameId, playerConnection.PlayerId);
                }
                
                ConnectedPlayersCount.Dec();
            }
        }

        private void OnNetworkError(NetworkConnectionToClient conn, TransportError error, string reason)
        {
            _logger.LogError("Network error for connection {ConnectionId}: {Error} - {Reason}", 
                conn.connectionId, error, reason);
        }

        private async void OnAuthenticationReceived(NetworkConnectionToClient conn, AuthenticationMessage message)
        {
            using var timer = ActionProcessingTime.NewTimer();
            
            try
            {
                var connectionId = conn.connectionId.ToString();
                if (!_playerConnections.TryGetValue(connectionId, out var playerConnection))
                {
                    conn.Disconnect();
                    return;
                }

                // Validate authentication token
                var isValid = await _tokenService.ValidateGameTokenAsync(message.Token);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid token received from connection {ConnectionId}", conn.connectionId);
                    conn.Send(new AuthenticationResponseMessage { Success = false, Reason = "Invalid token" });
                    conn.Disconnect();
                    return;
                }

                // Extract player information from token
                var claims = _tokenService.GetTokenClaims(message.Token);
                var playerId = claims.FindFirst("uid")?.Value;
                var allowedGameId = claims.FindFirst("gid")?.Value;

                if (string.IsNullOrEmpty(playerId))
                {
                    conn.Send(new AuthenticationResponseMessage { Success = false, Reason = "Invalid player ID" });
                    conn.Disconnect();
                    return;
                }

                // Update player connection
                playerConnection.IsAuthenticated = true;
                playerConnection.PlayerId = playerId;
                playerConnection.AllowedGameId = allowedGameId;

                conn.Send(new AuthenticationResponseMessage { Success = true, PlayerId = playerId });
                _logger.LogInformation("Player {PlayerId} authenticated successfully", playerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing authentication for connection {ConnectionId}", conn.connectionId);
                conn.Send(new AuthenticationResponseMessage { Success = false, Reason = "Authentication error" });
                conn.Disconnect();
            }
        }

        private async void OnJoinGameReceived(NetworkConnectionToClient conn, JoinGameMessage message)
        {
            using var timer = ActionProcessingTime.NewTimer();
            
            try
            {
                var connectionId = conn.connectionId.ToString();
                if (!_playerConnections.TryGetValue(connectionId, out var playerConnection) || 
                    !playerConnection.IsAuthenticated)
                {
                    conn.Send(new JoinGameResponseMessage { Success = false, Reason = "Not authenticated" });
                    return;
                }

                // Validate game access
                if (!string.IsNullOrEmpty(playerConnection.AllowedGameId) && 
                    playerConnection.AllowedGameId != message.GameId)
                {
                    conn.Send(new JoinGameResponseMessage { Success = false, Reason = "Not authorized for this game" });
                    return;
                }

                // Get or create game instance
                var game = await GetOrCreateGameInstance(message.GameId);
                if (game == null)
                {
                    conn.Send(new JoinGameResponseMessage { Success = false, Reason = "Failed to create game" });
                    return;
                }

                // Add player to game
                var success = await game.AddPlayerAsync(playerConnection.PlayerId, conn);
                if (success)
                {
                    playerConnection.CurrentGameId = message.GameId;
                    conn.Send(new JoinGameResponseMessage 
                    { 
                        Success = true, 
                        GameId = message.GameId,
                        GameState = await game.GetGameStateAsync()
                    });
                    
                    _logger.LogInformation("Player {PlayerId} joined game {GameId}", 
                        playerConnection.PlayerId, message.GameId);
                }
                else
                {
                    conn.Send(new JoinGameResponseMessage { Success = false, Reason = "Game is full or unavailable" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing join game request");
                conn.Send(new JoinGameResponseMessage { Success = false, Reason = "Internal error" });
            }
        }

        private async void OnLeaveGameReceived(NetworkConnectionToClient conn, LeaveGameMessage message)
        {
            var connectionId = conn.connectionId.ToString();
            if (_playerConnections.TryGetValue(connectionId, out var playerConnection))
            {
                await HandlePlayerLeaveGame(message.GameId, playerConnection.PlayerId);
                playerConnection.CurrentGameId = null;
                conn.Send(new LeaveGameResponseMessage { Success = true });
            }
        }

        private async void OnGameActionReceived(NetworkConnectionToClient conn, GameActionMessage message)
        {
            using var timer = ActionProcessingTime.NewTimer();
            GameActionsProcessed.Inc();
            
            try
            {
                var connectionId = conn.connectionId.ToString();
                if (!_playerConnections.TryGetValue(connectionId, out var playerConnection) || 
                    !playerConnection.IsAuthenticated)
                {
                    return;
                }

                // Validate player is in the specified game
                if (playerConnection.CurrentGameId != message.GameId)
                {
                    _logger.LogWarning("Player {PlayerId} attempted action on game {GameId} but is in game {CurrentGame}", 
                        playerConnection.PlayerId, message.GameId, playerConnection.CurrentGameId);
                    return;
                }

                // Get game instance
                if (!_activeGames.TryGetValue(message.GameId, out var game))
                {
                    _logger.LogWarning("Game {GameId} not found for player action", message.GameId);
                    return;
                }

                // Process the game action
                await ProcessGameActionAsync(game, message.Action, playerConnection.PlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game action");
            }
        }

        private async Task ProcessGameActionAsync(GameInstance game, GameAction action, string playerId)
        {
            try
            {
                // Validate action server-side
                if (!await ValidateActionAsync(game, action, playerId))
                {
                    _logger.LogWarning("Invalid action {ActionType} from player {PlayerId} in game {GameId}", 
                        action.ActionType, playerId, game.GameId);
                    return;
                }

                // Execute action through IronPython for card effects
                ScriptResult scriptResult = null;
                if (action.RequiresScriptExecution)
                {
                    var gameContext = await game.GetGameContextAsync();
                    scriptResult = await _pythonEngine.ExecuteCardScriptAsync(action.CardId, gameContext);
                    
                    if (!scriptResult.Success)
                    {
                        _logger.LogError("Script execution failed for card {CardId}: {Error}", 
                            action.CardId, scriptResult.ErrorMessage);
                        return;
                    }
                }

                // Apply action to game state
                var gameStateChange = await game.ApplyActionAsync(action, scriptResult);
                
                // Store event for replay and auditing
                await _eventStore.AppendEventAsync(new GameEvent
                {
                    GameId = game.GameId,
                    PlayerId = playerId,
                    Action = action,
                    Timestamp = DateTime.UtcNow,
                    ScriptResult = scriptResult,
                    StateChange = gameStateChange
                });

                // Update cache
                await _stateCache.UpdateGameStateAsync(game.GameId, gameStateChange);

                // Broadcast state change to all players in the game
                await BroadcastGameStateChange(game, gameStateChange);

                // Check for game end conditions
                if (gameStateChange.IsGameEnded)
                {
                    await HandleGameEnd(game, gameStateChange.GameResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game action for game {GameId}", game.GameId);
            }
        }

        private async Task<bool> ValidateActionAsync(GameInstance game, GameAction action, string playerId)
        {
            // Basic validation
            if (action == null || string.IsNullOrEmpty(action.ActionType))
                return false;

            // Check if it's the player's turn
            var gameState = await game.GetGameStateAsync();
            if (gameState.CurrentPlayerId != playerId)
                return false;

            // Validate action timing
            if (gameState.TurnTimeRemaining <= 0)
                return false;

            // Action-specific validation
            return action.ActionType switch
            {
                "PlayCard" => await ValidatePlayCardAction(game, action, playerId),
                "EndTurn" => await ValidateEndTurnAction(game, action, playerId),
                "UseAbility" => await ValidateAbilityAction(game, action, playerId),
                _ => false
            };
        }

        private async Task<bool> ValidatePlayCardAction(GameInstance game, GameAction action, string playerId)
        {
            var gameState = await game.GetGameStateAsync();
            var player = gameState.GetPlayer(playerId);
            
            // Check if player has the card
            if (!player.Hand.Contains(action.CardId))
                return false;

            // Check mana cost
            var card = await game.GetCardAsync(action.CardId);
            if (player.CurrentMana < card.ManaCost)
                return false;

            // Check board space for minions
            if (card.Type == "Minion" && player.Board.Count >= 7)
                return false;

            return true;
        }

        private async Task<bool> ValidateEndTurnAction(GameInstance game, GameAction action, string playerId)
        {
            var gameState = await game.GetGameStateAsync();
            return gameState.CurrentPlayerId == playerId;
        }

        private async Task<bool> ValidateAbilityAction(GameInstance game, GameAction action, string playerId)
        {
            // Implement ability-specific validation
            return true;
        }

        private async Task BroadcastGameStateChange(GameInstance game, GameStateChange stateChange)
        {
            var message = new GameStateUpdateMessage
            {
                GameId = game.GameId,
                StateChange = stateChange,
                Timestamp = DateTime.UtcNow
            };

            foreach (var player in game.GetConnectedPlayers())
            {
                try
                {
                    player.Connection.Send(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send state update to player {PlayerId}", player.PlayerId);
                }
            }
        }

        private async Task HandleGameEnd(GameInstance game, GameResult result)
        {
            try
            {
                _logger.LogInformation("Game {GameId} ended with result: {Result}", game.GameId, result);

                // Clean up game resources
                await game.CleanupAsync();
                _activeGames.TryRemove(game.GameId, out _);
                ActiveGamesCount.Dec();

                // Notify state coordinator
                await _stateCoordinator.OnGameCompleteAsync(game.GameId, result);

                // Send final results to players
                var endMessage = new GameEndMessage
                {
                    GameId = game.GameId,
                    Result = result,
                    Timestamp = DateTime.UtcNow
                };

                foreach (var player in game.GetConnectedPlayers())
                {
                    try
                    {
                        player.Connection.Send(endMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send game end message to player {PlayerId}", player.PlayerId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling game end for game {GameId}", game.GameId);
            }
        }

        private async Task HandlePlayerLeaveGame(string gameId, string playerId)
        {
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(playerId))
                return;

            if (_activeGames.TryGetValue(gameId, out var game))
            {
                await game.RemovePlayerAsync(playerId);
                
                // If no players remain, clean up the game
                if (game.GetPlayerCount() == 0)
                {
                    await game.CleanupAsync();
                    _activeGames.TryRemove(gameId, out _);
                    ActiveGamesCount.Dec();
                }
            }
        }

        private async Task<GameInstance> GetOrCreateGameInstance(string gameId)
        {
            if (_activeGames.TryGetValue(gameId, out var existingGame))
                return existingGame;

            // Check if we've reached the maximum games per instance
            if (_activeGames.Count >= _networkConfig.MaxGamesPerInstance)
            {
                _logger.LogWarning("Maximum games per instance reached ({Max})", _networkConfig.MaxGamesPerInstance);
                return null;
            }

            try
            {
                var newGame = new GameInstance(gameId, _eventStore, _stateCache, _pythonEngine, _logger);
                await newGame.InitializeAsync();
                
                if (_activeGames.TryAdd(gameId, newGame))
                {
                    ActiveGamesCount.Inc();
                    _logger.LogInformation("Created new game instance: {GameId}", gameId);
                    return newGame;
                }
                else
                {
                    await newGame.CleanupAsync();
                    return _activeGames.GetValueOrDefault(gameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create game instance for {GameId}", gameId);
                return null;
            }
        }

        private async Task UpdateServerMetrics()
        {
            try
            {
                ActiveGamesCount.Set(_activeGames.Count);
                ConnectedPlayersCount.Set(_playerConnections.Count);
                
                // Update health status in cache
                var healthData = new ServerHealthData
                {
                    ActiveGames = _activeGames.Count,
                    ConnectedPlayers = _playerConnections.Count,
                    MemoryUsage = GC.GetTotalMemory(false),
                    Timestamp = DateTime.UtcNow
                };
                
                await _stateCache.SetHealthDataAsync(Environment.MachineName, healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating server metrics");
            }
        }

        private async Task CleanupExpiredGames()
        {
            var expiredGames = new List<string>();
            var cutoffTime = DateTime.UtcNow.AddHours(-1); // Games idle for 1 hour

            foreach (var kvp in _activeGames)
            {
                if (kvp.Value.LastActivity < cutoffTime && kvp.Value.GetPlayerCount() == 0)
                {
                    expiredGames.Add(kvp.Key);
                }
            }

            foreach (var gameId in expiredGames)
            {
                if (_activeGames.TryRemove(gameId, out var game))
                {
                    try
                    {
                        await game.CleanupAsync();
                        ActiveGamesCount.Dec();
                        _logger.LogInformation("Cleaned up expired game: {GameId}", gameId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up expired game {GameId}", gameId);
                    }
                }
            }
        }

        private async Task StopServerAsync()
        {
            _logger.LogInformation("Stopping Mirror Game Server");
            
            try
            {
                // Disconnect all players
                foreach (var connection in _playerConnections.Values)
                {
                    connection.Connection?.Disconnect();
                }

                // Clean up all active games
                foreach (var game in _activeGames.Values)
                {
                    await game.CleanupAsync();
                }

                // Stop Mirror networking
                NetworkServer.Shutdown();
                
                _logger.LogInformation("Mirror Game Server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping game server");
            }
        }

        // Health check endpoint data
        public ServerStatus GetServerStatus()
        {
            return new ServerStatus
            {
                IsHealthy = NetworkServer.active,
                ActiveGames = _activeGames.Count,
                ConnectedPlayers = _playerConnections.Count,
                MemoryUsage = GC.GetTotalMemory(false),
                Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                LastUpdate = DateTime.UtcNow
            };
        }
    }

    // Configuration classes and interfaces
    public class NetworkConfig
    {
        public string Protocol { get; set; } = "WebSocket";
        public int GamePort { get; set; } = 7777;
        public int MaxConnections { get; set; } = 100;
        public int MaxGamesPerInstance { get; set; } = 50;
        public int MaxMessageSize { get; set; } = 16384; // 16KB
        public int SendRate { get; set; } = 10; // Hz
        public float TickInterval { get; set; } = 0.1f; // 100ms
        public float ConnectionTimeout { get; set; } = 10f;
        public float ResponseTimeout { get; set; } = 5f;
        public float KeepAliveInterval { get; set; } = 1f;
    }

    public class PlayerConnection
    {
        public int ConnectionId { get; set; }
        public NetworkConnectionToClient Connection { get; set; }
        public string PlayerId { get; set; }
        public string CurrentGameId { get; set; }
        public string AllowedGameId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    public class ServerHealthData
    {
        public int ActiveGames { get; set; }
        public int ConnectedPlayers { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ServerStatus
    {
        public bool IsHealthy { get; set; }
        public int ActiveGames { get; set; }
        public int ConnectedPlayers { get; set; }
        public long MemoryUsage { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
