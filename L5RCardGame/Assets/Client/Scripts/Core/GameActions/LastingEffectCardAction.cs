using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ILastingEffectCardProperties : ILastingEffectGeneralProperties
    {
        object TargetLocation { get; set; } // Can be Locations or Locations[]
    }

    public class LastingEffectCardProperties : LastingEffectGeneralProperties, ILastingEffectCardProperties
    {
        public object TargetLocation { get; set; } // Can be Locations or Locations[]
    }

    public class LastingEffectCardAction : CardGameAction
    {
        public override string Name => "applyLastingEffect";
        public override string EventName => EventNames.OnEffectApplied;
        public override string Effect => "apply a lasting effect to {0}";

        protected override ILastingEffectCardProperties DefaultProperties => new LastingEffectCardProperties
        {
            Duration = Durations.UntilEndOfConflict,
            Effect = new List<object>()
        };

        public LastingEffectCardAction(object properties) : base(properties) { }

        public LastingEffectCardAction(Func<AbilityContext, object> propertiesFactory) : base(propertiesFactory) { }

        protected override ILastingEffectCardProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ILastingEffectCardProperties;
            
            if (properties.Effect != null && !(properties.Effect is IList<object>))
            {
                properties.Effect = new List<object> { properties.Effect };
            }
            
            return properties;
        }

        public override bool CanAffect(BaseCard card, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var effectList = properties.Effect as IList<object>;
            
            if (effectList != null)
            {
                // Convert effect factories to actual effects
                var effects = effectList.Select(factory => 
                {
                    if (factory is Func<Game, EffectSource, object, object> effectFactory)
                    {
                        return effectFactory(context.Game, context.Source, properties);
                    }
                    return factory;
                }).ToList();

                properties.Effect = effects;
            }

            var lastingEffectRestrictions = card.GetEffects(EffectNames.CannotApplyLastingEffects);
            
            return base.CanAffect(card, context) && 
                   (effectList?.Any(props => 
                   {
                       // Assuming props has an Effect property that can be checked
                       var effect = GetEffectFromProps(props);
                       return effect?.CanBeApplied(card) == true && 
                              !lastingEffectRestrictions.Any(condition => 
                              {
                                  if (condition is Func<object, bool> conditionFunc)
                                  {
                                      return conditionFunc(effect);
                                  }
                                  return false;
                              });
                   }) ?? false);
        }

        protected override void EventHandler(object eventObj, object additionalProperties = null)
        {
            if (eventObj is GameEvent gameEvent && gameEvent.Card != null)
            {
                var properties = GetProperties(gameEvent.Context, additionalProperties);
                var lastingEffectRestrictions = gameEvent.Card.GetEffects(EffectNames.CannotApplyLastingEffects);
                
                var effectProperties = new
                {
                    match = gameEvent.Card,
                    location = Locations.Any,
                    duration = properties.Duration,
                    condition = properties.Condition,
                    until = properties.Until,
                    targetLocation = properties.TargetLocation
                };

                var effectList = properties.Effect as IList<object>;
                if (effectList != null)
                {
                    var effects = effectList.Select(factory =>
                    {
                        if (factory is Func<Game, EffectSource, object, object> effectFactory)
                        {
                            return effectFactory(gameEvent.Context.Game, gameEvent.Context.Source, effectProperties);
                        }
                        return factory;
                    }).ToList();

                    var filteredEffects = effects.Where(props =>
                    {
                        var effect = GetEffectFromProps(props);
                        return effect?.CanBeApplied(gameEvent.Card) == true &&
                               !lastingEffectRestrictions.Any(condition =>
                               {
                                   if (condition is Func<object, bool> conditionFunc)
                                   {
                                       return conditionFunc(effect);
                                   }
                                   return false;
                               });
                    }).ToList();

                    foreach (var effect in filteredEffects)
                    {
                        gameEvent.Context.Game.EffectEngine.Add(effect);
                    }
                }
            }
        }

        private object GetEffectFromProps(object props)
        {
            // This method should extract the effect from the properties object
            // Implementation depends on your property structure
            if (props != null)
            {
                var type = props.GetType();
                var effectProperty = type.GetProperty("Effect") ?? type.GetProperty("effect");
                return effectProperty?.GetValue(props);
            }
            return null;
        }
    }
}
