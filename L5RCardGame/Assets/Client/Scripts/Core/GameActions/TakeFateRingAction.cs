using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITakeFateRingProperties : IRingActionProperties
    {
        int Amount { get; set; }
    }

    public class TakeFateRingProperties : RingActionProperties, ITakeFateRingProperties
    {
        public int Amount { get; set; }
    }

    public class TakeFateRingAction : RingAction
    {
        public override string Name => "takeFate";
        public override string EventName => EventNames.OnMoveFate;

        protected override ITakeFateRingProperties DefaultProperties => new TakeFateRingProperties
        {
            Amount = 1
        };

        public TakeFateRingAction(object properties) : base(properties) { }

        public TakeFateRingAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITakeFateRingProperties;
            return ("take {1} fate from {0}", new object[] { properties.Target, properties.Amount });
        }

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITakeFateRingProperties;
            return context.Player.CheckRestrictions("takeFateFromRings", context) &&
                   ring.Fate > 0 && properties.Amount > 0 && base.CanAffect(ring, context);
        }

        protected override void AddPropertiesToEvent(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ITakeFateRingProperties;
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Fate = properties.Amount;
                gameEvent.Origin = ring;
                gameEvent.Context = context;
                gameEvent.Recipient = context.Player;
            }
        }

        protected override bool CheckEventCondition(object eventObj)
        {
            return MoveFateEventCondition(eventObj);
        }

        protected override bool IsEventFullyResolved(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ITakeFateRingProperties;
            
            if (eventObj is GameEvent gameEvent)
            {
                return !gameEvent.Cancelled && 
                       gameEvent.Name == this.EventName && 
                       gameEvent.Fate == properties.Amount && 
                       gameEvent.Origin == ring && 
                       gameEvent.Recipient == context.Player;
            }
            
            return false;
        }

        protected override void EventHandler(object eventObj)
        {
            MoveFateEventHandler(eventObj);
        }
    }
}
