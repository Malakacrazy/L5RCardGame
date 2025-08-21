// File: CardGameNetworking.cs
// Additional networking components for the L5R Card Game

using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace L5RGame.Client
{
    /// <summary>
    /// Card-specific network messages for L5R Card Game
    /// </summary>
    
    public struct PlayCardMessage : NetworkMessage
    {
        public string CardId;
        public string PlayerId;
        public Vector2 Position;
        public string TargetId;
        public Dictionary<string, object> PlayData;
    }
    
    public struct DrawCardMessage : NetworkMessage
    {
        public string PlayerId;
        public int CardCount;
        public string DrawSource; // deck, discard, etc.
    }
    
    public struct CardMovedMessage : NetworkMessage
    {
        public string CardId;
        public string FromZone;
        public string ToZone;
        public string PlayerId;
        public Vector2 NewPosition;
    }
    
    public struct DeckShuffledMessage : NetworkMessage
    {
        public string PlayerId;
        public string DeckType; // dynasty, conflict, etc.
    }
    
    public struct ResourceChangedMessage : NetworkMessage
    {
        public string PlayerId;
        public string ResourceType; // fate, honor, etc.
        public int OldValue;
        public int NewValue;
        public int Delta;
    }
    
    public struct TurnPhaseChangedMessage : NetworkMessage
    {
        public string GameId;
        public string NewPhase;
        public string ActivePlayerId;
        public float PhaseTimeLimit;
    }
    
    public struct PlayerActionRequestMessage : NetworkMessage
    {
        public string PlayerId;
        public string ActionType;
        public string[] AvailableOptions;
        public float TimeLimit;
    }
    
    public struct GameEndedMessage : NetworkMessage
    {
        public string GameId;
        public string WinnerId;
        public string WinCondition;
        public Dictionary<string, object> FinalStats;
    }
}

namespace L5RCardGame.Data
{
    /// <summary>
    /// Card data structures for L5R Card Game
    /// </summary>
    
    [System.Serializable]
    public class Card
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Character, Event, Holding, etc.
        public string Clan { get; set; }
        public int Cost { get; set; }
        public int MilitarySkill { get; set; }
        public int PoliticalSkill { get; set; }
        public int Glory { get; set; }
        public string[] Traits { get; set; }
        public string Text { get; set; }
        public string FlavorText { get; set; }
        public string ArtworkUrl { get; set; }
        public string Set { get; set; }
        public int Number { get; set; }
        public bool IsUnique { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
        
        // IronPython script reference for card behavior
        public string ScriptName { get; set; }
    }
    
    [System.Serializable]
    public class Deck
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public string PrimaryClan { get; set; }
        public string SecondaryClan { get; set; }
        public List<string> StrongholdCards { get; set; }
        public List<string> ProvinceCards { get; set; }
        public List<string> DynastyCards { get; set; }
        public List<string> ConflictCards { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsValid { get; set; }
        public string Format { get; set; } // Standard, Skirmish, etc.
    }
    
    [System.Serializable]
    public class GameZone
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ZoneType { get; set; } // Hand, Play, Discard, Deck, etc.
        public string OwnerId { get; set; }
        public List<Card> Cards { get; set; }
        public bool IsPublic { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
    }
    
    [System.Serializable]
    public class PlayerState
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string ClanChoice { get; set; }
        public int Honor { get; set; }
        public int Fate { get; set; }
        public int ImperialFavor { get; set; } // 0 = none, 1 = military, 2 = political
        public bool IsActivePlayer { get; set; }
        public bool IsFirstPlayer { get; set; }
        public Dictionary<string, GameZone> Zones { get; set; }
        public List<string> AvailableActions { get; set; }
        public float ActionTimeRemaining { get; set; }
    }
    
    [System.Serializable]
    public class L5RGameState : GameState
    {
        public string GameFormat { get; set; }
        public int Round { get; set; }
        public string CurrentPhase { get; set; }
        public List<PlayerState> Players { get; set; }
        public string[] ProvinceCards { get; set; }
        public bool[] ProvincesBroken { get; set; }
        public string CurrentConflictType { get; set; } // military, political
        public string AttackingPlayerId { get; set; }
        public string DefendingPlayerId { get; set; }
        public List<string> ConflictParticipants { get; set; }
        public Dictionary<string, int> ConflictSkillTotals { get; set; }
        public List<string> RingStatus { get; set; } // claimed rings
        public DateTime GameStartTime { get; set; }
        public TimeSpan GameDuration { get; set; }
    }
}

namespace L5RCardGame.Scripting
{
    /// <summary>
    /// IronPython scripting integration for card behaviors
    /// </summary>
    
    public interface ICardScript
    {
        void OnPlay(Card card, PlayerState player, L5RGameState gameState);
        void OnEnterPlay(Card card, PlayerState player, L5RGameState gameState);
        void OnLeavePlay(Card card, PlayerState player, L5RGameState gameState);
        bool CanPlay(Card card, PlayerState player, L5RGameState gameState);
        void OnActivate(Card card, PlayerState player, L5RGameState gameState);
        void OnConflictDeclared(Card card, PlayerState player, L5RGameState gameState);
        void OnConflictResolved(Card card, PlayerState player, L5RGameState gameState);
    }
    
    public class CardScriptManager : MonoBehaviour
    {
        [Header("Scripting Configuration")]
        [SerializeField] private bool enableScripting = true;
        [SerializeField] private string scriptsPath = "Assets/Scripts/Cards/";
        
