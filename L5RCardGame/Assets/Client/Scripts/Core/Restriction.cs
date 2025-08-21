using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame.Client.Scripts.Core
{
    public static class CheckRestrictions
    {
        private static readonly Dictionary<string, Func<AbilityContext, bool>> SimpleChecks = 
            new Dictionary<string, Func<AbilityContext, bool>>
            {
                { "characters", context => context.Source.Type == CardTypes.Character },
                { "events", context => context.Source.Type == CardTypes.Event },
                { "nonSpellEvents", context => context.Source.Type == CardTypes.Event && !context.Source.HasTrait("spell") }
            };

        private static readonly Dictionary<string, Func<AbilityContext, Player, BaseCard, object, bool>> ComplexChecks = 
            new Dictionary<string, Func<AbilityContext, Player, BaseCard, object, bool>>
            {
                { "copiesOfDiscardEvents", (context, player, source, param) => 
                    context.Source.Type == CardTypes.Event && 
                    context.Player.ConflictDiscardPile.Any(card => card.Name == context.Source.Name) },
                
                { "copiesOfX", (context, player, source, param) => 
                    context.Source.Name == param?.ToString() },
                
                { "opponentsCardEffects", (context, player, source, param) =>
                    context.Player == player.Opponent && 
                    (context.Ability.IsCardAbility() || !context.Ability.IsCardPlayed()) &&
                    new[] { CardTypes.Event, CardTypes.Character, CardTypes.Holding, 
                           CardTypes.Attachment, CardTypes.Stronghold, CardTypes.Province, CardTypes.Role }
                    .Contains(context.Source.Type) },
                
                { "opponentsEvents", (context, player, source, param) =>
                    context.Player != null && context.Player == player.Opponent && 
                    context.Source.Type == CardTypes.Event },
                
                { "opponentsRingEffects", (context, player, source, param) =>
                    context.Player != null && context.Player == player.Opponent && 
                    context.Source.Type.ToString() == "ring" },
                
                { "opponentsTriggeredAbilities", (context, player, source, param) =>
                    context.Player == player.Opponent && context.Ability.IsTriggeredAbility() },
                
                { "source", (context, player, source, param) => context.Source == source }
            };

        public static bool Check(string restriction, AbilityContext context, Player player = null, BaseCard source = null, object param = null)
        {
            if (SimpleChecks.ContainsKey(restriction))
            {
                return SimpleChecks[restriction](context);
            }

            if (ComplexChecks.ContainsKey(restriction))
            {
                return ComplexChecks[restriction](context, player, source, param);
            }

            // Default to trait check
            return context.Source.HasTrait(restriction);
        }
    }

    public class Restriction : EffectValue
    {
        public string Type { get; private set; }
        public string RestrictionName { get; private set; }
        public object Params { get; private set; }

        public Restriction(object properties) : base()
        {
            if (properties is string stringType)
            {
                Type = stringType;
            }
            else if (properties is RestrictionProperties props)
            {
                Type = props.Type;
                RestrictionName = props.Restricts;
                Params = props.Params;
            }
        }

        public override object GetValue()
        {
            return this;
        }

        public bool IsMatch(string type, AbilityContext abilityContext)
        {
            return (string.IsNullOrEmpty(Type) || Type == type) && CheckCondition(abilityContext);
        }

        public bool CheckCondition(AbilityContext context)
        {
            if (string.IsNullOrEmpty(RestrictionName))
            {
                return true;
            }
            
            if (context == null)
            {
                throw new ArgumentException("checkCondition called without a context");
            }

            var player = Context.Player ?? (Context.Source?.Controller);
            return CheckRestrictions.Check(RestrictionName, context, player, Context.Source, Params);
        }
    }

    public class RestrictionProperties
    {
        public string Type { get; set; }
        public string Restricts { get; set; }
        public object Params { get; set; }
    }
}
