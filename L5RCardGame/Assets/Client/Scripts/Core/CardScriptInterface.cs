using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using IronPython.Runtime;
#endif

namespace L5RGame
{
    /// <summary>
    /// Interface between Unity C# card system and IronPython card scripts.
    /// Provides a bridge for dynamic card behaviors written in Python.
    /// </summary>
    public class CardScriptInterface : MonoBehaviour
    {
        [Header("Script Configuration")]
        public bool enableScripting = true;
        public bool debugMode = false;
        public bool hotReloadEnabled = true;
        public float hotReloadCheckInterval = 1.0f;

        [Header("Script Paths")]
        public string cardScriptsPath = "StreamingAssets/CardScripts";
        public string sharedScriptsPath = "StreamingAssets/SharedScripts";

        // Python engine management
#if UNITY_EDITOR || UNITY_STANDALONE
        private ScriptEngine pythonEngine;
        private ScriptScope globalScope;
#endif

        // Script caching
        private Dictionary<string, CardScript> loadedScripts = new Dictionary<string, CardScript>();
        private Dictionary<string, DateTime> scriptTimestamps = new Dictionary<string, DateTime>();
        
        // Event system
        private Game game;
        private AbilityWindow abilityWindow;

        // Hot reload timer
        private float hotReloadTimer = 0f;

        void Awake()
        {
            game = FindObjectOfType<Game>();
            abilityWindow = FindObjectOfType<AbilityWindow>();
            
            if (enableScripting)
            {
                InitializePythonEngine();
                LoadSharedScripts();
            }
        }

        void Update()
        {
            if (enableScripting && hotReloadEnabled)
            {
                hotReloadTimer += Time.deltaTime;
                if (hotReloadTimer >= hotReloadCheckInterval)
                {
                    CheckForScriptChanges();
                    hotReloadTimer = 0f;
                }
            }
        }

        void OnDestroy()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            pythonEngine?.Runtime?.Shutdown();
#endif
        }

        #region Python Engine Management

        /// <summary>
        /// Initialize the IronPython engine
        /// </summary>
        private void InitializePythonEngine()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                pythonEngine = Python.CreateEngine();
                globalScope = pythonEngine.CreateScope();
                
                // Add Unity and game references to Python scope
                SetupPythonEnvironment();
                
                if (debugMode)
                {
                    Debug.Log("🐍 IronPython engine initialized successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to initialize IronPython: {e.Message}");
                enableScripting = false;
            }
#else
            Debug.LogWarning("⚠️ IronPython not available in this build configuration");
            enableScripting = false;
#endif
        }

        /// <summary>
        /// Setup Python environment with game references
        /// </summary>
        private void SetupPythonEnvironment()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (globalScope == null) return;

            // Add game objects to Python scope
            globalScope.SetVariable("game", game);
            globalScope.SetVariable("ability_window", abilityWindow);
            globalScope.SetVariable("Unity", typeof(Debug));
            globalScope.SetVariable("log", new Action<string>(Debug.Log));
            globalScope.SetVariable("log_warning", new Action<string>(Debug.LogWarning));
            globalScope.SetVariable("log_error", new Action<string>(Debug.LogError));
            
            // Add utility functions
            globalScope.SetVariable("create_ability_context", new Func<BaseCard, Player, object, AbilityContext>(CreateAbilityContext));
            globalScope.SetVariable("create_card_effect", new Func<BaseCard, object, string, CardEffect>(CreateCardEffect));
            globalScope.SetVariable("get_cards_with_trait", new Func<string, List<BaseCard>>(GetCardsWithTrait));
            globalScope.SetVariable("get_participating_characters", new Func<List<BaseCard>>(GetParticipatingCharacters));
            
            // Add constants
            SetupPythonConstants();
            
            if (debugMode)
            {
                Debug.Log("🔧 Python environment setup complete");
            }
