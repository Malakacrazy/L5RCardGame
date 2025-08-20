using UnityEngine;

namespace L5RGame
{
    public interface IMoveToConflictProperties : ICardActionProperties
    {
    }

    public class MoveToConflictProperties : CardActionProperties, IMoveToConflictProperties
    {
    }

    public class MoveToConflictAction : CardGameAction
    {
        public override string Name => "moveToConflict";
        public override string EventName => EventNames.OnMoveToConflict;
        public override string Effect => "move {0} into the conflict";
        public override CardTypes[] TargetType => new CardTypes[] { CardTypes.Character };

        public MoveToConflictAction(object properties) : base(properties) { }

        public override bool CanAffect(BaseCard card, AbilityContext context)
        {
            if (!base.CanAffect(card, context))
            {
                return false;
            }
            
            if (context.Game.CurrentConflict == null || card.IsParticipating())
            {
                return false;
            }
            
            if (card.Controller.IsAttackingPlayer())
            {
                if (!card.CanParticipateAsAttacker())
                {
                    return false;
                }
            }
            else if (!card.CanParticipateAsDefender())
            {
                return false;
            }
            
            return card.Location == Locations.PlayArea;
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Card != null)
            {
                if (gameEvent.Card.Controller.IsAttackingPlayer())
                {
                    gameEvent.Context.Game.CurrentConflict.AddAttacker(gameEvent.Card);
                }
                else
                {
                    gameEvent.Context.Game.CurrentConflict.AddDefender(gameEvent.Card);
                }
            }
        }
    }
}
