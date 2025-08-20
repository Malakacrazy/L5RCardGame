using UnityEngine;

namespace L5RGame
{
    public interface ISwitchConflictElementProperties : IRingActionProperties
    {
    }

    public class SwitchConflictElementProperties : RingActionProperties, ISwitchConflictElementProperties
    {
    }

    public class SwitchConflictElementAction : RingAction
    {
        public override string Name => "switchConflictElement";
        public override string Cost => "switching the contested ring to {0}";
        public override string Effect => "switch the contested ring to {0}";
        public override string EventName => EventNames.OnSwitchConflictElement;

        public SwitchConflictElementAction(object properties) : base(properties) { }

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            return ring.IsUnclaimed() && context.Game.IsDuringConflict() && 
                   base.CanAffect(ring, context, additionalProperties);
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Ring != null)
            {
                gameEvent.Context.Game.CurrentConflict.SwitchElement(gameEvent.Ring.Element);
            }
        }
    }
}
