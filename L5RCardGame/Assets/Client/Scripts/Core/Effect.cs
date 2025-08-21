using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Core;
using L5RGame.Constants;
using Mirror;

namespace L5RGame.Effects
{
    /// <summary>
    /// Represents a card-based effect applied to one or more targets.
    /// Unity C# implementation of the JavaScript Effect class with networking support.
    /// </summary>
    [Serializable]
    public class Effect : NetworkBehaviour
    {
        #region Properties
        
        [Header("Effect Configuration")]
        [SerializeField] private string effectId;
        [SerializeField] private EffectDuration duration;
        [SerializeField] private CardLocation sourceLocation = CardLocation.PlayArea;
        [SerializeField] private TargetController targetController = TargetController.Self;
        [SerializeField] private CardLocation targetLocation = CardLocation.PlayArea;
        [SerializeField] private bool canChangeZoneOnce = false;
        
        // Core references
        protected Game Game { get; private set; }
        protected BaseCard Source { get; private set; }
        protected List<GameObject> Targets { get; private set; }
        protected EffectContext Context { get; private set; }
        
        // Effect behavior delegates
        public Func<GameObject, EffectContext, bool> MatchCondition { get; set; }
        public Func<EffectContext, bool> ActiveCondition { get; set; }
        public Func<GameObject, bool> TargetValidation { get; set; }
        
        // Effect implementation
        protected IEffectImplementation EffectImplementation { get; set; }
        
        // Network sync
        [SyncVar] private bool isActive = true;
        [SyncVar] private int targetCount = 0;
        
        // Events
        public event Action<GameObject> OnTargetAdded;
        public event Action<GameObject> OnTargetRemoved;
        public event Action OnEffectCancelled;
        
        #endregion
        
        #region Public Properties
        
        public string EffectId => effectId;
        public EffectDuration Duration => duration;
        public CardLocation SourceLocation => sourceLocation;
        public TargetController TargetController => targetController;
        public CardLocation TargetLocation => targetLocation;
        public bool CanChangeZoneOnce => canChangeZoneOnce;
        public bool IsActive => isActive;
        public int TargetCount => Targets?.Count ?? 0;
        public IReadOnlyList<GameObject> GetTargets() => Targets?.AsReadOnly();
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the effect with game context
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="source">Source card of the effect</param>
        /// <param name="properties">Effect properties</param>
        /// <param name="effectImpl">Effect implementation</param>
        public virtual void Initialize(Game game, BaseCard source, EffectProperties properties, IEffectImplementation effectImpl)
        {
            Game = game;
            Source = source;
            Targets = new List<GameObject>();
            EffectImplementation = effectImpl;
            
            // Apply properties
            ApplyProperties(properties);
            
            // Set up default conditions if not provided
            SetupDefaultConditions();
            
            // Refresh context
            RefreshContext();
            
            // Initialize effect implementation
            EffectImplementation?.Initialize(this, Context);
            
            // Generate unique ID if not set
            if (string.IsNullOrEmpty(effectId))
            {
                effectId = GenerateEffectId();
            }
            
            isActive = true;
        }
        
        /// <summary>
        /// Apply effect properties
        /// </summary>
        /// <param name="properties">Properties to apply</param>
        private void ApplyProperties(EffectProperties properties)
        {
            duration = properties.Duration;
            sourceLocation = properties.SourceLocation;
            targetController = properties.TargetController;
            targetLocation = properties.TargetLocation;
            canChangeZoneOnce = properties.CanChangeZoneOnce;
            
            MatchCondition = properties.MatchCondition;
            ActiveCondition = properties.ActiveCondition;
            TargetValidation = properties.TargetValidation;
        }
        
        /// <summary>
        /// Set up default conditions if not provided
        /// </summary>
        private void SetupDefaultConditions()
        {
            MatchCondition ??= (target, context) => true;
            ActiveCondition ??= (context) => true;
            TargetValidation ??= (target) => true;
        }
        
        /// <summary>
        /// Generate a unique effect ID
        /// </summary>
        /// <returns>Unique effect identifier</returns>
        private string GenerateEffectId()
        {
            return $"{Source.CardId}_{GetType().Name}_{GetHashCode()}_{Time.time}";
        }
        
        #endregion
        
        #region Context Management
        
