using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class PlayCardResolver : AbilityResolver
    {
        public PlayCardAction PlayGameAction { get; set; }
        public AbilityContext GameActionContext { get; set; }
        public object GameActionProperties { get; set; }
        public bool CancelPressed { get; set; }

        public PlayCardResolver(Game game, AbilityContext context, PlayCardAction playGameAction, 
                               AbilityContext gameActionContext, object gameActionProperties) 
            : base(game, context)
        {
            PlayGameAction = playGameAction;
            GameActionContext = gameActionContext;
            GameActionProperties = gameActionProperties;
            CancelPressed = false;
        }

        public override void CheckForCancel()
        {
            base.CheckForCancel();
            var properties = GameActionProperties as IPlayCardProperties;
            if (Cancelled && properties?.ResetOnCancel == true)
            {
                PlayGameAction.CancelAction(GameActionContext);
                CancelPressed = true;
            }
        }

        public override void PayCosts()
        {
            base.PayCosts();
            var properties = GameActionProperties as IPlayCardProperties;
            if (Cancelled && properties?.ResetOnCancel == true)
            {
                PlayGameAction.CancelAction(GameActionContext);
                CancelPressed = true;
            }
        }

        public override void ExecuteHandler()
        {
            base.ExecuteHandler();
            if (!CancelPressed)
            {
                var properties = GameActionProperties as IPlayCardProperties;
                Game.QueueSimpleStep(() => properties?.PostHandler?.Invoke(Context.Source as DrawCard));
            }
        }
    }

    public interface IPlayCardProperties : ICardActionProperties
    {
        bool ResetOnCancel { get; set; }
        Action<DrawCard> PostHandler { get; set; }
        Locations Location { get; set; }
    }

    public class PlayCardProperties : CardActionProperties, IPlayCardProperties
    {
        public bool ResetOnCancel { get; set; }
        public Action<DrawCard> PostHandler { get; set; }
        public Locations Location { get; set; }
    }

    public class PlayCardAction : CardGameAction
    {
        public override string Name => "playCard";
        public override string Effect => "play {0} as if it were in their hand";

        protected override IPlayCardProperties DefaultProperties => new PlayCardProperties
        {
            ResetOnCancel = false,
            PostHandler = (card) => { },
            Location = Locations.Hand
        };

        public PlayCardAction(object properties) : base(properties) { }

        public PlayCardAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        protected override IPlayCardProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            return base.GetProperties(context, additionalProperties) as IPlayCardProperties;
        }

        public override bool CanAffect(DrawCard card, AbilityContext context, object additionalProperties = null)
        {
            if (!base.CanAffect(card, context))
            {
                return false;
            }
            
            var actions = card.GetPlayActions();
            return GetLegalActions(actions, context).Count > 0;
        }

        public List<CardAbility> GetLegalActions(List<CardAbility> actions, AbilityContext context)
        {
            // Filter actions to exclude actions which involve this game action, or which are not legal
            return actions.Where(action =>
            {
                var newContext = action.CreateContext(context.Player);
                var newChain = new List<GameAction>(context.GameActionsResolutionChain) { this };
                newContext.GameActionsResolutionChain = newChain;
                return !action.MeetsRequirements(newContext, new string[] { "location", "player" });
            }).ToList();
        }

        public void CancelAction(AbilityContext context)
        {
            context.Ability.ExecuteHandler(context);
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var targets = properties.Target as IList<DrawCard>;
            
            if (targets == null || targets.Count == 0)
            {
                return;
            }
            
            var card = targets[0];
            var actions = GetLegalActions(card.GetPlayActions(), context);
            
            if (actions.Count == 1)
            {
                events.Add(GetPlayCardEvent(card, context, actions[0].CreateContext(context.Player), additionalProperties));
                return;
            }
            
            var choices = actions.Select(action => action.Title).ToList();
            if (properties.ResetOnCancel)
            {
                choices.Add("Cancel");
            }
            
            var handlers = actions.Select<CardAbility, Action>(action => 
                () => events.Add(GetPlayCardEvent(card, context, action.CreateContext(context.Player), additionalProperties))
            ).ToList();
            
            if (properties.ResetOnCancel)
            {
                handlers.Add(() => CancelAction(context));
            }
            
            var promptProperties = new
            {
                source = card,
                choices = choices,
                handlers = handlers
            };
            
            context.Game.PromptWithHandlerMenu(context.Player, promptProperties);
        }

        public GameEvent GetPlayCardEvent(DrawCard card, AbilityContext context, AbilityContext actionContext, object additionalProperties)
        {
            var properties = GetProperties(context, additionalProperties);
            var gameEvent = CreateEvent(card, context, additionalProperties);
            UpdateEvent(gameEvent, card, context, additionalProperties);
            
            gameEvent.ReplaceHandler(() => 
                context.Game.QueueStep(new PlayCardResolver(context.Game, actionContext, this, context, properties))
            );
            
            return gameEvent;
        }

        protected override bool CheckEventCondition(object eventObj)
        {
            return true;
        }
    }
}
