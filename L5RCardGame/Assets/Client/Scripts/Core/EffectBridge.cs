using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Core;
using L5RGame.Scripting;
using L5RGame.Constants;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame.Effects
{
    /// <summary>
    /// Bridge class that integrates C# Effect with IronPython scripting
    /// Provides advanced effect management and hot-reload capabilities
    /// </summary>
    [Serializable]
    public class EffectBridge : Effect
    {
        #region Fields
        
        [Header("Python Integration")]
        [SerializeField] private bool preferPythonImplementation = true;
        [SerializeField] private bool fallbackToCSharp = true;
        [SerializeField] private string pythonScriptName = "effect";
        [SerializeField] private bool enableHotReload = true;
        
        [Header("Advanced Features")]
        [SerializeField] private bool enableEffectChaining = true;
        [SerializeField] private bool enableEffectStacking = true;
        [SerializeField] private bool trackEffectHistory = true;
        [SerializeField] private bool enablePerformanceMetrics = true;
        
        [Header("Debug & Monitoring")]
        [SerializeField] private bool logDetailedEvents = false;
        [SerializeField] private bool validateTargetsOnUpdate = true;
        [SerializeField] private bool enableEffectProfiling = false;
        
        private bool pythonScriptLoaded = false;
        private bool pythonExecutionFailed = false;
        private string pythonEffectId;
        private List<EffectHistoryEntry> effectHistory;
        private EffectPerformanceMetrics performanceMetrics;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(Game game, BaseCard source, EffectProperties properties, IEffectImplementation effectImpl)
        {
            base.Initialize(game, source, properties, effectImpl);
            
            // Initialize additional features
            InitializeAdvancedFeatures();
            
            // Initialize Python integration if enabled
            if (preferPythonImplementation)
            {
                InitializePythonIntegration();
            }
        }
        
        /// <summary>
        /// Initialize advanced features
        /// </summary>
        private void InitializeAdvancedFeatures()
        {
            if (trackEffectHistory)
            {
                effectHistory = new List<EffectHistoryEntry>();
                LogHistoryEntry("Effect Created", $"Effect {EffectId} created from source {Source?.Name}");
            }
            
            if (enablePerformanceMetrics)
            {
                performanceMetrics = new EffectPerformanceMetrics();
            }
        }
        
        /// <summary>
        /// Initialize Python integration
        /// </summary>
        private void InitializePythonIntegration()
        {
            if (!PythonManager.Instance.IsEnabled)
            {
                Debug.LogWarning("Python not available, falling back to C# implementation");
                return;
            }
            
            try
            {
                LoadPythonScript();
                CreatePythonEffect();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Python integration for Effect: {e.Message}");
                pythonExecutionFailed = true;
            }
        }
        
        /// <summary>
        /// Load the Python script
        /// </summary>
        private void LoadPythonScript()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (PythonManager.Instance.LoadScript(pythonScriptName))
            {
                pythonScriptLoaded = true;
                Debug.Log($"✅ Python script '{pythonScriptName}' loaded successfully");
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to load Python script '{pythonScriptName}'");
            }
#endif
        }
        
        /// <summary>
        /// Create Python effect instance
        /// </summary>
        private void CreatePythonEffect()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!pythonScriptLoaded) return;
            
            try
            {
                // Convert C# properties to Python format
                var pythonProperties = CreatePythonProperties();
                var pythonImplementation = CreatePythonImplementation();
                
                var result = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "create_effect",
                    Game, Source, pythonProperties, pythonImplementation
                );
                
                if (result != null)
                {
                    pythonEffectId = result.ToString();
                    Debug.Log($"🐍 Created Python effect with ID: {pythonEffectId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create Python effect: {e.Message}");
                pythonExecutionFailed = true;
            }
