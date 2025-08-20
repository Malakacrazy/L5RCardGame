using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class NoCostsAbilityResolver : AbilityResolver
    {
        public override void Initialise()
        {
            Pipeline.Initialise(new List<BaseStep>
            {
                new SimpleStep(Game, () => CreateSnapshot()),
                new SimpleStep(Game, () => OpenInitiateAbilityEventWindow()),
                new SimpleStep(Game, () => RefillProvinces())
            });
        }

        public void OpenInitiateAbilityEventWindow()
        {
            var events = new List<GameEvent>
            {
                Game.GetEvent(EventNames.OnCardAbilityInitiated, 
                    new { card = Context.Source, ability = Context.Ability, context = Context }, 
                    () =>
                    {
                        Game.QueueSimpleStep(() => ResolveTargets());
                        Game.QueueSimpleStep(() => InitiateAbilityEffects());
                        Game.QueueSimpleStep(() => ExecuteHandler());
                    })
            };

            if (Context.Ability.IsTriggeredAbility() && !Context.SubResolution)
            {
                events.Add(Game.GetEvent(EventNames.OnCardAbilityTriggered, new
                {
                    player = Context.Player,
                    card = Context.Source,
                    context = Context
                }));
            }

            Game.OpenEventWindow(events);
        }

        public void InitiateAbilityEffects()
        {
            if (Cancelled)
            {
                foreach (var eventObj in Events)
                {
                    if (eventObj is GameEvent gameEvent)
                    {
                        gameEvent.Cancel();
                    }
                }
                return;
            }
            else if (Context.Ability.Max != null && !Context.SubResolution)
            {
                Context.Player.IncrementAbilityMax(Context.Ability.MaxIdentifier);
            }

            Context.Ability.DisplayMessage(Context, "resolves");
            Game.OpenEventWindow(new InitiateCardAbilityEvent(
                new { card = Context.Source, context = Context },
                () => InitiateAbility = true));
        }
    }

    public interface IResolveAbilityProperties : ICardActionProperties
    {
        CardAbility Ability { get; set; }
        bool SubResolution { get; set; }
        Player Player { get; set; }
        GameEvent Event { get; set; }
    }

    public class ResolveAbilityProperties : CardActionProperties, IResolveAbilityProperties
    {
        public CardAbility Ability { get; set; }
        public bool SubResolution { get; set; }
        public Player Player { get; set; }
        public GameEvent Event { get; set; }
    }

    public class ResolveAbilityAction : CardGameAction
    {
        public override string Name => "resolveAbility";

        protected override IResolveAbilityProperties DefaultProperties => new ResolveAbilityProperties
        {
            Ability = null,
            SubResolution = false
        };

        public ResolveAbilityAction(object properties) : base(properties) { }

        public ResolveAbilityAction(Func<TriggeredAbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IResolveAbilityProperties;
            return ("resolve {0}'s {1} ability", new object[] { properties.Target, properties.Ability?.Title });
        }

        public override bool CanAffect(DrawCard card, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveAbilityProperties;
            var ability = properties.Ability as TriggeredAbility;
            var player = properties.Player ?? context.Player;
            var newContextEvent = properties.Event;

            if (!base.CanAffect(card, context) || ability == null || 
                (!properties.SubResolution && player.IsAbilityAtMax(ability.MaxIdentifier)))
            {
                return false;
            }

            var newContext = ability.CreateContext(player, newContextEvent);
            if (ability.Targets.Count == 0)
            {
                return ability.GameAction.Count == 0 || ability.GameAction.Any(action => action.HasLegalTarget(newContext));
            }

            return ability.CanResolveTargets(newContext);
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties) as IResolveAbilityProperties;
                var player = properties.Player ?? gameEvent.Context.Player;
                var newContextEvent = properties.Event;
                var newContext = (properties.Ability as TriggeredAbility).CreateContext(player, newContextEvent);
                newContext.SubResolution = properties.SubResolution;
                gameEvent.Context.Game.QueueStep(new NoCostsAbilityResolver(gameEvent.Context.Game, newContext));
            }
        }

        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveAbilityProperties;
            return properties.Ability.HasTargetsChosenByInitiatingPlayer(context);
        }
    }
}
