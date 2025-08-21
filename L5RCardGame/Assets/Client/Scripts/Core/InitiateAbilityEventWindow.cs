using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class InitiateAbilityInterruptWindow : TriggeredAbilityWindow
    {
        private GameEvent playEvent;

        public InitiateAbilityInterruptWindow(Game game, AbilityTypes abilityType, EventWindow eventWindow) 
            : base(game, abilityType, eventWindow)
        {
            playEvent = eventWindow.Events.FirstOrDefault(eventObj => eventObj.Name == EventNames.OnCardPlayed);
        }

        protected override object GetPromptForSelectProperties()
        {
            var buttons = new List<object>();
            
            if (playEvent != null && CurrentPlayer == playEvent.Player && playEvent.Resolver.CanCancel)
            {
                buttons.Add(new { text = "Cancel", arg = "cancel" });
            }
            
            if (GetMinCostReduction() == 0)
            {
                buttons.Add(new { text = "Pass", arg = "pass" });
            }

            var baseProperties = base.GetPromptForSelectProperties();
            
            // Merge base properties with new buttons and onCancel handler
            return new
            {
                // Copy all properties from base
                buttons = buttons,
                onCancel = new Action(() =>
                {
                    if (playEvent?.Resolver != null)
                    {
                        playEvent.Resolver.Cancelled = true;
                        Complete = true;
                    }
                })
            };
        }

        private int GetMinCostReduction()
        {
            if (playEvent != null)
            {
                var context = playEvent.Context;
                var alternatePools = context.Player.GetAlternateFatePools(playEvent.PlayType, context.Source, context);
                var alternatePoolTotal = alternatePools.Sum(pool => pool.Fate);
                var maxPlayerFate = context.Player.CheckRestrictions("spendFate", context) ? context.Player.Fate : 0;
                return Math.Max(context.Ability.GetReducedCost(context) - maxPlayerFate - alternatePoolTotal, 0);
            }
            return 0;
        }

        public override void ResolveAbility(AbilityContext context)
        {
            if (playEvent?.Resolver != null)
            {
                playEvent.Resolver.CanCancel = false;
            }
            base.ResolveAbility(context);
        }
    }

    public class InitiateAbilityEventWindow : EventWindow
    {
        public InitiateAbilityEventWindow(Game game, List<GameEvent> events, Action handler = null) 
            : base(game, events, handler)
        {
        }

        public override void OpenWindow(AbilityTypes abilityType)
        {
            if (Events.Count > 0 && abilityType == AbilityTypes.Interrupt)
            {
                QueueStep(new InitiateAbilityInterruptWindow(Game, abilityType, this));
            }
            else
            {
                base.OpenWindow(abilityType);
            }
        }

        public override void ExecuteHandler()
        {
            // Sort events by order
            Events = Events.OrderBy(eventObj => eventObj.Order).ToList();
            
            foreach (var gameEvent in Events)
            {
                gameEvent.CheckCondition();
                if (!gameEvent.Cancelled)
                {
                    gameEvent.ExecuteHandler();
                }
            }
            
            // We need to separate executing the handler and emitting events as in this window, the handler just
            // queues ability resolution steps, and we don't want the events to be emitted until step 8
            Game.QueueSimpleStep(() => EmitEvents());
        }

        private void EmitEvents()
        {
            foreach (var gameEvent in Events)
            {
                Game.Emit(gameEvent.Name, gameEvent);
            }
        }
    }
}