#endif
        }
        
        /// <summary>
        /// Create Python properties object
        /// </summary>
        /// <returns>Python properties</returns>
        private object CreatePythonProperties()
        {
            var pythonDict = new Dictionary<string, object>
            {
                ["duration"] = Duration.ToString().ToLower(),
                ["source_location"] = SourceLocation.ToString().ToLower(),
                ["target_controller"] = TargetController.ToString().ToLower(),
                ["target_location"] = TargetLocation.ToString().ToLower(),
                ["can_change_zone_once"] = CanChangeZoneOnce
            };
            
            // Convert delegates to Python functions if possible
            if (MatchCondition != null)
            {
                pythonDict["match_condition"] = MatchCondition;
            }
            
            if (ActiveCondition != null)
            {
                pythonDict["active_condition"] = ActiveCondition;
            }
            
            if (TargetValidation != null)
            {
                pythonDict["target_validation"] = TargetValidation;
            }
            
            return pythonDict;
        }
        
        /// <summary>
        /// Create Python effect implementation
        /// </summary>
        /// <returns>Python implementation wrapper</returns>
        private object CreatePythonImplementation()
        {
            if (EffectImplementation == null) return null;
            
            // Create a wrapper that bridges C# implementation to Python
            return new PythonEffectImplementationWrapper(EffectImplementation);
        }
        
        #endregion
        
        #region Effect Operations Override
        
        public override bool AddTarget(GameObject target)
        {
            bool result;
            
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                result = AddTargetPython(target);
            }
            else
            {
                result = base.AddTarget(target);
            }
            
            // Track performance metrics
            if (enablePerformanceMetrics)
            {
                performanceMetrics.IncrementTargetOperations();
            }
            
            // Log history
            if (trackEffectHistory && result)
            {
                LogHistoryEntry("Target Added", $"Added target {target?.name} to effect");
            }
            
            // Log detailed events
            if (logDetailedEvents)
            {
                Debug.Log($"Effect {EffectId}: AddTarget({target?.name}) = {result}");
            }
            
            return result;
        }
        
        public override bool RemoveTarget(GameObject target)
        {
            bool result;
            
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                result = RemoveTargetPython(target);
            }
            else
            {
                result = base.RemoveTarget(target);
            }
            
            // Track performance metrics
            if (enablePerformanceMetrics)
            {
                performanceMetrics.IncrementTargetOperations();
            }
            
            // Log history
            if (trackEffectHistory && result)
            {
                LogHistoryEntry("Target Removed", $"Removed target {target?.name} from effect");
            }
            
            // Log detailed events
            if (logDetailedEvents)
            {
                Debug.Log($"Effect {EffectId}: RemoveTarget({target?.name}) = {result}");
            }
            
            return result;
        }
        
        public override bool CheckCondition(bool stateChanged = false)
        {
            var startTime = enablePerformanceMetrics ? Time.realtimeSinceStartup : 0f;
            
            bool result;
            
            // Try Python implementation first
            if (ShouldUsePythonImplementation())
            {
                result = CheckConditionPython(stateChanged);
            }
            else
            {
                result = base.CheckCondition(stateChanged);
            }
            
            // Track performance metrics
            if (enablePerformanceMetrics)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                performanceMetrics.AddConditionCheckTime(elapsed);
            }
            
            // Validate targets if enabled
            if (validateTargetsOnUpdate && result)
            {
                ValidateAllTargets();
            }
            
            // Log history
            if (trackEffectHistory && result)
            {
                LogHistoryEntry("Condition Checked", $"Condition check resulted in state change");
            }
            
            return result;
        }
        
        public override void Cancel()
        {
            // Cancel Python effect first
            if (ShouldUsePythonImplementation())
            {
                CancelPythonEffect();
            }
            
            // Then cancel C# effect
            base.Cancel();
            
            // Log history
            if (trackEffectHistory)
            {
                LogHistoryEntry("Effect Cancelled", $"Effect {EffectId} was cancelled");
            }
            
            // Clean up Python resources
            CleanupPythonResources();
        }
        
        #endregion
        
        #region Python Operations
        
        /// <summary>
        /// Check if Python implementation should be used
        /// </summary>
        /// <returns>True if Python should be used</returns>
        private bool ShouldUsePythonImplementation()
        {
            return preferPythonImplementation && 
                   pythonScriptLoaded && 
                   !pythonExecutionFailed && 
                   !string.IsNullOrEmpty(pythonEffectId) &&
                   PythonManager.Instance.IsEnabled;
        }
        
        /// <summary>
        /// Add target using Python implementation
        /// </summary>
        /// <param name="target">Target to add</param>
        /// <returns>True if successful</returns>
        private bool AddTargetPython(GameObject target)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                var pythonEffect = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "get_effect",
                    pythonEffectId
                );
                
                if (pythonEffect != null)
                {
                    var result = PythonManager.Instance.CallMethod(pythonEffect, "add_target", target);
                    return Convert.ToBoolean(result);
                }
            }
            catch (Exception e)
            {
                HandlePythonError("AddTargetPython", e);
            }
