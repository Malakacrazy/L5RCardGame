using UnityEngine;

namespace L5RGame
{
    public interface IMoveTokenProperties : ITokenActionProperties
    {
        DrawCard Recipient { get; set; }
    }

    public class MoveTokenProperties : TokenActionProperties, IMoveTokenProperties
    {
        public DrawCard Recipient { get; set; }
    }

    public class MoveTokenAction : TokenAction
    {
        public override string Name => "moveStatusToken";
        public override string EventName => EventNames.OnStatusTokenMoved;

        public MoveTokenAction(object properties) : base(properties) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IMoveTokenProperties;
            var target = properties.Target as StatusToken;
            return ("move {0}'s status token to {1}", new object[] { target?.Card, properties.Recipient });
        }

        public override bool CanAffect(StatusToken token, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context) as IMoveTokenProperties;
            if (properties.Recipient == null || properties.Recipient.Location != Locations.PlayArea)
            {
                return false;
            }
            else if (token.Honored && (properties.Recipient.IsHonored || !properties.Recipient.CheckRestrictions("receiveHonorToken", context)))
            {
                return false;
            }
            else if (token.Dishonored && (properties.Recipient.IsDishonored || !properties.Recipient.CheckRestrictions("receiveDishonorToken", context)))
            {
                return false;
            }
            return base.CanAffect(token, context, additionalProperties);
        }

        protected override void AddPropertiesToEvent(object eventObj, StatusToken token, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context) as IMoveTokenProperties;
            base.AddPropertiesToEvent(eventObj, token, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Recipient = properties.Recipient;
            }
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Token != null && gameEvent.Recipient != null)
            {
                if (gameEvent.Token.Card.PersonalHonor == gameEvent.Token)
                {
                    gameEvent.Token.Card.MakeOrdinary();
                    
                    if ((gameEvent.Recipient.IsHonored && gameEvent.Token.Dishonored) || 
                        (gameEvent.Recipient.IsDishonored && gameEvent.Token.Honored))
                    {
                        gameEvent.Recipient.MakeOrdinary();
                    }
                    else if (gameEvent.Recipient.PersonalHonor == null)
                    {
                        gameEvent.Recipient.SetPersonalHonor(gameEvent.Token);
                    }
                }
            }
        }
    }
}
