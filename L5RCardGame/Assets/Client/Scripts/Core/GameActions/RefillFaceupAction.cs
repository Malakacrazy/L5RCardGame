using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IRefillFaceupProperties : IPlayerActionProperties
    {
        Locations Location { get; set; }
    }

    public class RefillFaceupProperties : PlayerActionProperties, IRefillFaceupProperties
    {
        public Locations Location { get; set; }
    }

    public class RefillFaceupAction : PlayerAction
    {
        public override string Name => "refill";
        public override string Effect => "refill its province faceup";

        public RefillFaceupAction(object propertyFactory) : base(propertyFactory) { }

        public RefillFaceupAction(Func<AbilityContext, object> propertyFactory) : base(propertyFactory) { }

        public override List<Player> DefaultTargets(AbilityContext context)
        {
            return new List<Player> { context.Player };
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Player != null)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties) as IRefillFaceupProperties;
                
                if (gameEvent.Player.ReplaceDynastyCard(properties.Location))
                {
                    gameEvent.Context.Game.QueueSimpleStep(() =>
                    {
                        var card = gameEvent.Player.GetDynastyCardInProvince(properties.Location);
                        if (card != null)
                        {
                            card.Facedown = false;
                        }
                    });
                }
            }
        }
    }
}