#endif
            
            return false;
        }
        
        /// <summary>
        /// Remove target using Python implementation
        /// </summary>
        /// <param name="target">Target to remove</param>
        /// <returns>True if successful</returns>
        private bool RemoveTargetPython(GameObject target)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                var pythonEffect = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "get_effect",
                    pythonEffectId
                );
                
                if (pythonEffect != null)
                {
                    var result = PythonManager.Instance.CallMethod(pythonEffect, "remove_target", target);
                    return Convert.ToBoolean(result);
                }
            }
            catch (Exception e)
            {
                HandlePythonError("RemoveTargetPython", e);
            }
#endif
            
            return false;
        }
        
        /// <summary>
        /// Check condition using Python implementation
        /// </summary>
        /// <param name="stateChanged">Current state change status</param>
        /// <returns>True if state changed</returns>
        private bool CheckConditionPython(bool stateChanged)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                var pythonEffect = PythonManager.Instance.ExecuteFunction(
                    pythonScriptName,
                    "get_effect",
                    pythonEffectId
                );
                
                if (pythonEffect != null)
                {
                    var result = PythonManager.Instance.CallMethod(pythonEffect, "check_condition", stateChanged);
                    return Convert.ToBoolean(result);
                }
            }
            catch (Exception e)
            {
                HandlePythonError("CheckConditionPython", e);
            }
