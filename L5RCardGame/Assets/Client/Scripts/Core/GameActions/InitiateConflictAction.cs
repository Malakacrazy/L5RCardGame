using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IInitiateConflictProperties : IPlayerActionProperties
    {
        bool CanPass { get; set; }
        ConflictTypes? ForcedDeclaredType { get; set; }
    }

    public class InitiateConflictProperties : PlayerActionProperties, IInitiateConflictProperties
    {
        public bool CanPass { get; set; }
        public ConflictTypes? ForcedDeclaredType { get; set; }
    }

    public class InitiateConflictAction : PlayerAction
    {
        public override string Name => "initiateConflict";
        public override string EventName => EventNames.OnConflictInitiated;
        public override string Effect => "declare a new conflict";

        protected override IInitiateConflictProperties DefaultProperties => new InitiateConflictProperties
        {
            CanPass = true
        };

        public InitiateConflictAction(object properties) : base(properties) { }

        public InitiateConflictAction(System.Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override bool CanAffect(Player player, AbilityContext context)
        {
            var properties = GetProperties(context) as IInitiateConflictProperties;
            return base.CanAffect(player, context) && 
                   player.HasLegalConflictDeclaration(new { forcedDeclaredType = properties.ForcedDeclaredType });
        }

        public override List<Player> DefaultTargets(AbilityContext context)
        {
            return new List<Player> { context.Player };
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties) as IInitiateConflictProperties;
                gameEvent.Context.Game.InitiateConflict(gameEvent.Player, properties.CanPass, properties.ForcedDeclaredType);
            }
        }
    }
}
