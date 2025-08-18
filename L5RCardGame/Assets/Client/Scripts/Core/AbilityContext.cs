using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for creating an AbilityContext
    /// </summary>
    [System.Serializable]
    public class AbilityContextProperties
    {
        public Game game;
        public object source;
        public Player player;
        public BaseAbility ability;
        public Dictionary<string, object> costs;
        public Dictionary<string, object> targets;
        public Dictionary<string, object> rings;
        public Dictionary<string, object> selects;
        public Dictionary<string, object> tokens;
        public List<object> events;
        public string stage;
        public object targetAbility;
    }

    /// <summary>
    /// Provides context for ability execution, including targets, costs, and game state
    /// </summary>
    public class AbilityContext
    {
        [Header("Core Context")]
        public Game game;
        public object source;
        public Player player;
        public BaseAbility ability;

        [Header("Ability Data")]
        public Dictionary<string, object> costs = new Dictionary<string, object>();
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        public Dictionary<string, object> rings = new Dictionary<string, object>();
        public Dictionary<string, object> selects = new Dictionary<string, object>();
        public Dictionary<string, object> tokens = new Dictionary<string, object>();
        public List<object> events = new List<object>();
        public string stage;
        public object targetAbility;

        [Header("Resolved Context")]
        public object target;
        public string select;
        public Ring ring;
        public StatusToken token;

        [Header("Game State")]
        public List<ProvinceRefillData> provincesToRefill = new List<ProvinceRefillData>();
        public bool subResolution = false;
        public Player choosingPlayerOverride = null;
        public List<GameAction> gameActionsResolutionChain = new List<GameAction>();
        public string playType;

        /// <summary>
        /// Initialize the ability context with the provided properties
        /// </summary>
        /// <param name="properties">Context properties</param>
        public AbilityContext(AbilityContextProperties properties)
        {
            game = properties.game;
            source = properties.source ?? new EffectSource();
            player = properties.player;
            ability = properties.ability ?? new BaseAbility();
            costs = properties.costs ?? new Dictionary<string, object>();
            targets = properties.targets ?? new Dictionary<string, object>();
            rings = properties.rings ?? new Dictionary<string, object>();
            selects = properties.selects ?? new Dictionary<string, object>();
            tokens = properties.tokens ?? new Dictionary<string, object>();
            events = properties.events ?? new List<object>();
            stage = properties.stage ?? Stages.Effect;
            targetAbility = properties.targetAbility;

            // Determine play type from player's playable locations
            if (player != null && source != null)
            {
                var location = player.playableLocations?.FirstOrDefault(loc => loc.Contains(source));
                playType = location?.playingType;
            }
        }

        /// <summary>
        /// Creates a copy of this context with optional new properties
        /// </summary>
        /// <param name="newProps">Properties to override in the copy</param>
        /// <returns>New AbilityContext with modified properties</returns>
        public AbilityContext Copy(Dictionary<string, object> newProps = null)
        {
            var copy = CreateCopy(newProps);
            
            // Copy resolved context
            copy.target = target;
            copy.select = select;
            copy.ring = ring;
            copy.token = token;
            
            // Copy game state
            copy.provincesToRefill = new List<ProvinceRefillData>(provincesToRefill);
            copy.subResolution = subResolution;
            copy.choosingPlayerOverride = choosingPlayerOverride;
            copy.gameActionsResolutionChain = new List<GameAction>(gameActionsResolutionChain);
            copy.playType = playType;
            
            return copy;
        }

        /// <summary>
        /// Creates a copy with new properties merged in
        /// </summary>
        /// <param name="newProps">Properties to merge</param>
        /// <returns>New AbilityContext instance</returns>
        public AbilityContext CreateCopy(Dictionary<string, object> newProps = null)
        {
            var props = GetProps();
            
            if (newProps != null)
            {
                foreach (var kvp in newProps)
                {
                    var property = typeof(AbilityContextProperties).GetProperty(kvp.Key);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(props, kvp.Value);
                    }
                }
            }
            
            return new AbilityContext(props);
        }

        /// <summary>
        /// Queues a province to be refilled after the current ability resolves
        /// </summary>
        /// <param name="targetPlayer">Player whose province should be refilled</param>
        /// <param name="location">Province location to refill</param>
        public void RefillProvince(Player targetPlayer, string location)
        {
            provincesToRefill.Add(new ProvinceRefillData
            {
                player = targetPlayer,
                location = location
            });
        }

        /// <summary>
        /// Executes all queued province refills
        /// </summary>
        public void Refill()
        {
            foreach (var player in game.GetPlayersInFirstPlayerOrder())
            {
                var playerRefills = provincesToRefill.Where(refill => refill.player == player).ToList();
                
                foreach (var refill in playerRefills)
                {
                    game.QueueSimpleStep(() =>
                    {
                        refill.player.ReplaceDynastyCard(refill.location);
                        return true;
                    });
                }
            }
            
            provincesToRefill.Clear();
        }

        /// <summary>
        /// Gets the properties that define this context
        /// </summary>
        /// <returns>Properties object for creating copies</returns>
        public AbilityContextProperties GetProps()
        {
            return new AbilityContextProperties
            {
                game = game,
                source = source,
                player = player,
                ability = ability,
                costs = new Dictionary<string, object>(costs),
                targets = new Dictionary<string, object>(targets),
                rings = new Dictionary<string, object>(rings),
                selects = new Dictionary<string, object>(selects),
                tokens = new Dictionary<string, object>(tokens),
                events = new List<object>(events),
                stage = stage,
                targetAbility = targetAbility
            };
        }

        /// <summary>
        /// Gets the resolved target of the specified name
        /// </summary>
        /// <param name="targetName">Name of the target to retrieve</param>
        /// <returns>Resolved target or null if not found</returns>
        public object GetTarget(string targetName)
        {
            return targets.GetValueOrDefault(targetName);
        }

        /// <summary>
        /// Gets all resolved targets as a list
        /// </summary>
        /// <returns>List of all resolved targets</returns>
        public List<object> GetTargets()
        {
            return targets.Values.ToList();
        }

        /// <summary>
        /// Gets resolved targets of a specific type
        /// </summary>
        /// <typeparam name="T">Type to filter by</typeparam>
        /// <returns>List of targets of the specified type</returns>
        public List<T> GetTargets<T>() where T : class
        {
            return targets.Values.OfType<T>().ToList();
        }

        /// <summary>
        /// Sets a resolved target
        /// </summary>
        /// <param name="targetName">Name of the target</param>
        /// <param name="targetValue">Value of the target</param>
        public void SetTarget(string targetName, object targetValue)
        {
            targets[targetName] = targetValue;
        }

        /// <summary>
        /// Gets the resolved cost of the specified name
        /// </summary>
        /// <param name="costName">Name of the cost to retrieve</param>
        /// <returns>Resolved cost or null if not found</returns>
        public object GetCost(string costName)
        {
            return costs.GetValueOrDefault(costName);
        }

        /// <summary>
        /// Sets a resolved cost
        /// </summary>
        /// <param name="costName">Name of the cost</param>
        /// <param name="costValue">Value of the cost</param>
        public void SetCost(string costName, object costValue)
        {
            costs[costName] = costValue;
        }

        /// <summary>
        /// Gets the resolved ring of the specified name
        /// </summary>
        /// <param name="ringName">Name of the ring to retrieve</param>
        /// <returns>Resolved ring or null if not found</returns>
        public Ring GetRing(string ringName)
        {
            return rings.GetValueOrDefault(ringName) as Ring;
        }

        /// <summary>
        /// Sets a resolved ring
        /// </summary>
        /// <param name="ringName">Name of the ring</param>
        /// <param name="ringValue">Ring object</param>
        public void SetRing(string ringName, Ring ringValue)
        {
            rings[ringName] = ringValue;
        }

        /// <summary>
        /// Gets the resolved selection of the specified name
        /// </summary>
        /// <param name="selectName">Name of the selection to retrieve</param>
        /// <returns>Resolved selection or null if not found</returns>
        public object GetSelect(string selectName)
        {
            return selects.GetValueOrDefault(selectName);
        }

        /// <summary>
        /// Sets a resolved selection
        /// </summary>
        /// <param name="selectName">Name of the selection</param>
        /// <param name="selectValue">Value of the selection</param>
        public void SetSelect(string selectName, object selectValue)
        {
            selects[selectName] = selectValue;
        }

        /// <summary>
        /// Adds an event to the context
        /// </summary>
        /// <param name="gameEvent">Event to add</param>
        public void AddEvent(object gameEvent)
        {
            if (!events.Contains(gameEvent))
            {
                events.Add(gameEvent);
            }
        }

        /// <summary>
        /// Removes an event from the context
        /// </summary>
        /// <param name="gameEvent">Event to remove</param>
        public void RemoveEvent(object gameEvent)
        {
            events.Remove(gameEvent);
        }

        /// <summary>
        /// Checks if the context contains a specific event
        /// </summary>
        /// <param name="gameEvent">Event to check for</param>
        /// <returns>True if the event is in the context</returns>
        public bool HasEvent(object gameEvent)
        {
            return events.Contains(gameEvent);
        }

        /// <summary>
        /// Gets all events of a specific type
        /// </summary>
        /// <typeparam name="T">Type of events to retrieve</typeparam>
        /// <returns>List of events of the specified type</returns>
        public List<T> GetEvents<T>() where T : class
        {
            return events.OfType<T>().ToList();
        }

        /// <summary>
        /// Adds a game action to the resolution chain
        /// </summary>
        /// <param name="action">Game action to add</param>
        public void AddToResolutionChain(GameAction action)
        {
            if (!gameActionsResolutionChain.Contains(action))
            {
                gameActionsResolutionChain.Add(action);
            }
        }

        /// <summary>
        /// Removes a game action from the resolution chain
        /// </summary>
        /// <param name="action">Game action to remove</param>
        public void RemoveFromResolutionChain(GameAction action)
        {
            gameActionsResolutionChain.Remove(action);
        }

        /// <summary>
        /// Checks if the context is during a specific stage
        /// </summary>
        /// <param name="targetStage">Stage to check for</param>
        /// <returns>True if currently in the specified stage</returns>
        public bool IsDuringStage(string targetStage)
        {
            return stage == targetStage;
        }

        /// <summary>
        /// Changes the current stage of the context
        /// </summary>
        /// <param name="newStage">New stage to set</param>
        public void SetStage(string newStage)
        {
            stage = newStage;
        }

        /// <summary>
        /// Gets the effective choosing player (with override consideration)
        /// </summary>
        /// <returns>The player who should make choices</returns>
        public Player GetChoosingPlayer()
        {
            return choosingPlayerOverride ?? player;
        }

        /// <summary>
        /// Sets a temporary override for the choosing player
        /// </summary>
        /// <param name="overridePlayer">Player to override with</param>
        public void SetChoosingPlayerOverride(Player overridePlayer)
        {
            choosingPlayerOverride = overridePlayer;
        }

        /// <summary>
        /// Clears the choosing player override
        /// </summary>
        public void ClearChoosingPlayerOverride()
        {
            choosingPlayerOverride = null;
        }

        /// <summary>
        /// Checks if this is a sub-resolution of another ability
        /// </summary>
        /// <returns>True if this is a sub-resolution</returns>
        public bool IsSubResolution()
        {
            return subResolution;
        }

        /// <summary>
        /// Marks this context as a sub-resolution
        /// </summary>
        /// <param name="isSubResolution">Whether this is a sub-resolution</param>
        public void SetSubResolution(bool isSubResolution)
        {
            subResolution = isSubResolution;
        }

        /// <summary>
        /// Gets the source as a specific type
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <returns>Source cast to the specified type</returns>
        public T GetSource<T>() where T : class
        {
            return source as T;
        }

        /// <summary>
        /// Checks if the source is of a specific type
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if source is of the specified type</returns>
        public bool IsSourceOfType<T>() where T : class
        {
            return source is T;
        }

        /// <summary>
        /// Gets the play type if this context involves playing a card
        /// </summary>
        /// <returns>Play type or null if not applicable</returns>
        public string GetPlayType()
        {
            return playType;
        }

        /// <summary>
        /// Checks if the context is for playing from a specific location
        /// </summary>
        /// <param name="targetPlayType">Play type to check</param>
        /// <returns>True if playing from the specified location type</returns>
        public bool IsPlayType(string targetPlayType)
        {
            return playType == targetPlayType;
        }

        /// <summary>
        /// Creates a framework context for system effects
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="contextPlayer">Player for the context (optional)</param>
        /// <returns>Framework ability context</returns>
        public static AbilityContext CreateFrameworkContext(Game gameInstance, Player contextPlayer = null)
        {
            return new AbilityContext(new AbilityContextProperties
            {
                game = gameInstance,
                player = contextPlayer,
                source = new EffectSource(),
                ability = new BaseAbility(),
                stage = Stages.Effect
            });
        }

        /// <summary>
        /// Creates a context for card ability execution
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceCard">Source card</param>
        /// <param name="contextPlayer">Player executing the ability</param>
        /// <param name="cardAbility">The ability being executed</param>
        /// <returns>Card ability context</returns>
        public static AbilityContext CreateCardContext(Game gameInstance, BaseCard sourceCard, Player contextPlayer, BaseAbility cardAbility)
        {
            return new AbilityContext(new AbilityContextProperties
            {
                game = gameInstance,
                source = sourceCard,
                player = contextPlayer,
                ability = cardAbility,
                stage = Stages.PreTarget
            });
        }

        /// <summary>
        /// Creates a context for ring effect execution
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="sourceRing">Source ring</param>
        /// <param name="contextPlayer">Player executing the effect</param>
        /// <returns>Ring effect context</returns>
        public static AbilityContext CreateRingContext(Game gameInstance, Ring sourceRing, Player contextPlayer)
        {
            return new AbilityContext(new AbilityContextProperties
            {
                game = gameInstance,
                source = sourceRing,
                player = contextPlayer,
                stage = Stages.Effect
            });
        }

        /// <summary>
        /// Debug method to log context information
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogContext()
        {
            Debug.Log($"🎯 AbilityContext - Stage: {stage}, Player: {player?.name}, Source: {source?.GetType().Name}");
            Debug.Log($"   Targets: {targets.Count}, Costs: {costs.Count}, Events: {events.Count}");
            
            if (provincesToRefill.Count > 0)
                Debug.Log($"   Provinces to refill: {provincesToRefill.Count}");
                
            if (gameActionsResolutionChain.Count > 0)
                Debug.Log($"   Resolution chain: {gameActionsResolutionChain.Count} actions");
        }

        /// <summary>
        /// Gets a string representation of the context for debugging
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"AbilityContext[{stage}] - {source?.GetType().Name ?? "Unknown"} by {player?.name ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Data for province refill operations
    /// </summary>
    [System.Serializable]
    public class ProvinceRefillData
    {
        public Player player;
        public string location;
    }

    /// <summary>
    /// Stages of ability execution
    /// </summary>
    public static class Stages
    {
        public const string PreTarget = "preTarget";
        public const string Target = "target";
        public const string Cost = "cost";
        public const string Effect = "effect";
        public const string PostEffect = "postEffect";
    }

    /// <summary>
    /// Play types for different card locations
    /// </summary>
    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
        public const string PlayFromDiscard = "playFromDiscard";
        public const string PlayFromDeck = "playFromDeck";
    }

    /// <summary>
    /// Base ability class (placeholder for ability system)
    /// </summary>
    public class BaseAbility
    {
        public string title = "";
        public int defaultPriority = 0;
        public object limit;
        public object cost;
        public object target;
        public object effect;

        public BaseAbility() { }
        
        public BaseAbility(Dictionary<string, object> properties)
        {
            // Initialize from properties dictionary
            if (properties.ContainsKey("title"))
                title = properties["title"] as string;
            if (properties.ContainsKey("limit"))
                limit = properties["limit"];
            if (properties.ContainsKey("cost"))
                cost = properties["cost"];
            if (properties.ContainsKey("target"))
                target = properties["target"];
            if (properties.ContainsKey("effect"))
                effect = properties["effect"];
        }
    }

    /// <summary>
    /// Game action base class (placeholder)
    /// </summary>
    public class GameAction
    {
        public string actionType;
        public object target;
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public virtual bool CanExecute(AbilityContext context) { return true; }
        public virtual void Execute(AbilityContext context) { }
    }

    /// <summary>
    /// Status token class (placeholder)
    /// </summary>
    public class StatusToken
    {
        public string type;
        public object value;
        public BaseCard attachedTo;
    }

    /// <summary>
    /// Extension methods for AbilityContext
    /// </summary>
    public static class AbilityContextExtensions
    {
        /// <summary>
        /// Checks if the context has any resolved targets
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if there are resolved targets</returns>
        public static bool HasTargets(this AbilityContext context)
        {
            return context.targets.Count > 0;
        }

        /// <summary>
        /// Checks if the context has any resolved costs
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if there are resolved costs</returns>
        public static bool HasCosts(this AbilityContext context)
        {
            return context.costs.Count > 0;
        }

        /// <summary>
        /// Checks if the context has any events
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <returns>True if there are events</returns>
        public static bool HasEvents(this AbilityContext context)
        {
            return context.events.Count > 0;
        }

        /// <summary>
        /// Gets the first target of a specific type
        /// </summary>
        /// <typeparam name="T">Type to look for</typeparam>
        /// <param name="context">Context to search</param>
        /// <returns>First target of the specified type, or null</returns>
        public static T GetFirstTarget<T>(this AbilityContext context) where T : class
        {
            return context.targets.Values.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Checks if the context is for a specific player
        /// </summary>
        /// <param name="context">Context to check</param>
        /// <param name="targetPlayer">Player to check for</param>
        /// <returns>True if the context is for the specified player</returns>
        public static bool IsForPlayer(this AbilityContext context, Player targetPlayer)
        {
            return context.player == targetPlayer;
        }
    }
}