#endif
            
            return stateChanged;
        }
        
        /// <summary>
        /// Cancel Python effect
        /// </summary>
        private void CancelPythonEffect()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                if (!string.IsNullOrEmpty(pythonEffectId))
                {
                    PythonManager.Instance.ExecuteFunction(
                        pythonScriptName,
                        "remove_effect",
                        pythonEffectId
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to cancel Python effect: {e.Message}");
            }
#endif
        }
        
        /// <summary>
        /// Handle Python execution errors
        /// </summary>
        /// <param name="method">Method that failed</param>
        /// <param name="exception">Exception details</param>
        private void HandlePythonError(string method, Exception exception)
        {
            Debug.LogError($"❌ Python execution failed in {method}: {exception.Message}");
            
            if (fallbackToCSharp)
            {
                Debug.Log("🔄 Falling back to C# implementation");
                pythonExecutionFailed = true;
            }
            else
            {
                throw exception;
            }
        }
        
        /// <summary>
        /// Clean up Python resources
        /// </summary>
        private void CleanupPythonResources()
        {
            pythonEffectId = null;
            pythonExecutionFailed = false;
        }
        
        #endregion
        
        #region Advanced Features
        
        /// <summary>
        /// Validate all current targets
        /// </summary>
        private void ValidateAllTargets()
        {
            if (Targets == null) return;
            
            var invalidTargets = new List<GameObject>();
            
            foreach (var target in Targets)
            {
                if (target == null || !IsValidTarget(target))
                {
                    invalidTargets.Add(target);
                }
            }
            
            if (invalidTargets.Count > 0)
            {
                Debug.LogWarning($"Effect {EffectId}: Found {invalidTargets.Count} invalid targets during validation");
                RemoveTargets(invalidTargets);
            }
        }
        
        /// <summary>
        /// Log history entry
        /// </summary>
        /// <param name="action">Action description</param>
        /// <param name="details">Additional details</param>
        private void LogHistoryEntry(string action, string details)
        {
            if (!trackEffectHistory || effectHistory == null) return;
            
            var entry = new EffectHistoryEntry
            {
                Timestamp = Time.time,
                Action = action,
                Details = details,
                TargetCount = TargetCount,
                IsActive = IsActive
            };
            
            effectHistory.Add(entry);
            
            // Limit history size to prevent memory issues
            if (effectHistory.Count > 1000)
            {
                effectHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Get effect chaining candidates
        /// </summary>
        /// <returns>List of effects that can be chained</returns>
        public List<Effect> GetChainingCandidates()
        {
            if (!enableEffectChaining) return new List<Effect>();
            
            var candidates = new List<Effect>();
            var allEffects = Game.EffectManager.GetAllActiveEffects();
            
            foreach (var effect in allEffects)
            {
                if (effect != this && CanChainWith(effect))
                {
                    candidates.Add(effect);
                }
            }
            
            return candidates;
        }
        
        /// <summary>
        /// Check if this effect can chain with another effect
        /// </summary>
        /// <param name="other">Other effect</param>
        /// <returns>True if can chain</returns>
        private bool CanChainWith(Effect other)
        {
            // Basic chaining rules - can be overridden
            return other.Source == Source || // Same source
                   Targets.Any(t => other.Targets.Contains(t)) || // Shared targets
                   other.GetType() == GetType(); // Same effect type
        }
        
        /// <summary>
        /// Check for effect stacking conflicts
        /// </summary>
        /// <returns>List of conflicting effects</returns>
        public List<Effect> GetStackingConflicts()
        {
            if (!enableEffectStacking) return new List<Effect>();
            
            var conflicts = new List<Effect>();
            var allEffects = Game.EffectManager.GetAllActiveEffects();
            
            foreach (var effect in allEffects)
            {
                if (effect != this && HasStackingConflictWith(effect))
                {
                    conflicts.Add(effect);
                }
            }
            
            return conflicts;
        }
        
        /// <summary>
        /// Check if this effect has stacking conflict with another
        /// </summary>
        /// <param name="other">Other effect</param>
        /// <returns>True if there's a conflict</returns>
        private bool HasStackingConflictWith(Effect other)
        {
            // Check for same target with conflicting modifications
            return Targets.Any(t => other.Targets.Contains(t)) &&
                   GetType() == other.GetType() &&
                   !CanStackWith(other);
        }
        
        /// <summary>
        /// Check if this effect can stack with another effect
        /// </summary>
        /// <param name="other">Other effect</param>
        /// <returns>True if can stack</returns>
        protected virtual bool CanStackWith(Effect other)
        {
            // Default: allow stacking unless overridden
            return true;
        }
        
        #endregion
        
        #region Hot Reload Support
        
        /// <summary>
        /// Reload Python script for hot reload during development
        /// </summary>
        [ContextMenu("Reload Python Script")]
        public void ReloadPythonScript()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Python script reload only available during play mode");
                return;
            }
            
            if (!enableHotReload)
            {
                Debug.LogWarning("Hot reload is disabled for this effect");
                return;
            }
            
            try
            {
                // Store current state
                var currentTargets = Targets?.ToList() ?? new List<GameObject>();
                var wasActive = IsActive;
                
                // Clean up current Python effect
                CleanupPythonResources();
                
                // Reset flags
                pythonScriptLoaded = false;
                pythonExecutionFailed = false;
                
                // Execute reload function in Python
#if UNITY_EDITOR || UNITY_STANDALONE
                PythonManager.Instance.ExecuteFunction(pythonScriptName, "reload_script");
#endif
                
                // Reinitialize Python integration
                InitializePythonIntegration();
                
                // Restore state if possible
                if (ShouldUsePythonImplementation() && wasActive)
                {
                    foreach (var target in currentTargets)
                    {
                        AddTarget(target);
                    }
                }
                
                LogHistoryEntry("Script Reloaded", "Python script was hot-reloaded");
                Debug.Log("🔄 Python script reloaded successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to reload Python script: {e.Message}");
            }
        }
        
        #endregion
        
        #region Debug & Monitoring
        
        /// <summary>
        /// Get enhanced debug information
        /// </summary>
        /// <returns>Enhanced debug information</returns>
        public override EffectDebugInfo GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            
            return new EnhancedEffectDebugInfo
            {
                EffectId = baseInfo.EffectId,
                SourceName = baseInfo.SourceName,
                TargetNames = baseInfo.TargetNames,
                IsActive = baseInfo.IsActive,
                ConditionMet = baseInfo.ConditionMet,
                TargetCount = baseInfo.TargetCount,
                Duration = baseInfo.Duration,
                EffectType = baseInfo.EffectType,
                ImplementationInfo = baseInfo.ImplementationInfo,
                
                // Enhanced information
                UsingPythonImplementation = ShouldUsePythonImplementation(),
                PythonEffectId = pythonEffectId,
                PythonScriptLoaded = pythonScriptLoaded,
                PythonExecutionFailed = pythonExecutionFailed,
                HistoryEntryCount = effectHistory?.Count ?? 0,
                PerformanceMetrics = performanceMetrics?.GetSummary(),
                ChainingCandidatesCount = GetChainingCandidates().Count,
                StackingConflictsCount = GetStackingConflicts().Count
            };
        }
        
        /// <summary>
        /// Get effect history
        /// </summary>
        /// <returns>Effect history entries</returns>
        public List<EffectHistoryEntry> GetEffectHistory()
        {
            return effectHistory?.ToList() ?? new List<EffectHistoryEntry>();
        }
        
        /// <summary>
        /// Get performance metrics
        /// </summary>
        /// <returns>Performance metrics</returns>
        public EffectPerformanceMetrics GetPerformanceMetrics()
        {
            return performanceMetrics;
        }
        
        /// <summary>
        /// Get implementation status for debugging
        /// </summary>
        /// <returns>Status information</returns>
        public string GetImplementationStatus()
        {
            var status = $"Effect Bridge Implementation Status:\n";
            status += $"• Effect ID: {EffectId}\n";
            status += $"• Source: {Source?.Name ?? "Unknown"}\n";
            status += $"• Prefer Python: {preferPythonImplementation}\n";
            status += $"• Python Loaded: {pythonScriptLoaded}\n";
            status += $"• Python Failed: {pythonExecutionFailed}\n";
            status += $"• Python Effect ID: {pythonEffectId ?? "None"}\n";
            status += $"• Using Python: {ShouldUsePythonImplementation()}\n";
            status += $"• Fallback Enabled: {fallbackToCSharp}\n";
            status += $"• Hot Reload: {enableHotReload}\n";
            status += $"• Effect Chaining: {enableEffectChaining}\n";
            status += $"• Effect Stacking: {enableEffectStacking}\n";
            status += $"• Track History: {trackEffectHistory}\n";
            status += $"• Performance Metrics: {enablePerformanceMetrics}";
            
            return status;
        }
        
        #endregion
        
        #region Unity Inspector
        
