using System;
using UnityEngine;

namespace L5RGame
{
    public interface ISwitchConflictTypeProperties : IRingActionProperties
    {
        ConflictTypes? TargetConflictType { get; set; }
    }

    public class SwitchConflictTypeProperties : RingActionProperties, ISwitchConflictTypeProperties
    {
        public ConflictTypes? TargetConflictType { get; set; }
    }

    public class SwitchConflictTypeAction : RingAction
    {
        public override string Name => "switchConflictType";
        public override string EventName => EventNames.OnSwitchConflictType;

        public SwitchConflictTypeAction(object properties) : base(properties) { }

        public override (string, object[]) GetCostMessage(AbilityContext context)
        {
            var currentConflictType = context.Game.CurrentConflict?.ConflictType;
            var newConflictType = currentConflictType == ConflictTypes.Military ? ConflictTypes.Political : ConflictTypes.Military;
            return ("switching the conflict type from {0} to {1}", new object[] { currentConflictType, newConflictType });
        }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var currentConflictType = context.Game.CurrentConflict?.ConflictType;
            var newConflictType = currentConflictType == ConflictTypes.Military ? ConflictTypes.Political : ConflictTypes.Military;
            return ("switch the conflict type from {0} to {1}", new object[] { currentConflictType, newConflictType });
        }

        protected override ISwitchConflictTypeProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            return base.GetProperties(context, additionalProperties) as ISwitchConflictTypeProperties;
        }

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            if (context.Game.CurrentConflict == null)
            {
                return false;
            }
            
            var properties = GetProperties(context);
            return ring.ConflictType != properties.TargetConflictType;
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Context.Game.CurrentConflict != null)
            {
                gameEvent.Context.Game.CurrentConflict.SwitchType();
            }
        }
    }
}
