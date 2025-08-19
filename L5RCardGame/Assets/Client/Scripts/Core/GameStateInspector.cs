using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mirror;
using MessagePack;

namespace UnityCardGame.Server.Debugging
{
    /// <summary>
    /// Comprehensive game state inspection and debugging system for Unity Card Game Server
    /// Provides real-time monitoring, state validation, performance analysis, and troubleshooting tools
    /// </summary>
    public class GameStateInspector
    {
        private readonly ILogger<GameStateInspector> _logger;
        private readonly IConfiguration _config;
        private readonly IEventStore _eventStore;
        private readonly IRedisCache _stateCache;
        
        // Inspection systems
        private readonly StateValidator _stateValidator;
        private readonly PerformanceProfiler _performanceProfiler;
        private readonly NetworkInspector _networkInspector;
        private readonly MemoryAnalyzer _memoryAnalyzer;
        private readonly EventTracker _eventTracker;
        
        // State tracking
        private readonly ConcurrentDictionary<string, GameStateSnapshot> _gameSnapshots;
        private readonly ConcurrentDictionary<string, List<StateChange>> _stateHistory;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _gameMetrics;
        
        // Configuration
        private readonly InspectorConfig _inspectorConfig;
        private readonly object _snapshotLock = new object();
        
        public GameStateInspector(
            ILogger<GameStateInspector> logger,
            IConfiguration config,
            IEventStore eventStore,
            IRedisCache stateCache)
        {
            _logger = logger;
            _config = config;
            _eventStore = eventStore;
            _stateCache = stateCache;
            
            _stateValidator = new StateValidator(logger);
            _performanceProfiler = new PerformanceProfiler();
            _networkInspector = new NetworkInspector();
            _memoryAnalyzer = new MemoryAnalyzer();
            _eventTracker = new EventTracker();
            
            _gameSnapshots = new ConcurrentDictionary<string, GameStateSnapshot>();
            _stateHistory = new ConcurrentDictionary<string, List<StateChange>>();
            _gameMetrics = new ConcurrentDictionary<string, PerformanceMetrics>();
            
            _inspectorConfig = new InspectorConfig(config);
        }

        #region Public API

        /// <summary>
        /// Capture a comprehensive snapshot of a game's current state
        /// </summary>
        public async Task<GameStateSnapshot> CaptureGameSnapshotAsync(string gameId)
        {
            using var activity = _performanceProfiler.StartActivity("CaptureGameSnapshot");
            
            try
            {
                var gameState = await _stateCache.GetGameStateAsync(gameId);
                if (gameState == null)
                {
                    _logger.LogWarning("Game state not found for game {GameId}", gameId);
                    return null;
                }

                var snapshot = new GameStateSnapshot
                {
                    GameId = gameId,
                    Timestamp = DateTime.UtcNow,
                    GameState = DeepClone(gameState),
                    PlayerStates = CapturePlayerStates(gameState),
                    BoardState = CaptureBoardState(gameState),
                    NetworkMetrics = await _networkInspector.GetGameNetworkMetricsAsync(gameId),
                    MemoryUsage = _memoryAnalyzer.GetGameMemoryUsage(gameId),
                    ActiveConnections = GetActiveConnections(gameId),
                    ValidationResult = await _stateValidator.ValidateGameStateAsync(gameState),
                    EventHistory = await GetRecentEvents(gameId, TimeSpan.FromMinutes(5))
                };

                // Store snapshot for analysis
                _gameSnapshots.AddOrUpdate(gameId, snapshot, (key, old) => snapshot);
                
                // Track state changes
                await TrackStateChange(gameId, snapshot);
                
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture game snapshot for {GameId}", gameId);
                return null;
            }
        }

