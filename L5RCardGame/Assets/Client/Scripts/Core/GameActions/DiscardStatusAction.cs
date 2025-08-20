using UnityEngine;

namespace L5RGame
{
    public interface IDiscardStatusProperties : ITokenActionProperties
    {
    }

    public class DiscardStatusProperties : TokenActionProperties, IDiscardStatusProperties
    {
    }

    public class DiscardStatusAction : TokenAction
    {
        public override string Name => "discardStatus";
        public override string EventName => EventNames.OnStatusTokenDiscarded;
        public override string Effect => "discard {0}'s status token";
        public override string Cost => "discarding {0}'s status token";

        public DiscardStatusAction(object properties) : base(properties) { }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Token != null)
            {
                if (gameEvent.Token.Card.PersonalHonor == gameEvent.Token)
                {
                    gameEvent.Token.Card.MakeOrdinary();
                }
            }
        }
    }
}
