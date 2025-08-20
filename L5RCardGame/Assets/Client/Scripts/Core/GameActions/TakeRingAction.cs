using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITakeRingProperties : IRingActionProperties
    {
        bool TakeFate { get; set; }
    }

    public class TakeRingProperties : RingActionProperties, ITakeRingProperties
    {
        public bool TakeFate { get; set; }
    }

    public class TakeRingAction : RingAction
    {
        public override string Name => "takeFate";
        public override string EventName => EventNames.OnTakeRing;
        public override string Effect => "take {0}";

        protected override ITakeRingProperties DefaultProperties => new TakeRingProperties
        {
            TakeFate = true
        };

        public TakeRingAction(object properties) : base(properties) { }

        public TakeRingAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override bool CanAffect(Ring ring, AbilityContext context)
        {
            return ring.ClaimedBy != context.Player.Name && base.CanAffect(ring, context);
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Ring != null)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties) as ITakeRingProperties;
                var ring = gameEvent.Ring;
                var context = gameEvent.Context;
                
                ring.ClaimRing(context.Player);
                ring.Contested = false;
                
                if (properties.TakeFate && context.Player.CheckRestrictions("takeFateFromRings", context))
                {
                    context.Game.AddMessage("{0} takes {1} fate from {2}", context.Player, ring.Fate, ring);
                    context.Player.ModifyFate(ring.Fate);
                    ring.RemoveFate();
                }
            }
        }
    }
}
