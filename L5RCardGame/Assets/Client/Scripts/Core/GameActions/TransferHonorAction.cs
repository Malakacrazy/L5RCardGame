using System;
using UnityEngine;

namespace L5RGame
{
    public interface ITransferHonorProperties : IPlayerActionProperties
    {
        int Amount { get; set; }
        bool AfterBid { get; set; }
    }

    public class TransferHonorProperties : PlayerActionProperties, ITransferHonorProperties
    {
        public int Amount { get; set; }
        public bool AfterBid { get; set; }
    }

    public class TransferHonorAction : PlayerAction
    {
        public override string Name => "takeHonor";
        public override string EventName => EventNames.OnTransferHonor;

        protected override ITransferHonorProperties DefaultProperties => new TransferHonorProperties
        {
            Amount = 1,
            AfterBid = false
        };

        public TransferHonorAction(object propertyFactory) : base(propertyFactory) { }

        public TransferHonorAction(Func<AbilityContext, object> propertyFactory) : base(propertyFactory) { }

        public override (string, object[]) GetCostMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferHonorProperties;
            return ("giving {1} honor to {2}", new object[] { properties.Amount, context.Player.Opponent });
        }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as ITransferHonorProperties;
            return ("take {1} honor from {0}", new object[] { properties.Target, properties.Amount });
        }

        public override bool CanAffect(Player player, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferHonorProperties;
            return player.Opponent != null && properties.Amount > 0 && base.CanAffect(player, context);
        }

        protected override void AddPropertiesToEvent(object eventObj, Player player, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as ITransferHonorProperties;
            base.AddPropertiesToEvent(eventObj, player, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Amount = properties.Amount;
                gameEvent.AfterBid = properties.AfterBid;
            }
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Player != null && gameEvent.Player.Opponent != null)
            {
                gameEvent.Player.ModifyHonor(-gameEvent.Amount);
                gameEvent.Player.Opponent.ModifyHonor(gameEvent.Amount);
            }
        }
    }
}
