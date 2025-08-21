using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for configuring additional piles
    /// </summary>
    [System.Serializable]
    public class AdditionalPileProperties
    {
        [Header("Basic Properties")]
        public string name;
        public string displayName;
        public string description;

        [Header("Visibility")]
        public bool isPrivate = true;
        public bool showToOwner = true;
        public bool showToOpponent = false;
        public bool showToSpectators = false;

        [Header("Behavior")]
        public bool ordered = false;
        public bool shuffleWhenAdded = false;
        public bool faceDown = true;
        public int maxSize = -1; // -1 means unlimited

        [Header("Access Control")]
        public List<string> allowedPlayers = new List<string>();
        public bool ownerCanView = true;
        public bool ownerCanModify = true;
        public bool opponentCanView = false;
        public bool opponentCanModify = false;

        [Header("Auto Management")]
        public bool autoRemoveEmpty = false;
        public bool persistBetweenGames = false;
        public string autoSortBy = ""; // "name", "cost", "type", etc.

        /// <summary>
        /// Default constructor
        /// </summary>
        public AdditionalPileProperties()
        {
            name = "";
            displayName = "";
            description = "";
            allowedPlayers = new List<string>();
        }

        /// <summary>
        /// Constructor with name
        /// </summary>
        /// <param name="pileName">Name of the pile</param>
        public AdditionalPileProperties(string pileName)
        {
            name = pileName;
            displayName = pileName;
            description = $"Additional pile: {pileName}";
            allowedPlayers = new List<string>();
        }

        /// <summary>
        /// Create properties for a private pile
        /// </summary>
        /// <param name="pileName">Name of the pile</param>
        /// <returns>Private pile properties</returns>
        public static AdditionalPileProperties CreatePrivate(string pileName)
        {
            return new AdditionalPileProperties(pileName)
            {
                isPrivate = true,
                showToOwner = true,
                showToOpponent = false,
                ownerCanView = true,
                ownerCanModify = true,
                opponentCanView = false,
                opponentCanModify = false
            };
        }

        /// <summary>
        /// Create properties for a public pile
        /// </summary>
        /// <param name="pileName">Name of the pile</param>
        /// <returns>Public pile properties</returns>
        public static AdditionalPileProperties CreatePublic(string pileName)
        {
            return new AdditionalPileProperties(pileName)
            {
                isPrivate = false,
                showToOwner = true,
                showToOpponent = true,
                showToSpectators = true,
                ownerCanView = true,
                ownerCanModify = true,
                opponentCanView = true,
                opponentCanModify = false
            };
        }

        /// <summary>
        /// Create properties for a shared pile
        /// </summary>
        /// <param name="pileName">Name of the pile</param>
        /// <returns>Shared pile properties</returns>
        public static AdditionalPileProperties CreateShared(string pileName)
        {
            return new AdditionalPileProperties(pileName)
            {
                isPrivate = false,
                showToOwner = true,
                showToOpponent = true,
                ownerCanView = true,
                ownerCanModify = true,
                opponentCanView = true,
                opponentCanModify = true
            };
        }
    }

    /// <summary>
    /// Represents an additional pile of cards beyond the standard game zones.
    /// Used for special game effects, temporary storage, or custom game modes.
    /// </summary>
    [System.Serializable]
    public class AdditionalPile
    {
        [Header("Identity")]
        public string name;
        public string uuid;
        public Player owner;
        public Game game;

        [Header("Cards")]
        public List<BaseCard> cards = new List<BaseCard>();

        [Header("Configuration")]
        public AdditionalPileProperties properties;

        [Header("State")]
        public DateTime createdAt;
        public DateTime lastModified;
        public int totalCardsAdded = 0;
        public int totalCardsRemoved = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public AdditionalPile()
        {
            cards = new List<BaseCard>();
            uuid = Guid.NewGuid().ToString();
            createdAt = DateTime.Now;
            lastModified = DateTime.Now;
        }

        /// <summary>
        /// Constructor with properties
        /// </summary>
        /// <param name="pileProperties">Pile configuration</param>
        /// <param name="pileOwner">Owner of the pile</param>
        /// <param name="gameInstance">Game instance</param>
        public AdditionalPile(AdditionalPileProperties pileProperties, Player pileOwner, Game gameInstance)
        {
            properties = pileProperties ?? new AdditionalPileProperties();
            name = properties.name;
            owner = pileOwner;
            game = gameInstance;
            cards = new List<BaseCard>();
            uuid = Guid.NewGuid().ToString();
            createdAt = DateTime.Now;
            lastModified = DateTime.Now;

            Debug.Log($"📚 AdditionalPile '{name}' created for {pileOwner?.name ?? "unknown"}");
        }

        /// <summary>
        /// Number of cards in the pile
        /// </summary>
        public int Count => cards.Count;

        /// <summary>
        /// Check if the pile is empty
        /// </summary>
        public bool IsEmpty => cards.Count == 0;

        /// <summary>
        /// Check if the pile is at maximum capacity
        /// </summary>
        public bool IsFull => properties.maxSize > 0 && cards.Count >= properties.maxSize;

        /// <summary>
        /// Get the display name for the pile
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(properties.displayName) ? name : properties.displayName;

        /// <summary>
        /// Add a card to the pile
        /// </summary>
        /// <param name="card">Card to add</param>
        /// <param name="index">Position to add at (-1 for end)</param>
        /// <returns>True if card was added successfully</returns>
        public bool AddCard(BaseCard card, int index = -1)
        {
            if (card == null)
            {
                Debug.LogWarning($"📚 Attempted to add null card to pile '{name}'");
                return false;
            }

            if (IsFull)
            {
                Debug.LogWarning($"📚 Pile '{name}' is at maximum capacity ({properties.maxSize})");
                return false;
            }

            // Check if card is already in this pile
            if (cards.Contains(card))
            {
                Debug.LogWarning($"📚 Card {card.name} is already in pile '{name}'");
                return false;
            }

            // Add the card
            if (index < 0 || index >= cards.Count)
            {
                cards.Add(card);
            }
            else
            {
                cards.Insert(index, card);
            }

            // Update card location
            card.location = name;
            if (properties.faceDown)
            {
                card.facedown = true;
            }

            // Update statistics
            totalCardsAdded++;
            lastModified = DateTime.Now;

            // Apply pile-specific effects
            ApplyPileEffects(card, true);

            // Auto-shuffle if configured
            if (properties.shuffleWhenAdded)
            {
                Shuffle();
            }

            // Auto-sort if configured
            if (!string.IsNullOrEmpty(properties.autoSortBy))
            {
                Sort(properties.autoSortBy);
            }

            // Trigger events
            game?.EmitEvent(EventNames.OnCardMoved, new Dictionary<string, object>
            {
                {"card", card},
                {"originalLocation", card.location},
                {"newLocation", name},
                {"pile", this}
            });

            Debug.Log($"📚 Added {card.name} to pile '{name}' (now {cards.Count} cards)");
            return true;
        }

        /// <summary>
        /// Add multiple cards to the pile
        /// </summary>
        /// <param name="cardsToAdd">Cards to add</param>
        /// <returns>Number of cards successfully added</returns>
        public int AddCards(IEnumerable<BaseCard> cardsToAdd)
        {
            int addedCount = 0;
            foreach (var card in cardsToAdd)
            {
                if (AddCard(card))
                {
                    addedCount++;
                }
            }
            return addedCount;
        }

        /// <summary>
        /// Remove a card from the pile
        /// </summary>
        /// <param name="card">Card to remove</param>
        /// <returns>True if card was removed successfully</returns>
        public bool RemoveCard(BaseCard card)
        {
            if (card == null || !cards.Contains(card))
            {
                return false;
            }

            bool removed = cards.Remove(card);
            if (removed)
            {
                // Update statistics
                totalCardsRemoved++;
                lastModified = DateTime.Now;

                // Apply pile-specific effects
                ApplyPileEffects(card, false);

                // Auto-remove pile if empty and configured
                if (IsEmpty && properties.autoRemoveEmpty && owner != null)
                {
                    owner.RemoveAdditionalPile(name);
                }

                Debug.Log($"📚 Removed {card.name} from pile '{name}' (now {cards.Count} cards)");
            }

            return removed;
        }

        /// <summary>
        /// Remove a card at a specific index
        /// </summary>
        /// <param name="index">Index to remove from</param>
        /// <returns>Removed card or null</returns>
        public BaseCard RemoveCardAt(int index)
        {
            if (index < 0 || index >= cards.Count)
            {
                return null;
            }

            var card = cards[index];
            if (RemoveCard(card))
            {
                return card;
            }

            return null;
        }

        /// <summary>
        /// Remove multiple cards from the pile
        /// </summary>
        /// <param name="cardsToRemove">Cards to remove</param>
        /// <returns>Number of cards successfully removed</returns>
        public int RemoveCards(IEnumerable<BaseCard> cardsToRemove)
        {
            int removedCount = 0;
            foreach (var card in cardsToRemove.ToList()) // ToList to avoid modification during enumeration
            {
                if (RemoveCard(card))
                {
                    removedCount++;
                }
            }
            return removedCount;
        }

        /// <summary>
        /// Get a card at a specific index
        /// </summary>
        /// <param name="index">Index to get card from</param>
        /// <returns>Card at index or null</returns>
        public BaseCard GetCardAt(int index)
        {
            if (index < 0 || index >= cards.Count)
            {
                return null;
            }
            return cards[index];
        }

        /// <summary>
        /// Get the top card of the pile
        /// </summary>
        /// <returns>Top card or null if empty</returns>
        public BaseCard GetTopCard()
        {
            return IsEmpty ? null : cards.Last();
        }

        /// <summary>
        /// Get the bottom card of the pile
        /// </summary>
        /// <returns>Bottom card or null if empty</returns>
        public BaseCard GetBottomCard()
        {
            return IsEmpty ? null : cards.First();
        }

        /// <summary>
        /// Find cards matching a condition
        /// </summary>
        /// <param name="predicate">Condition to match</param>
        /// <returns>List of matching cards</returns>
        public List<BaseCard> FindCards(System.Func<BaseCard, bool> predicate)
        {
            return cards.Where(predicate).ToList();
        }

        /// <summary>
        /// Find the first card matching a condition
        /// </summary>
        /// <param name="predicate">Condition to match</param>
        /// <returns>First matching card or null</returns>
        public BaseCard FindCard(System.Func<BaseCard, bool> predicate)
        {
            return cards.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Check if the pile contains a specific card
        /// </summary>
        /// <param name="card">Card to check for</param>
        /// <returns>True if pile contains the card</returns>
        public bool Contains(BaseCard card)
        {
            return cards.Contains(card);
        }

        /// <summary>
        /// Check if the pile contains a card with a specific UUID
        /// </summary>
        /// <param name="uuid">UUID to search for</param>
        /// <returns>True if pile contains card with that UUID</returns>
        public bool ContainsUuid(string uuid)
        {
            return cards.Any(card => card.uuid == uuid);
        }

        /// <summary>
        /// Get the index of a card in the pile
        /// </summary>
        /// <param name="card">Card to find index of</param>
        /// <returns>Index of card or -1 if not found</returns>
        public int IndexOf(BaseCard card)
        {
            return cards.IndexOf(card);
        }

        /// <summary>
        /// Shuffle the cards in the pile
        /// </summary>
        public void Shuffle()
        {
            if (cards.Count <= 1) return;

            cards = cards.OrderBy(x => UnityEngine.Random.value).ToList();
            lastModified = DateTime.Now;

            game?.EmitEvent("onPileShuffled", new Dictionary<string, object>
            {
                {"pile", this},
                {"player", owner}
            });

            Debug.Log($"🔀 Shuffled pile '{name}' ({cards.Count} cards)");
        }

        /// <summary>
        /// Sort the cards in the pile
        /// </summary>
        /// <param name="sortBy">Property to sort by</param>
        /// <param name="ascending">Sort order</param>
        public void Sort(string sortBy, bool ascending = true)
        {
            if (cards.Count <= 1) return;

            switch (sortBy.ToLower())
            {
                case "name":
                    cards = ascending ? cards.OrderBy(c => c.name).ToList() : cards.OrderByDescending(c => c.name).ToList();
                    break;
                case "cost":
                    cards = ascending ? cards.OrderBy(c => c.GetCost()).ToList() : cards.OrderByDescending(c => c.GetCost()).ToList();
                    break;
                case "type":
                    cards = ascending ? cards.OrderBy(c => c.type).ToList() : cards.OrderByDescending(c => c.type).ToList();
                    break;
                case "faction":
                    cards = ascending ? cards.OrderBy(c => c.GetPrintedFaction()).ToList() : cards.OrderByDescending(c => c.GetPrintedFaction()).ToList();
                    break;
                default:
                    Debug.LogWarning($"📚 Unknown sort property: {sortBy}");
                    return;
            }

            lastModified = DateTime.Now;
            Debug.Log($"📚 Sorted pile '{name}' by {sortBy} ({(ascending ? "ascending" : "descending")})");
        }

        /// <summary>
        /// Clear all cards from the pile
        /// </summary>
        public void Clear()
        {
            var clearedCards = cards.ToList();
            cards.Clear();
            lastModified = DateTime.Now;

            foreach (var card in clearedCards)
            {
                ApplyPileEffects(card, false);
            }

            Debug.Log($"📚 Cleared pile '{name}' ({clearedCards.Count} cards removed)");
        }

        /// <summary>
        /// Check if a player can view this pile
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if player can view the pile</returns>
        public bool CanPlayerView(Player player)
        {
            if (player == null) return false;

            // Owner permissions
            if (player == owner)
            {
                return properties.ownerCanView;
            }

            // Opponent permissions
            if (player == owner?.opponent)
            {
                return properties.opponentCanView;
            }

            // Explicit allowed players
            if (properties.allowedPlayers.Contains(player.id))
            {
                return true;
            }

            // Public piles
            if (!properties.isPrivate)
            {
                return properties.showToSpectators;
            }

            return false;
        }

        /// <summary>
        /// Check if a player can modify this pile
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if player can modify the pile</returns>
        public bool CanPlayerModify(Player player)
        {
            if (player == null) return false;

            // Owner permissions
            if (player == owner)
            {
                return properties.ownerCanModify;
            }

            // Opponent permissions
            if (player == owner?.opponent)
            {
                return properties.opponentCanModify;
            }

            // Explicit allowed players (with modify permission)
            if (properties.allowedPlayers.Contains(player.id))
            {
                return true; // Could be more granular in the future
            }

            return false;
        }

        /// <summary>
        /// Apply pile-specific effects to a card
        /// </summary>
        /// <param name="card">Card to apply effects to</param>
        /// <param name="entering">True if entering pile, false if leaving</param>
        private void ApplyPileEffects(BaseCard card, bool entering)
        {
            if (card == null) return;

            // Apply face-down state
            if (entering && properties.faceDown)
            {
                card.facedown = true;
            }

            // Trigger card scripts for pile events
            if (entering)
            {
                card.ExecutePythonScript("on_enter_pile", this, properties);
            }
            else
            {
                card.ExecutePythonScript("on_leave_pile", this, properties);
            }
        }

        /// <summary>
        /// Get pile summary for UI display
        /// </summary>
        /// <param name="viewingPlayer">Player viewing the summary</param>
        /// <returns>Pile summary</returns>
        public AdditionalPileSummary GetSummary(Player viewingPlayer)
        {
            bool canView = CanPlayerView(viewingPlayer);
            bool canModify = CanPlayerModify(viewingPlayer);

            var summary = new AdditionalPileSummary
            {
                name = name,
                displayName = DisplayName,
                description = properties.description,
                uuid = uuid,
                ownerName = owner?.name ?? "Unknown",
                cardCount = cards.Count,
                canView = canView,
                canModify = canModify,
                isPrivate = properties.isPrivate,
                maxSize = properties.maxSize,
                isFull = IsFull,
                isEmpty = IsEmpty,
                createdAt = createdAt,
                lastModified = lastModified
            };

            // Include cards if player can view them
            if (canView)
            {
                summary.cards = cards.Select(card => card.GetSummary(viewingPlayer)).ToList();
                summary.topCard = GetTopCard()?.GetSummary(viewingPlayer);
            }

            return summary;
        }

        /// <summary>
        /// Get pile statistics
        /// </summary>
        /// <returns>Pile statistics</returns>
        public AdditionalPileStatistics GetStatistics()
        {
            var cardsByType = cards.GroupBy(c => c.type).ToDictionary(g => g.Key, g => g.Count());
            var cardsByFaction = cards.GroupBy(c => c.GetPrintedFaction()).ToDictionary(g => g.Key, g => g.Count());

            return new AdditionalPileStatistics
            {
                name = name,
                totalCards = cards.Count,
                totalCardsAdded = totalCardsAdded,
                totalCardsRemoved = totalCardsRemoved,
                cardsByType = cardsByType,
                cardsByFaction = cardsByFaction,
                averageCost = cards.Count > 0 ? cards.Average(c => c.GetCost()) : 0,
                createdAt = createdAt,
                lastModified = lastModified,
                maxSize = properties.maxSize
            };
        }

        /// <summary>
        /// Export pile to a serializable format
        /// </summary>
        /// <returns>Exportable pile data</returns>
        public AdditionalPileData Export()
        {
            return new AdditionalPileData
            {
                name = name,
                uuid = uuid,
                properties = properties,
                cardUuids = cards.Select(c => c.uuid).ToList(),
                createdAt = createdAt,
                lastModified = lastModified,
                totalCardsAdded = totalCardsAdded,
                totalCardsRemoved = totalCardsRemoved
            };
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>Pile description</returns>
        public override string ToString()
        {
            return $"{DisplayName} ({cards.Count} cards) - {(properties.isPrivate ? "Private" : "Public")}";
        }
    }

    /// <summary>
    /// Summary data for additional pile UI display
    /// </summary>
    [System.Serializable]
    public class AdditionalPileSummary
    {
        public string name;
        public string displayName;
        public string description;
        public string uuid;
        public string ownerName;
        public int cardCount;
        public bool canView;
        public bool canModify;
        public bool isPrivate;
        public int maxSize;
        public bool isFull;
        public bool isEmpty;
        public DateTime createdAt;
        public DateTime lastModified;
        public List<object> cards;
        public object topCard;
    }

    /// <summary>
    /// Statistics for additional pile analysis
    /// </summary>
    [System.Serializable]
    public class AdditionalPileStatistics
    {
        public string name;
        public int totalCards;
        public int totalCardsAdded;
        public int totalCardsRemoved;
        public Dictionary<string, int> cardsByType;
        public Dictionary<string, int> cardsByFaction;
        public double averageCost;
        public DateTime createdAt;
        public DateTime lastModified;
        public int maxSize;
    }

    /// <summary>
    /// Serializable data for additional pile export/import
    /// </summary>
    [System.Serializable]
    public class AdditionalPileData
    {
        public string name;
        public string uuid;
        public AdditionalPileProperties properties;
        public List<string> cardUuids;
        public DateTime createdAt;
        public DateTime lastModified;
        public int totalCardsAdded;
        public int totalCardsRemoved;
    }

    /// <summary>
    /// Extension methods for additional pile management
    /// </summary>
    public static class AdditionalPileExtensions
    {
        /// <summary>
        /// Create additional pile for player
        /// </summary>
        /// <param name="player">Player to create pile for</param>
        /// <param name="pileName">Name of the pile</param>
        /// <param name="properties">Pile properties</param>
        /// <returns>Created pile</returns>
        public static AdditionalPile CreateAdditionalPile(this Player player, string pileName, AdditionalPileProperties properties = null)
        {
            properties = properties ?? AdditionalPileProperties.CreatePrivate(pileName);
            var pile = new AdditionalPile(properties, player, player.game);
            player.additionalPiles[pileName] = pile;
            return pile;
        }

        /// <summary>
        /// Remove additional pile from player
        /// </summary>
        /// <param name="player">Player to remove pile from</param>
        /// <param name="pileName">Name of pile to remove</param>
        /// <returns>True if pile was removed</returns>
        public static bool RemoveAdditionalPile(this Player player, string pileName)
        {
            if (player.additionalPiles.TryGetValue(pileName, out var pile))
            {
                pile.Clear();
                player.additionalPiles.Remove(pileName);
                Debug.Log($"📚 Removed additional pile '{pileName}' from {player.name}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get additional pile by name
        /// </summary>
        /// <param name="player">Player to get pile from</param>
        /// <param name="pileName">Name of pile to get</param>
        /// <returns>Pile or null if not found</returns>
        public static AdditionalPile GetAdditionalPile(this Player player, string pileName)
        {
            return player.additionalPiles.GetValueOrDefault(pileName);
        }

        /// <summary>
        /// Check if player has additional pile
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <param name="pileName">Name of pile to check for</param>
        /// <returns>True if player has the pile</returns>
        public static bool HasAdditionalPile(this Player player, string pileName)
        {
            return player.additionalPiles.ContainsKey(pileName);
        }

        /// <summary>
        /// Get all additional pile names for player
        /// </summary>
        /// <param name="player">Player to get pile names for</param>
        /// <returns>List of pile names</returns>
        public static List<string> GetAdditionalPileNames(this Player player)
        {
            return player.additionalPiles.Keys.ToList();
        }

        /// <summary>
        /// Move card to additional pile
        /// </summary>
        /// <param name="player">Player who owns the pile</param>
        /// <param name="card">Card to move</param>
        /// <param name="pileName">Name of pile to move to</param>
        /// <returns>True if card was moved successfully</returns>
        public static bool MoveCardToAdditionalPile(this Player player, BaseCard card, string pileName)
        {
            var pile = player.GetAdditionalPile(pileName);
            if (pile == null)
            {
                Debug.LogWarning($"📚 Pile '{pileName}' not found for player {player.name}");
                return false;
            }

            // Remove from current location
            player.RemoveCardFromPile(card);

            // Add to pile
            return pile.AddCard(card);
        }

        /// <summary>
        /// Move card from additional pile to location
        /// </summary>
        /// <param name="player">Player who owns the pile</param>
        /// <param name="card">Card to move</param>
        /// <param name="pileName">Name of pile to move from</param>
        /// <param name="destination">Destination location</param>
        /// <returns>True if card was moved successfully</returns>
        public static bool MoveCardFromAdditionalPile(this Player player, BaseCard card, string pileName, string destination)
        {
            var pile = player.GetAdditionalPile(pileName);
            if (pile == null || !pile.Contains(card))
            {
                return false;
            }

            // Remove from pile
            if (pile.RemoveCard(card))
            {
                // Move to destination
                player.MoveCard(card, destination);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Constants for common additional pile names
    /// </summary>
    public static class AdditionalPileNames
    {
        public const string SetAside = "set_aside";
        public const string Exile = "exile";
        public const string LookingAt = "looking_at";
        public const string Revealed = "revealed";
        public const string Temporary = "temporary";
        public const string Searching = "searching";
        public const string FaceUp = "face_up";
        public const string Hidden = "hidden";
        public const string Memories = "memories";
        public const string Visions = "visions";
    }
}