#if UNITY_EDITOR
        /// <summary>
        /// Custom inspector validation
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(pythonScriptName))
            {
                pythonScriptName = "effect";
            }
        }
        
        /// <summary>
        /// Show status in console
        /// </summary>
        [ContextMenu("Show Status")]
        public void ShowStatusInConsole()
        {
            Debug.Log(GetImplementationStatus());
        }
        
        /// <summary>
        /// Show effect history in console
        /// </summary>
        [ContextMenu("Show Effect History")]
        public void ShowEffectHistoryInConsole()
        {
            if (effectHistory == null || effectHistory.Count == 0)
            {
                Debug.Log("No effect history available");
                return;
            }
            
            Debug.Log($"📜 Effect History ({effectHistory.Count} entries):");
            var recentEntries = effectHistory.TakeLast(10);
            
            foreach (var entry in recentEntries)
            {
                Debug.Log($"  {entry.Timestamp:F2}s: {entry.Action} - {entry.Details}");
            }
        }
        
        /// <summary>
        /// Show performance metrics in console
        /// </summary>
        [ContextMenu("Show Performance Metrics")]
        public void ShowPerformanceMetricsInConsole()
        {
            if (performanceMetrics == null)
            {
                Debug.Log("No performance metrics available");
                return;
            }
            
            Debug.Log($"📊 Effect Performance Metrics:\n{performanceMetrics.GetDetailedReport()}");
        }
