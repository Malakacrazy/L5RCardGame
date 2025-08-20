using System;
using System.Collections.Generic;
using System.Linq;

namespace L5RGame
{
    public interface ICardMenuProperties : ICardActionProperties
    {
        string ActivePromptTitle { get; set; }
        Players? Player { get; set; }
        List<BaseCard> Cards { get; set; }
        Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
        List<string> Choices { get; set; }
        List<Action> Handlers { get; set; }
        bool Targets { get; set; }
        string Message { get; set; }
        Func<BaseCard, Player, List<BaseCard>, object[]> MessageArgs { get; set; }
        Func<BaseCard, object> SubActionProperties { get; set; }
        GameAction GameAction { get; set; }
        Func<AbilityContext, bool> GameActionHasLegalTarget { get; set; }
    }

    public class CardMenuProperties : CardActionProperties, ICardMenuProperties
    {
        public string ActivePromptTitle { get; set; }
        public Players? Player { get; set; }
        public List<BaseCard> Cards { get; set; }
        public Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
        public List<string> Choices { get; set; }
        public List<Action> Handlers { get; set; }
        public bool Targets { get; set; }
        public string Message { get; set; }
        public Func<BaseCard, Player, List<BaseCard>, object[]> MessageArgs { get; set; }
        public Func<BaseCard, object> SubActionProperties { get; set; }
        public GameAction GameAction { get; set; }
        public Func<AbilityContext, bool> GameActionHasLegalTarget { get; set; }
    }

    public class CardMenuAction : CardGameAction
    {
        public override string Effect => "choose a target for {0}";

        protected override ICardMenuProperties DefaultProperties => new CardMenuProperties
        {
            ActivePromptTitle = "Select a card:",
            SubActionProperties = card => new { target = card },
            Targets = false,
            Cards = new List<BaseCard>(),
            CardCondition = (card, context) => true,
            GameAction = null
        };

        public CardMenuAction(object properties) : base(properties) { }

        public CardMenuAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        protected override ICardMenuProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ICardMenuProperties;
            properties.GameAction?.SetDefaultTarget(() => properties.Target);
            return properties;
        }

        public override bool CanAffect(BaseCard card, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Cards.Any(c =>
                properties.GameAction.CanAffect(card, context, MergeProperties(additionalProperties, properties.SubActionProperties(c)))
            );
        }

        public override bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.Handlers != null && properties.Handlers.Count > 0)
            {
                return true;
            }
            
            if (properties.GameActionHasLegalTarget != null)
            {
                return properties.GameActionHasLegalTarget(context);
            }
            
            return properties.Cards.Any(card =>
                properties.GameAction.HasLegalTarget(context, MergeProperties(additionalProperties, properties.SubActionProperties(card)))
            );
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            Func<BaseCard, AbilityContext, bool> cardCondition = (card, ctx) =>
                properties.GameAction.HasLegalTarget(ctx, MergeProperties(additionalProperties, properties.SubActionProperties(card))) &&
                properties.CardCondition(card, ctx);

            if ((properties.Cards.Count == 0 && properties.Choices.Count == 0) || 
                (properties.Player == Players.Opponent && context.Player.Opponent == null))
            {
                return;
            }

            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;
            if (properties.Targets && context.ChoosingPlayerOverride != null)
            {
                player = context.ChoosingPlayerOverride;
            }

            Action<BaseCard> cardHandler = (card) =>
            {
                properties.GameAction.AddEventsToArray(events, context, MergeProperties(additionalProperties, properties.SubActionProperties(card)));
                if (!string.IsNullOrEmpty(properties.Message))
                {
                    var cards = properties.Cards.Where(c => cardCondition(c, context)).ToList();
                    context.Game.AddMessage(properties.Message, properties.MessageArgs(card, player, cards));
                }
            };

            var promptProperties = new
            {
                context = context,
                cardHandler = cardHandler,
                activePromptTitle = properties.ActivePromptTitle,
                cards = properties.Cards,
                cardCondition = cardCondition,
                choices = properties.Choices,
                handlers = properties.Handlers,
                targets = properties.Targets
            };

            context.Game.PromptWithHandlerMenu(player, promptProperties);
        }

        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Targets || properties.GameAction.HasTargetsChosenByInitiatingPlayer(context, additionalProperties);
        }

        private object MergeProperties(object additionalProperties, object subActionProperties)
        {
            // Implementation to merge properties - you may need to adjust based on your property merging logic
            if (additionalProperties == null) return subActionProperties;
            if (subActionProperties == null) return additionalProperties;
            
            // This is a simplified merge - you may need a more sophisticated approach
            return new { additionalProperties, subActionProperties };
        }
    }
}