        /// <summary>
        /// Refresh the effect context
        /// </summary>
        public virtual void RefreshContext()
        {
            Context = Game.GetFrameworkContext(Source.Owner);
            Context.Source = Source;
            Context.Effect = this;
            
            EffectImplementation?.SetContext(Context);
        }
        
        #endregion
        
        #region Target Management
        
        /// <summary>
        /// Check if a target is valid for this effect
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <returns>True if target is valid</returns>
        public virtual bool IsValidTarget(GameObject target)
        {
            if (target == null) return false;
            
            // Check target validation function
            if (!TargetValidation(target)) return false;
            
            // Check if target matches the controller requirement
            if (!CheckTargetController(target)) return false;
            
            // Check if target is in the correct location
            if (!CheckTargetLocation(target)) return false;
            
            return true;
        }
        
        /// <summary>
        /// Check if target matches the controller requirement
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if controller matches</returns>
        private bool CheckTargetController(GameObject target)
        {
            var targetCard = target.GetComponent<BaseCard>();
            if (targetCard == null && targetController != TargetController.Any) return false;
            
            return targetController switch
            {
                TargetController.Self => targetCard?.Owner == Source.Owner,
                TargetController.Opponent => targetCard?.Owner != Source.Owner,
                TargetController.Any => true,
                _ => true
            };
        }
        
        /// <summary>
        /// Check if target is in the correct location
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if location matches</returns>
        private bool CheckTargetLocation(GameObject target)
        {
            var targetCard = target.GetComponent<BaseCard>();
            if (targetCard == null) return true; // Non-card targets ignore location
            
            return targetCard.Location == targetLocation;
        }
        
        /// <summary>
        /// Get default target for this effect
        /// </summary>
        /// <param name="context">Effect context</param>
        /// <returns>Default target or null</returns>
        public virtual GameObject GetDefaultTarget(EffectContext context)
        {
            return null; // Override in derived classes
        }
        
        /// <summary>
        /// Get all potential targets for this effect
        /// </summary>
        /// <returns>List of potential targets</returns>
        public virtual List<GameObject> GetPotentialTargets()
        {
            var potentialTargets = new List<GameObject>();
            
            // Get all cards in the target location
            var cardsInLocation = Game.GameState.GetCardsInLocation(targetLocation);
            
            foreach (var card in cardsInLocation)
            {
                if (IsValidTarget(card.gameObject) && MatchCondition(card.gameObject, Context))
                {
                    potentialTargets.Add(card.gameObject);
                }
            }
            
            return potentialTargets;
        }
        
        /// <summary>
        /// Add a target to this effect
        /// </summary>
        /// <param name="target">Target to add</param>
        /// <returns>True if target was added successfully</returns>
        [Server]
        public virtual bool AddTarget(GameObject target)
        {
            if (target == null || Targets.Contains(target)) return false;
            
            if (!IsValidTarget(target)) return false;
            
            Targets.Add(target);
            
            // Apply effect to target
            EffectImplementation?.Apply(target);
            
            // Update network state
            targetCount = Targets.Count;
            
            // Fire event
            OnTargetAdded?.Invoke(target);
            
            // Sync to clients
            RpcTargetAdded(target.GetComponent<NetworkIdentity>().netId);
            
            return true;
        }
        
        /// <summary>
        /// Remove a target from this effect
        /// </summary>
        /// <param name="target">Target to remove</param>
        /// <returns>True if target was removed successfully</returns>
        [Server]
        public virtual bool RemoveTarget(GameObject target)
        {
            if (target == null || !Targets.Contains(target)) return false;
            
            Targets.Remove(target);
            
            // Unapply effect from target
            EffectImplementation?.Unapply(target);
            
            // Update network state
            targetCount = Targets.Count;
            
            // Fire event
            OnTargetRemoved?.Invoke(target);
            
            // Sync to clients
            RpcTargetRemoved(target.GetComponent<NetworkIdentity>().netId);
            
            return true;
        }
        
        /// <summary>
        /// Remove multiple targets from this effect
        /// </summary>
        /// <param name="targets">Targets to remove</param>
        [Server]
        public virtual void RemoveTargets(IEnumerable<GameObject> targets)
        {
            foreach (var target in targets.ToList())
            {
                RemoveTarget(target);
            }
        }
        
        /// <summary>
        /// Check if this effect has a specific target
        /// </summary>
        /// <param name="target">Target to check</param>
        /// <returns>True if effect has this target</returns>
        public bool HasTarget(GameObject target)
        {
            return Targets.Contains(target);
        }
        
