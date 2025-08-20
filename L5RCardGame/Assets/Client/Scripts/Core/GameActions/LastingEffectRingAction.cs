using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface ILastingEffectRingProperties : ILastingEffectGeneralProperties
    {
    }

    public class LastingEffectRingProperties : LastingEffectGeneralProperties, ILastingEffectRingProperties
    {
    }

    public class LastingEffectRingAction : RingAction
    {
        public override string Name => "applyLastingEffect";
        public override string EventName => EventNames.OnEffectApplied;
        public override string Effect => "apply a lasting effect";

        protected override ILastingEffectRingProperties DefaultProperties => new LastingEffectRingProperties
        {
            Duration = Durations.UntilEndOfConflict,
            Effect = new List<object>()
        };

        public LastingEffectRingAction(object properties) : base(properties) { }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Ring != null)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties) as ILastingEffectRingProperties;
                
                var effectProperties = new
                {
                    match = gameEvent.Ring,
                    duration = properties.Duration,
                    condition = properties.Condition,
                    until = properties.Until,
                    effect = properties.Effect
                };

                // Apply the lasting effect based on duration
                switch (properties.Duration)
                {
                    case Durations.UntilEndOfConflict:
                        gameEvent.Context.Source.UntilEndOfConflict(() => effectProperties);
                        break;
                    case Durations.UntilEndOfPhase:
                        gameEvent.Context.Source.UntilEndOfPhase(() => effectProperties);
                        break;
                    case Durations.UntilEndOfRound:
                        gameEvent.Context.Source.UntilEndOfRound(() => effectProperties);
                        break;
                    // Add other duration cases as needed
                }
            }
        }
    }
}
