using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public enum Direction
    {
        Decrease,
        Increase,
        Prompt
    }

    public interface IModifyBidProperties : IPlayerActionProperties
    {
        int Amount { get; set; }
        Direction Direction { get; set; }
    }

    public class ModifyBidProperties : PlayerActionProperties, IModifyBidProperties
    {
        public int Amount { get; set; }
        public Direction Direction { get; set; }
    }

    public class ModifyBidAction : PlayerAction
    {
        public override string Name => "modifyBid";
        public override string EventName => EventNames.OnModifyBid;

        protected override IModifyBidProperties DefaultProperties => new ModifyBidProperties
        {
            Amount = 1,
            Direction = Direction.Increase
        };

        public ModifyBidAction(object propertyFactory) : base(propertyFactory) { }

        public ModifyBidAction(Func<AbilityContext, object> propertyFactory) : base(propertyFactory) { }

        public override List<Player> DefaultTargets(AbilityContext context)
        {
            return new List<Player> { context.Player };
        }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IModifyBidProperties;
            if (properties.Direction == Direction.Prompt)
            {
                return ("modify their honor bid by {0}", new object[] { properties.Amount });
            }
            return ("{0} their bid by {1}", new object[] { properties.Direction.ToString().ToLower(), properties.Amount });
        }

        public override bool CanAffect(Player player, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IModifyBidProperties;
            if (properties.Amount == 0 || (properties.Direction == Direction.Decrease && player.HonorBid == 0))
            {
                return false;
            }
            return base.CanAffect(player, context);
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IModifyBidProperties;
            if (properties.Direction != Direction.Prompt)
            {
                base.AddEventsToArray(events, context);
                return;
            }

            var targets = properties.Target as IEnumerable<Player>;
            if (targets != null)
            {
                foreach (var player in targets)
                {
                    if (player.HonorBid == 0)
                    {
                        var gameEvent = GetEvent(player, context, additionalProperties) as GameEvent;
                        if (gameEvent != null)
                        {
                            gameEvent.Direction = Direction.Increase;
                            context.Game.AddMessage("{0} chooses to increase their honor bid", player);
                            events.Add(gameEvent);
                        }
                    }
                    else
                    {
                        var choices = new List<string> { "Increase honor bid", "Decrease honor bid" };
                        Action<string> choiceHandler = choice =>
                        {
                            var gameEvent = GetEvent(player, context, additionalProperties) as GameEvent;
                            if (gameEvent != null)
                            {
                                if (choice == "Increase honor bid")
                                {
                                    context.Game.AddMessage("{0} chooses to increase their honor bid", player);
                                    gameEvent.Direction = Direction.Increase;
                                }
                                else
                                {
                                    context.Game.AddMessage("{0} chooses to decrease their honor bid", player);
                                    gameEvent.Direction = Direction.Decrease;
                                }
                                events.Add(gameEvent);
                            }
                        };

                        var promptProperties = new
                        {
                            context = context,
                            choices = choices,
                            choiceHandler = choiceHandler
                        };

                        context.Game.PromptWithHandlerMenu(player, promptProperties);
                    }
                }
            }
        }

        protected override void AddPropertiesToEvent(object eventObj, Player player, AbilityContext context, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties) as IModifyBidProperties;
            base.AddPropertiesToEvent(eventObj, player, context, additionalProperties);
            
            if (eventObj is GameEvent gameEvent)
            {
                gameEvent.Amount = properties.Amount;
                gameEvent.Direction = properties.Direction;
            }
        }

        protected override void EventHandler(object eventObj)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Player != null)
            {
                if (gameEvent.Direction == Direction.Increase)
                {
                    gameEvent.Player.HonorBidModifier += gameEvent.Amount;
                }
                else
                {
                    gameEvent.Player.HonorBidModifier -= gameEvent.Amount;
                }
            }
        }
    }
}
