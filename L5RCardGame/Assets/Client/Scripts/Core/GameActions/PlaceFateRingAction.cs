using System;
using UnityEngine;

namespace L5RGame
{
    public interface IPlaceFateRingProperties : IRingActionProperties
    {
        int Amount { get; set; }
        object Origin { get; set; } // Can be DrawCard, Player, or Ring
    }

    public class PlaceFateRingProperties : RingActionProperties, IPlaceFateRingProperties
    {
        public int Amount { get; set; }
        public object Origin { get; set; }
    }

    public class PlaceFateRingAction : RingAction
    {
        public override string Name => "placeFate";
        public override string EventName => EventNames.OnMoveFate;

        protected override IPlaceFateRingProperties DefaultProperties => new PlaceFateRingProperties
        {
            Amount = 1
        };

        public PlaceFateRingAction(object properties) : base(properties) { }

        public PlaceFateRingAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override (string, object[]) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IPlaceFateRingProperties;
            return ("placing {1} fate on {0}", new object[] { properties.Target, properties.Amount });
        }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IPlaceFateRingProperties;
            if (properties.Origin != null)
            {
                return ("move {1} fate from {2} to {0}", new object[] { properties.Target, properties.Amount, properties.Origin });
            }
            return ("place {1} fate on {0}", new object[] { properties.Target, properties.Amount });
        }

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IPlaceFateRingProperties;
            
            if (properties.Origin != null)
            {
                bool canSpendFate = false;
                int originFate = 0;
                
                if (properties.Origin is DrawCard card)
                {
                    canSpendFate = card.CheckRestrictions("spendFate", context);
                    originFate = card.Fate;
                }
                else if (properties.Origin is Player player)
                {
                    canSpendFate = player.CheckRestrictions("spendFate", context);
                    originFate = player.Fate;
                }
                else if (properties.Origin is Ring originRing)
                {
                    canSpendFate = originRing.CheckRestrictions("spendFate", context);
                    originFate = originRing.Fate;
                }
                
                if (!canSpendFate || originFate == 0)
                {
                    return false;
                }
            }
            
            return properties.Amount > 0 && base.CanAffect(ring, context);
        }

        protected override void AddPropertiesToEvent(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as IPlaceFateRingProperties;
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Fate = properties.Amount;
                gameEvent.Origin = properties.Origin;
                gameEvent.Context = context;
                gameEvent.Recipient = ring;
            }
        }

        protected override bool CheckEventCondition(object eventObj)
        {
            return MoveFateEventCondition(eventObj);
        }

        protected override bool IsEventFullyResolved(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as IPlaceFateRingProperties;
            
            if (eventObj is GameEvent gameEvent)
            {
                return !gameEvent.Cancelled && 
                       gameEvent.Name == this.EventName && 
                       gameEvent.Fate == properties.Amount && 
                       gameEvent.Origin == properties.Origin && 
                       gameEvent.Recipient == ring;
            }
            
            return false;
        }

        protected override void EventHandler(object eventObj)
        {
            MoveFateEventHandler(eventObj);
        }
    }
}
