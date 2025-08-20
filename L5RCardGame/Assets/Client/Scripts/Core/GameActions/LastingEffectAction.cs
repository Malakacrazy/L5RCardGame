using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ILastingEffectGeneralProperties : IGameActionProperties
    {
        Durations Duration { get; set; }
        Func<AbilityContext, bool> Condition { get; set; }
        WhenType Until { get; set; }
        object Effect { get; set; }
    }

    public interface ILastingEffectProperties : ILastingEffectGeneralProperties
    {
        Players? TargetController { get; set; }
    }

    public class LastingEffectProperties : GameActionProperties, ILastingEffectProperties
    {
        public Durations Duration { get; set; }
        public Func<AbilityContext, bool> Condition { get; set; }
        public WhenType Until { get; set; }
        public object Effect { get; set; }
        public Players? TargetController { get; set; }
    }

    public class LastingEffectAction : GameAction
    {
        public override string Name => "applyLastingEffect";
        public override string EventName => EventNames.OnEffectApplied;
        public override string Effect => "apply a lasting effect";

        protected override ILastingEffectProperties DefaultProperties => new LastingEffectProperties
        {
            Duration = Durations.UntilEndOfConflict,
            Effect = new List<object>()
        };

        public LastingEffectAction(object properties) : base(properties) { }

        protected override ILastingEffectProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ILastingEffectProperties;
            
            if (properties.Effect != null && !(properties.Effect is IList<object>))
            {
                properties.Effect = new List<object> { properties.Effect };
            }
            
            return properties;
        }

        public override bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var effectList = properties.Effect as IList<object>;
            return effectList != null && effectList.Count > 0;
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            if (HasLegalTarget(context, additionalProperties))
            {
                events.Add(GetEvent(null, context, additionalProperties));
            }
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties);
                
                // This would need to be implemented based on your duration system
                // For example: gameEvent.Context.Source.ApplyDuration(properties.Duration, () => properties);
                switch (properties.Duration)
                {
                    case Durations.UntilEndOfConflict:
                        gameEvent.Context.Source.UntilEndOfConflict(() => properties);
                        break;
                    case Durations.UntilEndOfPhase:
                        gameEvent.Context.Source.UntilEndOfPhase(() => properties);
                        break;
                    case Durations.UntilEndOfRound:
                        gameEvent.Context.Source.UntilEndOfRound(() => properties);
                        break;
                    // Add other duration cases as needed
                }
            }
        }
    }
}
