using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for creating a CardEffect
    /// </summary>
    [System.Serializable]
    public class CardEffectProperties : EffectProperties
    {
        public Func<BaseCard, AbilityContext, bool> match;
        public string targetController = Players.Self;
        public string targetLocation = Locations.PlayArea;
    }

    /// <summary>
    /// Represents an effect that specifically targets cards based on location, controller, and matching conditions.
    /// Extends the base Effect class with card-specific targeting logic.
    /// </summary>
    public class CardEffect : Effect
    {
        [Header("Card Effect Properties")]
        public string targetController = Players.Self;
        public string targetLocation = Locations.PlayArea;
        public Func<BaseCard, AbilityContext, bool> match;

        /// <summary>
        /// Create a new CardEffect
        /// </summary>
        public CardEffect(Game game, EffectSource source, CardEffectProperties properties, object effect) 
            : base(game, source, properties, effect)
        {
            InitializeCardEffect(properties);
        }

        /// <summary>
        /// Initialize card-specific effect properties
        /// </summary>
        private void InitializeCardEffect(CardEffectProperties properties)
        {
            targetController = properties.targetController ?? Players.Self;
            targetLocation = properties.targetLocation ?? Locations.PlayArea;
            match = properties.match;

            // Set default match function if none provided
            if (match == null)
            {
                SetDefaultMatch(properties);
            }
        }

        /// <summary>
        /// Set default match function based on properties
        /// </summary>
        private void SetDefaultMatch(CardEffectProperties properties)
        {
            // Default: match the source card
            match = (card, context) => card == context.source;

            // Determine target location based on source type if not specified
            if (string.IsNullOrEmpty(properties.targetLocation))
            {
                if (properties.location == Locations.Any)
                {
                    targetLocation = Locations.Any;
                }
                else if (source is BaseCard sourceCard && IsProvinceTypeCard(sourceCard))
                {
                    targetLocation = Locations.Provinces;
                }
                else
                {
                    targetLocation = Locations.PlayArea;
                }
            }
        }

        /// <summary>
        /// Check if a card is a province-type card (Province, Stronghold, or Holding)
        /// </summary>
        private bool IsProvinceTypeCard(BaseCard card)
        {
            var provinceTypes = new string[] 
            { 
                CardTypes.Province, 
                CardTypes.Stronghold, 
                CardTypes.Holding 
            };
            return provinceTypes.Contains(card.GetCardType());
        }

        /// <summary>
        /// Check if a target is valid for this effect
        /// </summary>
        public override bool IsValidTarget(object target)
        {
            if (target == match)
            {
                // This is a hack to check whether this is a lasting effect
                return true;
            }

            if (!(target is BaseCard card))
            {
                return false;
            }

            // Check if card allows this effect to be applied
            if (!card.AllowGameAction("applyEffect", context))
            {
                return false;
            }

            // Check controller restrictions
            if (!IsValidController(card))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if the card's controller is valid for this effect
        /// </summary>
        private bool IsValidController(BaseCard card)
        {
            switch (targetController)
            {
                case Players.Self:
                    return card.controller == source.controller;
                    
                case Players.Opponent:
                    return card.controller != source.controller;
                    
                case Players.Any:
                    return true;
                    
                default:
                    // Custom controller check (if needed)
                    return true;
            }
        }

        /// <summary>
        /// Get all valid targets for this effect
        /// </summary>
        public override List<object> GetTargets()
        {
            List<BaseCard> candidateCards = GetCandidateCards();
            
            return candidateCards
                .Where(card => match(card, context))
                .Cast<object>()
                .ToList();
        }

        /// <summary>
        /// Get candidate cards based on target location
        /// </summary>
        private List<BaseCard> GetCandidateCards()
        {
            switch (targetLocation)
            {
                case Locations.Any:
                    return GetAllCards();
                    
                case Locations.Provinces:
                    return GetProvinceCards();
                    
                case Locations.PlayArea:
                    return GetPlayAreaCards();
                    
                case Locations.Hand:
                    return GetHandCards();
                    
                case Locations.ConflictDeck:
                    return GetConflictDeckCards();
                    
                case Locations.DynastyDeck:
                    return GetDynastyDeckCards();
                    
                case Locations.ConflictDiscardPile:
                    return GetConflictDiscardCards();
                    
                case Locations.DynastyDiscardPile:
                    return GetDynastyDiscardCards();
                    
                case Locations.RemovedFromGame:
                    return GetRemovedCards();
                    
                default:
                    return GetCardsInSpecificLocation(targetLocation);
            }
        }

        /// <summary>
        /// Get all cards in the game
        /// </summary>
        private List<BaseCard> GetAllCards()
        {
            return game.GetAllCards();
        }

        /// <summary>
        /// Get all cards in provinces
        /// </summary>
        private List<BaseCard> GetProvinceCards()
        {
            return game.GetAllCards().Where(card => card.IsInProvince()).ToList();
        }

        /// <summary>
        /// Get all cards in play area
        /// </summary>
        private List<BaseCard> GetPlayAreaCards()
        {
            return game.FindAnyCardsInPlay(card => true);
        }

        /// <summary>
        /// Get all cards in players' hands
        /// </summary>
        private List<BaseCard> GetHandCards()
        {
            return game.GetPlayers()
                .SelectMany(player => player.hand)
                .ToList();
        }

        /// <summary>
        /// Get all cards in conflict decks
        /// </summary>
        private List<BaseCard> GetConflictDeckCards()
        {
            return game.GetPlayers()
                .SelectMany(player => player.conflictDeck)
                .ToList();
        }

        /// <summary>
        /// Get all cards in dynasty decks
        /// </summary>
        private List<BaseCard> GetDynastyDeckCards()
        {
            return game.GetPlayers()
                .SelectMany(player => player.dynastyDeck)
                .ToList();
        }

        /// <summary>
        /// Get all cards in conflict discard piles
        /// </summary>
        private List<BaseCard> GetConflictDiscardCards()
        {
            return game.GetPlayers()
                .SelectMany(player => player.conflictDiscardPile)
                .ToList();
        }

        /// <summary>
        /// Get all cards in dynasty discard piles
        /// </summary>
        private List<BaseCard> GetDynastyDiscardCards()
        {
            return game.GetPlayers()
                .SelectMany(player => player.dynastyDiscardPile)
                .ToList();
        }

        /// <summary>
        /// Get all removed cards
        /// </summary>
        private List<BaseCard> GetRemovedCards()
        {
            return game.GetPlayers()
                .SelectMany(player => player.removedFromGame)
                .ToList();
        }

        /// <summary>
        /// Get cards in a specific location
        /// </summary>
        private List<BaseCard> GetCardsInSpecificLocation(string location)
        {
            return game.GetAllCards()
                .Where(card => card.location == location)
                .ToList();
        }

        /// <summary>
        /// Apply the effect to a specific target
        /// </summary>
        public override void Apply(object target)
        {
            if (target is BaseCard card && IsValidTarget(card))
            {
                base.Apply(target);
                
                if (debugMode)
                {
                    Debug.Log($"🎯 Applied CardEffect to {card.name} in {card.location}");
                }
            }
        }

        /// <summary>
        /// Remove the effect from a specific target
        /// </summary>
        public override void Unapply(object target)
        {
            if (target is BaseCard card)
            {
                base.Unapply(target);
                
                if (debugMode)
                {
                    Debug.Log($"🚫 Removed CardEffect from {card.name}");
                }
            }
        }

        /// <summary>
        /// Get a summary of this effect for debugging
        /// </summary>
        public override string GetEffectSummary()
        {
            var summary = base.GetEffectSummary();
            summary += $"\nTarget Controller: {targetController}";
            summary += $"\nTarget Location: {targetLocation}";
            summary += $"\nCurrent Targets: {GetTargets().Count}";
            return summary;
        }

        /// <summary>
        /// Create a simple card effect that affects the source card
        /// </summary>
        public static CardEffect CreateSelfEffect(Game game, EffectSource source, object effect, 
            string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = (card, context) => card == context.source,
                targetController = Players.Self,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create a card effect that affects all cards in play
        /// </summary>
        public static CardEffect CreateGlobalEffect(Game game, EffectSource source, object effect,
            Func<BaseCard, AbilityContext, bool> condition = null, string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = condition ?? ((card, context) => true),
                targetController = Players.Any,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create a card effect that affects opponent's cards
        /// </summary>
        public static CardEffect CreateOpponentEffect(Game game, EffectSource source, object effect,
            Func<BaseCard, AbilityContext, bool> condition = null, string targetLocation = Locations.PlayArea,
            string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = condition ?? ((card, context) => true),
                targetController = Players.Opponent,
                targetLocation = targetLocation
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create a card effect that affects cards by type
        /// </summary>
        public static CardEffect CreateTypeEffect(Game game, EffectSource source, object effect,
            string cardType, string targetController = Players.Any, string targetLocation = Locations.PlayArea,
            string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = (card, context) => card.GetCardType() == cardType,
                targetController = targetController,
                targetLocation = targetLocation
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create a card effect that affects cards by trait
        /// </summary>
        public static CardEffect CreateTraitEffect(Game game, EffectSource source, object effect,
            string trait, string targetController = Players.Any, string targetLocation = Locations.PlayArea,
            string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = (card, context) => card.HasTrait(trait),
                targetController = targetController,
                targetLocation = targetLocation
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create a card effect that affects cards by faction
        /// </summary>
        public static CardEffect CreateFactionEffect(Game game, EffectSource source, object effect,
            string faction, string targetController = Players.Any, string targetLocation = Locations.PlayArea,
            string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = (card, context) => card.IsFaction(faction),
                targetController = targetController,
                targetLocation = targetLocation
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create a province-specific effect
        /// </summary>
        public static CardEffect CreateProvinceEffect(Game game, EffectSource source, object effect,
            Func<BaseCard, AbilityContext, bool> condition = null, string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = condition ?? ((card, context) => card.IsInProvince()),
                targetController = Players.Any,
                targetLocation = Locations.Provinces
            };

            return new CardEffect(game, source, properties, effect);
        }
    }

    /// <summary>
    /// Extension methods for easy CardEffect creation
    /// </summary>
    public static class CardEffectExtensions
    {
        /// <summary>
        /// Create a self-targeting effect on a card
        /// </summary>
        public static CardEffect CreateSelfEffect(this BaseCard card, object effect, string duration = Durations.Persistent)
        {
            return CardEffect.CreateSelfEffect(card.game, card, effect, duration);
        }

        /// <summary>
        /// Create an effect that affects all characters
        /// </summary>
        public static CardEffect CreateCharacterEffect(this EffectSource source, object effect, 
            Func<BaseCard, AbilityContext, bool> condition = null, string targetController = Players.Any)
        {
            return CardEffect.CreateTypeEffect(source.game, source, effect, CardTypes.Character, 
                targetController, Locations.PlayArea);
        }

        /// <summary>
        /// Create an effect that affects all holdings
        /// </summary>
        public static CardEffect CreateHoldingEffect(this EffectSource source, object effect,
            Func<BaseCard, AbilityContext, bool> condition = null, string targetController = Players.Any)
        {
            return CardEffect.CreateTypeEffect(source.game, source, effect, CardTypes.Holding,
                targetController, Locations.Provinces);
        }

        /// <summary>
        /// Create an effect with custom targeting
        /// </summary>
        public static CardEffect CreateCustomEffect(this EffectSource source, object effect,
            Func<BaseCard, AbilityContext, bool> match, string targetController = Players.Any,
            string targetLocation = Locations.PlayArea, string duration = Durations.Persistent)
        {
            var properties = new CardEffectProperties
            {
                duration = duration,
                match = match,
                targetController = targetController,
                targetLocation = targetLocation
            };

            return new CardEffect(source.game, source, properties, effect);
        }
    }

    /// <summary>
    /// Common card effect patterns for L5R
    /// </summary>
    public static class L5RCardEffects
    {
        /// <summary>
        /// Create an effect that affects participating characters
        /// </summary>
        public static CardEffect ParticipatingCharacters(Game game, EffectSource source, object effect,
            string targetController = Players.Any, string duration = Durations.UntilEndOfConflict)
        {
            return CardEffect.CreateTypeEffect(game, source, effect, CardTypes.Character, 
                targetController, Locations.PlayArea, duration);
        }

        /// <summary>
        /// Create an effect that affects characters with specific traits
        /// </summary>
        public static CardEffect TraitBasedEffect(Game game, EffectSource source, object effect,
            List<string> traits, string targetController = Players.Any)
        {
            var properties = new CardEffectProperties
            {
                duration = Durations.Persistent,
                match = (card, context) => traits.Any(trait => card.HasTrait(trait)),
                targetController = targetController,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create an effect that affects unique characters
        /// </summary>
        public static CardEffect UniqueCharacters(Game game, EffectSource source, object effect,
            string targetController = Players.Any)
        {
            var properties = new CardEffectProperties
            {
                duration = Durations.Persistent,
                match = (card, context) => card.GetCardType() == CardTypes.Character && card.IsUnique(),
                targetController = targetController,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create an effect that affects cards with specific cost
        /// </summary>
        public static CardEffect CostBasedEffect(Game game, EffectSource source, object effect,
            int minCost, int maxCost = int.MaxValue, string targetController = Players.Any)
        {
            var properties = new CardEffectProperties
            {
                duration = Durations.Persistent,
                match = (card, context) => 
                {
                    int cost = card.GetFateCost();
                    return cost >= minCost && cost <= maxCost;
                },
                targetController = targetController,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create an effect that affects dishonored characters
        /// </summary>
        public static CardEffect DishonoredCharacters(Game game, EffectSource source, object effect,
            string targetController = Players.Any)
        {
            var properties = new CardEffectProperties
            {
                duration = Durations.Persistent,
                match = (card, context) => card.GetCardType() == CardTypes.Character && card.isDishonored,
                targetController = targetController,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }

        /// <summary>
        /// Create an effect that affects honored characters
        /// </summary>
        public static CardEffect HonoredCharacters(Game game, EffectSource source, object effect,
            string targetController = Players.Any)
        {
            var properties = new CardEffectProperties
            {
                duration = Durations.Persistent,
                match = (card, context) => card.GetCardType() == CardTypes.Character && card.isHonored,
                targetController = targetController,
                targetLocation = Locations.PlayArea
            };

            return new CardEffect(game, source, properties, effect);
        }
    }
}