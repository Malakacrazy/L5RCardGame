using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a fully prepared deck with instantiated cards ready for gameplay.
    /// Contains all the cards organized by type and location for easy game management.
    /// </summary>
    [System.Serializable]
    public class PreparedDeck
    {
        [Header("Deck Identity")]
        public string deckId;
        public string deckName;
        public Faction faction;
        public Player owner;

        [Header("Core Cards")]
        public StrongholdCard stronghold;
        public RoleCard role;

        [Header("Card Collections")]
        public List<BaseCard> conflictCards = new List<BaseCard>();
        public List<BaseCard> dynastyCards = new List<BaseCard>();
        public List<BaseCard> provinceCards = new List<BaseCard>();

        [Header("All Cards Reference")]
        public List<BaseCard> allCards = new List<BaseCard>();

        [Header("Deck Statistics")]
        public int totalCardCount;
        public int conflictCardCount;
        public int dynastyCardCount;
        public int provinceCardCount;

        [Header("Validation")]
        public bool isValid = true;
        public List<string> validationErrors = new List<string>();

        [Header("Preparation Info")]
        public DateTime preparedAt;
        public string preparedVersion;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PreparedDeck()
        {
            conflictCards = new List<BaseCard>();
            dynastyCards = new List<BaseCard>();
            provinceCards = new List<BaseCard>();
            allCards = new List<BaseCard>();
            validationErrors = new List<string>();
            preparedAt = DateTime.Now;
        }

        /// <summary>
        /// Constructor with basic information
        /// </summary>
        /// <param name="deckData">Source deck data</param>
        /// <param name="deckOwner">Player who owns this deck</param>
        public PreparedDeck(DeckData deckData, Player deckOwner)
        {
            deckId = deckData.id;
            deckName = deckData.name;
            faction = deckData.faction;
            owner = deckOwner;
            
            conflictCards = new List<BaseCard>();
            dynastyCards = new List<BaseCard>();
            provinceCards = new List<BaseCard>();
            allCards = new List<BaseCard>();
            validationErrors = new List<string>();
            preparedAt = DateTime.Now;
            preparedVersion = Application.version;
            
            Debug.Log($"🎴 PreparedDeck created for {deckOwner.name}: {deckName}");
        }

        /// <summary>
        /// Initialize the prepared deck with all card collections
        /// </summary>
        /// <param name="conflicts">Conflict cards</param>
        /// <param name="dynasties">Dynasty cards</param>
        /// <param name="provinces">Province cards</param>
        /// <param name="strongholdCard">Stronghold card</param>
        /// <param name="roleCard">Role card (optional)</param>
        public void Initialize(List<BaseCard> conflicts, List<BaseCard> dynasties, 
                              List<BaseCard> provinces, StrongholdCard strongholdCard, 
                              RoleCard roleCard = null)
        {
            conflictCards = conflicts ?? new List<BaseCard>();
            dynastyCards = dynasties ?? new List<BaseCard>();
            provinceCards = provinces ?? new List<BaseCard>();
            stronghold = strongholdCard;
            role = roleCard;

            // Update statistics
            conflictCardCount = conflictCards.Count;
            dynastyCardCount = dynastyCards.Count;
            provinceCardCount = provinceCards.Count;
            totalCardCount = conflictCardCount + dynastyCardCount + provinceCardCount;

            // Add stronghold and role to total if they exist
            if (stronghold != null) totalCardCount++;
            if (role != null) totalCardCount++;

            // Build the all cards collection
            BuildAllCardsCollection();

            // Validate the prepared deck
            ValidatePreparedDeck();

            Debug.Log($"🎴 PreparedDeck initialized: {conflictCardCount} conflict, " +
                     $"{dynastyCardCount} dynasty, {provinceCardCount} provinces");
        }

        /// <summary>
        /// Build the all cards collection from individual collections
        /// </summary>
        private void BuildAllCardsCollection()
        {
            allCards.Clear();

            // Add all card types
            allCards.AddRange(conflictCards);
            allCards.AddRange(dynastyCards);
            allCards.AddRange(provinceCards);

            if (stronghold != null)
            {
                allCards.Add(stronghold);
            }

            if (role != null)
            {
                allCards.Add(role);
            }

            // Ensure all cards have proper UUIDs
            foreach (var card in allCards)
            {
                if (string.IsNullOrEmpty(card.uuid))
                {
                    card.uuid = Guid.NewGuid().ToString();
                }
            }
        }

        /// <summary>
        /// Validate the prepared deck for game rules compliance
        /// </summary>
        private void ValidatePreparedDeck()
        {
            validationErrors.Clear();
            isValid = true;

            // Check stronghold requirement
            if (stronghold == null)
            {
                AddValidationError("Deck must have exactly one stronghold");
            }

            // Check province count
            if (provinceCardCount < 4)
            {
                AddValidationError($"Deck must have at least 4 provinces (has {provinceCardCount})");
            }
            else if (provinceCardCount > 5)
            {
                AddValidationError($"Deck cannot have more than 5 provinces (has {provinceCardCount})");
            }

            // Check conflict deck size
            if (conflictCardCount < 40)
            {
                AddValidationError($"Conflict deck must have at least 40 cards (has {conflictCardCount})");
            }
            else if (conflictCardCount > 45)
            {
                AddValidationError($"Conflict deck cannot have more than 45 cards (has {conflictCardCount})");
            }

            // Check dynasty deck size
            if (dynastyCardCount < 40)
            {
                AddValidationError($"Dynasty deck must have at least 40 cards (has {dynastyCardCount})");
            }
            else if (dynastyCardCount > 45)
            {
                AddValidationError($"Dynasty deck cannot have more than 45 cards (has {dynastyCardCount})");
            }

            // Check faction consistency
            ValidateFactionConsistency();

            // Check for duplicate cards
            ValidateCardLimits();

            // Check influence usage
            ValidateInfluenceUsage();

            Debug.Log($"🎴 PreparedDeck validation: {(isValid ? "VALID" : "INVALID")} " +
                     $"({validationErrors.Count} errors)");
        }

        /// <summary>
        /// Validate faction consistency across all cards
        /// </summary>
        private void ValidateFactionConsistency()
        {
            if (faction == null) return;

            foreach (var card in allCards)
            {
                if (!faction.CanUseCard(card))
                {
                    int influenceCost = faction.GetInfluenceCost(card);
                    if (influenceCost == 0)
                    {
                        AddValidationError($"Card {card.name} cannot be used by faction {faction.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Validate card count limits (max 3 of any card, max 1 of limited cards)
        /// </summary>
        private void ValidateCardLimits()
        {
            var cardCounts = new Dictionary<string, int>();

            // Count all cards by ID
            foreach (var card in allCards)
            {
                string cardId = card.id;
                cardCounts[cardId] = cardCounts.GetValueOrDefault(cardId, 0) + 1;
            }

            // Check limits
            foreach (var kvp in cardCounts)
            {
                string cardId = kvp.Key;
                int count = kvp.Value;
                var card = allCards.FirstOrDefault(c => c.id == cardId);

                if (card != null)
                {
                    // Check general limit (3 copies max)
                    if (count > 3)
                    {
                        AddValidationError($"Cannot have more than 3 copies of {card.name} (has {count})");
                    }

                    // Check limited cards (1 copy max)
                    if (card.HasTrait("limited") && count > 1)
                    {
                        AddValidationError($"Cannot have more than 1 copy of limited card {card.name} (has {count})");
                    }

                    // Check unique cards (1 copy max)
                    if (card.IsUnique() && count > 1)
                    {
                        AddValidationError($"Cannot have more than 1 copy of unique card {card.name} (has {count})");
                    }
                }
            }
        }

        /// <summary>
        /// Validate influence usage for out-of-faction cards
        /// </summary>
        private void ValidateInfluenceUsage()
        {
            if (faction == null) return;

            int usedInfluence = 0;
            var influenceCards = new List<BaseCard>();

            foreach (var card in allCards)
            {
                if (!faction.CanUseCard(card))
                {
                    int influenceCost = faction.GetInfluenceCost(card);
                    if (influenceCost > 0)
                    {
                        usedInfluence += influenceCost;
                        influenceCards.Add(card);
                    }
                }
            }

            if (usedInfluence > faction.influencePool)
            {
                AddValidationError($"Deck uses {usedInfluence} influence but faction {faction.name} " +
                                 $"only has {faction.influencePool} available");
                
                foreach (var card in influenceCards)
                {
                    AddValidationError($"  - {card.name} costs {faction.GetInfluenceCost(card)} influence");
                }
            }
        }

        /// <summary>
        /// Add a validation error
        /// </summary>
        /// <param name="error">Error message</param>
        private void AddValidationError(string error)
        {
            validationErrors.Add(error);
            isValid = false;
        }

        /// <summary>
        /// Shuffle all deck sections
        /// </summary>
        public void ShuffleAllDecks()
        {
            ShuffleConflictDeck();
            ShuffleDynastyDeck();
            ShuffleProvinces();
        }

        /// <summary>
        /// Shuffle the conflict deck
        /// </summary>
        public void ShuffleConflictDeck()
        {
            conflictCards = conflictCards.OrderBy(x => UnityEngine.Random.value).ToList();
            Debug.Log($"🔀 Conflict deck shuffled ({conflictCards.Count} cards)");
        }

        /// <summary>
        /// Shuffle the dynasty deck
        /// </summary>
        public void ShuffleDynastyDeck()
        {
            dynastyCards = dynastyCards.OrderBy(x => UnityEngine.Random.value).ToList();
            Debug.Log($"🔀 Dynasty deck shuffled ({dynastyCards.Count} cards)");
        }

        /// <summary>
        /// Shuffle the provinces
        /// </summary>
        public void ShuffleProvinces()
        {
            provinceCards = provinceCards.OrderBy(x => UnityEngine.Random.value).ToList();
            Debug.Log($"🔀 Provinces shuffled ({provinceCards.Count} cards)");
        }

        /// <summary>
        /// Get a specific card by UUID
        /// </summary>
        /// <param name="uuid">Card UUID</param>
        /// <returns>Card or null if not found</returns>
        public BaseCard GetCardByUuid(string uuid)
        {
            return allCards.FirstOrDefault(card => card.uuid == uuid);
        }

        /// <summary>
        /// Get all cards by name
        /// </summary>
        /// <param name="name">Card name</param>
        /// <returns>List of matching cards</returns>
        public List<BaseCard> GetCardsByName(string name)
        {
            return allCards.Where(card => card.name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Get all cards by type
        /// </summary>
        /// <param name="cardType">Card type</param>
        /// <returns>List of matching cards</returns>
        public List<BaseCard> GetCardsByType(string cardType)
        {
            return allCards.Where(card => card.type == cardType).ToList();
        }

        /// <summary>
        /// Get all cards with a specific trait
        /// </summary>
        /// <param name="trait">Trait to search for</param>
        /// <returns>List of matching cards</returns>
        public List<BaseCard> GetCardsByTrait(string trait)
        {
            return allCards.Where(card => card.HasTrait(trait)).ToList();
        }

        /// <summary>
        /// Get deck statistics
        /// </summary>
        /// <returns>Deck statistics object</returns>
        public PreparedDeckStatistics GetStatistics()
        {
            var stats = new PreparedDeckStatistics
            {
                deckName = deckName,
                factionName = faction?.name ?? "Unknown",
                totalCards = totalCardCount,
                conflictCards = conflictCardCount,
                dynastyCards = dynastyCardCount,
                provinceCards = provinceCardCount,
                hasStronghold = stronghold != null,
                hasRole = role != null,
                isValid = isValid,
                errorCount = validationErrors.Count,
                preparedAt = preparedAt
            };

            // Calculate card type distributions
            stats.characterCards = GetCardsByType(CardTypes.Character).Count;
            stats.attachmentCards = GetCardsByType(CardTypes.Attachment).Count;
            stats.eventCards = GetCardsByType(CardTypes.Event).Count;
            stats.holdingCards = GetCardsByType(CardTypes.Holding).Count;

            // Calculate faction distribution
            var factionCounts = new Dictionary<string, int>();
            foreach (var card in allCards)
            {
                string cardFaction = card.GetPrintedFaction();
                factionCounts[cardFaction] = factionCounts.GetValueOrDefault(cardFaction, 0) + 1;
            }
            stats.factionDistribution = factionCounts;

            return stats;
        }

        /// <summary>
        /// Get deck summary for UI display
        /// </summary>
        /// <returns>Deck summary</returns>
        public PreparedDeckSummary GetSummary()
        {
            return new PreparedDeckSummary
            {
                deckId = deckId,
                deckName = deckName,
                faction = faction?.GetSummary(),
                ownerName = owner?.name ?? "Unknown",
                totalCards = totalCardCount,
                isValid = isValid,
                validationErrors = validationErrors,
                strongholdName = stronghold?.name ?? "None",
                roleName = role?.name ?? "None",
                preparedAt = preparedAt,
                statistics = GetStatistics()
            };
        }

        /// <summary>
        /// Create a copy of this prepared deck (for testing or scenarios)
        /// </summary>
        /// <param name="newOwner">New owner for the copy</param>
        /// <returns>Copied prepared deck</returns>
        public PreparedDeck CreateCopy(Player newOwner)
        {
            var copy = new PreparedDeck
            {
                deckId = deckId + "_copy",
                deckName = deckName + " (Copy)",
                faction = faction,
                owner = newOwner,
                preparedAt = DateTime.Now,
                preparedVersion = preparedVersion
            };

            // Copy all cards (this would need proper card cloning in a real implementation)
            copy.conflictCards = conflictCards.ToList();
            copy.dynastyCards = dynastyCards.ToList();
            copy.provinceCards = provinceCards.ToList();
            copy.stronghold = stronghold;
            copy.role = role;

            copy.BuildAllCardsCollection();
            copy.ValidatePreparedDeck();

            return copy;
        }

        /// <summary>
        /// Reset all cards to their initial state
        /// </summary>
        public void ResetAllCards()
        {
            foreach (var card in allCards)
            {
                card.tokens.Clear();
                card.selected = false;
                card.facedown = false;
                
                // Reset card to its proper location
                if (conflictCards.Contains(card))
                    card.location = Locations.ConflictDeck;
                else if (dynastyCards.Contains(card))
                    card.location = Locations.DynastyDeck;
                else if (provinceCards.Contains(card))
                    card.location = Locations.ProvinceDeck;
                else if (card == stronghold)
                    card.location = Locations.StrongholdProvince;
                else if (card == role)
                    card.location = Locations.Role;
            }
        }

        /// <summary>
        /// Apply faction effects to the owner player
        /// </summary>
        /// <param name="game">Game instance</param>
        public void ApplyFactionEffects(Game game)
        {
            if (faction != null && owner != null)
            {
                faction.ApplyFactionEffects(owner, game);
            }
        }

        /// <summary>
        /// Execute setup for all cards (triggers enter play effects, etc.)
        /// </summary>
        /// <param name="game">Game instance</param>
        public void SetupCards(Game game)
        {
            // Set up stronghold
            if (stronghold != null)
            {
                stronghold.owner = owner;
                stronghold.controller = owner;
                stronghold.game = game;
                stronghold.location = Locations.StrongholdProvince;
                stronghold.facedown = false;
            }

            // Set up role
            if (role != null)
            {
                role.owner = owner;
                role.controller = owner;
                role.game = game;
                role.location = Locations.Role;
                role.facedown = false;
            }

            // Set up all other cards
            foreach (var card in allCards)
            {
                if (card != stronghold && card != role)
                {
                    card.owner = owner;
                    card.controller = owner;
                    card.game = game;
                    card.facedown = true; // Most cards start face down
                }
            }

            Debug.Log($"🎴 Cards setup complete for {owner.name}");
        }

        /// <summary>
        /// Check if deck is ready for play
        /// </summary>
        /// <returns>True if deck is ready</returns>
        public bool IsReadyForPlay()
        {
            return isValid && 
                   stronghold != null && 
                   conflictCards.Count >= 40 && 
                   dynastyCards.Count >= 40 && 
                   provinceCards.Count >= 4 &&
                   owner != null;
        }

        /// <summary>
        /// Get validation report as formatted string
        /// </summary>
        /// <returns>Formatted validation report</returns>
        public string GetValidationReport()
        {
            if (isValid)
            {
                return $"✅ Deck '{deckName}' is valid and ready for play.";
            }

            var report = $"❌ Deck '{deckName}' has {validationErrors.Count} validation error(s):\n";
            for (int i = 0; i < validationErrors.Count; i++)
            {
                report += $"{i + 1}. {validationErrors[i]}\n";
            }

            return report;
        }

        /// <summary>
        /// Cleanup when deck is destroyed
        /// </summary>
        public void Cleanup()
        {
            // Clear all collections
            conflictCards?.Clear();
            dynastyCards?.Clear();
            provinceCards?.Clear();
            allCards?.Clear();
            validationErrors?.Clear();

            // Clear references
            stronghold = null;
            role = null;
            faction = null;
            owner = null;

            Debug.Log($"🎴 PreparedDeck {deckName} cleaned up");
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>Deck description</returns>
        public override string ToString()
        {
            return $"{deckName} ({faction?.name ?? "Unknown"}) - " +
                   $"{totalCardCount} cards ({(isValid ? "Valid" : "Invalid")})";
        }
    }

    /// <summary>
    /// Statistics for a prepared deck
    /// </summary>
    [System.Serializable]
    public class PreparedDeckStatistics
    {
        public string deckName;
        public string factionName;
        public int totalCards;
        public int conflictCards;
        public int dynastyCards;
        public int provinceCards;
        public bool hasStronghold;
        public bool hasRole;
        public bool isValid;
        public int errorCount;
        public DateTime preparedAt;

        // Card type breakdown
        public int characterCards;
        public int attachmentCards;
        public int eventCards;
        public int holdingCards;

        // Faction distribution
        public Dictionary<string, int> factionDistribution = new Dictionary<string, int>();
    }

    /// <summary>
    /// Summary for a prepared deck (for UI display)
    /// </summary>
    [System.Serializable]
    public class PreparedDeckSummary
    {
        public string deckId;
        public string deckName;
        public FactionSummary faction;
        public string ownerName;
        public int totalCards;
        public bool isValid;
        public List<string> validationErrors;
        public string strongholdName;
        public string roleName;
        public DateTime preparedAt;
        public PreparedDeckStatistics statistics;
    }

    /// <summary>
    /// Extension methods for prepared deck functionality
    /// </summary>
    public static class PreparedDeckExtensions
    {
        /// <summary>
        /// Check if deck contains a specific card
        /// </summary>
        /// <param name="deck">Prepared deck</param>
        /// <param name="cardId">Card ID to search for</param>
        /// <returns>True if deck contains the card</returns>
        public static bool ContainsCard(this PreparedDeck deck, string cardId)
        {
            return deck.allCards.Any(card => card.id == cardId);
        }

        /// <summary>
        /// Count copies of a specific card in the deck
        /// </summary>
        /// <param name="deck">Prepared deck</param>
        /// <param name="cardId">Card ID to count</param>
        /// <returns>Number of copies</returns>
        public static int CountCard(this PreparedDeck deck, string cardId)
        {
            return deck.allCards.Count(card => card.id == cardId);
        }

        /// <summary>
        /// Get all unique card names in the deck
        /// </summary>
        /// <param name="deck">Prepared deck</param>
        /// <returns>List of unique card names</returns>
        public static List<string> GetUniqueCardNames(this PreparedDeck deck)
        {
            return deck.allCards.Select(card => card.name).Distinct().ToList();
        }

        /// <summary>
        /// Check if deck is tournament legal
        /// </summary>
        /// <param name="deck">Prepared deck</param>
        /// <returns>True if tournament legal</returns>
        public static bool IsTournamentLegal(this PreparedDeck deck)
        {
            return deck.isValid && 
                   deck.conflictCards.Count >= 40 && deck.conflictCards.Count <= 45 &&
                   deck.dynastyCards.Count >= 40 && deck.dynastyCards.Count <= 45 &&
                   deck.provinceCards.Count >= 4 && deck.provinceCards.Count <= 5;
        }

        /// <summary>
        /// Export deck to a format suitable for saving/sharing
        /// </summary>
        /// <param name="deck">Prepared deck</param>
        /// <returns>Exportable deck data</returns>
        public static DeckData ExportToDeckData(this PreparedDeck deck)
        {
            var deckData = new DeckData
            {
                id = deck.deckId,
                name = deck.deckName,
                faction = deck.faction,
                selected = false
            };

            // Group cards by ID and create deck entries
            var conflictGroups = deck.conflictCards.GroupBy(c => c.id);
            foreach (var group in conflictGroups)
            {
                var card = group.First();
                deckData.conflictCards.Add(new DeckCardEntry(card.cardData, group.Count()));
            }

            var dynastyGroups = deck.dynastyCards.GroupBy(c => c.id);
            foreach (var group in dynastyGroups)
            {
                var card = group.First();
                deckData.dynastyCards.Add(new DeckCardEntry(card.cardData, group.Count()));
            }

            var provinceGroups = deck.provinceCards.GroupBy(c => c.id);
            foreach (var group in provinceGroups)
            {
                var card = group.First();
                deckData.provinceCards.Add(new DeckCardEntry(card.cardData, group.Count()));
            }

            if (deck.stronghold != null)
            {
                deckData.stronghold.Add(new DeckCardEntry(deck.stronghold.cardData, 1));
            }

            if (deck.role != null)
            {
                deckData.role.Add(new DeckCardEntry(deck.role.cardData, 1));
            }

            return deckData;
        }
    }
}
