using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface IResolveConflictRingProperties : IRingActionProperties
    {
        bool ResolveAsAttacker { get; set; }
    }

    public class ResolveConflictRingProperties : RingActionProperties, IResolveConflictRingProperties
    {
        public bool ResolveAsAttacker { get; set; }
    }

    public class ResolveConflictRingAction : RingAction
    {
        public override string Name => "resolveRing";
        public override string EventName => EventNames.OnResolveConflictRing;

        protected override IResolveConflictRingProperties DefaultProperties => new ResolveConflictRingProperties
        {
            ResolveAsAttacker = true
        };

        public ResolveConflictRingAction(object properties) : base(properties) { }

        public ResolveConflictRingAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IResolveConflictRingProperties;
            var suffix = properties.ResolveAsAttacker ? "" : " for the attacking player";
            return ($"resolve {{0}}{suffix}", new object[] { properties.Target });
        }

        protected override void AddPropertiesToEvent(object eventObj, Ring ring, AbilityContext context, object additionalProperties)
        {
            base.AddPropertiesToEvent(eventObj, ring, context, additionalProperties);
            var properties = GetProperties(context, additionalProperties) as IResolveConflictRingProperties;
            var conflict = context.Game.CurrentConflict;

            if (eventObj is GameEvent gameEvent)
            {
                if (conflict == null && !properties.ResolveAsAttacker)
                {
                    gameEvent.Name = EventNames.Unnamed;
                    return;
                }

                gameEvent.Conflict = conflict;
                gameEvent.Player = properties.ResolveAsAttacker ? context.Player : conflict?.AttackingPlayer;
            }
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Name == this.EventName)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties) as IResolveConflictRingProperties;
                var elements = gameEvent.Ring.GetElements();
                var player = gameEvent.Player;

                if (elements.Count == 1 || (!properties.ResolveAsAttacker && gameEvent.Conflict.ElementsToResolve >= elements.Count))
                {
                    ResolveRingEffects(player, elements, properties.ResolveAsAttacker);
                }
                else
                {
                    ChooseElementsToResolve(player, elements, properties.ResolveAsAttacker, gameEvent.Conflict.ElementsToResolve);
                }
            }
        }

        public void ChooseElementsToResolve(Player player, List<string> elements, bool optional, int elementsToResolve, List<string> chosenElements = null)
        {
            if (chosenElements == null) chosenElements = new List<string>();

            if (elements.Count == 0 || elementsToResolve == 0)
            {
                ResolveRingEffects(player, chosenElements, optional);
                return;
            }

            var activePromptTitle = "Choose a ring effect to resolve (click the ring you want to resolve)";
            if (chosenElements.Count > 0)
            {
                activePromptTitle = chosenElements.Aggregate(activePromptTitle + "\nChosen elements:", (str, element) => str + " " + element);
            }

            var buttons = new List<object>();
            if (optional)
            {
                if (chosenElements.Count > 0)
                {
                    buttons.Add(new { text = "Done", arg = "done" });
                }
                if (elementsToResolve >= elements.Count)
                {
                    buttons.Add(new { text = "Resolve All Elements", arg = "all" });
                }
                buttons.Add(new { text = "Don't Resolve the Conflict Ring", arg = "cancel" });
            }

            var promptProperties = new
            {
                activePromptTitle = activePromptTitle,
                buttons = buttons,
                source = "Resolve Ring Effect",
                ringCondition = new Func<Ring, bool>(ring => elements.Contains(ring.Element)),
                onSelect = new Func<Player, Ring, bool>((p, ring) =>
                {
                    elementsToResolve--;
                    chosenElements.Add(ring.Element);
                    ChooseElementsToResolve(p, elements.Where(e => e != ring.Element).ToList(), optional, elementsToResolve, chosenElements);
                    return true;
                }),
                onCancel = new Action<Player>(p => p.Game.AddMessage("{0} chooses not to resolve the conflict ring", p)),
                onMenuCommand = new Func<Player, string, bool>((p, arg) =>
                {
                    if (arg == "all")
                    {
                        ResolveRingEffects(p, elements.Concat(chosenElements).ToList());
                    }
                    else if (arg == "done")
                    {
                        ResolveRingEffects(p, chosenElements, optional);
                    }
                    return true;
                })
            };

            player.Game.PromptForRingSelect(player, promptProperties);
        }

        public void ResolveRingEffects(Player player, List<string> elements, bool optional = true)
        {
            if (elements == null) elements = new List<string>();

            var rings = elements.Select(element => player.Game.Rings[element]).ToList();
            var action = new ResolveElementAction(new
            {
                target = rings,
                optional = optional,
                physicalRing = player.Game.CurrentConflict?.Ring
            });

            var events = new List<object>();
            action.AddEventsToArray(events, player.Game.GetFrameworkContext(player));
            player.Game.OpenThenEventWindow(events);
        }

        public void ResolveRingEffects(Player player, string element, bool optional = true)
        {
            ResolveRingEffects(player, new List<string> { element }, optional);
        }
    }
}