#endif
        }

        /// <summary>
        /// Setup Python constants for easy access
        /// </summary>
        private void SetupPythonConstants()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (globalScope == null) return;

            // Card types
            var cardTypes = new Dictionary<string, string>
            {
                { "CHARACTER", CardTypes.Character },
                { "EVENT", CardTypes.Event },
                { "HOLDING", CardTypes.Holding },
                { "PROVINCE", CardTypes.Province },
                { "STRONGHOLD", CardTypes.Stronghold },
                { "ATTACHMENT", CardTypes.Attachment }
            };
            globalScope.SetVariable("CARD_TYPES", cardTypes);

            // Locations
            var locations = new Dictionary<string, string>
            {
                { "HAND", Locations.Hand },
                { "PLAY_AREA", Locations.PlayArea },
                { "PROVINCES", Locations.Provinces },
                { "DYNASTY_DECK", Locations.DynastyDeck },
                { "CONFLICT_DECK", Locations.ConflictDeck },
                { "DYNASTY_DISCARD", Locations.DynastyDiscardPile },
                { "CONFLICT_DISCARD", Locations.ConflictDiscardPile }
            };
            globalScope.SetVariable("LOCATIONS", locations);

            // Players
            var players = new Dictionary<string, string>
            {
                { "SELF", Players.Self },
                { "OPPONENT", Players.Opponent },
                { "ANY", Players.Any }
            };
            globalScope.SetVariable("PLAYERS", players);

            // Ability types
            var abilityTypes = new Dictionary<string, string>
            {
                { "REACTION", AbilityTypes.Reaction },
                { "INTERRUPT", AbilityTypes.Interrupt },
                { "FORCED_REACTION", AbilityTypes.ForcedReaction },
                { "FORCED_INTERRUPT", AbilityTypes.ForcedInterrupt },
                { "ACTION", AbilityTypes.Action }
            };
            globalScope.SetVariable("ABILITY_TYPES", abilityTypes);
