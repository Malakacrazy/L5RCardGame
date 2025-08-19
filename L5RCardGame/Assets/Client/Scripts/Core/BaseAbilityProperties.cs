using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties configuration for BaseAbility instances.
    /// Defines costs, targets, timing, and behavior for card abilities.
    /// </summary>
    [System.Serializable]
    public class BaseAbilityProperties
    {
        [Header("Basic Properties")]
        [SerializeField] public string title = "";
        [SerializeField] public string printedText = "";
        [SerializeField] public bool printedAbility = true;
        [SerializeField] public bool persistentEffect = false;
        [SerializeField] public bool triggeredAbility = false;
        [SerializeField] public bool keywordAbility = false;
        [SerializeField] public bool cannotTargetFirst = false;
        
        [Header("Costs")]
        [SerializeReference] public List<ICost> costs = new List<ICost>();
        
        [Header("Targeting")]
        [SerializeReference] public List<ITargetDefinition> targets = new List<ITargetDefinition>();
        
        [Header("Timing")]
        [SerializeField] public AbilityType abilityType = AbilityType.Action;
        [SerializeField] public string triggeredByEvent = "";
        [SerializeField] public string phase = "";
        [SerializeField] public List<string> when = new List<string>();
        [SerializeField] public bool optional = true;
        [SerializeField] public int limit = -1; // -1 means no limit
        [SerializeField] public string limitType = ""; // "perTurn", "perRound", "perPhase", etc.
        
        [Header("Conditions")]
        [SerializeField] public string condition = "";
        [SerializeReference] public System.Func<AbilityContext, bool> conditionFunc;
        [SerializeReference] public System.Func<AbilityContext, bool> canTrigger;
        [SerializeReference] public System.Func<AbilityContext, bool> canResolve;
        
        [Header("Effects")]
        [SerializeReference] public System.Action<AbilityContext> effect;
        [SerializeReference] public System.Action<AbilityContext> then;
        [SerializeReference] public List<GameAction> gameActions = new List<GameAction>();
        
        [Header("Max/Min Targeting")]
        [SerializeField] public int max = -1; // Maximum targets (-1 = no limit)
        [SerializeField] public int maxPerTarget = 1; // Max per individual target
        [SerializeField] public int min = 0; // Minimum targets required
        [SerializeField] public string maxStat = ""; // Max based on card stat
        [SerializeField] public string minStat = ""; // Min based on card stat
        
        [Header("Priority and Ordering")]
        [SerializeField] public int priority = 0;
        [SerializeField] public bool immediateEffect = false;
        [SerializeField] public bool cannotBeCancelled = false;
        
        [Header("Meta Properties")]
        [SerializeField] public string location = ""; // Where ability can be used
        [SerializeField] public string[] locations = new string[0]; // Multiple valid locations
        [SerializeField] public bool doesNotTarget = false;
        [SerializeField] public bool ignoreEventCosts = false;
        [SerializeField] public bool resetOnCancel = false;
        
        #region Constructors
        
        public BaseAbilityProperties()
        {
            Initialize();
        }
        
        public BaseAbilityProperties(string title, System.Action<AbilityContext> effect = null)
        {
            this.title = title;
            this.effect = effect;
            Initialize();
        }
        
        public BaseAbilityProperties(string title, string printedText, System.Action<AbilityContext> effect = null)
        {
            this.title = title;
            this.printedText = printedText;
            this.effect = effect;
            Initialize();
        }
        
        private void Initialize()
        {
            if (costs == null) costs = new List<ICost>();
            if (targets == null) targets = new List<ITargetDefinition>();
            if (gameActions == null) gameActions = new List<GameAction>();
            if (when == null) when = new List<string>();
        }
        
        #endregion
        
        #region Fluent Builder Methods
        
        /// <summary>
        /// Set the ability title
        /// </summary>
        public BaseAbilityProperties WithTitle(string title)
        {
            this.title = title;
            return this;
        }
        
        /// <summary>
        /// Set the printed text
        /// </summary>
        public BaseAbilityProperties WithText(string text)
        {
            this.printedText = text;
            return this;
        }
        
        /// <summary>
        /// Set the ability type
        /// </summary>
        public BaseAbilityProperties WithType(AbilityType type)
        {
            this.abilityType = type;
            return this;
        }
        
        /// <summary>
        /// Add a cost to the ability
        /// </summary>
        public BaseAbilityProperties WithCost(ICost cost)
        {
            if (cost != null)
                costs.Add(cost);
            return this;
        }
        
        /// <summary>
        /// Add multiple costs to the ability
        /// </summary>
        public BaseAbilityProperties WithCosts(params ICost[] costs)
        {
            if (costs != null)
                this.costs.AddRange(costs.Where(c => c != null));
            return this;
        }
        
        /// <summary>
        /// Add a target definition
        /// </summary>
        public BaseAbilityProperties WithTarget(ITargetDefinition target)
        {
            if (target != null)
                targets.Add(target);
            return this;
        }
        
        /// <summary>
        /// Add multiple target definitions
        /// </summary>
        public BaseAbilityProperties WithTargets(params ITargetDefinition[] targets)
        {
            if (targets != null)
                this.targets.AddRange(targets.Where(t => t != null));
            return this;
        }
        
        /// <summary>
        /// Add a target by name and properties
        /// </summary>
        public BaseAbilityProperties WithTarget(string name, TargetProperties properties)
        {
            var target = new TargetDefinition(name, properties);
            return WithTarget(target);
        }
        
        /// <summary>
        /// Set the effect function
        /// </summary>
        public BaseAbilityProperties WithEffect(System.Action<AbilityContext> effect)
        {
            this.effect = effect;
            return this;
        }
        
        /// <summary>
        /// Set the then effect (executes after main effect)
        /// </summary>
        public BaseAbilityProperties WithThen(System.Action<AbilityContext> then)
        {
            this.then = then;
            return this;
        }
        
        /// <summary>
        /// Add a game action
        /// </summary>
        public BaseAbilityProperties WithGameAction(GameAction action)
        {
            if (action != null)
                gameActions.Add(action);
            return this;
        }
        
        /// <summary>
        /// Add multiple game actions
        /// </summary>
        public BaseAbilityProperties WithGameActions(params GameAction[] actions)
        {
            if (actions != null)
                gameActions.AddRange(actions.Where(a => a != null));
            return this;
        }
        
        /// <summary>
        /// Set the condition function
        /// </summary>
        public BaseAbilityProperties WithCondition(System.Func<AbilityContext, bool> condition)
        {
            this.conditionFunc = condition;
            return this;
        }
        
        /// <summary>
        /// Set when the ability can trigger
        /// </summary>
        public BaseAbilityProperties WithWhen(params string[] whenConditions)
        {
            if (whenConditions != null)
                when.AddRange(whenConditions);
            return this;
        }
        
        /// <summary>
        /// Set the triggered event
        /// </summary>
        public BaseAbilityProperties TriggeredBy(string eventName)
        {
            this.triggeredByEvent = eventName;
            this.triggeredAbility = true;
            return this;
        }
        
        /// <summary>
        /// Set the phase restriction
        /// </summary>
        public BaseAbilityProperties InPhase(string phase)
        {
            this.phase = phase;
            return this;
        }
        
        /// <summary>
        /// Set targeting limits
        /// </summary>
        public BaseAbilityProperties WithLimits(int min = 0, int max = -1)
        {
            this.min = min;
            this.max = max;
            return this;
        }
        
        /// <summary>
        /// Set usage limit
        /// </summary>
        public BaseAbilityProperties WithLimit(int limit, string limitType = "perTurn")
        {
            this.limit = limit;
            this.limitType = limitType;
            return this;
        }
        
        /// <summary>
        /// Set location restriction
        /// </summary>
        public BaseAbilityProperties InLocation(string location)
        {
            this.location = location;
            return this;
        }
        
        /// <summary>
        /// Set multiple location restrictions
        /// </summary>
        public BaseAbilityProperties InLocations(params string[] locations)
        {
            this.locations = locations ?? new string[0];
            return this;
        }
        
        /// <summary>
        /// Mark as optional ability
        /// </summary>
        public BaseAbilityProperties AsOptional(bool optional = true)
        {
            this.optional = optional;
            return this;
        }
        
        /// <summary>
        /// Mark as persistent effect
        /// </summary>
        public BaseAbilityProperties AsPersistent(bool persistent = true)
        {
            this.persistentEffect = persistent;
            return this;
        }
        
        /// <summary>
        /// Mark as keyword ability
        /// </summary>
        public BaseAbilityProperties AsKeyword(bool keyword = true)
        {
            this.keywordAbility = keyword;
            return this;
        }
        
        /// <summary>
        /// Mark as non-printed ability
        /// </summary>
        public BaseAbilityProperties AsNonPrinted(bool nonPrinted = true)
        {
            this.printedAbility = !nonPrinted;
            return this;
        }
        
        /// <summary>
        /// Mark as immediate effect
        /// </summary>
        public BaseAbilityProperties AsImmediate(bool immediate = true)
        {
            this.immediateEffect = immediate;
            return this;
        }
        
        /// <summary>
        /// Mark as uncancellable
        /// </summary>
        public BaseAbilityProperties AsUncancellable(bool uncancellable = true)
        {
            this.cannotBeCancelled = uncancellable;
            return this;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate the ability properties
        /// </summary>
        public bool IsValid()
        {
            // Check required fields
            if (string.IsNullOrEmpty(title))
                return false;
            
            // Check that we have either an effect or game actions
            if (effect == null && gameActions.Count == 0)
                return false;
            
            // Validate costs
            if (costs.Any(cost => cost == null))
                return false;
            
            // Validate targets
            if (targets.Any(target => target == null))
                return false;
            
            // Check min/max consistency
            if (max >= 0 && min > max)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Get validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            if (string.IsNullOrEmpty(title))
                errors.Add("Title is required");
            
            if (effect == null && gameActions.Count == 0)
                errors.Add("Either effect function or game actions must be specified");
            
            if (costs.Any(cost => cost == null))
                errors.Add("All costs must be non-null");
            
            if (targets.Any(target => target == null))
                errors.Add("All targets must be non-null");
            
            if (max >= 0 && min > max)
                errors.Add("Minimum targets cannot exceed maximum targets");
            
            return errors;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action ability properties
        /// </summary>
        public static BaseAbilityProperties CreateAction(string title, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.Action);
        }
        
        /// <summary>
        /// Create triggered ability properties
        /// </summary>
        public static BaseAbilityProperties CreateTriggered(string title, string triggeredBy, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.Triggered)
                .TriggeredBy(triggeredBy);
        }
        
        /// <summary>
        /// Create persistent effect properties
        /// </summary>
        public static BaseAbilityProperties CreatePersistent(string title, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.Persistent)
                .AsPersistent(true);
        }
        
        /// <summary>
        /// Create keyword ability properties
        /// </summary>
        public static BaseAbilityProperties CreateKeyword(string title)
        {
            return new BaseAbilityProperties(title)
                .WithType(AbilityType.Keyword)
                .AsKeyword(true)
                .AsNonPrinted(true);
        }
        
        /// <summary>
        /// Create forced triggered ability properties
        /// </summary>
        public static BaseAbilityProperties CreateForcedTriggered(string title, string triggeredBy, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.ForcedTriggered)
                .TriggeredBy(triggeredBy)
                .AsOptional(false);
        }
        
        /// <summary>
        /// Create forced reaction properties
        /// </summary>
        public static BaseAbilityProperties CreateForcedReaction(string title, string triggeredBy, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.ForcedReaction)
                .TriggeredBy(triggeredBy)
                .AsOptional(false);
        }
        
        /// <summary>
        /// Create reaction properties
        /// </summary>
        public static BaseAbilityProperties CreateReaction(string title, string triggeredBy, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.Reaction)
                .TriggeredBy(triggeredBy)
                .AsOptional(true);
        }
        
        /// <summary>
        /// Create interrupt properties
        /// </summary>
        public static BaseAbilityProperties CreateInterrupt(string title, string triggeredBy, System.Action<AbilityContext> effect)
        {
            return new BaseAbilityProperties(title, effect)
                .WithType(AbilityType.Interrupt)
                .TriggeredBy(triggeredBy)
                .AsOptional(true);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Copy these properties
        /// </summary>
        public BaseAbilityProperties Copy()
        {
            var copy = new BaseAbilityProperties
            {
                title = title,
                printedText = printedText,
                printedAbility = printedAbility,
                persistentEffect = persistentEffect,
                triggeredAbility = triggeredAbility,
                keywordAbility = keywordAbility,
                cannotTargetFirst = cannotTargetFirst,
                abilityType = abilityType,
                triggeredByEvent = triggeredByEvent,
                phase = phase,
                optional = optional,
                limit = limit,
                limitType = limitType,
                condition = condition,
                max = max,
                maxPerTarget = maxPerTarget,
                min = min,
                maxStat = maxStat,
                minStat = minStat,
                priority = priority,
                immediateEffect = immediateEffect,
                cannotBeCancelled = cannotBeCancelled,
                location = location,
                doesNotTarget = doesNotTarget,
                ignoreEventCosts = ignoreEventCosts,
                resetOnCancel = resetOnCancel
            };
            
            copy.costs = new List<ICost>(costs);
            copy.targets = new List<ITargetDefinition>(targets);
            copy.gameActions = new List<GameAction>(gameActions);
            copy.when = new List<string>(when);
            copy.locations = (string[])locations.Clone();
            
            // Note: Functions are not copied as they may contain references
            // These should be set separately if needed
            
            return copy;
        }
        
        /// <summary>
        /// Get debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"BaseAbilityProperties[{title}]({abilityType}, Costs:{costs.Count}, Targets:{targets.Count})";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Types of abilities in the game
    /// </summary>
    public enum AbilityType
    {
        Action,
        Triggered,
        Persistent,
        Keyword,
        ForcedTriggered,
        ForcedReaction,
        Reaction,
        Interrupt,
        WouldInterrupt
    }
    
    /// <summary>
    /// Properties for target definitions
    /// </summary>
    [System.Serializable]
    public class TargetProperties
    {
        public string cardType = "";
        public string controller = "";
        public string location = "";
        public bool optional = false;
        public System.Func<BaseCard, AbilityContext, bool> cardCondition;
        public string mode = "single"; // "single", "multiple", "unlimited"
        public int min = 0;
        public int max = -1;
        
        public TargetProperties() { }
        
        public TargetProperties(string cardType, string location = "any")
        {
            this.cardType = cardType;
            this.location = location;
        }
    }
    
    /// <summary>
    /// Interface for target definitions
    /// </summary>
    public interface ITargetDefinition
    {
        string Name { get; }
        TargetProperties Properties { get; }
        bool CanTarget(BaseCard card, AbilityContext context);
        List<BaseCard> GetLegalTargets(AbilityContext context);
    }
    
    /// <summary>
    /// Basic target definition implementation
    /// </summary>
    [System.Serializable]
    public class TargetDefinition : ITargetDefinition
    {
        [SerializeField] private string name;
        [SerializeField] private TargetProperties properties;
        
        public string Name => name;
        public TargetProperties Properties => properties;
        
        public TargetDefinition(string name, TargetProperties properties)
        {
            this.name = name;
            this.properties = properties;
        }
        
        public bool CanTarget(BaseCard card, AbilityContext context)
        {
            if (card == null) return false;
            
            // Check card type
            if (!string.IsNullOrEmpty(properties.cardType) && 
                card.GetCardType() != properties.cardType)
                return false;
            
            // Check controller
            if (!string.IsNullOrEmpty(properties.controller))
            {
                switch (properties.controller.ToLower())
                {
                    case "self":
                        if (card.controller != context.player) return false;
                        break;
                    case "opponent":
                        if (card.controller == context.player) return false;
                        break;
                    case "any":
                        break;
                    default:
                        return false;
                }
            }
            
            // Check location
            if (!string.IsNullOrEmpty(properties.location) && properties.location != "any")
            {
                if (card.location != properties.location) return false;
            }
            
            // Check custom condition
            if (properties.cardCondition != null)
            {
                if (!properties.cardCondition(card, context)) return false;
            }
            
            // Check if card can be targeted
            if (!card.CanBeTargeted(context)) return false;
            
            return true;
        }
        
        public List<BaseCard> GetLegalTargets(AbilityContext context)
        {
            var allCards = context.game.GetAllCards();
            return allCards.Where(card => CanTarget(card, context)).ToList();
        }
    }
}
