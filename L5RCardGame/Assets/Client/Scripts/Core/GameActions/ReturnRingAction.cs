using UnityEngine;

namespace L5RGame
{
    public interface IReturnRingProperties : IRingActionProperties
    {
    }

    public class ReturnRingProperties : RingActionProperties, IReturnRingProperties
    {
    }

    public class ReturnRingAction : RingAction
    {
        public override string Name => "returnRing";
        public override string EventName => EventNames.OnReturnRing;
        public override string Effect => "return {0} to the unclaimed pool";

        public ReturnRingAction(object properties) : base(properties) { }

        public override bool CanAffect(Ring ring, AbilityContext context)
        {
            return !ring.IsUnclaimed() && base.CanAffect(ring, context);
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Ring != null)
            {
                gameEvent.Ring.ResetRing();
            }
        }
    }
}