        /// <summary>
        /// Generate comprehensive diagnostic report for a game
        /// </summary>
        public async Task<GameDiagnosticReport> GenerateDiagnosticReportAsync(string gameId)
        {
            try
            {
                var snapshot = await CaptureGameSnapshotAsync(gameId);
                if (snapshot == null)
                {
                    return new GameDiagnosticReport
                    {
                        GameId = gameId,
                        Status = DiagnosticStatus.GameNotFound,
                        Message = "Game state could not be retrieved"
                    };
                }

                var report = new GameDiagnosticReport
                {
                    GameId = gameId,
                    Timestamp = DateTime.UtcNow,
                    Status = DiagnosticStatus.Healthy,
                    Snapshot = snapshot,
                    PerformanceAnalysis = await AnalyzePerformance(gameId),
                    StateValidation = snapshot.ValidationResult,
                    NetworkAnalysis = await AnalyzeNetworkHealth(gameId),
                    MemoryAnalysis = await AnalyzeMemoryUsage(gameId),
                    RecommendedActions = GenerateRecommendations(snapshot),
                    HistoricalComparison = await CompareWithHistory(gameId, snapshot)
                };

                // Determine overall health status
                report.Status = DetermineOverallStatus(report);
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate diagnostic report for {GameId}", gameId);
                return new GameDiagnosticReport
                {
                    GameId = gameId,
                    Status = DiagnosticStatus.Error,
                    Message = $"Diagnostic generation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Inspect specific game state components
        /// </summary>
        public async Task<ComponentInspectionResult> InspectGameComponentAsync(string gameId, string componentType)
        {
            var gameState = await _stateCache.GetGameStateAsync(gameId);
            if (gameState == null)
            {
                return new ComponentInspectionResult
                {
                    ComponentType = componentType,
                    Status = InspectionStatus.NotFound,
                    Message = "Game state not found"
                };
            }

            return componentType.ToLower() switch
            {
                "players" => InspectPlayers(gameState),
                "board" => InspectBoard(gameState),
                "deck" => InspectDecks(gameState),
                "hand" => InspectHands(gameState),
                "graveyard" => InspectGraveyards(gameState),
                "effects" => InspectActiveEffects(gameState),
                "turn" => InspectTurnState(gameState),
                "network" => await InspectNetworkState(gameId),
                "performance" => InspectPerformance(gameId),
                _ => new ComponentInspectionResult
                {
                    ComponentType = componentType,
                    Status = InspectionStatus.UnsupportedComponent,
                    Message = $"Component type '{componentType}' is not supported"
                }
            };
        }

        /// <summary>
        /// Search for specific patterns or issues across all games
        /// </summary>
        public async Task<List<GameIssue>> SearchForIssuesAsync(IssueSearchCriteria criteria)
        {
            var issues = new List<GameIssue>();
            
            foreach (var gameId in _gameSnapshots.Keys)
            {
                var snapshot = _gameSnapshots[gameId];
                var gameIssues = await AnalyzeGameForIssues(snapshot, criteria);
                issues.AddRange(gameIssues);
            }

            return issues.OrderByDescending(i => i.Severity).ToList();
        }

        /// <summary>
        /// Get real-time metrics for server monitoring dashboards
        /// </summary>
        public ServerMetrics GetServerMetrics()
        {
            var metrics = new ServerMetrics
            {
                Timestamp = DateTime.UtcNow,
                ActiveGamesCount = _gameSnapshots.Count,
                TotalMemoryUsage = GC.GetTotalMemory(false),
                NetworkConnections = _networkInspector.GetTotalConnections(),
                AverageResponseTime = _performanceProfiler.GetAverageResponseTime(),
                ErrorRate = _eventTracker.GetErrorRate(),
                GameMetrics = _gameMetrics.Values.ToList()
            };

            // Add detailed breakdown
            metrics.GamesByPhase = GetGamesByPhase();
            metrics.ResourceUtilization = GetResourceUtilization();
            metrics.PerformanceBreakdown = _performanceProfiler.GetBreakdown();

            return metrics;
        }

        #endregion

        #region State Capture Methods

        private List<PlayerStateInfo> CapturePlayerStates(GameState gameState)
        {
            return gameState.Players.Select(player => new PlayerStateInfo
            {
                PlayerId = player.PlayerId,
                DisplayName = player.DisplayName,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                CurrentMana = player.CurrentMana,
                MaxMana = player.MaxMana,
                HandSize = player.Hand.Count,
                DeckSize = player.Deck.Count,
                BoardSize = player.Board.Count,
                GraveyardSize = player.Graveyard.Count,
                Status = player.Status,
                Resources = new Dictionary<string, int>(player.Resources),
                ActiveEffects = ExtractPlayerEffects(player),
                ConnectionStatus = GetPlayerConnectionStatus(player.PlayerId)
            }).ToList();
        }

        private BoardStateInfo CaptureBoardState(GameState gameState)
        {
            return new BoardStateInfo
            {
                GlobalEffects = new Dictionary<string, object>(gameState.Board.GlobalEffects),
                PlayerBoards = gameState.Board.PlayerBoards.Select((board, index) => 
                    new PlayerBoardInfo
                    {
                        PlayerId = gameState.Players[index].PlayerId,
                        Cards = board.Select(card => new CardStateInfo
                        {
                            CardId = card.CardId,
                            Name = card.Name,
                            Type = card.Type,
                            Attack = card.Attack,
                            Health = card.Health,
                            MaxHealth = card.MaxHealth,
                            CanAttack = card.CanAttack,
                            HasSummoned = card.HasSummoned,
                            Abilities = new List<string>(card.Abilities),
                            Properties = new Dictionary<string, object>(card.Properties)
                        }).ToList()
                    }).ToList(),
                TotalCardsOnBoard = gameState.Board.PlayerBoards.Sum(board => board.Count),
                MaxBoardSize = gameState.Board.MaxBoardSize
            };
        }

        private async Task<List<GameEvent>> GetRecentEvents(string gameId, TimeSpan timeWindow)
        {
            var fromTime = DateTime.UtcNow - timeWindow;
            return await _eventStore.GetEventsAsync(gameId, fromTime);
        }

        private List<string> GetActiveConnections(string gameId)
        {
            // This would integrate with your NetworkServer to get actual connection info
            return _networkInspector.GetActiveConnectionsForGame(gameId);
        }

        #endregion

        #region Analysis Methods

        private async Task<PerformanceAnalysis> AnalyzePerformance(string gameId)
        {
            var metrics = _gameMetrics.GetValueOrDefault(gameId, new PerformanceMetrics());
            
            return new PerformanceAnalysis
            {
                AverageActionTime = metrics.AverageActionProcessingTime,
                PeakActionTime = metrics.PeakActionProcessingTime,
                ActionsPerSecond = metrics.ActionsPerSecond,
                MemoryTrend = metrics.MemoryUsageHistory.ToList(),
                CpuUsage = metrics.CpuUsagePercentage,
                NetworkLatency = await _networkInspector.GetAverageLatencyAsync(gameId),
                ThroughputMbps = await _networkInspector.GetThroughputAsync(gameId),
                BottleneckAnalysis = IdentifyBottlenecks(metrics)
            };
        }

        private async Task<NetworkAnalysis> AnalyzeNetworkHealth(string gameId)
        {
            return new NetworkAnalysis
            {
                AverageLatency = await _networkInspector.GetAverageLatencyAsync(gameId),
                PacketLoss = await _networkInspector.GetPacketLossAsync(gameId),
                Throughput = await _networkInspector.GetThroughputAsync(gameId),
                ConnectionQuality = await _networkInspector.GetConnectionQualityAsync(gameId),
                MessageQueueSize = _networkInspector.GetMessageQueueSize(gameId),
                RecentDisconnections = _networkInspector.GetRecentDisconnections(gameId),
                BandwidthUtilization = await _networkInspector.GetBandwidthUtilizationAsync(gameId)
            };
        }

        private async Task<MemoryAnalysis> AnalyzeMemoryUsage(string gameId)
        {
            var usage = _memoryAnalyzer.GetGameMemoryUsage(gameId);
            
            return new MemoryAnalysis
            {
                CurrentUsageMB = usage.CurrentUsageMB,
                PeakUsageMB = usage.PeakUsageMB,
                AverageUsageMB = usage.AverageUsageMB,
                GCPressure = usage.GCPressure,
                LargeObjectHeapSize = usage.LargeObjectHeapSize,
                MemoryLeaks = DetectMemoryLeaks(gameId),
                RecommendedCleanup = SuggestMemoryCleanup(usage)
            };
        }

        private List<string> GenerateRecommendations(GameStateSnapshot snapshot)
        {
            var recommendations = new List<string>();

            // Performance recommendations
            if (snapshot.MemoryUsage.CurrentUsageMB > 50)
            {
                recommendations.Add("Consider memory cleanup - usage above 50MB");
            }

            // State validation recommendations
            if (snapshot.ValidationResult.HasErrors)
            {
                recommendations.Add($"Fix {snapshot.ValidationResult.Errors.Count} state validation errors");
            }

            // Network recommendations
            if (snapshot.NetworkMetrics.AverageLatency > 100)
            {
                recommendations.Add("High network latency detected - check connection quality");
            }

            // Game state recommendations
            if (snapshot.GameState.TurnTimeRemaining < 5)
            {
                recommendations.Add("Turn time running low - consider turn extension");
            }

            return recommendations;
        }

        private async Task<HistoricalComparison> CompareWithHistory(string gameId, GameStateSnapshot currentSnapshot)
        {
            var history = _stateHistory.GetValueOrDefault(gameId, new List<StateChange>());
            var recentHistory = history.TakeLast(10).ToList();

            return new HistoricalComparison
            {
                TrendDirection = AnalyzeTrend(recentHistory),
                PerformanceChange = CalculatePerformanceChange(recentHistory),
                MemoryChange = CalculateMemoryChange(recentHistory),
                StabilityScore = CalculateStabilityScore(recentHistory),
                AnomaliesDetected = DetectAnomalies(currentSnapshot, recentHistory)
            };
        }

        #endregion

        #region Component Inspection Methods

        private ComponentInspectionResult InspectPlayers(GameState gameState)
        {
            var analysis = new StringBuilder();
            analysis.AppendLine($"=== PLAYER INSPECTION ({gameState.Players.Count} players) ===");

            foreach (var player in gameState.Players)
            {
                analysis.AppendLine($"\nPlayer: {player.DisplayName} ({player.PlayerId})");
                analysis.AppendLine($"  Health: {player.Health}/{player.MaxHealth}");
                analysis.AppendLine($"  Mana: {player.CurrentMana}/{player.MaxMana}");
                analysis.AppendLine($"  Cards - Hand: {player.Hand.Count}, Board: {player.Board.Count}, Deck: {player.Deck.Count}");
                analysis.AppendLine($"  Status: {player.Status}");
                
                if (player.Resources.Any())
                {
                    analysis.AppendLine($"  Resources: {string.Join(", ", player.Resources.Select(r => $"{r.Key}:{r.Value}"))}");
                }
            }

            return new ComponentInspectionResult
            {
                ComponentType = "players",
                Status = InspectionStatus.Success,
                Details = analysis.ToString(),
                Metrics = new Dictionary<string, object>
                {
                    ["PlayerCount"] = gameState.Players.Count,
                    ["AverageHealth"] = gameState.Players.Average(p => p.Health),
                    ["TotalCardsInPlay"] = gameState.Players.Sum(p => p.Hand.Count + p.Board.Count)
                }
            };
        }

        private ComponentInspectionResult InspectBoard(GameState gameState)
        {
            var analysis = new StringBuilder();
            analysis.AppendLine("=== BOARD INSPECTION ===");

            var totalCards = gameState.Board.PlayerBoards.Sum(board => board.Count);
            analysis.AppendLine($"Total cards on board: {totalCards}");
            analysis.AppendLine($"Max board size: {gameState.Board.MaxBoardSize}");

            for (int i = 0; i < gameState.Board.PlayerBoards.Count; i++)
            {
                var board = gameState.Board.PlayerBoards[i];
                var player = gameState.Players[i];
                
                analysis.AppendLine($"\n{player.DisplayName}'s Board ({board.Count} cards):");
                foreach (var card in board)
                {
                    analysis.AppendLine($"  - {card.Name} ({card.Attack}/{card.Health}) {(card.CanAttack ? "[Ready]" : "[Exhausted]")}");
                }
            }

            if (gameState.Board.GlobalEffects.Any())
            {
                analysis.AppendLine($"\nGlobal Effects ({gameState.Board.GlobalEffects.Count}):");
                foreach (var effect in gameState.Board.GlobalEffects)
                {
                    analysis.AppendLine($"  - {effect.Key}: {effect.Value}");
                }
            }

            return new ComponentInspectionResult
            {
                ComponentType = "board",
                Status = InspectionStatus.Success,
                Details = analysis.ToString(),
                Metrics = new Dictionary<string, object>
                {
                    ["TotalCardsOnBoard"] = totalCards,
                    ["BoardUtilization"] = (float)totalCards / (gameState.Board.MaxBoardSize * gameState.Players.Count),
                    ["GlobalEffectsCount"] = gameState.Board.GlobalEffects.Count
                }
            };
        }

        private async Task<ComponentInspectionResult> InspectNetworkState(string gameId)
        {
            var networkMetrics = await _networkInspector.GetGameNetworkMetricsAsync(gameId);
            var analysis = new StringBuilder();
            
            analysis.AppendLine("=== NETWORK INSPECTION ===");
            analysis.AppendLine($"Average Latency: {networkMetrics.AverageLatency}ms");
            analysis.AppendLine($"Packet Loss: {networkMetrics.PacketLoss:P2}");
            analysis.AppendLine($"Throughput: {networkMetrics.Throughput:F2} Mbps");
            analysis.AppendLine($"Active Connections: {networkMetrics.ActiveConnections}");
            analysis.AppendLine($"Message Queue Size: {networkMetrics.MessageQueueSize}");

            var status = networkMetrics.AverageLatency > 200 || networkMetrics.PacketLoss > 0.05 
                ? InspectionStatus.Warning 
                : InspectionStatus.Success;

            return new ComponentInspectionResult
            {
                ComponentType = "network",
                Status = status,
                Details = analysis.ToString(),
                Metrics = new Dictionary<string, object>
                {
                    ["AverageLatency"] = networkMetrics.AverageLatency,
                    ["PacketLoss"] = networkMetrics.PacketLoss,
                    ["Throughput"] = networkMetrics.Throughput,
                    ["ConnectionQuality"] = networkMetrics.AverageLatency < 100 ? "Excellent" : 
                                          networkMetrics.AverageLatency < 200 ? "Good" : "Poor"
                }
            };
        }

        #endregion

        #region Utility Methods

        private T DeepClone<T>(T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(json);
        }

        private async Task TrackStateChange(string gameId, GameStateSnapshot snapshot)
        {
            var stateChange = new StateChange
            {
                Timestamp = snapshot.Timestamp,
                MemoryUsage = snapshot.MemoryUsage.CurrentUsageMB,
                PlayerCount = snapshot.PlayerStates.Count,
                TurnNumber = snapshot.GameState.TurnNumber,
                Phase = snapshot.GameState.Phase.ToString()
            };

            _stateHistory.AddOrUpdate(gameId, 
                new List<StateChange> { stateChange },
                (key, existing) =>
                {
                    existing.Add(stateChange);
                    if (existing.Count > 100) // Keep last 100 changes
                        existing.RemoveAt(0);
                    return existing;
                });
        }

        private DiagnosticStatus DetermineOverallStatus(GameDiagnosticReport report)
        {
            if (report.StateValidation.HasErrors)
                return DiagnosticStatus.Error;
            
            if (report.PerformanceAnalysis.AverageActionTime > 1000 || 
                report.NetworkAnalysis.AverageLatency > 500 ||
                report.MemoryAnalysis.CurrentUsageMB > 100)
                return DiagnosticStatus.Warning;
            
            return DiagnosticStatus.Healthy;
        }

        private List<string> ExtractPlayerEffects(Player player)
        {
            var effects = new List<string>();
            
            // Extract effects from cards
            foreach (var card in player.Board)
            {
                effects.AddRange(card.Abilities);
            }
            
            return effects.Distinct().ToList();
        }

        private ConnectionStatus GetPlayerConnectionStatus(string playerId)
        {
            return _networkInspector.GetPlayerConnectionStatus(playerId);
        }

        private Dictionary<string, int> GetGamesByPhase()
        {
            return _gameSnapshots.Values
                .GroupBy(s => s.GameState.Phase.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private ResourceUtilization GetResourceUtilization()
        {
            return new ResourceUtilization
            {
                CpuPercentage = _performanceProfiler.GetCurrentCpuUsage(),
                MemoryPercentage = _memoryAnalyzer.GetMemoryUtilizationPercentage(),
                NetworkPercentage = _networkInspector.GetNetworkUtilizationPercentage(),
                DiskIOPercentage = _performanceProfiler.GetDiskIOPercentage()
            };
        }

        #endregion
    }

    #region Data Structures

    public class GameStateSnapshot
    {
        public string GameId { get; set; }
        public DateTime Timestamp { get; set; }
        public GameState GameState { get; set; }
        public List<PlayerStateInfo> PlayerStates { get; set; }
        public BoardStateInfo BoardState { get; set; }
        public NetworkMetrics NetworkMetrics { get; set; }
        public MemoryUsage MemoryUsage { get; set; }
        public List<string> ActiveConnections { get; set; }
        public ValidationResult ValidationResult { get; set; }
        public List<GameEvent> EventHistory { get; set; }
    }

    public class PlayerStateInfo
    {
        public string PlayerId { get; set; }
        public string DisplayName { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public int HandSize { get; set; }
        public int DeckSize { get; set; }
        public int BoardSize { get; set; }
        public int GraveyardSize { get; set; }
        public PlayerStatus Status { get; set; }
        public Dictionary<string, int> Resources { get; set; }
        public List<string> ActiveEffects { get; set; }
        public ConnectionStatus ConnectionStatus { get; set; }
    }

    public class BoardStateInfo
    {
        public Dictionary<string, object> GlobalEffects { get; set; }
        public List<PlayerBoardInfo> PlayerBoards { get; set; }
        public int TotalCardsOnBoard { get; set; }
        public int MaxBoardSize { get; set; }
    }

    public class PlayerBoardInfo
    {
        public string PlayerId { get; set; }
        public List<CardStateInfo> Cards { get; set; }
    }

    public class CardStateInfo
    {
        public string CardId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool CanAttack { get; set; }
        public bool HasSummoned { get; set; }
        public List<string> Abilities { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class GameDiagnosticReport
    {
        public string GameId { get; set; }
        public DateTime Timestamp { get; set; }
        public DiagnosticStatus Status { get; set; }
        public string Message { get; set; }
        public GameStateSnapshot Snapshot { get; set; }
        public PerformanceAnalysis PerformanceAnalysis { get; set; }
        public ValidationResult StateValidation { get; set; }
        public NetworkAnalysis NetworkAnalysis { get; set; }
        public MemoryAnalysis MemoryAnalysis { get; set; }
        public List<string> RecommendedActions { get; set; }
        public HistoricalComparison HistoricalComparison { get; set; }
    }

    public class ComponentInspectionResult
    {
        public string ComponentType { get; set; }
        public InspectionStatus Status { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public Dictionary<string, object> Metrics { get; set; }
    }

    public class ServerMetrics
    {
        public DateTime Timestamp { get; set; }
        public int ActiveGamesCount { get; set; }
        public long TotalMemoryUsage { get; set; }
        public int NetworkConnections { get; set; }
        public float AverageResponseTime { get; set; }
        public float ErrorRate { get; set; }
        public List<PerformanceMetrics> GameMetrics { get; set; }
        public Dictionary<string, int> GamesByPhase { get; set; }
        public ResourceUtilization ResourceUtilization { get; set; }
        public Dictionary<string, float> PerformanceBreakdown { get; set; }
    }

    public enum DiagnosticStatus
    {
        Healthy,
        Warning,
        Error,
        GameNotFound
    }

    public enum InspectionStatus
    {
        Success,
        Warning,
        Error,
        NotFound,
        UnsupportedComponent
    }

    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
        Unstable,
        Unknown
    }

    #endregion

    #region Supporting Systems

    public class InspectorConfig
    {
        public bool EnableRealTimeMonitoring { get; set; } = true;
        public bool EnablePerformanceTracing { get; set; } = true;
        public bool EnableMemoryTracking { get; set; } = true;
        public bool EnableNetworkAnalysis { get; set; } = true;
        public int MaxSnapshotsPerGame { get; set; } = 50;
        public TimeSpan SnapshotRetention { get; set; } = TimeSpan.FromHours(24);
        public TimeSpan MetricsUpdateInterval { get; set; } = TimeSpan.FromSeconds(30);

        public InspectorConfig(IConfiguration config)
        {
            config.GetSection("GameStateInspector").Bind(this);
        }
    }

    // Additional supporting classes would be implemented here:
    // - StateValidator
    // - PerformanceProfiler  
    // - NetworkInspector
    // - MemoryAnalyzer
    // - EventTracker
    // - ValidationResult
    // - PerformanceAnalysis
    // - NetworkAnalysis
    // - MemoryAnalysis
    // - HistoricalComparison
    // - ResourceUtilization
    // - PerformanceMetrics
    // - NetworkMetrics
    // - MemoryUsage
    // - StateChange
    // - GameIssue
    // - IssueSearchCriteria

    #endregion
}