#endif
        }

        #endregion

        #region Script Loading and Management

        /// <summary>
        /// Load shared utility scripts
        /// </summary>
        private void LoadSharedScripts()
        {
            string sharedPath = Path.Combine(Application.streamingAssetsPath, sharedScriptsPath.Replace("StreamingAssets/", ""));
            
            if (!Directory.Exists(sharedPath))
            {
                Debug.LogWarning($"⚠️ Shared scripts directory not found: {sharedPath}");
                return;
            }

            var sharedFiles = Directory.GetFiles(sharedPath, "*.py");
            foreach (string filePath in sharedFiles)
            {
                LoadSharedScript(filePath);
            }
        }

        /// <summary>
        /// Load a shared utility script
        /// </summary>
        private void LoadSharedScript(string filePath)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                string scriptContent = File.ReadAllText(filePath);
                pythonEngine.Execute(scriptContent, globalScope);
                
                if (debugMode)
                {
                    Debug.Log($"📜 Loaded shared script: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load shared script {filePath}: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Load script for a specific card
        /// </summary>
        public CardScript LoadCardScript(BaseCard card)
        {
            if (!enableScripting)
            {
                return null;
            }

            string scriptId = GetScriptId(card);
            
            // Return cached script if available
            if (loadedScripts.ContainsKey(scriptId))
            {
                return loadedScripts[scriptId];
            }

            // Load new script
            string scriptPath = GetScriptPath(card);
            if (!File.Exists(scriptPath))
            {
                if (debugMode)
                {
                    Debug.Log($"📄 No script found for {card.name} at {scriptPath}");
                }
                return null;
            }

            CardScript cardScript = LoadScriptFromFile(card, scriptPath);
            if (cardScript != null)
            {
                loadedScripts[scriptId] = cardScript;
                scriptTimestamps[scriptPath] = File.GetLastWriteTime(scriptPath);
            }

            return cardScript;
        }

        /// <summary>
        /// Load script from file
        /// </summary>
        private CardScript LoadScriptFromFile(BaseCard card, string scriptPath)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                string scriptContent = File.ReadAllText(scriptPath);
                var scriptScope = pythonEngine.CreateScope();
                
                // Copy global scope to script scope
                foreach (var variable in globalScope.GetVariableNames())
                {
                    scriptScope.SetVariable(variable, globalScope.GetVariable(variable));
                }
                
                // Add card-specific variables
                scriptScope.SetVariable("card", card);
                scriptScope.SetVariable("controller", card.controller);
                
                // Execute script
                pythonEngine.Execute(scriptContent, scriptScope);
                
                var cardScript = new CardScript(card, scriptScope, this);
                
                if (debugMode)
                {
                    Debug.Log($"🎴 Loaded script for {card.name}");
                }
                
                return cardScript;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load script for {card.name}: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Check for script file changes and reload if necessary
        /// </summary>
        private void CheckForScriptChanges()
        {
            var pathsToCheck = scriptTimestamps.Keys.ToList();
            
            foreach (string scriptPath in pathsToCheck)
            {
                if (File.Exists(scriptPath))
                {
                    DateTime lastWrite = File.GetLastWriteTime(scriptPath);
                    if (lastWrite > scriptTimestamps[scriptPath])
                    {
                        ReloadScript(scriptPath);
                        scriptTimestamps[scriptPath] = lastWrite;
                    }
                }
            }
        }

        /// <summary>
        /// Reload a specific script
        /// </summary>
        private void ReloadScript(string scriptPath)
        {
            // Find cards using this script
            var cardsToReload = loadedScripts.Values
                .Where(script => script.scriptPath == scriptPath)
                .Select(script => script.card)
                .ToList();

            // Remove old scripts
            var scriptsToRemove = loadedScripts
                .Where(kvp => kvp.Value.scriptPath == scriptPath)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string scriptId in scriptsToRemove)
            {
                loadedScripts.Remove(scriptId);
            }

            // Reload scripts for affected cards
            foreach (BaseCard card in cardsToReload)
            {
                LoadCardScript(card);
            }

            if (debugMode && cardsToReload.Count > 0)
            {
                Debug.Log($"🔄 Hot-reloaded script: {Path.GetFileName(scriptPath)} ({cardsToReload.Count} cards affected)");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get script identifier for a card
        /// </summary>
        private string GetScriptId(BaseCard card)
        {
            return $"{card.id}_{card.name.Replace(" ", "_").ToLower()}";
        }

        /// <summary>
        /// Get script file path for a card
        /// </summary>
        private string GetScriptPath(BaseCard card)
        {
            string scriptsDir = Path.Combine(Application.streamingAssetsPath, 
                cardScriptsPath.Replace("StreamingAssets/", ""));
            string fileName = $"{card.id}.py";
            return Path.Combine(scriptsDir, fileName);
        }

        /// <summary>
        /// Create ability context (Python utility)
        /// </summary>
        private AbilityContext CreateAbilityContext(BaseCard card, Player player, object ability)
        {
            return AbilityContext.CreateCardContext(game, card, player, ability);
        }

        /// <summary>
        /// Create card effect (Python utility)
        /// </summary>
        private CardEffect CreateCardEffect(BaseCard source, object effect, string duration)
        {
            return CardEffect.CreateSelfEffect(game, source, effect, duration);
        }

        /// <summary>
        /// Get cards with specific trait (Python utility)
        /// </summary>
        private List<BaseCard> GetCardsWithTrait(string trait)
        {
            return game.GetAllCards().Where(card => card.HasTrait(trait)).ToList();
        }

        /// <summary>
        /// Get participating characters (Python utility)
        /// </summary>
        private List<BaseCard> GetParticipatingCharacters()
        {
            if (game.currentConflict == null)
                return new List<BaseCard>();
            
            return game.GetAllCards()
                .Where(card => card.GetCardType() == CardTypes.Character && card.IsParticipating())
                .ToList();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Execute a Python function on a card script
        /// </summary>
        public object ExecuteFunction(BaseCard card, string functionName, params object[] parameters)
        {
            if (!enableScripting)
                return null;

            string scriptId = GetScriptId(card);
            if (!loadedScripts.ContainsKey(scriptId))
            {
                LoadCardScript(card);
            }

            if (loadedScripts.ContainsKey(scriptId))
            {
                return loadedScripts[scriptId].ExecuteFunction(functionName, parameters);
            }

            return null;
        }

        /// <summary>
        /// Check if a card has a specific function
        /// </summary>
        public bool HasFunction(BaseCard card, string functionName)
        {
            if (!enableScripting)
                return false;

            string scriptId = GetScriptId(card);
            if (!loadedScripts.ContainsKey(scriptId))
            {
                LoadCardScript(card);
            }

            return loadedScripts.ContainsKey(scriptId) && 
                   loadedScripts[scriptId].HasFunction(functionName);
        }

        /// <summary>
        /// Register card's triggered abilities with the ability window
        /// </summary>
        public void RegisterCardAbilities(BaseCard card)
        {
            if (!enableScripting || abilityWindow == null)
                return;

            CardScript script = LoadCardScript(card);
            script?.RegisterTriggeredAbilities();
        }

        /// <summary>
        /// Unregister card's abilities when it leaves play
        /// </summary>
        public void UnregisterCardAbilities(BaseCard card)
        {
            if (!enableScripting || abilityWindow == null)
                return;

            abilityWindow.UnregisterAllAbilities(card);
            
            string scriptId = GetScriptId(card);
            if (loadedScripts.ContainsKey(scriptId))
            {
                loadedScripts[scriptId].Cleanup();
            }
        }

        /// <summary>
        /// Get debug information about loaded scripts
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"CardScriptInterface Debug Info:\n";
            info += $"Scripting Enabled: {enableScripting}\n";
            info += $"Hot Reload Enabled: {hotReloadEnabled}\n";
            info += $"Loaded Scripts: {loadedScripts.Count}\n";
            info += $"Tracked Files: {scriptTimestamps.Count}\n";

            if (loadedScripts.Count > 0)
            {
                info += "\nLoaded Scripts:\n";
                foreach (var kvp in loadedScripts)
                {
                    info += $"  - {kvp.Value.card.name}: {kvp.Value.GetAvailableFunctions().Count} functions\n";
                }
            }

            return info;
        }

        #endregion
    }

    /// <summary>
    /// Represents a loaded Python script for a specific card
    /// </summary>
    public class CardScript
    {
        public BaseCard card;
        public string scriptPath;
        private CardScriptInterface scriptInterface;

#if UNITY_EDITOR || UNITY_STANDALONE
        private ScriptScope scope;
#endif

        public CardScript(BaseCard card, object scope, CardScriptInterface scriptInterface)
        {
            this.card = card;
            this.scriptInterface = scriptInterface;
            this.scriptPath = scriptInterface.GetType().GetMethod("GetScriptPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(scriptInterface, new object[] { card })?.ToString();

#if UNITY_EDITOR || UNITY_STANDALONE
            this.scope = scope as ScriptScope;
#endif
        }

        /// <summary>
        /// Execute a Python function with parameters
        /// </summary>
        public object ExecuteFunction(string functionName, params object[] parameters)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                if (scope.ContainsVariable(functionName))
                {
                    var function = scope.GetVariable(functionName);
                    if (function != null)
                    {
                        return scope.Engine.Operations.Invoke(function, parameters);
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error executing {functionName} on {card.name}: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Check if function exists in script
        /// </summary>
        public bool HasFunction(string functionName)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return scope.ContainsVariable(functionName);
#else
            return false;
#endif
        }

        /// <summary>
        /// Get list of available functions
        /// </summary>
        public List<string> GetAvailableFunctions()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return scope.GetVariableNames().Where(name => 
            {
                var variable = scope.GetVariable(name);
                return variable != null && scope.Engine.Operations.IsCallable(variable);
            }).ToList();
#else
            return new List<string>();
#endif
        }

        /// <summary>
        /// Register triggered abilities with the ability window
        /// </summary>
        public void RegisterTriggeredAbilities()
        {
            var abilityWindow = scriptInterface.GetComponent<AbilityWindow>();
            if (abilityWindow == null) return;

            // Common triggered ability patterns
            var triggerMappings = new Dictionary<string, string>
            {
                { "on_enter_play", "onCharacterEntersPlay" },
                { "on_leave_play", "onCardLeavesPlay" },
                { "on_conflict_declared", "onConflictDeclared" },
                { "on_conflict_resolved", "onConflictResolved" },
                { "on_card_played", "onCardPlayed" },
                { "on_ring_claimed", "onClaimRing" },
                { "on_card_honored", "onCardHonored" },
                { "on_card_dishonored", "onCardDishonored" },
                { "on_phase_started", "onPhaseStarted" },
                { "on_phase_ended", "onPhaseEnded" }
            };

            foreach (var mapping in triggerMappings)
            {
                string pythonFunction = mapping.Key;
                string eventName = mapping.Value;

                if (HasFunction(pythonFunction))
                {
                    // Determine ability type based on function naming
                    string abilityType = AbilityTypes.Reaction;
                    if (HasFunction($"{pythonFunction}_interrupt"))
                    {
                        abilityType = AbilityTypes.Interrupt;
                    }
                    else if (HasFunction($"{pythonFunction}_forced"))
                    {
                        abilityType = AbilityTypes.ForcedReaction;
                    }

                    // Register the ability
                    abilityWindow.RegisterAbility(
                        eventName: eventName,
                        abilityType: abilityType,
                        source: card,
                        ability: pythonFunction,
                        condition: (context) => CanTrigger(pythonFunction, context)
                    );
                }
            }
        }

        /// <summary>
        /// Check if a triggered ability can trigger
        /// </summary>
        private bool CanTrigger(string functionName, AbilityContext context)
        {
            string conditionFunction = $"can_{functionName}";
            if (HasFunction(conditionFunction))
            {
                var result = ExecuteFunction(conditionFunction, card, context);
                return result is bool boolResult ? boolResult : true;
            }
            return true;
        }

        /// <summary>
        /// Cleanup script resources
        /// </summary>
        public void Cleanup()
        {
            // Cleanup any script-specific resources
#if UNITY_EDITOR || UNITY_STANDALONE
            scope = null;
#endif
        }
    }

    /// <summary>
    /// Extension methods for card script integration
    /// </summary>
    public static class CardScriptExtensions
    {
        /// <summary>
        /// Execute Python function on card
        /// </summary>
        public static object ExecutePythonFunction(this BaseCard card, string functionName, params object[] parameters)
        {
            var scriptInterface = Object.FindObjectOfType<CardScriptInterface>();
            return scriptInterface?.ExecuteFunction(card, functionName, parameters);
        }

        /// <summary>
        /// Check if card has Python function
        /// </summary>
        public static bool HasPythonFunction(this BaseCard card, string functionName)
        {
            var scriptInterface = Object.FindObjectOfType<CardScriptInterface>();
            return scriptInterface?.HasFunction(card, functionName) ?? false;
        }

        /// <summary>
        /// Register card's Python abilities
        /// </summary>
        public static void RegisterPythonAbilities(this BaseCard card)
        {
            var scriptInterface = Object.FindObjectOfType<CardScriptInterface>();
            scriptInterface?.RegisterCardAbilities(card);
        }

        /// <summary>
        /// Unregister card's Python abilities
        /// </summary>
        public static void UnregisterPythonAbilities(this BaseCard card)
        {
            var scriptInterface = Object.FindObjectOfType<CardScriptInterface>();
            scriptInterface?.UnregisterCardAbilities(card);
        }
    }
}