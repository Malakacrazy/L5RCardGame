using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITransferFateProperties : IPlayerActionProperties
    {
        int Amount { get; set; }
    }

    public class TransferFateProperties : PlayerActionProperties, ITransferFateProperties
    {
        public int Amount { get; set; }
    }

    public class TransferFateAction : PlayerAction
    {
        public override string Name => "takeFate";
        public override string EventName => EventNames.OnMoveFate;

        protected override ITransferFateProperties DefaultProperties => new TransferFateProperties
        {
            Amount = 1
        };

        public TransferFateAction(object propertyFactory) : base(propertyFactory) { }

        public TransferFateAction(Func<AbilityContext, object> propertyFactory) : base(propertyFactory) { }

        public override (string, object[]) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferFateProperties;
            return ("giving {1} fate to {2}", new object[] { properties.Amount, context.Player.Opponent });
        }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferFateProperties;
            return ("take {1} fate from {0}", new object[] { properties.Target, properties.Amount });
        }

        public override bool CanAffect(Player player, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferFateProperties;
            return player.Opponent != null && properties.Amount > 0 && 
                   player.Fate >= properties.Amount && base.CanAffect(player, context);
        }

        protected override void AddPropertiesToEvent(object eventObj, Player player, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferFateProperties;
            base.AddPropertiesToEvent(eventObj, player, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Fate = properties.Amount;
                gameEvent.Origin = player;
                gameEvent.Recipient = player.Opponent;
            }
        }

        protected override bool CheckEventCondition(object eventObj)
        {
            return MoveFateEventCondition(eventObj);
        }

        protected override void EventHandler(object eventObj)
        {
            MoveFateEventHandler(eventObj);
        }
    }
}
