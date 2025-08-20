using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ISelectRingProperties : IRingActionProperties
    {
        string ActivePromptTitle { get; set; }
        Players? Player { get; set; }
        bool Targets { get; set; }
        Func<Ring, AbilityContext, bool> RingCondition { get; set; }
        Action CancelHandler { get; set; }
        Func<Ring, object> SubActionProperties { get; set; }
        string Message { get; set; }
        Func<Ring, Player, object[]> MessageArgs { get; set; }
        GameAction GameAction { get; set; }
    }

    public class SelectRingProperties : RingActionProperties, ISelectRingProperties
    {
        public string ActivePromptTitle { get; set; }
        public Players? Player { get; set; }
        public bool Targets { get; set; }
        public Func<Ring, AbilityContext, bool> RingCondition { get; set; }
        public Action CancelHandler { get; set; }
        public Func<Ring, object> SubActionProperties { get; set; }
        public string Message { get; set; }
        public Func<Ring, Player, object[]> MessageArgs { get; set; }
        public GameAction GameAction { get; set; }
    }

    public class SelectRingAction : RingAction
    {
        protected override ISelectRingProperties DefaultProperties => new SelectRingProperties
        {
            RingCondition = (ring, context) => true,
            SubActionProperties = ring => new { target = ring },
            GameAction = null
        };

        public SelectRingAction(object properties) : base(properties) { }

        public SelectRingAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("choose a ring for {0}", new object[] { properties.Target });
        }

        public override bool CanAffect(Ring ring, AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectRingProperties;
            if (properties.Player == Players.Opponent && context.Player.Opponent == null)
            {
                return false;
            }
            return base.CanAffect(ring, context) && properties.RingCondition(ring, context);
        }

        public override bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            return context.Game.Rings.Values.Any(ring => CanAffect(ring, context, additionalProperties));
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectRingProperties;
            
            if (properties.Player == Players.Opponent && context.Player.Opponent == null)
            {
                return;
            }
            else if (!context.Game.Rings.Values.Any(ring => properties.RingCondition(ring, context)))
            {
                return;
            }
            
            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;
            if (properties.Targets && context.ChoosingPlayerOverride != null)
            {
                player = context.ChoosingPlayerOverride;
            }
            
            var buttons = new List<object>();
            if (properties.CancelHandler != null)
            {
                buttons.Add(new { text = "Cancel", arg = "cancel" });
            }
            
            Action<Player, Ring> onSelect = (p, ring) =>
            {
                if (!string.IsNullOrEmpty(properties.Message))
                {
                    context.Game.AddMessage(properties.Message, properties.MessageArgs(ring, p));
                }
                properties.GameAction.AddEventsToArray(events, context, MergeProperties(additionalProperties, properties.SubActionProperties(ring)));
            };
            
            var promptProperties = new
            {
                context = context,
                buttons = buttons,
                onCancel = properties.CancelHandler,
                onSelect = onSelect,
                activePromptTitle = properties.ActivePromptTitle,
                ringCondition = properties.RingCondition,
                targets = properties.Targets
            };
            
            context.Game.PromptForRingSelect(player, promptProperties);
        }

        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectRingProperties;
            return properties.Targets && properties.Player != Players.Opponent;
        }

        private object MergeProperties(object additionalProperties, object subActionProperties)
        {
            if (additionalProperties == null) return subActionProperties;
            if (subActionProperties == null) return additionalProperties;
            
            return new { additionalProperties, subActionProperties };
        }
    }
}
