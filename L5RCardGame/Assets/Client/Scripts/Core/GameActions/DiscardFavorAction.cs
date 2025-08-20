using UnityEngine;

namespace L5RGame
{
    public interface IDiscardFavorProperties : IPlayerActionProperties
    {
    }

    public class DiscardFavorProperties : PlayerActionProperties, IDiscardFavorProperties
    {
    }

    public class DiscardFavorAction : PlayerAction
    {
        public override string Name => "discardFavor";
        public override string EventName => EventNames.OnDiscardFavor;
        public override string Cost => "discarding the Imperial Favor";
        public override string Effect => "make {0} lose the Imperial Favor";

        public DiscardFavorAction(object properties) : base(properties) { }

        public override bool CanAffect(Player player, AbilityContext context)
        {
            return player.ImperialFavor && base.CanAffect(player, context);
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Player != null)
            {
                gameEvent.Player.LoseImperialFavor();
            }
        }
    }
}
