using UnityEngine;

namespace L5RGame
{
    public interface ITurnCardFacedownProperties : ICardActionProperties
    {
    }

    public class TurnCardFacedownProperties : CardActionProperties, ITurnCardFacedownProperties
    {
    }

    public class TurnCardFacedownAction : CardGameAction
    {
        public override string Name => "turnFacedown";
        public override string EventName => EventNames.OnCardTurnedFacedown;
        public override string Cost => "turning {0} facedown";
        public override string Effect => "turn {0} facedown";
        public override CardTypes[] TargetType => new CardTypes[] { CardTypes.Character, CardTypes.Holding, CardTypes.Province };

        public TurnCardFacedownAction(object properties) : base(properties) { }

        public override bool CanAffect(BaseCard card, AbilityContext context)
        {
            return !card.Facedown && base.CanAffect(card, context) && card.IsInProvince();
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Card != null)
            {
                gameEvent.Card.LeavesPlay();
                
                if (gameEvent.Card.IsConflictProvince())
                {
                    gameEvent.Context.Game.AddMessage("{0} is immediately revealed again!", gameEvent.Card);
                    gameEvent.Card.InConflict = true;
                    
                    var revealEvent = gameEvent.Context.Game.Actions.Reveal()
                        .GetEvent(gameEvent.Card, gameEvent.Context.Game.GetFrameworkContext());
                    gameEvent.Context.Game.OpenThenEventWindow(revealEvent);
                }
                else
                {
                    gameEvent.Card.Facedown = true;
                }
            }
        }
    }
}
