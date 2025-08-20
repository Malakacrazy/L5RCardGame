using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface IMenuPromptProperties : IGameActionProperties
    {
        string ActivePromptTitle { get; set; }
        Players? Player { get; set; }
        GameAction GameAction { get; set; }
        object Choices { get; set; } // Can be string[] or Func<object, string[]>
        Func<string, bool, IMenuPromptProperties, object> ChoiceHandler { get; set; }
    }

    public class MenuPromptProperties : GameActionProperties, IMenuPromptProperties
    {
        public string ActivePromptTitle { get; set; }
        public Players? Player { get; set; }
        public GameAction GameAction { get; set; }
        public object Choices { get; set; }
        public Func<string, bool, IMenuPromptProperties, object> ChoiceHandler { get; set; }
    }

    public class MenuPromptAction : GameAction
    {
        public MenuPromptAction(object properties) : base(properties) { }

        public MenuPromptAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("make a choice for {0}", new object[] { properties.Target });
        }

        protected override IMenuPromptProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as IMenuPromptProperties;
            
            if (properties.Choices is Func<object, string[]> choicesFunc)
            {
                properties.Choices = choicesFunc(properties);
            }
            
            return properties;
        }

        public override bool CanAffect(object target, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var choices = properties.Choices as string[];
            
            if (choices == null) return false;
            
            return choices.Any(choice =>
            {
                var childProperties = properties.ChoiceHandler(choice, false, properties);
                return properties.GameAction.CanAffect(target, context, childProperties);
            });
        }

        public override bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var choices = properties.Choices as string[];
            
            if (choices == null) return false;
            
            return choices.Any(choice =>
            {
                var childProperties = properties.ChoiceHandler(choice, false, properties);
                return properties.GameAction.HasLegalTarget(context, childProperties);
            });
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var choices = properties.Choices as string[];
            
            if (choices == null || choices.Length == 0 || 
                (properties.Player == Players.Opponent && context.Player.Opponent == null))
            {
                return;
            }
            
            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;
            
            Action<string> choiceHandler = choice =>
            {
                var childProperties = properties.ChoiceHandler(choice, true, properties);
                properties.GameAction.AddEventsToArray(events, context, childProperties);
            };
            
            if (choices.Length == 1)
            {
                choiceHandler(choices[0]);
                return;
            }
            
            var promptProperties = new
            {
                activePromptTitle = properties.ActivePromptTitle,
                context = context,
                choiceHandler = choiceHandler,
                choices = choices,
                gameAction = properties.GameAction
            };
            
            context.Game.PromptWithHandlerMenu(player, promptProperties);
        }

        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.GameAction.HasTargetsChosenByInitiatingPlayer(context);
        }
    }
}
