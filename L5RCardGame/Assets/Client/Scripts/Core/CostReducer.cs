using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for creating a cost reducer
    /// </summary>
    [System.Serializable]
    public class CostReducerProperties
    {
        [Header("Basic Properties")]
        public object limit;
        public string cardType;
        public object amount = 1;
        public string description;
        public bool persistent = true;

        [Header("Condition Functions")]
        public Func<BaseCard, EffectSource, bool> match;
        public Func<object, EffectSource, bool> targetCondition;

        [Header("Play Type Restrictions")]
        public List<string> playingTypes;
        public object playingTypesRaw; // For handling both arrays and single values

        [Header("Advanced Options")]
        public bool ignoreType = false;
        public int priority = 0;
        public Dictionary<string, object> customProperties;

        /// <summary>
        /// Constructor with defaults
        /// </summary>
        public CostReducerProperties()
        {
            // Set default match function
            match = (card, source) => true;
            customProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor from dictionary (for serialization/deserialization)
        /// </summary>
        public CostReducerProperties(Dictionary<string, object> properties)
        {
            // Initialize defaults
            match = (card, source) => true;
            amount = 1;
            persistent = true;
            customProperties = new Dictionary<string, object>();

            // Parse properties
            if (properties != null)
            {
                ParseFromDictionary(properties);
            }
        }

        /// <summary>
        /// Parse properties from dictionary
        /// </summary>
        private void ParseFromDictionary(Dictionary<string, object> properties)
        {
            foreach (var kvp in properties)
            {
                switch (kvp.Key.ToLower())
                {
                    case "limit":
                        limit = kvp.Value;
                        break;
                    case "cardtype":
                        cardType = kvp.Value as string;
                        break;
                    case "amount":
                        amount = kvp.Value;
                        break;
                    case "description":
                        description = kvp.Value as string;
                        break;
                    case "persistent":
                        if (kvp.Value is bool boolValue)
                            persistent = boolValue;
                        break;
                    case "playingtypes":
                        playingTypesRaw = kvp.Value;
                        ParsePlayingTypes();
                        break;
                    case "ignoretype":
                        if (kvp.Value is bool ignoreValue)
                            ignoreType = ignoreValue;
                        break;
                    case "priority":
                        if (kvp.Value is int priorityValue)
                            priority = priorityValue;
                        break;
                    default:
                        customProperties[kvp.Key] = kvp.Value;
                        break;
                }
            }
        }

        /// <summary>
        /// Parse playing types from raw value (handles both arrays and single values)
        /// </summary>
        private void ParsePlayingTypes()
        {
            if (playingTypesRaw == null)
            {
                playingTypes = null;
                return;
            }

            if (playingTypesRaw is List<string> list)
            {
                playingTypes = list;
            }
            else if (playingTypesRaw is string[] array)
            {
                playingTypes = array.ToList();
            }
            else if (playingTypesRaw is string singleType)
            {
                playingTypes = new List<string> { singleType };
            }
            else
            {
                // Try to convert to string array
                try
                {
                    var stringValue = playingTypesRaw.ToString();
                    if (stringValue.Contains(","))
                    {
                        playingTypes = stringValue.Split(',').Select(s => s.Trim()).ToList();
                    }
                    else
                    {
                        playingTypes = new List<string> { stringValue };
                    }
                }
                catch
                {
                    playingTypes = null;
                }
            }
        }

        /// <summary>
        /// Validate the properties
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = null;

            // Validate card type
            if (!string.IsNullOrEmpty(cardType) && !ConstantsHelper.IsValidCardType(cardType))
            {
                errorMessage = $"Invalid card type: {cardType}";
                return false;
            }

            // Validate playing types
            if (playingTypes != null)
            {
                foreach (var playingType in playingTypes)
                {
                    if (string.IsNullOrEmpty(playingType))
                    {
                        errorMessage = "Playing type cannot be null or empty";
                        return false;
                    }
                }
            }

            // Validate amount
            if (amount != null && !(amount is int) && !(amount is Func<BaseCard, Player, int>))
            {
                if (!int.TryParse(amount.ToString(), out _))
                {
                    errorMessage = "Amount must be an integer or a function";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Clone the properties
        /// </summary>
        public CostReducerProperties Clone()
        {
            var clone = new CostReducerProperties
            {
                limit = limit,
                cardType = cardType,
                amount = amount,
                description = description,
                persistent = persistent,
                playingTypes = playingTypes?.ToList(),
                playingTypesRaw = playingTypesRaw,
                ignoreType = ignoreType,
                priority = priority,
                match = match,
                targetCondition = targetCondition,
                customProperties = new Dictionary<string, object>(customProperties ?? new Dictionary<string, object>())
            };

            return clone;
        }

        /// <summary>
        /// Convert to dictionary for serialization
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();

            if (limit != null) dict["limit"] = limit;
            if (!string.IsNullOrEmpty(cardType)) dict["cardType"] = cardType;
            if (amount != null) dict["amount"] = amount;
            if (!string.IsNullOrEmpty(description)) dict["description"] = description;
            if (!persistent) dict["persistent"] = persistent;
            if (playingTypes != null && playingTypes.Count > 0) dict["playingTypes"] = playingTypes;
            if (ignoreType) dict["ignoreType"] = ignoreType;
            if (priority != 0) dict["priority"] = priority;

            // Add custom properties
            if (customProperties != null)
            {
                foreach (var kvp in customProperties)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Create properties for common patterns
        /// </summary>
        public static CostReducerProperties ForCardType(string cardType, int amount, string description = null)
        {
            return new CostReducerProperties
            {
                cardType = cardType,
                amount = amount,
                description = description ?? $"Reduce {cardType} cost by {amount}"
            };
        }

        public static CostReducerProperties ForTrait(string trait, int amount, string cardType = null)
        {
            return new CostReducerProperties
            {
                cardType = cardType,
                amount = amount,
                match = (card, source) => card.HasTrait(trait),
                description = $"Reduce {trait} {cardType ?? "card"} cost by {amount}"
            };
        }

        public static CostReducerProperties ForFaction(string faction, int amount, string cardType = null)
        {
            return new CostReducerProperties
            {
                cardType = cardType,
                amount = amount,
                match = (card, source) => card.IsFaction(faction),
                description = $"Reduce {faction} {cardType ?? "card"} cost by {amount}"
            };
        }

        public static CostReducerProperties ForPlayingType(List<string> playingTypes, int amount)
        {
            return new CostReducerProperties
            {
                playingTypes = playingTypes,
                amount = amount,
                description = $"Reduce cost by {amount} when playing from {string.Join(" or ", playingTypes)}"
            };
        }

        public static CostReducerProperties WithLimit(int amount, int maxUses, string limitType = "perRound")
        {
            return new CostReducerProperties
            {
                amount = amount,
                limit = new AbilityLimit { maxUses = maxUses, limitType = limitType },
                description = $"Reduce cost by {amount} ({maxUses} per {limitType})"
            };
        }
    }

    /// <summary>
    /// Reduces the cost of playing cards based on specific conditions.
    /// Can be limited by uses, card types, playing types, and custom conditions.
    /// </summary>
    public class CostReducer
    {
        [Header("Cost Reducer Properties")]
        public Game game;
        public EffectSource source;
        public int uses = 0;
        public AbilityLimit limit;
        public string cardType;
        public Func<BaseCard, EffectSource, bool> match;
        public Func<object, EffectSource, bool> targetCondition;
        public object amount = 1;
        public List<string> playingTypes;
        public string description;
        public bool persistent = true;

        // State tracking
        public bool isActive = true;
        public DateTime createdTime;

        /// <summary>
        /// Enhanced constructor with better playingTypes handling (matches test requirements)
        /// </summary>
        public CostReducer(Game game, EffectSource source, CostReducerProperties properties)
        {
            this.game = game;
            this.source = source;
            this.uses = 0; // Default to no uses (matches test expectation)
            this.limit = properties.limit as AbilityLimit;
            this.cardType = properties.cardType;
            this.match = properties.match ?? ((card, src) => true);
            this.targetCondition = properties.targetCondition;
            this.amount = properties.amount ?? 1; // Default to 1 (matches test expectation)
            this.description = properties.description ?? "Cost reduction";
            this.persistent = properties.persistent;
            this.createdTime = DateTime.Now;

            // Handle playing types with proper array conversion (matches test requirements)
            HandlePlayingTypes(properties);

            // Register limit events if applicable (matches test expectation)
            if (limit != null)
            {
                limit.RegisterEvents(game);
            }
        }

        /// <summary>
        /// Handle playing types conversion (matches test requirement for array wrapping)
        /// </summary>
        private void HandlePlayingTypes(CostReducerProperties properties)
        {
            if (properties.playingTypes != null)
            {
                this.playingTypes = properties.playingTypes.ToList();
            }
            else if (properties.playingTypesRaw != null)
            {
                if (properties.playingTypesRaw is List<string> list)
                {
                    this.playingTypes = list;
                }
                else if (properties.playingTypesRaw is string[] array)
                {
                    this.playingTypes = array.ToList();
                }
                else if (properties.playingTypesRaw is string singleType)
                {
                    // Wrap single string in array (matches test expectation)
                    this.playingTypes = new List<string> { singleType };
                }
            }
        }

        /// <summary>
        /// Check if this reducer can reduce the cost (enhanced to match all test cases)
        /// </summary>
        public bool CanReduce(string playingType, BaseCard card, object target = null, bool ignoreType = false)
        {
            if (!isActive)
            {
                return false;
            }

            // Check limit (matches test: should return false when limit reached)
            if (limit != null && limit.IsAtMax(source.controller))
            {
                return false;
            }

            // Check card type (matches test: should return false when type doesn't match)
            if (!ignoreType && !string.IsNullOrEmpty(cardType) && card.GetCardType() != cardType)
            {
                return false;
            }

            // Check playing types (matches test: should return false when play type doesn't match)
            if (playingTypes != null && playingTypes.Count > 0 && !playingTypes.Contains(playingType))
            {
                return false;
            }

            // Check match condition (matches test: should return false when match function returns false)
            if (!match(card, source))
            {
                return false;
            }

            // Check target condition
            return CheckTargetCondition(target);
        }

        /// <summary>
        /// Mark this reducer as used (enhanced to match test requirements)
        /// </summary>
        public void MarkUsed()
        {
            uses++;

            // Increment limit if present (matches test: should not crash when no limit)
            if (limit != null)
            {
                limit.Increment(source.controller);
            }

            // Log usage if debug mode
            if (game?.debugMode == true)
            {
                Debug.Log($"💰 Cost reducer used: {description} (Total uses: {uses})");
            }
        }

        /// <summary>
        /// Check if this reducer has expired (enhanced to match all test cases)
        /// </summary>
        public bool IsExpired()
        {
            if (!isActive)
            {
                return true;
            }

            // Handle no limit case (matches test: should return false when no limit)
            if (limit == null)
            {
                return false;
            }

            // Check if limit is exceeded and not repeatable (matches test cases)
            bool limitReached = limit.IsAtMax(source.controller);
            bool isRepeatable = limit.IsRepeatable();

            // Return true only if limit reached AND not repeatable (matches test logic)
            return limitReached && !isRepeatable;
        }

        /// <summary>
        /// Unregister events (enhanced to match test requirements)
        /// </summary>
        public void UnregisterEvents()
        {
            // Should not crash when no limit (matches test expectation)
            if (limit != null)
            {
                limit.UnregisterEvents(game);
            }
        }

        /// <summary>
        /// Check if the target meets the required condition
        /// </summary>
        public bool CheckTargetCondition(object target)
        {
            if (targetCondition == null)
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            return targetCondition(target, source);
        }

        /// <summary>
        /// Get the amount of cost reduction for the given card and player
        /// </summary>
        public int GetAmount(BaseCard card, Player player)
        {
            if (amount is Func<BaseCard, Player, int> amountFunc)
            {
                return amountFunc(card, player);
            }
            else if (amount is int intAmount)
            {
                return intAmount;
            }
            else if (amount is string stringAmount && int.TryParse(stringAmount, out int parsedAmount))
            {
                return parsedAmount;
            }

            return 1; // Default reduction
        }

        /// <summary>
        /// Mark this reducer as used and increment limit if applicable
        /// </summary>
        public void MarkUsed()
        {
            uses++;

            if (limit != null)
            {
                limit.Increment(source.controller);
            }

            // Log usage if debug mode
            if (game?.debugMode == true)
            {
                Debug.Log($"💰 Cost reducer used: {description} (Total uses: {uses})");
            }
        }

        /// <summary>
        /// Check if this reducer has expired and should be removed
        /// </summary>
        public bool IsExpired()
        {
            if (!isActive)
            {
                return true;
            }

            // Check if limit is exceeded and not repeatable
            if (limit != null && limit.IsAtMax(source.controller) && !limit.IsRepeatable())
            {
                return true;
            }

            // Check if source is no longer valid
            if (source == null || (source is BaseCard card && card.location == Locations.RemovedFromGame))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deactivate this reducer
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            UnregisterEvents();

            if (game?.debugMode == true)
            {
                Debug.Log($"💰 Cost reducer deactivated: {description}");
            }
        }

        /// <summary>
        /// Unregister any event listeners
        /// </summary>
        public void UnregisterEvents()
        {
            if (limit != null)
            {
                limit.UnregisterEvents(game);
            }
        }

        /// <summary>
        /// Get a summary of this cost reducer for debugging
        /// </summary>
        public string GetSummary()
        {
            var summary = $"Cost Reducer: {description}\n";
            summary += $"Source: {source?.name ?? "Unknown"}\n";
            summary += $"Amount: {amount}\n";
            summary += $"Card Type: {cardType ?? "Any"}\n";
            summary += $"Playing Types: {(playingTypes?.Count > 0 ? string.Join(", ", playingTypes) : "Any")}\n";
            summary += $"Uses: {uses}\n";
            summary += $"Active: {isActive}\n";
            summary += $"Expired: {IsExpired()}\n";

            if (limit != null)
            {
                summary += $"Limit: {limit.maxUses} per {limit.limitType}\n";
                summary += $"Limit Reached: {limit.IsAtMax(source.controller)}\n";
            }

            return summary;
        }

        /// <summary>
        /// Create a simple cost reducer
        /// </summary>
        public static CostReducer CreateSimple(Game game, EffectSource source, int amount, string cardType = null)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = cardType,
                description = $"Reduce {cardType ?? "card"} cost by {amount}"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a limited cost reducer
        /// </summary>
        public static CostReducer CreateLimited(Game game, EffectSource source, int amount, int maxUses, string limitType = "perRound", string cardType = null)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = cardType,
                limit = new AbilityLimit { maxUses = maxUses, limitType = limitType },
                description = $"Reduce {cardType ?? "card"} cost by {amount} ({maxUses} per {limitType})"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a conditional cost reducer
        /// </summary>
        public static CostReducer CreateConditional(Game game, EffectSource source, int amount, 
            Func<BaseCard, EffectSource, bool> condition, string description = null)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                match = condition,
                description = description ?? "Conditional cost reduction"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a trait-based cost reducer
        /// </summary>
        public static CostReducer CreateTraitBased(Game game, EffectSource source, int amount, string trait, string cardType = null)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = cardType,
                match = (card, src) => card.HasTrait(trait),
                description = $"Reduce {trait} {cardType ?? "card"} cost by {amount}"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a faction-based cost reducer
        /// </summary>
        public static CostReducer CreateFactionBased(Game game, EffectSource source, int amount, string faction, string cardType = null)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = cardType,
                match = (card, src) => card.IsFaction(faction),
                description = $"Reduce {faction} {cardType ?? "card"} cost by {amount}"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a playing type specific cost reducer
        /// </summary>
        public static CostReducer CreatePlayingTypeBased(Game game, EffectSource source, int amount, 
            List<string> playingTypes, string description = null)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                playingTypes = playingTypes,
                description = description ?? $"Reduce cost by {amount} when playing from {string.Join(" or ", playingTypes)}"
            };

            return new CostReducer(game, source, properties);
        }
    }

    /// <summary>
    /// Manages multiple cost reducers for a player
    /// </summary>
    public class CostReducerManager
    {
        [Header("Cost Reducer Management")]
        public Player player;
        public List<CostReducer> costReducers = new List<CostReducer>();
        public bool debugMode = false;

        public CostReducerManager(Player player)
        {
            this.player = player;
        }

        /// <summary>
        /// Add a cost reducer
        /// </summary>
        public void AddCostReducer(CostReducer reducer)
        {
            if (reducer != null)
            {
                costReducers.Add(reducer);

                if (debugMode)
                {
                    Debug.Log($"💰 Added cost reducer for {player.name}: {reducer.description}");
                }
            }
        }

        /// <summary>
        /// Remove a cost reducer
        /// </summary>
        public void RemoveCostReducer(CostReducer reducer)
        {
            if (costReducers.Remove(reducer))
            {
                reducer.Deactivate();

                if (debugMode)
                {
                    Debug.Log($"💰 Removed cost reducer for {player.name}: {reducer.description}");
                }
            }
        }

        /// <summary>
        /// Get total cost reduction for a card
        /// </summary>
        public int GetTotalReduction(string playingType, BaseCard card, object target = null)
        {
            int totalReduction = 0;
            var applicableReducers = new List<CostReducer>();

            foreach (var reducer in costReducers.Where(r => r.CanReduce(playingType, card, target)))
            {
                int reduction = reducer.GetAmount(card, player);
                totalReduction += reduction;
                applicableReducers.Add(reducer);

                if (debugMode)
                {
                    Debug.Log($"💰 Applying cost reduction: {reducer.description} (-{reduction})");
                }
            }

            return totalReduction;
        }

        /// <summary>
        /// Apply cost reductions and mark them as used
        /// </summary>
        public int ApplyReductions(string playingType, BaseCard card, object target = null)
        {
            int totalReduction = 0;
            var applicableReducers = costReducers.Where(r => r.CanReduce(playingType, card, target)).ToList();

            foreach (var reducer in applicableReducers)
            {
                int reduction = reducer.GetAmount(card, player);
                totalReduction += reduction;
                reducer.MarkUsed();

                if (debugMode)
                {
                    Debug.Log($"💰 Used cost reduction: {reducer.description} (-{reduction})");
                }
            }

            // Clean up expired reducers
            CleanupExpiredReducers();

            return totalReduction;
        }

        /// <summary>
        /// Remove expired cost reducers
        /// </summary>
        public void CleanupExpiredReducers()
        {
            var expiredReducers = costReducers.Where(r => r.IsExpired()).ToList();

            foreach (var reducer in expiredReducers)
            {
                RemoveCostReducer(reducer);
            }
        }

        /// <summary>
        /// Clear all cost reducers
        /// </summary>
        public void ClearAllReducers()
        {
            var reducersToRemove = costReducers.ToList();
            
            foreach (var reducer in reducersToRemove)
            {
                RemoveCostReducer(reducer);
            }

            costReducers.Clear();

            if (debugMode)
            {
                Debug.Log($"💰 Cleared all cost reducers for {player.name}");
            }
        }

        /// <summary>
        /// Get all active cost reducers
        /// </summary>
        public List<CostReducer> GetActiveReducers()
        {
            return costReducers.Where(r => r.isActive && !r.IsExpired()).ToList();
        }

        /// <summary>
        /// Get reducers that apply to a specific card
        /// </summary>
        public List<CostReducer> GetReducersForCard(BaseCard card, string playingType = null)
        {
            return costReducers.Where(r => 
                r.isActive && 
                !r.IsExpired() && 
                (playingType == null || r.CanReduce(playingType, card))
            ).ToList();
        }

        /// <summary>
        /// Get debug information about all cost reducers
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"Cost Reducers for {player.name}:\n";
            info += $"Total Reducers: {costReducers.Count}\n";
            info += $"Active Reducers: {GetActiveReducers().Count}\n\n";

            foreach (var reducer in costReducers)
            {
                info += reducer.GetSummary() + "\n";
            }

            return info;
        }
    }

    /// <summary>
    /// Extension methods for easy cost reducer integration
    /// </summary>
    public static class CostReducerExtensions
    {
        /// <summary>
        /// Add a cost reducer to a player
        /// </summary>
        public static void AddCostReducer(this Player player, CostReducer reducer)
        {
            if (player.costReducerManager == null)
            {
                player.costReducerManager = new CostReducerManager(player);
            }
            
            player.costReducerManager.AddCostReducer(reducer);
        }

        /// <summary>
        /// Get total cost reduction for a card
        /// </summary>
        public static int GetCostReduction(this Player player, string playingType, BaseCard card, object target = null)
        {
            if (player.costReducerManager == null)
            {
                return 0;
            }

            return player.costReducerManager.GetTotalReduction(playingType, card, target);
        }

        /// <summary>
        /// Apply cost reductions for a card
        /// </summary>
        public static int ApplyCostReduction(this Player player, string playingType, BaseCard card, object target = null)
        {
            if (player.costReducerManager == null)
            {
                return 0;
            }

            return player.costReducerManager.ApplyReductions(playingType, card, target);
        }

        /// <summary>
        /// Create and add a simple cost reducer
        /// </summary>
        public static CostReducer CreateSimpleCostReducer(this EffectSource source, int amount, string cardType = null)
        {
            var reducer = CostReducer.CreateSimple(source.game, source, amount, cardType);
            source.controller.AddCostReducer(reducer);
            return reducer;
        }

        /// <summary>
        /// Create and add a trait-based cost reducer
        /// </summary>
        public static CostReducer CreateTraitCostReducer(this EffectSource source, int amount, string trait, string cardType = null)
        {
            var reducer = CostReducer.CreateTraitBased(source.game, source, amount, trait, cardType);
            source.controller.AddCostReducer(reducer);
            return reducer;
        }

        /// <summary>
        /// Check if a card has any applicable cost reducers
        /// </summary>
        public static bool HasCostReducers(this BaseCard card, string playingType = null)
        {
            if (card.controller?.costReducerManager == null)
            {
                return false;
            }

            return card.controller.costReducerManager.GetReducersForCard(card, playingType).Any();
        }

        /// <summary>
        /// Get the final cost after all reductions
        /// </summary>
        public static int GetReducedCost(this BaseCard card, string playingType, object target = null)
        {
            int baseCost = card.GetFateCost();
            int reduction = card.controller.GetCostReduction(playingType, card, target);
            return Math.Max(0, baseCost - reduction);
        }
    }

    /// <summary>
    /// Common cost reducer patterns for L5R
    /// </summary>
    public static class L5RCostReducers
    {
        /// <summary>
        /// Create a clan loyalty reducer (reduce cost for own clan cards)
        /// </summary>
        public static CostReducer ClanLoyalty(Game game, EffectSource source, string clan, int amount = 1)
        {
            return CostReducer.CreateFactionBased(game, source, amount, clan, CardTypes.Character);
        }

        /// <summary>
        /// Create a province holding reducer
        /// </summary>
        public static CostReducer ProvinceHolding(Game game, EffectSource source, int amount = 1)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = CardTypes.Holding,
                playingTypes = new List<string> { PlayTypes.PlayFromProvince },
                description = $"Reduce holding cost by {amount} when playing from provinces"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a unique character reducer
        /// </summary>
        public static CostReducer UniqueCharacters(Game game, EffectSource source, int amount = 1)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = CardTypes.Character,
                match = (card, src) => card.IsUnique(),
                description = $"Reduce unique character cost by {amount}"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a low-cost character reducer
        /// </summary>
        public static CostReducer LowCostCharacters(Game game, EffectSource source, int maxCost = 2, int amount = 1)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = CardTypes.Character,
                match = (card, src) => card.GetFateCost() <= maxCost,
                description = $"Reduce cost by {amount} for characters with cost {maxCost} or less"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a honor status reducer
        /// </summary>
        public static CostReducer HonoredCharacters(Game game, EffectSource source, int amount = 1)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                cardType = CardTypes.Character,
                match = (card, src) => card.isHonored,
                description = $"Reduce honored character cost by {amount}"
            };

            return new CostReducer(game, source, properties);
        }

        /// <summary>
        /// Create a conflict type reducer
        /// </summary>
        public static CostReducer ConflictCards(Game game, EffectSource source, int amount = 1)
        {
            var properties = new CostReducerProperties
            {
                amount = amount,
                playingTypes = new List<string> { PlayTypes.PlayFromHand },
                match = (card, src) => card.isConflict,
                description = $"Reduce conflict card cost by {amount}"
            };

            return new CostReducer(game, source, properties);
        }
    }
}