using System;
using UnityEngine;

namespace L5RGame
{
    public interface ISetDialProperties : IPlayerActionProperties
    {
        int Value { get; set; }
    }

    public class SetDialProperties : PlayerActionProperties, ISetDialProperties
    {
        public int Value { get; set; }
    }

    public class SetDialAction : PlayerAction
    {
        public override string Name => "setDial";
        public override string EventName => EventNames.OnSetHonorDial;

        protected override ISetDialProperties DefaultProperties => new SetDialProperties
        {
            Value = 0
        };

        public SetDialAction(object propertyFactory) : base(propertyFactory) { }

        public SetDialAction(Func<AbilityContext, object> propertyFactory) : base(propertyFactory) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ISetDialProperties;
            return ("set {0}'s dial to {1}", new object[] { properties.Target, properties.Value });
        }

        public override bool CanAffect(Player player, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ISetDialProperties;
            return properties.Value > 0 && properties.Value < 6 && base.CanAffect(player, context);
        }

        protected override void AddPropertiesToEvent(object eventObj, Player player, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ISetDialProperties;
            base.AddPropertiesToEvent(eventObj, player, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Value = properties.Value;
            }
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Player != null)
            {
                gameEvent.Player.SetShowBid(gameEvent.Value);
            }
        }
    }
}
