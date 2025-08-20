using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface ITokenActionProperties : IGameActionProperties
    {
    }

    public class TokenActionProperties : GameActionProperties, ITokenActionProperties
    {
    }

    public class TokenAction : GameAction
    {
        public override string[] TargetType => new string[] { "token" };

        public TokenAction(object properties) : base(properties) { }

        public virtual List<StatusToken> DefaultTargets(AbilityContext context)
        {
            return context.Source.PersonalHonor != null 
                ? new List<StatusToken> { context.Source.PersonalHonor } 
                : new List<StatusToken>();
        }

        public virtual bool CanAffect(StatusToken target, AbilityContext context, object additionalProperties = null)
        {
            return target.Type == "token";
        }

        protected override bool CheckEventCondition(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Token != null)
            {
                return CanAffect(gameEvent.Token, gameEvent.Context, additionalProperties);
            }
            return false;
        }

        protected virtual void AddPropertiesToEvent(object eventObj, StatusToken token, AbilityContext context, object additionalProperties)
        {
            base.AddPropertiesToEvent(eventObj, token, context, additionalProperties);
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Token = token;
            }
        }
    }
}
