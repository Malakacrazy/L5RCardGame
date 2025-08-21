using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for configuring a SelectCardPrompt.
    /// Provides comprehensive options for card selection UI and validation.
    /// </summary>
    [System.Serializable]
    public class SelectCardPromptProperties
    {
        [Header("Prompt Display")]
        public string activePromptTitle;
        public string waitingPromptTitle;
        public string promptDescription;
        public string source;

        [Header("Selection Behavior")]
        public bool selectCard = true;
        public bool selectRing = false;
        public bool selectPlayer = false;
        public bool selectProvince = false;
        public bool ordered = false;
        public bool optional = false;

        [Header("Card Selection")]
        public System.Func<BaseCard, AbilityContext, bool> cardCondition;
        public string cardType;
        public List<string> cardTypes = new List<string>();
        public string cardTrait;
        public List<string> cardTraits = new List<string>();
        public string cardFaction;
        public List<string> cardFactions = new List<string>();
        public string cardLocation;
        public List<string> cardLocations = new List<string>();

        [Header("Player Targeting")]
        public string controller = Players.Any;
        public string targetController = Players.Any;
        public bool onlyOwned = false;
        public bool onlyControlled = false;

        [Header("Quantity")]
        public int numCards = 1;
        public int minCards = 0;
        public int maxCards = -1; // -1 means unlimited
        public bool exactlyVariable = false;
        public string numCardsVariable;

        [Header("Card States")]
        public bool canSelectFacedown = true;
        public bool canSelectFaceup = true;
        public bool canSelectBowed = true;
        public bool canSelectReady = true;
        public bool canSelectHonored = true;
        public bool canSelectDishonored = true;

        [Header("Context Requirements")]
        public bool requiresTarget = false;
        public bool multipleTargets = false;
        public List<BaseCard> mustSelect = new List<BaseCard>();
        public List<BaseCard> cannotSelect = new List<BaseCard>();

        [Header("Callbacks")]
        public System.Func<Player, object, bool> onSelect;
        public System.Func<Player, string, bool> onMenuCommand;
        public System.Func<Player, bool> onCancel;
        public System.Action<Player, BaseCard> onCardToggle;

        [Header("UI Customization")]
        public List<object> buttons = new List<object>();
        public List<object> controls = new List<object>();
        public bool showCancelButton = true;
        public bool showDoneButton = true;
        public string doneButtonText = "Done";
        public string cancelButtonText = "Cancel";

        [Header("Game Actions")]
        public object gameAction;
        public List<GameAction> gameActions = new List<GameAction>();

        [Header("Advanced Options")]
        public AbilityContext context;
        public BaseCardSelector selector;
        public Dictionary<string, object> customProperties = new Dictionary<string, object>();
        public List<string> tags = new List<string>();

        [Header("Validation")]
        public bool validateOnSelect = true;
        public System.Func<List<BaseCard>, string> customValidator;
        public bool allowEmptySelection = false;

        [Header("Stat-Based Selection")]
        public System.Func<AbilityContext, int> maxStat;
        public System.Func<BaseCard, int> cardStat;
        public bool useStatLimit = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SelectCardPromptProperties()
        {
            cardCondition = (card, context) => true;
            onSelect = (player, cards) => true;
            onMenuCommand = (player, arg) => true;
            onCancel = (player) => true;
            controller = Players.Any;
            targetController = Players.Any;
            buttons = new List<object>();
            controls = new List<object>();
            mustSelect = new List<BaseCard>();
            cannotSelect = new List<BaseCard>();
            cardTypes = new List<string>();
            cardTraits = new List<string>();
            cardFactions = new List<string>();
            cardLocations = new List<string>();
            gameActions = new List<GameAction>();
            customProperties = new Dictionary<string, object>();
            tags = new List<string>();
        }

        /// <summary>
        /// Constructor with basic parameters
        /// </summary>
        /// <param name=\"title\">Prompt title</param>
        /// <param name=\"num\">Number of cards to select</param>
        public SelectCardPromptProperties(string title, int num = 1)
        {
            activePromptTitle = title;
            numCards = num;
            cardCondition = (card, context) => true;
            onSelect = (player, cards) => true;
            onMenuCommand = (player, arg) => true;
            onCancel = (player) => true;
            controller = Players.Any;
            targetController = Players.Any;
            buttons = new List<object>();
            controls = new List<object>();
            mustSelect = new List<BaseCard>();
            cannotSelect = new List<BaseCard>();
            cardTypes = new List<string>();
            cardTraits = new List<string>();
            cardFactions = new List<string>();
            cardLocations = new List<string>();
            gameActions = new List<GameAction>();
            customProperties = new Dictionary<string, object>();
            tags = new List<string>();
        }

        /// <summary>
        /// Get the effective minimum number of cards to select
        /// </summary>
        /// <returns>Minimum cards</returns>
        public int GetMinCards()
        {
            if (optional && minCards == 0 && mustSelect.Count == 0)
            {
                return 0;
            }
            return Math.Max(minCards, mustSelect.Count);
        }

        /// <summary>
        /// Get the effective maximum number of cards to select
        /// </summary>
        /// <returns>Maximum cards (-1 for unlimited)</returns>
        public int GetMaxCards()
        {
            if (maxCards >= 0)
            {
                return maxCards;
            }
            if (numCards > 0)
            {
                return numCards;
            }
            return -1; // Unlimited
        }

        /// <summary>
        /// Check if exactly a specific number of cards must be selected
        /// </summary>
        /// <returns>True if exact number required</returns>
        public bool RequiresExactCount()
        {
            return !optional && numCards > 0 && minCards == 0 && maxCards == -1;
        }

        /// <summary>
        /// Get the target number of cards (for display purposes)
        /// </summary>
        /// <returns>Target number of cards</returns>
        public int GetTargetCount()
        {
            if (exactlyVariable && !string.IsNullOrEmpty(numCardsVariable))
            {
                // This would need to be resolved from the game context
                return numCards; // Fallback
            }
            return numCards;
        }

        /// <summary>
        /// Check if a card matches the selection criteria
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <param name=\"context\">Ability context</param>
        /// <returns>True if card matches criteria</returns>
        public bool MatchesCardCriteria(BaseCard card, AbilityContext context)
        {
            if (card == null) return false;

            // Check custom condition first
            if (cardCondition != null && !cardCondition(card, context))
            {
                return false;
            }

            // Check if card is in cannot select list
            if (cannotSelect.Contains(card))
            {
                return false;
            }

            // Check card type
            if (!string.IsNullOrEmpty(cardType) && card.type != cardType)
            {
                return false;
            }

            if (cardTypes.Count > 0 && !cardTypes.Contains(card.type))
            {
                return false;
            }

            // Check card traits
            if (!string.IsNullOrEmpty(cardTrait) && !card.HasTrait(cardTrait))
            {
                return false;
            }

            if (cardTraits.Count > 0 && !cardTraits.Any(trait => card.HasTrait(trait)))
            {
                return false;
            }

            // Check card faction
            if (!string.IsNullOrEmpty(cardFaction) && !card.IsFaction(cardFaction))
            {
                return false;
            }

            if (cardFactions.Count > 0 && !cardFactions.Any(faction => card.IsFaction(faction)))
            {
                return false;
            }

            // Check card location
            if (!string.IsNullOrEmpty(cardLocation) && card.location != cardLocation)
            {
                return false;
            }

            if (cardLocations.Count > 0 && !cardLocations.Contains(card.location))
            {
                return false;
            }

            // Check card states
            if (!canSelectFacedown && card.facedown)
            {
                return false;
            }

            if (!canSelectFaceup && !card.facedown)
            {
                return false;
            }

            if (!canSelectBowed && card.bowed)
            {
                return false;
            }

            if (!canSelectReady && !card.bowed)
            {
                return false;
            }

            if (!canSelectHonored && card.IsHonored())
            {
                return false;
            }

            if (!canSelectDishonored && card.IsDishonored())
            {
                return false;
            }

            // Check controller requirements
            if (!CheckControllerRequirements(card, context))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if card meets controller requirements
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <param name=\"context\">Ability context</param>
        /// <returns>True if controller requirements are met</returns>
        private bool CheckControllerRequirements(BaseCard card, AbilityContext context)
        {
            var player = context?.player;
            if (player == null) return true;

            // Check controller restriction
            switch (controller)
            {
                case Players.Self:
                    if (card.controller != player) return false;
                    break;
                case Players.Opponent:
                    if (card.controller != player.opponent) return false;
                    break;
                case Players.Any:
                    // No restriction
                    break;
            }

            // Check target controller restriction
            switch (targetController)
            {
                case Players.Self:
                    if (card.controller != player) return false;
                    break;
                case Players.Opponent:
                    if (card.controller != player.opponent) return false;
                    break;
                case Players.Any:
                    // No restriction
                    break;
            }

            // Check ownership requirements
            if (onlyOwned && card.owner != player)
            {
                return false;
            }

            if (onlyControlled && card.controller != player)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if stat-based selection would exceed limits
        /// </summary>
        /// <param name=\"selectedCards\">Currently selected cards</param>
        /// <param name=\"cardToCheck\">Card being considered for selection</param>
        /// <param name=\"context\">Ability context</param>
        /// <returns>True if selection would exceed stat limit</returns>
        public bool WouldExceedStatLimit(List<BaseCard> selectedCards, BaseCard cardToCheck, AbilityContext context)
        {
            if (!useStatLimit || maxStat == null || cardStat == null)
            {
                return false;
            }

            // If card is already selected, we're unselecting it, so no limit check needed
            if (selectedCards.Contains(cardToCheck))
            {
                return false;
            }

            int currentStat = selectedCards.Sum(card => cardStat(card));
            int cardStatValue = cardStat(cardToCheck);
            int maxStatValue = maxStat(context);

            return currentStat + cardStatValue > maxStatValue;
        }

        /// <summary>
        /// Validate a selection of cards
        /// </summary>
        /// <param name=\"selectedCards\">Cards that have been selected</param>
        /// <param name=\"context\">Ability context</param>
        /// <returns>Validation result</returns>
        public SelectionValidationResult ValidateSelection(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            var result = new SelectionValidationResult();

            // Check count requirements
            int minRequired = GetMinCards();
            int maxAllowed = GetMaxCards();

            if (selectedCards.Count < minRequired)
            {
                result.AddError($"Must select at least {minRequired} card(s)");
            }

            if (maxAllowed >= 0 && selectedCards.Count > maxAllowed)
            {
                result.AddError($"Cannot select more than {maxAllowed} card(s)");
            }

            if (RequiresExactCount() && selectedCards.Count != numCards)
            {
                result.AddError($"Must select exactly {numCards} card(s)");
            }

            // Check that all must-select cards are included
            foreach (var mustSelectCard in mustSelect)
            {
                if (!selectedCards.Contains(mustSelectCard))
                {
                    result.AddError($"Must select {mustSelectCard.name}");
                }
            }

            // Check empty selection
            if (selectedCards.Count == 0 && !allowEmptySelection && !optional)
            {
                result.AddError("Must select at least one card");
            }

            // Check stat limits
            if (useStatLimit && maxStat != null && cardStat != null && context != null)
            {
                int totalStat = selectedCards.Sum(card => cardStat(card));
                int maxStatValue = maxStat(context);
                
                if (totalStat > maxStatValue)
                {
                    result.AddError($"Total stat value ({totalStat}) exceeds maximum ({maxStatValue})");
                }
            }

            // Check game action requirements
            if (gameActions.Count > 0)
            {
                foreach (var card in selectedCards)
                {
                    bool canPerformAnyAction = gameActions.Any(action => action.CanAffect(card, context));
                    if (!canPerformAnyAction)
                    {
                        result.AddError($"{card.name} cannot be affected by the required game action(s)");
                    }
                }
            }

            // Run custom validator if provided
            if (customValidator != null)
            {
                string customError = customValidator(selectedCards);
                if (!string.IsNullOrEmpty(customError))
                {
                    result.AddError(customError);
                }
            }

            return result;
        }

        /// <summary>
        /// Get display text for the number of cards to select
        /// </summary>
        /// <returns>Display text</returns>
        public string GetSelectionCountText()
        {
            if (optional && GetMinCards() == 0)
            {
                if (GetMaxCards() >= 0)
                {
                    return $"up to {GetMaxCards()}";
                }
                return "any number of";
            }

            if (RequiresExactCount())
            {
                return numCards == 1 ? "a" : numCards.ToString();
            }

            int min = GetMinCards();
            int max = GetMaxCards();

            if (max >= 0 && min == max)
            {
                return min == 1 ? "a" : min.ToString();
            }

            if (max >= 0)
            {
                return $"{min} to {max}";
            }

            return $"at least {min}";
        }

        /// <summary>
        /// Get the default active prompt title if none specified
        /// </summary>
        /// <returns>Default prompt title</returns>
        public string GetDefaultActivePromptTitle()
        {
            if (!string.IsNullOrEmpty(activePromptTitle))
            {
                return activePromptTitle;
            }

            string countText = GetSelectionCountText();
            string cardText = GetCardTypeText();

            return $"Choose {countText} {cardText}";
        }

        /// <summary>
        /// Get descriptive text for the card type being selected
        /// </summary>
        /// <returns>Card type description</returns>
        private string GetCardTypeText()
        {
            if (!string.IsNullOrEmpty(cardType))
            {
                return cardType;
            }

            if (cardTypes.Count == 1)
            {
                return cardTypes[0];
            }

            if (cardTypes.Count > 1)
            {
                return string.Join(" or ", cardTypes);
            }

            if (!string.IsNullOrEmpty(cardTrait))
            {
                return $"{cardTrait} card";
            }

            if (cardTraits.Count > 0)
            {
                return $"{string.Join(" or ", cardTraits)} card";
            }

            return "card";
        }

        /// <summary>
        /// Add a button to the prompt
        /// </summary>
        /// <param name=\"text\">Button text</param>
        /// <param name=\"arg\">Button argument</param>
        /// <param name=\"disabled\">Whether button is disabled</param>
        public void AddButton(string text, string arg, bool disabled = false)
        {
            buttons.Add(new
            {
                text = text,
                arg = arg,
                disabled = disabled
            });
        }

        /// <summary>
        /// Add a custom property
        /// </summary>
        /// <param name=\"key\">Property key</param>
        /// <param name=\"value\">Property value</param>
        public void AddCustomProperty(string key, object value)
        {
            customProperties[key] = value;
        }

        /// <summary>
        /// Get a custom property value
        /// </summary>
        /// <param name=\"key\">Property key</param>
        /// <returns>Property value or null</returns>
        public object GetCustomProperty(string key)
        {
            return customProperties.GetValueOrDefault(key);
        }

        /// <summary>
        /// Add a tag
        /// </summary>
        /// <param name=\"tag\">Tag to add</param>
        public void AddTag(string tag)
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        /// <summary>
        /// Check if has a specific tag
        /// </summary>
        /// <param name=\"tag\">Tag to check for</param>
        /// <returns>True if has tag</returns>
        public bool HasTag(string tag)
        {
            return tags.Contains(tag);
        }

        /// <summary>
        /// Create a copy of these properties
        /// </summary>
        /// <returns>Copied properties</returns>
        public SelectCardPromptProperties CreateCopy()
        {
            var copy = new SelectCardPromptProperties
            {
                activePromptTitle = activePromptTitle,
                waitingPromptTitle = waitingPromptTitle,
                promptDescription = promptDescription,
                source = source,
                selectCard = selectCard,
                selectRing = selectRing,
                selectPlayer = selectPlayer,
                selectProvince = selectProvince,
                ordered = ordered,
                optional = optional,
                cardCondition = cardCondition,
                cardType = cardType,
                cardTypes = new List<string>(cardTypes),
                cardTrait = cardTrait,
                cardTraits = new List<string>(cardTraits),
                cardFaction = cardFaction,
                cardFactions = new List<string>(cardFactions),
                cardLocation = cardLocation,
                cardLocations = new List<string>(cardLocations),
                controller = controller,
                targetController = targetController,
                onlyOwned = onlyOwned,
                onlyControlled = onlyControlled,
                numCards = numCards,
                minCards = minCards,
                maxCards = maxCards,
                exactlyVariable = exactlyVariable,
                numCardsVariable = numCardsVariable,
                canSelectFacedown = canSelectFacedown,
                canSelectFaceup = canSelectFaceup,
                canSelectBowed = canSelectBowed,
                canSelectReady = canSelectReady,
                canSelectHonored = canSelectHonored,
                canSelectDishonored = canSelectDishonored,
                requiresTarget = requiresTarget,
                multipleTargets = multipleTargets,
                mustSelect = new List<BaseCard>(mustSelect),
                cannotSelect = new List<BaseCard>(cannotSelect),
                onSelect = onSelect,
                onMenuCommand = onMenuCommand,
                onCancel = onCancel,
                onCardToggle = onCardToggle,
                buttons = new List<object>(buttons),
                controls = new List<object>(controls),
                showCancelButton = showCancelButton,
                showDoneButton = showDoneButton,
                doneButtonText = doneButtonText,
                cancelButtonText = cancelButtonText,
                gameAction = gameAction,
                gameActions = new List<GameAction>(gameActions),
                context = context,
                selector = selector,
                customProperties = new Dictionary<string, object>(customProperties),
                tags = new List<string>(tags),
                validateOnSelect = validateOnSelect,
                customValidator = customValidator,
                allowEmptySelection = allowEmptySelection,
                maxStat = maxStat,
                cardStat = cardStat,
                useStatLimit = useStatLimit
            };

            return copy;
        }

        /// <summary>
        /// Get summary of prompt properties for display
        /// </summary>
        /// <returns>Summary string</returns>
        public string GetSummary()
        {
            var summary = new List<string>();

            if (!string.IsNullOrEmpty(activePromptTitle))
            {
                summary.Add($"Title: {activePromptTitle}");
            }

            string countText = GetSelectionCountText();
            string cardText = GetCardTypeText();
            summary.Add($"Selection: {countText} {cardText}");

            if (optional)
            {
                summary.Add("Optional");
            }

            if (mustSelect.Count > 0)
            {
                summary.Add($"Must select: {string.Join(", ", mustSelect.Select(c => c.name))}");
            }

            if (cannotSelect.Count > 0)
            {
                summary.Add($"Cannot select: {string.Join(", ", cannotSelect.Select(c => c.name))}");
            }

            if (useStatLimit)
            {
                summary.Add("Stat-limited selection");
            }

            if (tags.Count > 0)
            {
                summary.Add($"Tags: {string.Join(", ", tags)}");
            }

            return summary.Count > 0 ? string.Join("; ", summary) : "Basic card selection";
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetSummary();
        }
    }

    /// <summary>
    /// Result of selection validation
    /// </summary>
    public class SelectionValidationResult
    {
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public bool IsValid => errors.Count == 0;
        public bool HasWarnings => warnings.Count > 0;

        public void AddError(string error)
        {
            errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            warnings.Add(warning);
        }

        public string GetErrorSummary()
        {
            if (IsValid) return "Valid";
            return $"{errors.Count} error(s): {string.Join("; ", errors)}";
        }

        public string GetWarningSummary()
        {
            if (!HasWarnings) return "";
            return $"{warnings.Count} warning(s): {string.Join("; ", warnings)}";
        }
    }

    /// <summary>
    /// Factory class for creating common select card prompt properties
    /// </summary>
    public static class SelectCardPromptPropertiesFactory
    {
        /// <summary>
        /// Create properties for selecting a single card
        /// </summary>
        /// <param name=\"title\">Prompt title</param>
        /// <returns>Single card selection properties</returns>
        public static SelectCardPromptProperties CreateSingle(string title = "Choose a card")
        {
            return new SelectCardPromptProperties(title, 1);
        }

        /// <summary>
        /// Create properties for selecting multiple cards
        /// </summary>
        /// <param name=\"numCards\">Number of cards to select</param>
        /// <param name=\"title\">Prompt title</param>
        /// <returns>Multiple card selection properties</returns>
        public static SelectCardPromptProperties CreateMultiple(int numCards, string title = null)
        {
            title = title ?? $"Choose {numCards} cards";
            return new SelectCardPromptProperties(title, numCards);
        }

        /// <summary>
        /// Create properties for optional card selection
        /// </summary>
        /// <param name=\"maxCards\">Maximum cards to select</param>
        /// <param name=\"title\">Prompt title</param>
        /// <returns>Optional selection properties</returns>
        public static SelectCardPromptProperties CreateOptional(int maxCards = -1, string title = "Choose cards (optional)")
        {
            return new SelectCardPromptProperties(title, 0)
            {
                optional = true,
                maxCards = maxCards
            };
        }

        /// <summary>
        /// Create properties for selecting cards by type
        /// </summary>
        /// <param name=\"cardType\">Card type to select</param>
        /// <param name=\"numCards\">Number of cards</param>
        /// <returns>Type-specific selection properties</returns>
        public static SelectCardPromptProperties CreateByType(string cardType, int numCards = 1)
        {
            return new SelectCardPromptProperties($"Choose {(numCards == 1 ? "a" : numCards.ToString())} {cardType}", numCards)
            {
                cardType = cardType
            };
        }

        /// <summary>
        /// Create properties for selecting cards by trait
        /// </summary>
        /// <param name=\"trait\">Trait to select</param>
        /// <param name=\"numCards\">Number of cards</param>
        /// <returns>Trait-specific selection properties</returns>
        public static SelectCardPromptProperties CreateByTrait(string trait, int numCards = 1)
        {
            return new SelectCardPromptProperties($"Choose {(numCards == 1 ? "a" : numCards.ToString())} {trait} card", numCards)
            {
                cardTrait = trait
            };
        }

        /// <summary>
        /// Create properties for selecting cards from a specific location
        /// </summary>
        /// <param name=\"location\">Location to select from</param>
        /// <param name=\"numCards\">Number of cards</param>
        /// <returns>Location-specific selection properties</returns>
        public static SelectCardPromptProperties CreateFromLocation(string location, int numCards = 1)
        {
            return new SelectCardPromptProperties($"Choose {(numCards == 1 ? "a" : numCards.ToString())} card from {location}", numCards)
            {
                cardLocation = location
            };
        }

        /// <summary>
        /// Create properties for stat-based selection
        /// </summary>
        /// <param name=\"maxStatFunc\">Function to get maximum stat</param>
        /// <param name=\"cardStatFunc\">Function to get card stat</param>
        /// <param name=\"title\">Prompt title</param>
        /// <returns>Stat-based selection properties</returns>
        public static SelectCardPromptProperties CreateStatBased(System.Func<AbilityContext, int> maxStatFunc, 
                                                                System.Func<BaseCard, int> cardStatFunc, 
                                                                string title = "Choose cards")
        {
            return new SelectCardPromptProperties(title, 0)
            {
                maxStat = maxStatFunc,
                cardStat = cardStatFunc,
                useStatLimit = true,
                numCards = 0, // Unlimited base count, limited by stat
                optional = true
            };
        }

        /// <summary>
        /// Create properties with custom builder pattern
        /// </summary>
        /// <returns>Properties builder</returns>
        public static SelectCardPromptPropertiesBuilder CreateCustom()
        {
            return new SelectCardPromptPropertiesBuilder();
        }
    }

    /// <summary>
    /// Builder class for creating custom select card prompt properties
    /// </summary>
    public class SelectCardPromptPropertiesBuilder
    {
        private SelectCardPromptProperties properties;

        public SelectCardPromptPropertiesBuilder()
        {
            properties = new SelectCardPromptProperties();
        }

        public SelectCardPromptPropertiesBuilder WithTitle(string title)
        {
            properties.activePromptTitle = title;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithWaitingTitle(string title)
        {
            properties.waitingPromptTitle = title;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithSource(string source)
        {
            properties.source = source;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithNumCards(int numCards)
        {
            properties.numCards = numCards;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithMinCards(int minCards)
        {
            properties.minCards = minCards;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithMaxCards(int maxCards)
        {
            properties.maxCards = maxCards;
            return this;
        }

        public SelectCardPromptPropertiesBuilder AsOptional()
        {
            properties.optional = true;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithCardType(string cardType)
        {
            properties.cardType = cardType;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithCardTypes(params string[] cardTypes)
        {
            properties.cardTypes = cardTypes.ToList();
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithTrait(string trait)
        {
            properties.cardTrait = trait;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithTraits(params string[] traits)
        {
            properties.cardTraits = traits.ToList();
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithFaction(string faction)
        {
            properties.cardFaction = faction;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithLocation(string location)
        {
            properties.cardLocation = location;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithLocations(params string[] locations)
        {
            properties.cardLocations = locations.ToList();
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithController(string controller)
        {
            properties.controller = controller;
            return this;
        }

        public SelectCardPromptPropertiesBuilder OnlyOwned()
        {
            properties.onlyOwned = true;
            return this;
        }

        public SelectCardPromptPropertiesBuilder OnlyControlled()
        {
            properties.onlyControlled = true;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithCondition(System.Func<BaseCard, AbilityContext, bool> condition)
        {
            properties.cardCondition = condition;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithCannotSelect(params BaseCard[] cards)
        {
            properties.cannotSelect.AddRange(cards);
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithOnSelect(System.Func<Player, object, bool> onSelect)
        {
            properties.onSelect = onSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithOnCancel(System.Func<Player, bool> onCancel)
        {
            properties.onCancel = onCancel;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithOnMenuCommand(System.Func<Player, string, bool> onMenuCommand)
        {
            properties.onMenuCommand = onMenuCommand;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithOnCardToggle(System.Action<Player, BaseCard> onCardToggle)
        {
            properties.onCardToggle = onCardToggle;
            return this;
        }

        public SelectCardPromptPropertiesBuilder AsOrdered()
        {
            properties.ordered = true;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithButton(string text, string arg, bool disabled = false)
        {
            properties.AddButton(text, arg, disabled);
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithGameAction(object gameAction)
        {
            properties.gameAction = gameAction;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithGameActions(params GameAction[] gameActions)
        {
            properties.gameActions.AddRange(gameActions);
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithContext(AbilityContext context)
        {
            properties.context = context;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithSelector(BaseCardSelector selector)
        {
            properties.selector = selector;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithCustomValidator(System.Func<List<BaseCard>, string> validator)
        {
            properties.customValidator = validator;
            return this;
        }

        public SelectCardPromptPropertiesBuilder AllowEmptySelection()
        {
            properties.allowEmptySelection = true;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithStatLimit(System.Func<AbilityContext, int> maxStat, System.Func<BaseCard, int> cardStat)
        {
            properties.maxStat = maxStat;
            properties.cardStat = cardStat;
            properties.useStatLimit = true;
            return this;
        }

        public SelectCardPromptPropertiesBuilder CanSelectFacedown(bool canSelect = true)
        {
            properties.canSelectFacedown = canSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder CanSelectFaceup(bool canSelect = true)
        {
            properties.canSelectFaceup = canSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder CanSelectBowed(bool canSelect = true)
        {
            properties.canSelectBowed = canSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder CanSelectReady(bool canSelect = true)
        {
            properties.canSelectReady = canSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder CanSelectHonored(bool canSelect = true)
        {
            properties.canSelectHonored = canSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder CanSelectDishonored(bool canSelect = true)
        {
            properties.canSelectDishonored = canSelect;
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithTag(string tag)
        {
            properties.AddTag(tag);
            return this;
        }

        public SelectCardPromptPropertiesBuilder WithCustomProperty(string key, object value)
        {
            properties.AddCustomProperty(key, value);
            return this;
        }

        public SelectCardPromptProperties Build()
        {
            return properties.CreateCopy();
        }
    }

    /// <summary>
    /// Extension methods for select card prompt properties
    /// </summary>
    public static class SelectCardPromptPropertiesExtensions
    {
        /// <summary>
        /// Check if properties match a specific pattern
        /// </summary>
        /// <param name="properties">Properties to check</param>
        /// <param name="pattern">Pattern to match</param>
        /// <returns>True if properties match pattern</returns>
        public static bool MatchesPattern(this SelectCardPromptProperties properties, string pattern)
        {
            switch (pattern.ToLower())
            {
                case "single":
                    return properties.numCards == 1 && !properties.optional;
                case "multiple":
                    return properties.numCards > 1;
                case "optional":
                    return properties.optional;
                case "unlimited":
                    return properties.numCards == 0 || properties.GetMaxCards() == -1;
                case "stat-based":
                    return properties.useStatLimit;
                case "typed":
                    return !string.IsNullOrEmpty(properties.cardType) || properties.cardTypes.Count > 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get all valid targets for these properties
        /// </summary>
        /// <param name="properties">Properties to check</param>
        /// <param name="game">Game instance</param>
        /// <param name="context">Ability context</param>
        /// <returns>List of valid target cards</returns>
        public static List<BaseCard> GetValidTargets(this SelectCardPromptProperties properties, 
                                                    Game game, AbilityContext context)
        {
            var allCards = game.allCards ?? new List<BaseCard>();
            
            return allCards.Where(card => properties.MatchesCardCriteria(card, context)).ToList();
        }

        /// <summary>
        /// Check if properties allow targeting a specific card
        /// </summary>
        /// <param name="properties">Properties to check</param>
        /// <param name="card">Card to check</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if card can be targeted</returns>
        public static bool CanTarget(this SelectCardPromptProperties properties, BaseCard card, AbilityContext context)
        {
            return properties.MatchesCardCriteria(card, context);
        }

        /// <summary>
        /// Get default button configuration for properties
        /// </summary>
        /// <param name="properties">Properties to get buttons for</param>
        /// <param name="selectedCards">Currently selected cards</param>
        /// <returns>List of button configurations</returns>
        public static List<object> GetDefaultButtons(this SelectCardPromptProperties properties, List<BaseCard> selectedCards)
        {
            var buttons = new List<object>(properties.buttons);

            // Add done button if multiple selection and enough cards selected
            if ((properties.numCards > 1 || properties.numCards == 0) && 
                selectedCards.Count >= properties.GetMinCards() && 
                properties.showDoneButton)
            {
                buttons.Insert(0, new
                {
                    text = properties.doneButtonText,
                    arg = "done",
                    disabled = false
                });
            }

            // Add cancel button if allowed
            if (properties.showCancelButton)
            {
                buttons.Add(new
                {
                    text = properties.cancelButtonText,
                    arg = "cancel",
                    disabled = false
                });
            }

            return buttons;
        }

        /// <summary>
        /// Merge two sets of properties
        /// </summary>
        /// <param name="primary">Primary properties</param>
        /// <param name="secondary">Secondary properties to merge</param>
        /// <returns>Merged properties</returns>
        public static SelectCardPromptProperties MergeWith(this SelectCardPromptProperties primary, 
                                                          SelectCardPromptProperties secondary)
        {
            var merged = primary.CreateCopy();

            // Override basic properties
            if (!string.IsNullOrEmpty(secondary.activePromptTitle))
            {
                merged.activePromptTitle = secondary.activePromptTitle;
            }

            if (!string.IsNullOrEmpty(secondary.waitingPromptTitle))
            {
                merged.waitingPromptTitle = secondary.waitingPromptTitle;
            }

            if (!string.IsNullOrEmpty(secondary.source))
            {
                merged.source = secondary.source;
            }

            // Merge selection criteria (intersection for restrictions)
            if (!string.IsNullOrEmpty(secondary.cardType))
            {
                merged.cardType = secondary.cardType;
            }

            if (secondary.cardTypes.Count > 0)
            {
                if (merged.cardTypes.Count > 0)
                {
                    merged.cardTypes = merged.cardTypes.Intersect(secondary.cardTypes).ToList();
                }
                else
                {
                    merged.cardTypes = new List<string>(secondary.cardTypes);
                }
            }

            // Merge must select lists (union)
            merged.mustSelect.AddRange(secondary.mustSelect);
            merged.cannotSelect.AddRange(secondary.cannotSelect);

            // Take more restrictive counts
            merged.numCards = Math.Max(merged.numCards, secondary.numCards);
            merged.minCards = Math.Max(merged.minCards, secondary.minCards);
            
            if (secondary.maxCards >= 0)
            {
                merged.maxCards = merged.maxCards >= 0 ? 
                    Math.Min(merged.maxCards, secondary.maxCards) : secondary.maxCards;
            }

            // Merge state restrictions (logical AND)
            merged.canSelectFacedown = merged.canSelectFacedown && secondary.canSelectFacedown;
            merged.canSelectFaceup = merged.canSelectFaceup && secondary.canSelectFaceup;
            merged.canSelectBowed = merged.canSelectBowed && secondary.canSelectBowed;
            merged.canSelectReady = merged.canSelectReady && secondary.canSelectReady;
            merged.canSelectHonored = merged.canSelectHonored && secondary.canSelectHonored;
            merged.canSelectDishonored = merged.canSelectDishonored && secondary.canSelectDishonored;

            // Override callbacks if provided
            if (secondary.onSelect != null)
            {
                merged.onSelect = secondary.onSelect;
            }

            if (secondary.onMenuCommand != null)
            {
                merged.onMenuCommand = secondary.onMenuCommand;
            }

            if (secondary.onCancel != null)
            {
                merged.onCancel = secondary.onCancel;
            }

            // Merge buttons and controls
            merged.buttons.AddRange(secondary.buttons);
            merged.controls.AddRange(secondary.controls);

            // Override other properties
            merged.optional = merged.optional || secondary.optional;
            merged.ordered = merged.ordered || secondary.ordered;
            merged.validateOnSelect = merged.validateOnSelect && secondary.validateOnSelect;
            merged.allowEmptySelection = merged.allowEmptySelection || secondary.allowEmptySelection;

            // Merge stat limits (take secondary if present)
            if (secondary.useStatLimit)
            {
                merged.useStatLimit = true;
                merged.maxStat = secondary.maxStat;
                merged.cardStat = secondary.cardStat;
            }

            // Merge tags and custom properties
            foreach (var tag in secondary.tags)
            {
                merged.AddTag(tag);
            }

            foreach (var kvp in secondary.customProperties)
            {
                merged.customProperties[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        /// <summary>
        /// Create a restricted version of properties
        /// </summary>
        /// <param name="properties">Original properties</param>
        /// <param name="restriction">Restriction to apply</param>
        /// <returns>Restricted properties</returns>
        public static SelectCardPromptProperties RestrictBy(this SelectCardPromptProperties properties, 
                                                           SelectCardRestriction restriction)
        {
            var restricted = properties.CreateCopy();

            switch (restriction.type)
            {
                case SelectCardRestrictionType.ForbidType:
                    restricted.cardTypes.Remove(restriction.value as string);
                    if (restricted.cardType == (restriction.value as string))
                    {
                        restricted.cardType = "";
                    }
                    break;

                case SelectCardRestrictionType.RequireType:
                    restricted.cardType = restriction.value as string;
                    restricted.cardTypes = new List<string> { restriction.value as string };
                    break;

                case SelectCardRestrictionType.ForbidTrait:
                    restricted.cardTraits.Remove(restriction.value as string);
                    if (restricted.cardTrait == (restriction.value as string))
                    {
                        restricted.cardTrait = "";
                    }
                    break;

                case SelectCardRestrictionType.RequireTrait:
                    restricted.cardTrait = restriction.value as string;
                    restricted.cardTraits = new List<string> { restriction.value as string };
                    break;

                case SelectCardRestrictionType.ForbidLocation:
                    restricted.cardLocations.Remove(restriction.value as string);
                    if (restricted.cardLocation == (restriction.value as string))
                    {
                        restricted.cardLocation = "";
                    }
                    break;

                case SelectCardRestrictionType.RequireLocation:
                    restricted.cardLocation = restriction.value as string;
                    restricted.cardLocations = new List<string> { restriction.value as string };
                    break;

                case SelectCardRestrictionType.MaxCards:
                    restricted.maxCards = (int)restriction.value;
                    break;

                case SelectCardRestrictionType.MinCards:
                    restricted.minCards = (int)restriction.value;
                    break;

                case SelectCardRestrictionType.ExactCards:
                    restricted.numCards = (int)restriction.value;
                    restricted.minCards = (int)restriction.value;
                    restricted.maxCards = (int)restriction.value;
                    break;

                case SelectCardRestrictionType.ForbidCard:
                    restricted.cannotSelect.Add(restriction.value as BaseCard);
                    break;

                case SelectCardRestrictionType.RequireCard:
                    restricted.mustSelect.Add(restriction.value as BaseCard);
                    break;
            }

            return restricted;
        }

        /// <summary>
        /// Get optimization hints for card selection
        /// </summary>
        /// <param name="properties">Properties to analyze</param>
        /// <returns>Optimization hints</returns>
        public static List<string> GetOptimizationHints(this SelectCardPromptProperties properties)
        {
            var hints = new List<string>();

            if (properties.numCards == 1 && !properties.optional)
            {
                hints.Add("Single card selection - consider auto-select if only one valid target");
            }

            if (properties.useStatLimit)
            {
                hints.Add("Stat-based selection - pre-sort cards by stat value");
            }

            if (properties.cardLocations.Count == 1)
            {
                hints.Add($"Single location - filter cards to {properties.cardLocations[0]} only");
            }

            if (!string.IsNullOrEmpty(properties.cardType))
            {
                hints.Add($"Type-specific - filter to {properties.cardType} cards only");
            }

            if (properties.mustSelect.Count > 0)
            {
                hints.Add("Has required selections - pre-select must-select cards");
            }

            if (properties.cannotSelect.Count > 0)
            {
                hints.Add("Has forbidden selections - exclude cannot-select cards");
            }

            if (properties.optional && properties.GetMinCards() == 0)
            {
                hints.Add("Optional selection - show clear 'skip' option");
            }

            return hints;
        }
    }

    /// <summary>
    /// Types of restrictions for select card prompts
    /// </summary>
    public enum SelectCardRestrictionType
    {
        ForbidType,
        RequireType,
        ForbidTrait,
        RequireTrait,
        ForbidLocation,
        RequireLocation,
        ForbidFaction,
        RequireFaction,
        MaxCards,
        MinCards,
        ExactCards,
        ForbidCard,
        RequireCard,
        ForbidController,
        RequireController
    }

    /// <summary>
    /// Represents a restriction on card selection
    /// </summary>
    [System.Serializable]
    public class SelectCardRestriction
    {
        public SelectCardRestrictionType type;
        public object value;
        public string source;
        public int priority = 0;

        public SelectCardRestriction(SelectCardRestrictionType restrictionType, object restrictionValue, string restrictionSource = "")
        {
            type = restrictionType;
            value = restrictionValue;
            source = restrictionSource;
        }
    }

    /// <summary>
    /// Manager for select card prompt properties and restrictions
    /// </summary>
    public class SelectCardPromptPropertiesManager
    {
        private Game game;
        private List<SelectCardRestriction> activeRestrictions = new List<SelectCardRestriction>();
        private SelectCardPromptProperties baseProperties;

        public SelectCardPromptPropertiesManager(Game gameInstance)
        {
            game = gameInstance;
            baseProperties = new SelectCardPromptProperties();
        }

        /// <summary>
        /// Add a selection restriction
        /// </summary>
        /// <param name="restriction">Restriction to add</param>
        public void AddRestriction(SelectCardRestriction restriction)
        {
            activeRestrictions.Add(restriction);
            SortRestrictionsByPriority();
        }

        /// <summary>
        /// Remove a selection restriction
        /// </summary>
        /// <param name="restriction">Restriction to remove</param>
        public void RemoveRestriction(SelectCardRestriction restriction)
        {
            activeRestrictions.Remove(restriction);
        }

        /// <summary>
        /// Remove all restrictions from a specific source
        /// </summary>
        /// <param name="source">Source to remove restrictions from</param>
        public void RemoveRestrictionsFromSource(string source)
        {
            activeRestrictions.RemoveAll(r => r.source == source);
        }

        /// <summary>
        /// Get the current effective properties
        /// </summary>
        /// <param name="baseProps">Base properties to start with</param>
        /// <returns>Effective properties with restrictions applied</returns>
        public SelectCardPromptProperties GetEffectiveProperties(SelectCardPromptProperties baseProps)
        {
            var effective = baseProps.CreateCopy();

            foreach (var restriction in activeRestrictions)
            {
                effective = effective.RestrictBy(restriction);
            }

            return effective;
        }

        /// <summary>
        /// Sort restrictions by priority
        /// </summary>
        private void SortRestrictionsByPriority()
        {
            activeRestrictions.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        /// <summary>
        /// Clear all restrictions
        /// </summary>
        public void ClearAllRestrictions()
        {
            activeRestrictions.Clear();
        }

        /// <summary>
        /// Get all active restrictions
        /// </summary>
        /// <returns>List of active restrictions</returns>
        public List<SelectCardRestriction> GetActiveRestrictions()
        {
            return activeRestrictions.ToList();
        }
    }
}