#endif
        
        #endregion
        
        #region Unity Lifecycle
        
        protected override void OnDestroy()
        {
            // Clean up Python resources
            CleanupPythonResources();
            
            // Clear history
            effectHistory?.Clear();
            
            base.OnDestroy();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enhanced debug information for effect bridge
    /// </summary>
    [Serializable]
    public class EnhancedEffectDebugInfo : EffectDebugInfo
    {
        public bool UsingPythonImplementation;
        public string PythonEffectId;
        public bool PythonScriptLoaded;
        public bool PythonExecutionFailed;
        public int HistoryEntryCount;
        public object PerformanceMetrics;
        public int ChainingCandidatesCount;
        public int StackingConflictsCount;
        
        public override string ToString()
        {
            return $"{EffectType}: {SourceName} -> [{string.Join(", ", TargetNames)}] " +
                   $"(Active: {IsActive}, Python: {UsingPythonImplementation})";
        }
    }
    
    /// <summary>
    /// Effect history entry
    /// </summary>
    [Serializable]
    public class EffectHistoryEntry
    {
        public float Timestamp;
        public string Action;
        public string Details;
        public int TargetCount;
        public bool IsActive;
        
        public override string ToString()
        {
            return $"{Timestamp:F2}s: {Action} - {Details}";
        }
    }
    
    /// <summary>
    /// Effect performance metrics
    /// </summary>
    [Serializable]
    public class EffectPerformanceMetrics
    {
        private int targetOperations = 0;
        private List<float> conditionCheckTimes = new List<float>();
        private float creationTime;
        
        public EffectPerformanceMetrics()
        {
            creationTime = Time.time;
        }
        
        public void IncrementTargetOperations()
        {
            targetOperations++;
        }
        
        public void AddConditionCheckTime(float time)
        {
            conditionCheckTimes.Add(time);
            
            // Limit stored times to prevent memory issues
            if (conditionCheckTimes.Count > 1000)
            {
                conditionCheckTimes.RemoveAt(0);
            }
        }
        
        public object GetSummary()
        {
            return new
            {
                TargetOperations = targetOperations,
                ConditionChecks = conditionCheckTimes.Count,
                AverageConditionCheckTime = conditionCheckTimes.Count > 0 ? conditionCheckTimes.Average() : 0f,
                TotalLifetime = Time.time - creationTime
            };
        }
        
        public string GetDetailedReport()
        {
            var summary = GetSummary();
            return $"Target Operations: {targetOperations}\n" +
                   $"Condition Checks: {conditionCheckTimes.Count}\n" +
                   $"Avg Check Time: {(conditionCheckTimes.Count > 0 ? conditionCheckTimes.Average() * 1000f : 0f):F2}ms\n" +
                   $"Total Lifetime: {Time.time - creationTime:F2}s";
        }
    }
    
    /// <summary>
    /// Python effect implementation wrapper
    /// </summary>
    public class PythonEffectImplementationWrapper
    {
        private IEffectImplementation csharpImplementation;
        
        public PythonEffectImplementationWrapper(IEffectImplementation impl)
        {
            csharpImplementation = impl;
        }
        
        public void Initialize(object effect, object context)
        {
            // Bridge to C# implementation if needed
            // This would be called from Python
        }
        
        public void Apply(GameObject target)
        {
            csharpImplementation?.Apply(target);
        }
        
        public void Unapply(GameObject target)
        {
            csharpImplementation?.Unapply(target);
        }
        
        public bool Recalculate(GameObject target)
        {
            return csharpImplementation?.Recalculate(target) ?? false;
        }
        
        public object GetDebugInfo()
        {
            return csharpImplementation?.GetDebugInfo();
        }
    }
}