        #endregion
        
        #region Effect State Management
        
        /// <summary>
        /// Check if this effect is currently active
        /// </summary>
        /// <returns>True if effect is active</returns>
        public virtual bool IsEffectActive()
        {
            if (duration != EffectDuration.Persistent)
            {
                return isActive;
            }
            
            // Check if source card is in the correct location and not face down
            if (Source.Location != sourceLocation || Source.IsFaceDown)
            {
                return false;
            }
            
            // Check if source still has this effect
            bool effectOnSource = Source.PersistentEffects.Any(effect => effect == this);
            return effectOnSource && isActive;
        }
        
        /// <summary>
        /// Check conditions and update effect state
        /// </summary>
        /// <param name="stateChanged">Whether state has already changed</param>
        /// <returns>True if state changed during this check</returns>
        [Server]
        public virtual bool CheckCondition(bool stateChanged = false)
        {
            // Check if effect should be active
            if (!ActiveCondition(Context) || !IsEffectActive())
            {
                stateChanged = Targets.Count > 0 || stateChanged;
                Cancel();
                return stateChanged;
            }
            
            // Update existing targets
            stateChanged = UpdateExistingTargets(stateChanged);
            
            // Check for new targets
            stateChanged = CheckForNewTargets(stateChanged);
            
            return stateChanged;
        }
        
        /// <summary>
        /// Update existing targets and remove invalid ones
        /// </summary>
        /// <param name="stateChanged">Current state change status</param>
        /// <returns>Updated state change status</returns>
        private bool UpdateExistingTargets(bool stateChanged)
        {
            // Find invalid targets
            var invalidTargets = Targets.Where(target => 
                !MatchCondition(target, Context) || !IsValidTarget(target)).ToList();
            
            // Remove invalid targets
            RemoveTargets(invalidTargets);
            stateChanged = stateChanged || invalidTargets.Count > 0;
            
            // Recalculate effect for remaining valid targets
            foreach (var target in Targets.ToList())
            {
                if (EffectImplementation?.Recalculate(target) == true)
                {
                    stateChanged = true;
                }
            }
            
            return stateChanged;
        }
        
        /// <summary>
        /// Check for new targets and add them
        /// </summary>
        /// <param name="stateChanged">Current state change status</param>
        /// <returns>Updated state change status</returns>
        private bool CheckForNewTargets(bool stateChanged)
        {
            var potentialTargets = GetPotentialTargets();
            var newTargets = potentialTargets.Where(target => 
                !Targets.Contains(target) && IsValidTarget(target)).ToList();
            
            foreach (var newTarget in newTargets)
            {
                AddTarget(newTarget);
            }
            
            return stateChanged || newTargets.Count > 0;
        }
        
        /// <summary>
        /// Cancel this effect and remove all targets
        /// </summary>
        [Server]
        public virtual void Cancel()
        {
            var targetsToRemove = Targets.ToList();
            
            foreach (var target in targetsToRemove)
            {
                EffectImplementation?.Unapply(target);
            }
            
            Targets.Clear();
            targetCount = 0;
            isActive = false;
            
            OnEffectCancelled?.Invoke();
            
            // Sync to clients
            RpcEffectCancelled();
        }
        
        #endregion
        
        #region Network Synchronization
        
        /// <summary>
        /// Notify clients that a target was added
        /// </summary>
        /// <param name="targetNetId">Network ID of added target</param>
        [ClientRpc]
        protected virtual void RpcTargetAdded(uint targetNetId)
        {
            var targetObject = NetworkClient.spawned[targetNetId];
            if (targetObject != null)
            {
                // Handle client-side target addition
                HandleClientTargetAdded(targetObject.gameObject);
            }
        }
        
        /// <summary>
        /// Notify clients that a target was removed
        /// </summary>
        /// <param name="targetNetId">Network ID of removed target</param>
        [ClientRpc]
        protected virtual void RpcTargetRemoved(uint targetNetId)
        {
            if (NetworkClient.spawned.TryGetValue(targetNetId, out var targetObject))
            {
                // Handle client-side target removal
                HandleClientTargetRemoved(targetObject.gameObject);
            }
        }
        
