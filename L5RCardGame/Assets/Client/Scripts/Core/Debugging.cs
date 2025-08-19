using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace L5RGame.Debugging
{
    /// <summary>
    /// Comprehensive debugging system for L5R card game.
    /// Provides deep insights into game state, performance monitoring, and troubleshooting tools.
    /// </summary>
    public class L5RDebugger : MonoBehaviour
    {
        [Header("Debug Configuration")]
        public bool enableDebugMode = true;
        public bool enablePerformanceMonitoring = true;
        public bool enableAutoLogging = false;
        public bool enableMemoryTracking = true;
        public LogLevel minimumLogLevel = LogLevel.Info;

        [Header("Debug UI")]
        public bool showDebugUI = true;
        public KeyCode debugToggleKey = KeyCode.F12;
        public KeyCode performanceToggleKey = KeyCode.F11;
        public KeyCode gameStateKey = KeyCode.F10;

        [Header("Log Settings")]
        public bool saveLogsToFile = true;
        public int maxLogEntries = 1000;
        public string logFilePath = "Logs/L5R_Debug.log";

        // Core systems
        private Game game;
        private DebugConsole debugConsole;
        private PerformanceMonitor performanceMonitor;
        private GameStateInspector gameStateInspector;
        private MemoryTracker memoryTracker;
        private NetworkDebugger networkDebugger;

        // Debug state
        private List<DebugLogEntry> logEntries = new List<DebugLogEntry>();
        private Dictionary<string, object> debugVariables = new Dictionary<string, object>();
        private bool debugUIVisible = false;
        private Vector2 debugScrollPosition;

        void Awake()
        {
            InitializeDebugSystems();
        }

        void Start()
        {
            game = FindObjectOfType<Game>();
            if (game != null)
            {
                RegisterGameEvents();
            }

            LogInfo("L5R Debugger initialized successfully");
        }

        void Update()
        {
            HandleDebugInput();
            
            if (enablePerformanceMonitoring)
            {
                performanceMonitor.Update();
            }

            if (enableMemoryTracking)
            {
                memoryTracker.Update();
            }
        }

        void OnGUI()
        {
            if (showDebugUI && debugUIVisible)
            {
                DrawDebugUI();
            }
        }

        #region Initialization

        /// <summary>
        /// Initialize all debug subsystems
        /// </summary>
        private void InitializeDebugSystems()
        {
            debugConsole = new DebugConsole(this);
            performanceMonitor = new PerformanceMonitor();
            gameStateInspector = new GameStateInspector();
            memoryTracker = new MemoryTracker();
            networkDebugger = new NetworkDebugger();

            // Register debug commands
            RegisterDebugCommands();

            // Setup log file
            if (saveLogsToFile)
            {
                SetupLogFile();
            }
        }

        /// <summary>
        /// Register debug commands
        /// </summary>
        private void RegisterDebugCommands()
        {
            debugConsole.RegisterCommand("help", "Show all available commands", ShowHelp);
            debugConsole.RegisterCommand("gamestate", "Show current game state", () => LogInfo(GetGameStateInfo()));
            debugConsole.RegisterCommand("players", "Show player information", () => LogInfo(GetPlayersInfo()));
            debugConsole.RegisterCommand("cards", "Show cards in play", () => LogInfo(GetCardsInfo()));
            debugConsole.RegisterCommand("effects", "Show active effects", () => LogInfo(GetEffectsInfo()));
            debugConsole.RegisterCommand("rings", "Show ring states", () => LogInfo(GetRingsInfo()));
            debugConsole.RegisterCommand("performance", "Show performance metrics", () => LogInfo(performanceMonitor.GetReport()));
            debugConsole.RegisterCommand("memory", "Show memory usage", () => LogInfo(memoryTracker.GetReport()));
            debugConsole.RegisterCommand("clear", "Clear debug log", ClearLog);
            debugConsole.RegisterCommand("save", "Save debug log to file", SaveLogToFile);
            debugConsole.RegisterCommand("test", "Run system tests", RunSystemTests);
        }

        /// <summary>
        /// Register game event listeners
        /// </summary>
        private void RegisterGameEvents()
        {
            game.OnGameStateChanged += OnGameStateChanged;
            game.OnPhaseChanged += OnPhaseChanged;
            game.OnCardPlayed += OnCardPlayed;
            game.OnConflictStarted += OnConflictStarted;
            game.OnConflictFinished += OnConflictFinished;
        }

        #endregion

        #region Logging System

        /// <summary>
        /// Log levels for filtering
        /// </summary>
        public enum LogLevel
        {
            Verbose = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Critical = 5
        }

        /// <summary>
        /// Debug log entry
        /// </summary>
        [System.Serializable]
        public class DebugLogEntry
        {
            public DateTime timestamp;
            public LogLevel level;
            public string category;
            public string message;
            public string stackTrace;

            public DebugLogEntry(LogLevel level, string category, string message, string stackTrace = null)
            {
                this.timestamp = DateTime.Now;
                this.level = level;
                this.category = category;
                this.message = message;
                this.stackTrace = stackTrace;
            }

            public override string ToString()
            {
                return $"[{timestamp:HH:mm:ss}] [{level}] [{category}] {message}";
            }
        }

        /// <summary>
        /// Log a message with specified level
        /// </summary>
        public void Log(LogLevel level, string category, string message, bool includeStackTrace = false)
        {
            if (!enableDebugMode || level < minimumLogLevel)
                return;

            string stackTrace = includeStackTrace ? Environment.StackTrace : null;
            var entry = new DebugLogEntry(level, category, message, stackTrace);
            
            logEntries.Add(entry);

            // Trim log if too large
            if (logEntries.Count > maxLogEntries)
            {
                logEntries.RemoveAt(0);
            }

            // Unity console output
            switch (level)
            {
                case LogLevel.Error:
                case LogLevel.Critical:
                    UnityEngine.Debug.LogError($"[{category}] {message}");
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning($"[{category}] {message}");
                    break;
                default:
                    UnityEngine.Debug.Log($"[{category}] {message}");
                    break;
            }

            // Auto-save to file
            if (enableAutoLogging && saveLogsToFile)
            {
                AppendToLogFile(entry);
            }
        }

        public void LogVerbose(string message, string category = "General") => Log(LogLevel.Verbose, category, message);
        public void LogDebug(string message, string category = "General") => Log(LogLevel.Debug, category, message);
        public void LogInfo(string message, string category = "General") => Log(LogLevel.Info, category, message);
        public void LogWarning(string message, string category = "General") => Log(LogLevel.Warning, category, message);
        public void LogError(string message, string category = "General") => Log(LogLevel.Error, category, message, true);
        public void LogCritical(string message, string category = "General") => Log(LogLevel.Critical, category, message, true);

        #endregion

        #region Game State Inspection

        /// <summary>
        /// Get comprehensive game state information
        /// </summary>
        public string GetGameStateInfo()
        {
            if (game == null) return "Game not initialized";

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME STATE ===");
            sb.AppendLine($"Current Phase: {game.currentPhase}");
            sb.AppendLine($"Current Player: {game.currentPlayer?.name ?? "None"}");
            sb.AppendLine($"Round: {game.roundNumber}");
            sb.AppendLine($"Manual Mode: {game.manualMode}");
            
            if (game.currentConflict != null)
            {
                sb.AppendLine($"Current Conflict: {game.currentConflict.conflictType} at {game.currentConflict.ring?.element}");
                sb.AppendLine($"Attackers: {game.currentConflict.attackers.Count}");
                sb.AppendLine($"Defenders: {game.currentConflict.defenders.Count}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get detailed player information
        /// </summary>
        public string GetPlayersInfo()
        {
            if (game == null) return "Game not initialized";

            var sb = new StringBuilder();
            sb.AppendLine("=== PLAYERS ===");

            foreach (var player in game.GetPlayers())
            {
                sb.AppendLine($"\n--- {player.name} ---");
                sb.AppendLine($"Clan: {player.clan}");
                sb.AppendLine($"Honor: {player.honor}");
                sb.AppendLine($"Fate: {player.fate}");
                sb.AppendLine($"Cards in Hand: {player.hand.Count}");
                sb.AppendLine($"Cards in Play: {player.cardsInPlay.Count}");
                sb.AppendLine($"Dynasty Deck: {player.dynastyDeck.Count}");
                sb.AppendLine($"Conflict Deck: {player.conflictDeck.Count}");
                
                if (player.clock != null)
                {
                    sb.AppendLine($"Clock: {player.GetFormattedTimeRemaining()}");
                }

                if (player.costReducerManager != null)
                {
                    var activeReducers = player.costReducerManager.GetActiveReducers();
                    sb.AppendLine($"Cost Reducers: {activeReducers.Count}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get cards in play information
        /// </summary>
        public string GetCardsInfo()
        {
            if (game == null) return "Game not initialized";

            var sb = new StringBuilder();
            sb.AppendLine("=== CARDS IN PLAY ===");

            var allCards = game.GetAllCards().Where(c => c.location == Locations.PlayArea).ToList();
            
            foreach (var player in game.GetPlayers())
            {
                var playerCards = allCards.Where(c => c.controller == player).ToList();
                sb.AppendLine($"\n--- {player.name} ({playerCards.Count} cards) ---");
                
                foreach (var card in playerCards)
                {
                    sb.AppendLine($"  {card.name} ({card.GetCardType()}) - " +
                                 $"Mil: {card.militarySkill}, Pol: {card.politicalSkill}, " +
                                 $"Fate: {card.GetFate()}{(card.isBowed ? " [BOWED]" : "")}" +
                                 $"{(card.isHonored ? " [HONORED]" : "")}{(card.isDishonored ? " [DISHONORED]" : "")}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get active effects information
        /// </summary>
        public string GetEffectsInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ACTIVE EFFECTS ===");

            // This would require access to an EffectEngine
            // For now, we'll check card effects
            var allCards = game?.GetAllCards() ?? new List<BaseCard>();
            int effectCount = 0;

            foreach (var card in allCards)
            {
                if (card.effects != null && card.effects.Count > 0)
                {
                    sb.AppendLine($"\n{card.name}:");
                    foreach (var effect in card.effects)
                    {
                        sb.AppendLine($"  - {effect}");
                        effectCount++;
                    }
                }
            }

            sb.Insert(0, $"Total Effects: {effectCount}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Get rings information
        /// </summary>
        public string GetRingsInfo()
        {
            if (game?.rings == null) return "Rings not initialized";

            var sb = new StringBuilder();
            sb.AppendLine("=== RINGS ===");

            foreach (var kvp in game.rings)
            {
                var ring = kvp.Value;
                sb.AppendLine($"{ring.element.ToUpper()}: " +
                             $"Type: {ring.conflictType}, " +
                             $"Fate: {ring.GetFate()}, " +
                             $"Claimed: {(ring.claimed ? ring.claimedBy?.name ?? "Yes" : "No")}, " +
                             $"Contested: {ring.contested}");
            }

            return sb.ToString();
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Performance monitoring system
        /// </summary>
        public class PerformanceMonitor
        {
            private Dictionary<string, PerformanceMetric> metrics = new Dictionary<string, PerformanceMetric>();
            private float updateInterval = 1.0f;
            private float lastUpdateTime = 0f;

            public class PerformanceMetric
            {
                public float totalTime;
                public int callCount;
                public float averageTime => callCount > 0 ? totalTime / callCount : 0f;
                public float maxTime;
                public float minTime = float.MaxValue;
            }

            public void Update()
            {
                if (Time.time - lastUpdateTime >= updateInterval)
                {
                    UpdateFramerateMetrics();
                    lastUpdateTime = Time.time;
                }
            }

            private void UpdateFramerateMetrics()
            {
                float frameTime = Time.deltaTime * 1000f; // Convert to milliseconds
                RecordMetric("FrameTime", frameTime);
                RecordMetric("FPS", 1f / Time.deltaTime);
            }

            public void RecordMetric(string name, float value)
            {
                if (!metrics.ContainsKey(name))
                {
                    metrics[name] = new PerformanceMetric();
                }

                var metric = metrics[name];
                metric.totalTime += value;
                metric.callCount++;
                metric.maxTime = Mathf.Max(metric.maxTime, value);
                metric.minTime = Mathf.Min(metric.minTime, value);
            }

            public string GetReport()
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== PERFORMANCE METRICS ===");

                foreach (var kvp in metrics.OrderByDescending(x => x.Value.averageTime))
                {
                    var metric = kvp.Value;
                    sb.AppendLine($"{kvp.Key}: Avg: {metric.averageTime:F2}, " +
                                 $"Max: {metric.maxTime:F2}, Min: {metric.minTime:F2}, " +
                                 $"Calls: {metric.callCount}");
                }

                return sb.ToString();
            }

            public void ClearMetrics()
            {
                metrics.Clear();
            }
        }

        #endregion

        #region Memory Tracking

        /// <summary>
        /// Memory usage tracking
        /// </summary>
        public class MemoryTracker
        {
            private List<float> memorySnapshots = new List<float>();
            private float lastMemoryCheck = 0f;
            private float memoryCheckInterval = 5f; // Check every 5 seconds

            public void Update()
            {
                if (Time.time - lastMemoryCheck >= memoryCheckInterval)
                {
                    RecordMemorySnapshot();
                    lastMemoryCheck = Time.time;
                }
            }

            private void RecordMemorySnapshot()
            {
                long memoryUsage = GC.GetTotalMemory(false);
                float memoryMB = memoryUsage / (1024f * 1024f);
                
                memorySnapshots.Add(memoryMB);
                
                // Keep only last 100 snapshots
                if (memorySnapshots.Count > 100)
                {
                    memorySnapshots.RemoveAt(0);
                }
            }

            public string GetReport()
            {
                if (memorySnapshots.Count == 0)
                {
                    return "No memory data available";
                }

                var current = memorySnapshots.Last();
                var average = memorySnapshots.Average();
                var max = memorySnapshots.Max();
                var min = memorySnapshots.Min();

                var sb = new StringBuilder();
                sb.AppendLine("=== MEMORY USAGE ===");
                sb.AppendLine($"Current: {current:F2} MB");
                sb.AppendLine($"Average: {average:F2} MB");
                sb.AppendLine($"Peak: {max:F2} MB");
                sb.AppendLine($"Minimum: {min:F2} MB");
                sb.AppendLine($"Snapshots: {memorySnapshots.Count}");

                return sb.ToString();
            }
        }

        #endregion

        #region Debug Console

        /// <summary>
        /// Interactive debug console
        /// </summary>
        public class DebugConsole
        {
            private Dictionary<string, DebugCommand> commands = new Dictionary<string, DebugCommand>();
            private L5RDebugger debugger;

            public class DebugCommand
            {
                public string name;
                public string description;
                public Action action;

                public DebugCommand(string name, string description, Action action)
                {
                    this.name = name;
                    this.description = description;
                    this.action = action;
                }
            }

            public DebugConsole(L5RDebugger debugger)
            {
                this.debugger = debugger;
            }

            public void RegisterCommand(string name, string description, Action action)
            {
                commands[name] = new DebugCommand(name, description, action);
            }

            public bool ExecuteCommand(string commandLine)
            {
                if (string.IsNullOrEmpty(commandLine))
                    return false;

                var parts = commandLine.Split(' ');
                var commandName = parts[0].ToLower();

                if (commands.ContainsKey(commandName))
                {
                    try
                    {
                        commands[commandName].action.Invoke();
                        return true;
                    }
                    catch (Exception e)
                    {
                        debugger.LogError($"Command '{commandName}' failed: {e.Message}", "Console");
                        return false;
                    }
                }
                else
                {
                    debugger.LogWarning($"Unknown command: {commandName}", "Console");
                    return false;
                }
            }

            public List<DebugCommand> GetAllCommands()
            {
                return commands.Values.ToList();
            }
        }

        #endregion

        #region Network Debugging

        /// <summary>
        /// Network debugging utilities
        /// </summary>
        public class NetworkDebugger
        {
            private List<NetworkMessage> messageHistory = new List<NetworkMessage>();
            private int maxMessages = 500;

            public class NetworkMessage
            {
                public DateTime timestamp;
                public string direction; // "Sent" or "Received"
                public string messageType;
                public string content;
                public int size;

                public override string ToString()
                {
                    return $"[{timestamp:HH:mm:ss}] {direction} {messageType} ({size} bytes)";
                }
            }

            public void LogMessage(string direction, string messageType, string content)
            {
                var message = new NetworkMessage
                {
                    timestamp = DateTime.Now,
                    direction = direction,
                    messageType = messageType,
                    content = content,
                    size = System.Text.Encoding.UTF8.GetByteCount(content)
                };

                messageHistory.Add(message);

                if (messageHistory.Count > maxMessages)
                {
                    messageHistory.RemoveAt(0);
                }
            }

            public string GetReport()
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== NETWORK MESSAGES ===");
                sb.AppendLine($"Total Messages: {messageHistory.Count}");
                
                var recent = messageHistory.TakeLast(20);
                foreach (var message in recent)
                {
                    sb.AppendLine(message.ToString());
                }

                return sb.ToString();
            }
        }

        #endregion

        #region Debug UI

        /// <summary>
        /// Handle debug input
        /// </summary>
        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(debugToggleKey))
            {
                debugUIVisible = !debugUIVisible;
            }

            if (Input.GetKeyDown(performanceToggleKey))
            {
                LogInfo(performanceMonitor.GetReport());
            }

            if (Input.GetKeyDown(gameStateKey))
            {
                LogInfo(GetGameStateInfo());
            }
        }

        /// <summary>
        /// Draw debug UI
        /// </summary>
        private void DrawDebugUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            
            GUILayout.BeginVertical("box");
            GUILayout.Label("L5R Debug Console", GUI.skin.GetStyle("label"));

            // Debug controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Game State"))
            {
                LogInfo(GetGameStateInfo());
            }
            if (GUILayout.Button("Players"))
            {
                LogInfo(GetPlayersInfo());
            }
            if (GUILayout.Button("Cards"))
            {
                LogInfo(GetCardsInfo());
            }
            if (GUILayout.Button("Performance"))
            {
                LogInfo(performanceMonitor.GetReport());
            }
            if (GUILayout.Button("Clear Log"))
            {
                ClearLog();
            }
            GUILayout.EndHorizontal();

            // Log display
            debugScrollPosition = GUILayout.BeginScrollView(debugScrollPosition, GUILayout.Height(400));
            
            var recentLogs = logEntries.TakeLast(50).ToList();
            foreach (var entry in recentLogs)
            {
                var color = GetLogColor(entry.level);
                var oldColor = GUI.color;
                GUI.color = color;
                GUILayout.Label(entry.ToString());
                GUI.color = oldColor;
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Get color for log level
        /// </summary>
        private Color GetLogColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                case LogLevel.Critical:
                    return Color.red;
                case LogLevel.Warning:
                    return Color.yellow;
                case LogLevel.Info:
                    return Color.white;
                case LogLevel.Debug:
                    return Color.cyan;
                case LogLevel.Verbose:
                    return Color.gray;
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Event Handlers

        private void OnGameStateChanged(string change, object data)
        {
            LogDebug($"Game state changed: {change}", "GameState");
        }

        private void OnPhaseChanged(string oldPhase, string newPhase)
        {
            LogInfo($"Phase changed: {oldPhase} → {newPhase}", "Phase");
        }

        private void OnCardPlayed(Player player, BaseCard card)
        {
            LogDebug($"{player.name} played {card.name}", "Cards");
        }

        private void OnConflictStarted(Conflict conflict)
        {
            LogInfo($"Conflict started: {conflict.conflictType} at {conflict.ring?.element}", "Conflict");
        }

        private void OnConflictFinished(Conflict conflict)
        {
            LogInfo($"Conflict finished: {conflict.winner?.name ?? "No winner"} won", "Conflict");
        }

        #endregion

        #region Utility Methods

        private void ShowHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DEBUG COMMANDS ===");
            
            foreach (var command in debugConsole.GetAllCommands())
            {
                sb.AppendLine($"{command.name}: {command.description}");
            }
            
            LogInfo(sb.ToString());
        }

        private void ClearLog()
        {
            logEntries.Clear();
            LogInfo("Debug log cleared");
        }

        private void SaveLogToFile()
        {
            try
            {
                var logContent = string.Join("\n", logEntries.Select(e => e.ToString()));
                var filePath = Path.Combine(Application.persistentDataPath, logFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, logContent);
                LogInfo($"Log saved to: {filePath}");
            }
            catch (Exception e)
            {
                LogError($"Failed to save log: {e.Message}");
            }
        }

        private void SetupLogFile()
        {
            try
            {
                var filePath = Path.Combine(Application.persistentDataPath, logFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            catch (Exception e)
            {
                LogError($"Failed to setup log file: {e.Message}");
            }
        }

        private void AppendToLogFile(DebugLogEntry entry)
        {
            try
            {
                var filePath = Path.Combine(Application.persistentDataPath, logFilePath);
                File.AppendAllText(filePath, entry.ToString() + "\n");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to append to log file: {e.Message}");
            }
        }

        private void RunSystemTests()
        {
            LogInfo("Running system tests...", "Testing");
            
            // Run cost reducer tests
            var costReducerResult = CostReducerTestUtils.RunCostReducerTests();
            LogInfo($"Cost Reducer Tests: {(costReducerResult ? "PASSED" : "FAILED")}", "Testing");
            
            // Add more system tests here
            
            LogInfo("System tests completed", "Testing");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get debug variable
        /// </summary>
        public T GetDebugVariable<T>(string name, T defaultValue = default(T))
        {
            if (debugVariables.ContainsKey(name))
            {
                try
                {
                    return (T)debugVariables[name];
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set debug variable
        /// </summary>
        public void SetDebugVariable<T>(string name, T value)
        {
            debugVariables[name] = value;
        }

        /// <summary>
        /// Record performance metric
        /// </summary>
        public void RecordPerformance(string metricName, float value)
        {
            if (enablePerformanceMonitoring)
            {
                performanceMonitor.RecordMetric(metricName, value);
            }
        }

        /// <summary>
        /// Execute debug command
        /// </summary>
        public bool ExecuteDebugCommand(string command)
        {
            return debugConsole.ExecuteCommand(command);
        }

        /// <summary>
        /// Get comprehensive debug report
        /// </summary>
        public string GetFullDebugReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== L5R GAME DEBUG REPORT ===");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine();
            
            sb.AppendLine(GetGameStateInfo());
            sb.AppendLine();
            sb.AppendLine(GetPlayersInfo());
            sb.AppendLine();
            sb.AppendLine(GetCardsInfo());
            sb.AppendLine();
            sb.AppendLine(GetRingsInfo());
            sb.AppendLine();
            sb.AppendLine(performanceMonitor.GetReport());
            sb.AppendLine();
            sb.AppendLine(memoryTracker.GetReport());
            
            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for easy debugging integration
    /// </summary>
    public static class DebugExtensions
    {
        private static L5RDebugger debugger;

        static DebugExtensions()
        {
            debugger = Object.FindObjectOfType<L5RDebugger>();
        }

        /// <summary>
        /// Log debug message from any object
        /// </summary>
        public static void DebugLog(this object obj, string message, L5RDebugger.LogLevel level = L5RDebugger.LogLevel.Debug)
        {
            debugger?.Log(level, obj.GetType().Name, message);
        }

        /// <summary>
        /// Measure execution time of an action
        /// </summary>
        public static void MeasurePerformance(this object obj, string operationName, Action action)
        {
            if (debugger?.enablePerformanceMonitoring == true)
            {
                var stopwatch = Stopwatch.StartNew();
                action.Invoke();
                stopwatch.Stop();
                debugger.RecordPerformance(operationName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Assert condition with debug logging
        /// </summary>
        public static void DebugAssert(this object obj, bool condition, string message)
        {
            if (!condition)
            {
                debugger?.LogError($"Assertion failed: {message}", obj.GetType().Name);
            }
        }
    }
}