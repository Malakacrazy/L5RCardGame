using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    #region Interfaces
    
    /// <summary>
    /// Base interface for all game action properties
    /// </summary>
    public interface IGameActionProperties
    {
        List<object> Target { get; set; }
        bool CannotBeCancelled { get; set; }
        bool Optional { get; set; }
        GameAction ParentAction { get; set; }
    }

    /// <summary>
    /// Interface for card-specific action properties
    /// </summary>
    public interface ICardActionProperties : IGameActionProperties
    {
        BaseCard CardTarget { get; set; }
    }

    /// <summary>
    /// Interface for player-specific action properties
    /// </summary>
    public interface IPlayerActionProperties : IGameActionProperties
    {
        Player PlayerTarget { get; set; }
    }

    #endregion

    #region Base Properties Classes

    /// <summary>
    /// Base properties for all game actions
    /// </summary>
    [Serializable]
    public class GameActionProperties : IGameActionProperties
    {
        public List<object> Target { get; set; } = new List<object>();
        public bool CannotBeCancelled { get; set; }
        public bool Optional { get; set; }
        public GameAction ParentAction { get; set; }

        public GameActionProperties()
        {
            Target = new List<object>();
        }

        public GameActionProperties(List<object> targets, bool cannotBeCancelled = false, bool optional = false)
        {
            Target = targets ?? new List<object>();
            CannotBeCancelled = cannotBeCancelled;
            Optional = optional;
        }
    }

    /// <summary>
    /// Properties for card-targeting actions
    /// </summary>
    [Serializable]
    public class CardActionProperties : GameActionProperties, ICardActionProperties
    {
        public BaseCard CardTarget { get; set; }

        public CardActionProperties() : base() { }

        public CardActionProperties(BaseCard target) : base()
        {
            CardTarget = target;
            if (target != null)
                Target.Add(target);
        }
    }

    /// <summary>
    /// Properties for player-targeting actions
    /// </summary>
    [Serializable]
    public class PlayerActionProperties : GameActionProperties, IPlayerActionProperties
    {
        public Player PlayerTarget { get; set; }

        public PlayerActionProperties() : base() { }

        public PlayerActionProperties(Player target) : base()
        {
            PlayerTarget = target;
            if (target != null)
                Target.Add(target);
        }
    }

    #endregion

    #region Base GameAction Classes

    /// <summary>
    /// Base class for all game actions
    /// </summary>
    [Serializable]
    public abstract class GameAction
    {
        [Header("Game Action Configuration")]
        [SerializeField] protected string actionName = "";
        [SerializeField] protected string costMessage = "";
        [SerializeField] protected string effectMessage = "";
        [SerializeField] protected string eventName = EventNames.Unnamed;
        
        // Properties
        protected GameActionProperties defaultProperties;
        protected Func<AbilityContext, GameActionProperties> propertyFactory;
        protected GameActionProperties staticProperties;
        
        #region Properties
        
        public virtual string Name => actionName;
        public virtual string Cost => costMessage;
        public virtual string Effect => effectMessage;
        public virtual string EventName => eventName;
        public virtual string[] TargetType => new string[] { "any" };
        
        public GameActionProperties Properties => staticProperties ?? defaultProperties;
        
        #endregion

        #region Constructors

        protected GameAction()
        {
            Initialize();
        }

        protected GameAction(GameActionProperties properties)
        {
            Initialize();
            staticProperties = properties;
        }

        protected GameAction(Func<AbilityContext, GameActionProperties> factory)
        {
            Initialize();
            propertyFactory = factory;
        }

        #endregion

        #region Initialization

        protected virtual void Initialize()
        {
            defaultProperties = new GameActionProperties();
        }

        #endregion

        #region Virtual Methods

        public virtual bool CanAffect(object target, AbilityContext context, object additionalProperties = null)
        {
            return target != null;
        }

        public virtual bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Target.Any(t => CanAffect(t, context, additionalProperties));
        }

        public virtual void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            // Override in derived classes
        }

        public virtual bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            return false;
        }

        public virtual string GetEffectMessage(AbilityContext context)
        {
            return effectMessage;
        }

        public virtual void SetDefaultTarget(Func<object> targetFunc)
        {
            // Override in derived classes
        }

        protected virtual GameActionProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            if (propertyFactory != null)
                return propertyFactory(context);
            return staticProperties ?? defaultProperties;
        }

        protected virtual void AddPropertiesToEvent(object eventObj, object target, AbilityContext context, object additionalProperties)
        {
            // Override in derived classes
        }

        protected virtual bool CheckEventCondition(object eventObj, object additionalProperties = null)
        {
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Base class for card-targeting game actions
    /// </summary>
    [Serializable]
    public abstract class CardGameAction : GameAction
    {
        public override string[] TargetType => new string[] { "card" };

        protected CardGameAction() : base() { }
        protected CardGameAction(CardActionProperties properties) : base(properties) { }
        protected CardGameAction(Func<AbilityContext, CardActionProperties> factory) : base((context) => factory(context)) { }

        public override bool CanAffect(object target, AbilityContext context, object additionalProperties = null)
        {
            return target is BaseCard card && CanAffect(card, context, additionalProperties);
        }

        public virtual bool CanAffect(BaseCard card, AbilityContext context, object additionalProperties = null)
        {
            return card != null && base.CanAffect(card, context, additionalProperties);
        }
    }

    /// <summary>
    /// Base class for player-targeting game actions
    /// </summary>
    [Serializable]
    public abstract class PlayerGameAction : GameAction
    {
        public override string[] TargetType => new string[] { "player" };

        protected PlayerGameAction() : base() { }
        protected PlayerGameAction(PlayerActionProperties properties) : base(properties) { }
        protected PlayerGameAction(Func<AbilityContext, PlayerActionProperties> factory) : base((context) => factory(context)) { }

        public override bool CanAffect(object target, AbilityContext context, object additionalProperties = null)
        {
            return target is Player player && CanAffect(player, context, additionalProperties);
        }

        public virtual bool CanAffect(Player player, AbilityContext context, object additionalProperties = null)
        {
            return player != null && base.CanAffect(player, context, additionalProperties);
        }
    }

    #endregion
}
