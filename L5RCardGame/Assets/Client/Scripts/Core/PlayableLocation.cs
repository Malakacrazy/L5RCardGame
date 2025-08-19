using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a location from which cards can be played, with optional restrictions.
    /// This class defines where cards can be played from and which specific cards are allowed.
    /// </summary>
    [System.Serializable]
    public class PlayableLocation
    {
        #region Fields

        [Header("Location Properties")]
        [SerializeField] private string playingType;
        [SerializeField] private Player player;
        [SerializeField] private string location;
        [SerializeField] private List<BaseCard> cards;

        #endregion

        #region Properties

        /// <summary>
        /// The type of play action this location represents (e.g., "playFromHand", "playFromProvince")
        /// </summary>
        public string PlayingType
        {
            get => playingType;
            set => playingType = value;
        }

        /// <summary>
        /// The player who owns this playable location
        /// </summary>
        public Player Player
        {
            get => player;
            set => player = value;
        }

        /// <summary>
        /// The location identifier (e.g., "hand", "province 1", "play area")
        /// </summary>
        public string Location
        {
            get => location;
            set => location = value;
        }

        /// <summary>
        /// Specific cards that can be played from this location. 
        /// If empty, all cards in the location are playable.
        /// </summary>
        public List<BaseCard> Cards
        {
            get => cards ??= new List<BaseCard>();
            set => cards = value ?? new List<BaseCard>();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public PlayableLocation()
        {
            cards = new List<BaseCard>();
        }

        /// <summary>
        /// Constructor with all parameters
        /// </summary>
        /// <param name="playType">The type of play action</param>
        /// <param name="ownerPlayer">The player who owns this location</param>
        /// <param name="locationId">The location identifier</param>
        /// <param name="allowedCards">Specific cards allowed to be played (optional)</param>
        public PlayableLocation(string playType, Player ownerPlayer, string locationId, List<BaseCard> allowedCards = null)
        {
            playingType = playType;
            player = ownerPlayer;
            location = locationId;
            cards = allowedCards ?? new List<BaseCard>();
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Check if the specified card can be played from this location.
        /// This matches the JavaScript contains() method exactly.
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if the card can be played from this location</returns>
        public bool Contains(BaseCard card)
        {
            if (card == null)
            {
                return false;
            }

            // If specific cards are listed and the card is not in that list, return false
            if (cards.Count > 0 && !cards.Contains(card))
            {
                return false;
            }

            // Get the pile/list for this location from the player
            var pile = player?.GetSourceList(location);
            if (pile == null)
            {
                return false;
            }

            // Check if the pile contains the card
            return pile.Contains(card);
        }

        /// <summary>
        /// Check if any cards can be played from this location
        /// </summary>
        /// <returns>True if location has playable cards</returns>
        public bool HasPlayableCards()
        {
            if (player == null)
            {
                return false;
            }

            var pile = player.GetSourceList(location);
            if (pile == null || pile.Count == 0)
            {
                return false;
            }

            // If specific cards are restricted, check if any of them are in the pile
            if (cards.Count > 0)
            {
                return cards.Any(card => pile.Contains(card));
            }

            // Otherwise, any card in the pile is playable
            return pile.Count > 0;
        }

        /// <summary>
        /// Get all playable cards from this location
        /// </summary>
        /// <returns>List of cards that can be played from this location</returns>
        public List<BaseCard> GetPlayableCards()
        {
            if (player == null)
            {
                return new List<BaseCard>();
            }

            var pile = player.GetSourceList(location);
            if (pile == null)
            {
                return new List<BaseCard>();
            }

            // If specific cards are restricted, return only those that are in the pile
            if (cards.Count > 0)
            {
                return cards.Where(card => pile.Contains(card)).ToList();
            }

            // Otherwise, return all cards in the pile
            return new List<BaseCard>(pile);
        }

        /// <summary>
        /// Check if this location matches the specified criteria
        /// </summary>
        /// <param name="playType">Playing type to match (optional)</param>
        /// <param name="locationId">Location to match (optional)</param>
        /// <returns>True if location matches criteria</returns>
        public bool Matches(string playType = null, string locationId = null)
        {
            if (!string.IsNullOrEmpty(playType) && playingType != playType)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(locationId) && location != locationId)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Card Management

        /// <summary>
        /// Add a card to the restricted cards list
        /// </summary>
        /// <param name="card">Card to add</param>
        public void AddCard(BaseCard card)
        {
            if (card != null && !cards.Contains(card))
            {
                cards.Add(card);
            }
        }

        /// <summary>
        /// Remove a card from the restricted cards list
        /// </summary>
        /// <param name="card">Card to remove</param>
        public void RemoveCard(BaseCard card)
        {
            if (card != null)
            {
                cards.Remove(card);
            }
        }

        /// <summary>
        /// Clear all restricted cards (makes all cards in location playable)
        /// </summary>
        public void ClearCards()
        {
            cards.Clear();
        }

        /// <summary>
        /// Set the restricted cards list
        /// </summary>
        /// <param name="newCards">New list of restricted cards</param>
        public void SetCards(List<BaseCard> newCards)
        {
            cards = newCards ?? new List<BaseCard>();
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check if this playable location is valid
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            return player != null && 
                   !string.IsNullOrEmpty(location) && 
                   !string.IsNullOrEmpty(playingType);
        }

        /// <summary>
        /// Validate that the location exists on the player
        /// </summary>
        /// <returns>True if the location exists</returns>
        public bool LocationExists()
        {
            if (player == null)
            {
                return false;
            }

            var pile = player.GetSourceList(location);
            return pile != null;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Create a copy of this playable location
        /// </summary>
        /// <returns>Copied playable location</returns>
        public PlayableLocation Clone()
        {
            return new PlayableLocation(playingType, player, location, new List<BaseCard>(cards));
        }

        /// <summary>
        /// Check if this location is equivalent to another
        /// </summary>
        /// <param name="other">Other playable location</param>
        /// <returns>True if equivalent</returns>
        public bool IsEquivalentTo(PlayableLocation other)
        {
            if (other == null)
            {
                return false;
            }

            return playingType == other.playingType &&
                   player == other.player &&
                   location == other.location &&
                   cards.SequenceEqual(other.cards);
        }

        /// <summary>
        /// Get count of playable cards
        /// </summary>
        /// <returns>Number of playable cards</returns>
        public int GetPlayableCardCount()
        {
            return GetPlayableCards().Count;
        }

        #endregion

        #region Debug Support

        /// <summary>
        /// Get debug information about this playable location
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            var info = $"PlayableLocation:\n";
            info += $"  Playing Type: {playingType}\n";
            info += $"  Player: {player?.name ?? "null"}\n";
            info += $"  Location: {location}\n";
            info += $"  Restricted Cards: {cards.Count}\n";

            if (player != null)
            {
                var pile = player.GetSourceList(location);
                info += $"  Cards in Location: {pile?.Count ?? 0}\n";
                info += $"  Playable Cards: {GetPlayableCardCount()}\n";
            }

            if (cards.Count > 0)
            {
                info += "  Restricted to:\n";
                for (int i = 0; i < Math.Min(cards.Count, 5); i++) // Show first 5
                {
                    info += $"    - {cards[i].name}\n";
                }
                if (cards.Count > 5)
                {
                    info += $"    ... and {cards.Count - 5} more\n";
                }
            }

            return info;
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var cardInfo = cards.Count > 0 ? $" ({cards.Count} restricted)" : " (all cards)";
            return $"{playingType} from {location}{cardInfo}";
        }

        #endregion
    }

    /// <summary>
    /// Static class containing common playing types for consistency
    /// </summary>
    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
        public const string PlayFromDiscard = "playFromDiscard";
        public const string PlayFromDeck = "playFromDeck";
        public const string PlayFromRemovedFromGame = "playFromRemovedFromGame";
        public const string PlayFromUnderneathStronghold = "playFromUnderneathStronghold";
        public const string PlayFromOpponentDiscard = "playFromOpponentDiscard";
        public const string PlayFromAnyLocation = "playFromAnyLocation";
    }

    /// <summary>
    /// Extension methods for working with PlayableLocation collections
    /// </summary>
    public static class PlayableLocationExtensions
    {
        /// <summary>
        /// Find the first playable location that contains the specified card
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <param name="card">Card to find</param>
        /// <returns>First matching playable location or null</returns>
        public static PlayableLocation FindLocationForCard(this IEnumerable<PlayableLocation> locations, BaseCard card)
        {
            return locations.FirstOrDefault(location => location.Contains(card));
        }

        /// <summary>
        /// Find all playable locations that contain the specified card
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <param name="card">Card to find</param>
        /// <returns>List of matching playable locations</returns>
        public static List<PlayableLocation> FindAllLocationsForCard(this IEnumerable<PlayableLocation> locations, BaseCard card)
        {
            return locations.Where(location => location.Contains(card)).ToList();
        }

        /// <summary>
        /// Get all locations matching the specified playing type
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <param name="playingType">Playing type to match</param>
        /// <returns>List of matching locations</returns>
        public static List<PlayableLocation> ByPlayingType(this IEnumerable<PlayableLocation> locations, string playingType)
        {
            return locations.Where(location => location.PlayingType == playingType).ToList();
        }

        /// <summary>
        /// Get all locations for the specified location
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <param name="location">Location to match</param>
        /// <returns>List of matching locations</returns>
        public static List<PlayableLocation> ByLocation(this IEnumerable<PlayableLocation> locations, string location)
        {
            return locations.Where(loc => loc.Location == location).ToList();
        }

        /// <summary>
        /// Get all locations for the specified player
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <param name="player">Player to match</param>
        /// <returns>List of matching locations</returns>
        public static List<PlayableLocation> ByPlayer(this IEnumerable<PlayableLocation> locations, Player player)
        {
            return locations.Where(location => location.Player == player).ToList();
        }

        /// <summary>
        /// Check if any location contains the specified card
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <param name="card">Card to check</param>
        /// <returns>True if any location contains the card</returns>
        public static bool ContainsCard(this IEnumerable<PlayableLocation> locations, BaseCard card)
        {
            return locations.Any(location => location.Contains(card));
        }

        /// <summary>
        /// Get all playable cards from all locations
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <returns>List of all playable cards</returns>
        public static List<BaseCard> GetAllPlayableCards(this IEnumerable<PlayableLocation> locations)
        {
            return locations.SelectMany(location => location.GetPlayableCards()).Distinct().ToList();
        }

        /// <summary>
        /// Remove invalid locations from the collection
        /// </summary>
        /// <param name="locations">Collection of playable locations</param>
        /// <returns>List of valid locations</returns>
        public static List<PlayableLocation> ValidOnly(this IEnumerable<PlayableLocation> locations)
        {
            return locations.Where(location => location.IsValid()).ToList();
        }
    }
}