        /// <summary>
        /// Notify clients that effect was cancelled
        /// </summary>
        [ClientRpc]
        protected virtual void RpcEffectCancelled()
        {
            // Handle client-side effect cancellation
            HandleClientEffectCancelled();
        }
        
        /// <summary>
        /// Handle target addition on client side
        /// </summary>
        /// <param name="target">Target that was added</param>
        protected virtual void HandleClientTargetAdded(GameObject target)
        {
            // Override in derived classes for client-specific handling
            OnTargetAdded?.Invoke(target);
        }
        
        /// <summary>
        /// Handle target removal on client side
        /// </summary>
        /// <param name="target">Target that was removed</param>
        protected virtual void HandleClientTargetRemoved(GameObject target)
        {
            // Override in derived classes for client-specific handling
            OnTargetRemoved?.Invoke(target);
        }
        
        /// <summary>
        /// Handle effect cancellation on client side
        /// </summary>
        protected virtual void HandleClientEffectCancelled()
        {
            // Override in derived classes for client-specific handling
            OnEffectCancelled?.Invoke();
        }
        
        #endregion
        
        #region Debug and Utilities
        
        /// <summary>
        /// Get debug information about this effect
        /// </summary>
        /// <returns>Debug information</returns>
        public virtual EffectDebugInfo GetDebugInfo()
        {
            return new EffectDebugInfo
            {
                EffectId = effectId,
                SourceName = Source?.Name ?? "Unknown",
                TargetNames = Targets?.Select(t => t.name).ToList() ?? new List<string>(),
                IsActive = IsEffectActive(),
                ConditionMet = ActiveCondition(Context),
                TargetCount = Targets?.Count ?? 0,
                Duration = duration.ToString(),
                EffectType = GetType().Name,
                ImplementationInfo = EffectImplementation?.GetDebugInfo()
            };
        }
        
        /// <summary>
        /// Get a string representation of this effect
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"{GetType().Name}({effectId}) - Source: {Source?.Name}, Targets: {Targets?.Count ?? 0}, Active: {IsEffectActive()}";
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        protected virtual void OnDestroy()
        {
            // Clean up events
            OnTargetAdded = null;
            OnTargetRemoved = null;
            OnEffectCancelled = null;
            
            // Cancel effect if still active
            if (isActive)
            {
                Cancel();
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Effect duration enumeration
    /// </summary>
    public enum EffectDuration
    {
        Instant,
        UntilEndOfPhase,
        UntilEndOfTurn,
        UntilEndOfRound,
        Persistent,
        Custom
    }
    
    /// <summary>
    /// Target controller enumeration
    /// </summary>
    public enum TargetController
    {
        Self,
        Opponent,
        Any
    }
    
    /// <summary>
    /// Effect properties configuration
    /// </summary>
    [Serializable]
    public class EffectProperties
    {
        public EffectDuration Duration = EffectDuration.Persistent;
        public CardLocation SourceLocation = CardLocation.PlayArea;
        public TargetController TargetController = TargetController.Self;
        public CardLocation TargetLocation = CardLocation.PlayArea;
        public bool CanChangeZoneOnce = false;
        
        // Delegate functions
        public Func<GameObject, EffectContext, bool> MatchCondition;
        public Func<EffectContext, bool> ActiveCondition;
        public Func<GameObject, bool> TargetValidation;
    }
    
    /// <summary>
    /// Effect context data
    /// </summary>
    [Serializable]
    public class EffectContext
    {
        public Player Player;
        public BaseCard Source;
        public Effect Effect;
        public Game Game;
        public object AdditionalData;
    }
    
    /// <summary>
    /// Debug information for effects
    /// </summary>
    [Serializable]
    public class EffectDebugInfo
    {
        public string EffectId;
        public string SourceName;
        public List<string> TargetNames;
        public bool IsActive;
        public bool ConditionMet;
        public int TargetCount;
        public string Duration;
        public string EffectType;
        public object ImplementationInfo;
        
        public override string ToString()
        {
            return $"{EffectType}: {SourceName} -> [{string.Join(", ", TargetNames)}] (Active: {IsActive})";
        }
    }
    
    /// <summary>
    /// Interface for effect implementations
    /// </summary>
    public interface IEffectImplementation
    {
        void Initialize(Effect effect, EffectContext context);
        void SetContext(EffectContext context);
        void Apply(GameObject target);
        void Unapply(GameObject target);
        bool Recalculate(GameObject target);
        object GetDebugInfo();
    }
}