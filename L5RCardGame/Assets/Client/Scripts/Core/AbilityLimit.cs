using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base interface for all ability limits
    /// </summary>
    public interface IAbilityLimit
    {
        /// <summary>
        /// Check if this limit can reset automatically
        /// </summary>
        /// <returns>True if repeatable</returns>
        bool IsRepeatable();

        /// <summary>
        /// Get the maximum uses allowed for a player
        /// </summary>
        /// <param name="player">Player to check limit for</param>
        /// <returns>Maximum number of uses</returns>
        int GetModifiedMax(Player player);

        /// <summary>
        /// Check if a player has reached the maximum uses
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if at maximum uses</returns>
        bool IsAtMax(Player player);

        /// <summary>
        /// Increment the use count for a player
        /// </summary>
        /// <param name="player">Player who used the ability</param>
        void Increment(Player player);

        /// <summary>
        /// Reset the use counts for all players
        /// </summary>
        void Reset();

        /// <summary>
        /// Register for game events (if needed)
        /// </summary>
        /// <param name="game">Game instance to register with</param>
        void RegisterEvents(Game game);

        /// <summary>
        /// Unregister from game events
        /// </summary>
        /// <param name="game">Game instance to unregister from</param>
        void UnregisterEvents(Game game);

        /// <summary>
        /// Set the ability this limit belongs to
        /// </summary>
        /// <param name="ability">Ability instance</param>
        void SetAbility(object ability);
    }

    /// <summary>
    /// Fixed ability limit that never resets automatically
    /// </summary>
    [System.Serializable]
    public class FixedAbilityLimit : IAbilityLimit
    {
        [Header("Fixed Limit")]
        [SerializeField] private int max;
        [SerializeField] private Dictionary<string, int> useCount = new Dictionary<string, int>();
        
        /// <summary>
        /// The ability this limit belongs to
        /// </summary>
        public object ability { get; private set; }

        /// <summary>
        /// Create a fixed ability limit
        /// </summary>
        /// <param name="maximum">Maximum number of uses</param>
        public FixedAbilityLimit(int maximum)
        {
            max = maximum;
            useCount = new Dictionary<string, int>();
            ability = null;
        }

        /// <summary>
        /// Fixed limits are not repeatable
        /// </summary>
        /// <returns>Always false</returns>
        public virtual bool IsRepeatable()
        {
            return false;
        }

        /// <summary>
        /// Get the maximum uses, considering card modifications
        /// </summary>
        /// <param name="player">Player to check for</param>
        /// <returns>Modified maximum uses</returns>
        public virtual int GetModifiedMax(Player player)
        {
            // If we have an ability with a card, check for limit modifications
            if (ability is BaseAbility baseAbility && baseAbility.card != null)
            {
                return baseAbility.card.GetModifiedLimitMax(player, baseAbility, max);
            }

            return max;
        }

        /// <summary>
        /// Check if player has reached maximum uses
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if at maximum</returns>
        public virtual bool IsAtMax(Player player)
        {
            if (player == null) return false;

            int currentUses = useCount.GetValueOrDefault(player.name, 0);
            int maxUses = GetModifiedMax(player);
            
            return currentUses >= maxUses;
        }

        /// <summary>
        /// Increment use count for a player
        /// </summary>
        /// <param name="player">Player who used the ability</param>
        public virtual void Increment(Player player)
        {
            if (player == null) return;

            if (useCount.ContainsKey(player.name))
            {
                useCount[player.name]++;
            }
            else
            {
                useCount[player.name] = 1;
            }

            Debug.Log($"🔒 Ability limit incremented for {player.name}: {useCount[player.name]}/{GetModifiedMax(player)}");
        }

        /// <summary>
        /// Reset all use counts
        /// </summary>
        public virtual void Reset()
        {
            useCount.Clear();
            Debug.Log("🔒 Fixed ability limit reset");
        }

        /// <summary>
        /// Fixed limits don't register for events
        /// </summary>
        /// <param name="game">Game instance</param>
        public virtual void RegisterEvents(Game game)
        {
            // No event handling for fixed limits
        }

        /// <summary>
        /// Fixed limits don't unregister from events
        /// </summary>
        /// <param name="game">Game instance</param>
        public virtual void UnregisterEvents(Game game)
        {
            // No event handling for fixed limits
        }

        /// <summary>
        /// Set the ability this limit belongs to
        /// </summary>
        /// <param name="abilityInstance">Ability instance</param>
        public virtual void SetAbility(object abilityInstance)
        {
            ability = abilityInstance;
        }

        /// <summary>
        /// Get current use count for a player
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>Current use count</returns>
        public int GetUseCount(Player player)
        {
            return useCount.GetValueOrDefault(player?.name ?? "", 0);
        }

        /// <summary>
        /// Get remaining uses for a player
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>Remaining uses</returns>
        public int GetRemainingUses(Player player)
        {
            return Math.Max(0, GetModifiedMax(player) - GetUseCount(player));
        }
    }

    /// <summary>
    /// Repeatable ability limit that resets on specific game events
    /// </summary>
    [System.Serializable]
    public class RepeatableAbilityLimit : FixedAbilityLimit
    {
        [Header("Repeatable Limit")]
        [SerializeField] private string eventName;
        
        /// <summary>
        /// Event handler for resetting the limit
        /// </summary>
        private System.Action<GameEvent> resetHandler;

        /// <summary>
        /// Create a repeatable ability limit
        /// </summary>
        /// <param name="maximum">Maximum number of uses</param>
        /// <param name="resetEventName">Event that resets the limit</param>
        public RepeatableAbilityLimit(int maximum, string resetEventName) : base(maximum)
        {
            eventName = resetEventName;
            resetHandler = (gameEvent) => Reset();
        }

        /// <summary>
        /// Repeatable limits are repeatable
        /// </summary>
        /// <returns>Always true</returns>
        public override bool IsRepeatable()
        {
            return true;
        }

        /// <summary>
        /// Register for the reset event
        /// </summary>
        /// <param name="game">Game instance to register with</param>
        public override void RegisterEvents(Game game)
        {
            if (game != null && !string.IsNullOrEmpty(eventName))
            {
                game.on(eventName, resetHandler);
                Debug.Log($"🔒 Registered repeatable limit for event: {eventName}");
            }
        }

        /// <summary>
        /// Unregister from the reset event
        /// </summary>
        /// <param name="game">Game instance to unregister from</param>
        public override void UnregisterEvents(Game game)
        {
            if (game != null && !string.IsNullOrEmpty(eventName))
            {
                game.removeListener(eventName, resetHandler);
                Debug.Log($"🔒 Unregistered repeatable limit for event: {eventName}");
            }
        }

        /// <summary>
        /// Reset with logging for repeatable limits
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Debug.Log($"🔒 Repeatable ability limit reset on event: {eventName}");
        }

        /// <summary>
        /// Get the event name that resets this limit
        /// </summary>
        /// <returns>Event name</returns>
        public string GetEventName()
        {
            return eventName;
        }
    }

    /// <summary>
    /// Static factory class for creating ability limits
    /// </summary>
    public static class AbilityLimit
    {
        /// <summary>
        /// Create a fixed ability limit (never resets)
        /// </summary>
        /// <param name="max">Maximum number of uses</param>
        /// <returns>Fixed ability limit</returns>
        public static IAbilityLimit Fixed(int max)
        {
            return new FixedAbilityLimit(max);
        }

        /// <summary>
        /// Create a repeatable ability limit with custom event
        /// </summary>
        /// <param name="max">Maximum number of uses</param>
        /// <param name="eventName">Event that resets the limit</param>
        /// <returns>Repeatable ability limit</returns>
        public static IAbilityLimit Repeatable(int max, string eventName)
        {
            return new RepeatableAbilityLimit(max, eventName);
        }

        /// <summary>
        /// Create a per-conflict ability limit
        /// </summary>
        /// <param name="max">Maximum uses per conflict</param>
        /// <returns>Per-conflict ability limit</returns>
        public static IAbilityLimit PerConflict(int max)
        {
            return new RepeatableAbilityLimit(max, EventNames.OnConflictFinished);
        }

        /// <summary>
        /// Create a per-phase ability limit
        /// </summary>
        /// <param name="max">Maximum uses per phase</param>
        /// <returns>Per-phase ability limit</returns>
        public static IAbilityLimit PerPhase(int max)
        {
            return new RepeatableAbilityLimit(max, EventNames.OnPhaseEnded);
        }

        /// <summary>
        /// Create a per-round ability limit
        /// </summary>
        /// <param name="max">Maximum uses per round</param>
        /// <returns>Per-round ability limit</returns>
        public static IAbilityLimit PerRound(int max)
        {
            return new RepeatableAbilityLimit(max, EventNames.OnRoundEnded);
        }

        /// <summary>
        /// Create an unlimited per-conflict ability limit
        /// </summary>
        /// <returns>Unlimited per-conflict ability limit</returns>
        public static IAbilityLimit UnlimitedPerConflict()
        {
            return new RepeatableAbilityLimit(int.MaxValue, EventNames.OnConflictFinished);
        }

        /// <summary>
        /// Create an unlimited ability limit (no restrictions)
        /// </summary>
        /// <returns>Unlimited ability limit</returns>
        public static IAbilityLimit Unlimited()
        {
            return new UnlimitedAbilityLimit();
        }

        /// <summary>
        /// Create a per-duel ability limit
        /// </summary>
        /// <param name="max">Maximum uses per duel</param>
        /// <returns>Per-duel ability limit</returns>
        public static IAbilityLimit PerDuel(int max)
        {
            return new RepeatableAbilityLimit(max, EventNames.OnDuelFinished);
        }

        /// <summary>
        /// Create a limit that resets when a player passes priority
        /// </summary>
        /// <param name="max">Maximum uses per priority window</param>
        /// <returns>Per-priority ability limit</returns>
        public static IAbilityLimit PerPriority(int max)
        {
            return new RepeatableAbilityLimit(max, EventNames.OnPassActionPhasePriority);
        }
    }

    /// <summary>
    /// Unlimited ability limit for abilities with no restrictions
    /// </summary>
    public class UnlimitedAbilityLimit : IAbilityLimit
    {
        public bool IsRepeatable() => false;
        public int GetModifiedMax(Player player) => int.MaxValue;
        public bool IsAtMax(Player player) => false;
        public void Increment(Player player) { /* No tracking needed */ }
        public void Reset() { /* Nothing to reset */ }
        public void RegisterEvents(Game game) { /* No events needed */ }
        public void UnregisterEvents(Game game) { /* No events needed */ }
        public void SetAbility(object ability) { /* No ability reference needed */ }
    }

    /// <summary>
    /// Base class for card abilities that can have limits
    /// </summary>
    public abstract class BaseAbility
    {
        [Header("Ability Limit")]
        public IAbilityLimit limit;
        public BaseCard card;

        protected BaseAbility()
        {
            limit = AbilityLimit.Unlimited();
        }

        /// <summary>
        /// Set the limit for this ability
        /// </summary>
        /// <param name="abilityLimit">Limit to apply</param>
        public void SetLimit(IAbilityLimit abilityLimit)
        {
            // Unregister old limit
            if (limit != null && card?.game != null)
            {
                limit.UnregisterEvents(card.game);
            }

            limit = abilityLimit ?? AbilityLimit.Unlimited();
            limit.SetAbility(this);

            // Register new limit
            if (card?.game != null)
            {
                limit.RegisterEvents(card.game);
            }
        }

        /// <summary>
        /// Check if this ability can be used by a player
        /// </summary>
        /// <param name="player">Player attempting to use the ability</param>
        /// <returns>True if ability can be used</returns>
        public virtual bool CanUse(Player player)
        {
            return limit == null || !limit.IsAtMax(player);
        }

        /// <summary>
        /// Use this ability (increment the limit counter)
        /// </summary>
        /// <param name="player">Player using the ability</param>
        public virtual void Use(Player player)
        {
            limit?.Increment(player);
        }

        /// <summary>
        /// Get remaining uses for a player
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>Remaining uses</returns>
        public virtual int GetRemainingUses(Player player)
        {
            if (limit == null) return int.MaxValue;
            
            int maxUses = limit.GetModifiedMax(player);
            if (maxUses == int.MaxValue) return int.MaxValue;
            
            if (limit is FixedAbilityLimit fixedLimit)
            {
                return fixedLimit.GetRemainingUses(player);
            }
            
            return maxUses; // Fallback for other limit types
        }

        /// <summary>
        /// Initialize the ability with a card reference
        /// </summary>
        /// <param name="sourceCard">Card this ability belongs to</param>
        public virtual void Initialize(BaseCard sourceCard)
        {
            card = sourceCard;
            if (limit != null)
            {
                limit.SetAbility(this);
                if (card?.game != null)
                {
                    limit.RegisterEvents(card.game);
                }
            }
        }

        /// <summary>
        /// Cleanup when ability is destroyed
        /// </summary>
        public virtual void Cleanup()
        {
            if (limit != null && card?.game != null)
            {
                limit.UnregisterEvents(card.game);
            }
        }
    }

    /// <summary>
    /// Extension methods for working with ability limits
    /// </summary>
    public static class AbilityLimitExtensions
    {
        /// <summary>
        /// Check if a player can use an ability considering its limit
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <param name="ability">Ability to check</param>
        /// <returns>True if ability can be used</returns>
        public static bool CanUseAbility(this Player player, BaseAbility ability)
        {
            return ability?.CanUse(player) ?? true;
        }

        /// <summary>
        /// Use an ability and increment its limit counter
        /// </summary>
        /// <param name="player">Player using the ability</param>
        /// <param name="ability">Ability being used</param>
        public static void UseAbility(this Player player, BaseAbility ability)
        {
            ability?.Use(player);
        }

        /// <summary>
        /// Get a summary of ability limits for a player
        /// </summary>
        /// <param name="player">Player to get summary for</param>
        /// <param name="abilities">List of abilities to check</param>
        /// <returns>Dictionary of ability limits</returns>
        public static Dictionary<string, AbilityLimitSummary> GetAbilityLimitSummary(this Player player, 
                                                                                     List<BaseAbility> abilities)
        {
            var summary = new Dictionary<string, AbilityLimitSummary>();
            
            foreach (var ability in abilities)
            {
                if (ability?.limit != null && ability.limit.GetModifiedMax(player) < int.MaxValue)
                {
                    var key = ability.GetType().Name;
                    summary[key] = new AbilityLimitSummary
                    {
                        maxUses = ability.limit.GetModifiedMax(player),
                        remainingUses = ability.GetRemainingUses(player),
                        isRepeatable = ability.limit.IsRepeatable(),
                        eventName = ability.limit is RepeatableAbilityLimit repeatable ? 
                                   repeatable.GetEventName() : null
                    };
                }
            }
            
            return summary;
        }
    }

    /// <summary>
    /// Summary information about an ability limit
    /// </summary>
    [System.Serializable]
    public class AbilityLimitSummary
    {
        public int maxUses;
        public int remainingUses;
        public bool isRepeatable;
        public string eventName;
    }

    /// <summary>
    /// Additional event names for ability limits
    /// </summary>
    public static partial class EventNames
    {
        // These should already be defined in other classes, but including for completeness
        public const string OnConflictFinished = "onConflictFinished";
        public const string OnPhaseEnded = "onPhaseEnded";
        public const string OnRoundEnded = "onRoundEnded";
        public const string OnDuelFinished = "onDuelFinished";
        public const string OnPassActionPhasePriority = "onPassActionPhasePriority";
    }
}