        private Dictionary<string, ICardScript> loadedScripts = new Dictionary<string, ICardScript>();
        
        public void LoadCardScript(string scriptName)
        {
            if (!enableScripting) return;
            
            try
            {
                // Load and compile IronPython script
                // Implementation depends on IronPython integration
                Debug.Log($"Loading card script: {scriptName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load card script {scriptName}: {ex.Message}");
            }
        }
        
        public void ExecuteCardScript(string scriptName, string method, params object[] parameters)
        {
            if (!enableScripting || !loadedScripts.ContainsKey(scriptName)) return;
            
            try
            {
                var script = loadedScripts[scriptName];
                // Execute specific method on the script
                // Implementation depends on IronPython integration
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to execute {method} on script {scriptName}: {ex.Message}");
            }
        }
    }
}

namespace L5RCardGame.Mobile
{
    /// <summary>
    /// Mobile-specific optimizations for the card game
    /// </summary>
    
    public class MobileOptimizationManager : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField] private bool enableBatteryOptimization = true;
        [SerializeField] private bool enableLowPowerMode = false;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private float lowBatteryThreshold = 0.2f;
        
        [Header("Network Optimization")]
        [SerializeField] private bool enableDataCompression = true;
        [SerializeField] private bool enableImageCaching = true;
        [SerializeField] private int maxCachedImages = 100;
        
        private bool isLowPowerModeActive = false;
        
        void Start()
        {
            InitializeMobileOptimizations();
        }
        
        void Update()
        {
            MonitorBatteryLevel();
            AdjustPerformanceSettings();
        }
        
        private void InitializeMobileOptimizations()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Enable/disable vsync based on battery optimization
            QualitySettings.vSyncCount = enableBatteryOptimization ? 1 : 0;
            
            // Configure screen timeout
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            Debug.Log("Mobile optimizations initialized");
        }
        
        private void MonitorBatteryLevel()
        {
            if (!enableBatteryOptimization) return;
            
            float batteryLevel = SystemInfo.batteryLevel;
            bool shouldActivateLowPower = batteryLevel <= lowBatteryThreshold && batteryLevel > 0;
            
            if (shouldActivateLowPower != isLowPowerModeActive)
            {
                isLowPowerModeActive = shouldActivateLowPower;
                ApplyLowPowerMode(isLowPowerModeActive);
            }
        }
        
        private void ApplyLowPowerMode(bool enable)
        {
            if (enable)
            {
                // Reduce performance for battery saving
                Application.targetFrameRate = 30;
                QualitySettings.DecreaseLevel();
                Debug.Log("Low power mode activated");
            }
            else
            {
                // Restore normal performance
                Application.targetFrameRate = targetFrameRate;
                QualitySettings.IncreaseLevel();
                Debug.Log("Low power mode deactivated");
            }
        }
        
        private void AdjustPerformanceSettings()
        {
            // Dynamic quality adjustment based on performance
            if (Time.deltaTime > 1f / 30f) // Frame time > 33ms
            {
                if (QualitySettings.GetQualityLevel() > 0)
                {
                    QualitySettings.DecreaseLevel();
                }
            }
            else if (Time.deltaTime < 1f / 45f) // Frame time < 22ms
            {
                if (QualitySettings.GetQualityLevel() < QualitySettings.names.Length - 1)
                {
                    QualitySettings.IncreaseLevel();
                }
            }
        }
    }
    
    public class TouchInputManager : MonoBehaviour
    {
        [Header("Touch Settings")]
        [SerializeField] private float tapThreshold = 0.1f;
        [SerializeField] private float dragThreshold = 50f;
        [SerializeField] private float pinchThreshold = 0.1f;
        
        public event Action<Vector2> OnTap;
        public event Action<Vector2, Vector2> OnDrag;
        public event Action<float> OnPinch;
        
        private Vector2 lastTouchPosition;
        private float lastTouchTime;
        private bool isDragging = false;
        
        void Update()
        {
            HandleTouchInput();
        }
        
        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                HandleSingleTouch(touch);
            }
            else if (Input.touchCount == 2)
            {
                HandleMultiTouch();
            }
        }
        
        private void HandleSingleTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = touch.position;
                    lastTouchTime = Time.time;
                    isDragging = false;
                    break;
                    
                case TouchPhase.Moved:
                    if (!isDragging && Vector2.Distance(touch.position, lastTouchPosition) > dragThreshold)
                    {
                        isDragging = true;
                    }
                    
                    if (isDragging)
                    {
                        OnDrag?.Invoke(lastTouchPosition, touch.position);
                        lastTouchPosition = touch.position;
                    }
                    break;
                    
                case TouchPhase.Ended:
                    if (!isDragging && Time.time - lastTouchTime < tapThreshold)
                    {
                        OnTap?.Invoke(touch.position);
                    }
                    isDragging = false;
                    break;
            }
        }
        
        private void HandleMultiTouch()
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);
            
            // Calculate pinch
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);
            
            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                float prevDistance = Vector2.Distance(
                    touch1.position - touch1.deltaPosition,
                    touch2.position - touch2.deltaPosition
                );
                
                float deltaDistance = currentDistance - prevDistance;
                if (Mathf.Abs(deltaDistance) > pinchThreshold)
                {
                    OnPinch?.Invoke(deltaDistance);
                }
            }
        }
    }
}