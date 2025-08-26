using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    [System.Serializable]
    public class ConflictOpportunities
    {
        public int military = 1;
        public int political = 1;
        public int total = 2;
    }

    [System.Serializable]
    public class PlayerSettings
    {
        public Dictionary<string, bool> promptedActionWindows = new Dictionary<string, bool>
        {
            {"dynasty", true},
            {"draw", true},
            {"preConflict", true},
            {"conflict", true},
            {"fate", true},
            {"regroup", true}
        };
        
        public Dictionary<string, object> timerSettings = new Dictionary<string, object>();
        public Dictionary<string, object> optionSettings = new Dictionary<string, object>();
        public int windowTimer = 10;
    }

    public class Player : MonoBehaviour
    {
        [Header("Player Identity")]
        public UserInfo user;
        public string emailHash;
        public string id;
        public bool owner;
        public string printedType = "player";
        
        [Header("Network State")]
        public object socket;
        public bool disconnected = false;
        public bool left = false;
        public string lobbyId;

        [Header("Card Collections")]
        public List<BaseCard> dynastyDeck = new List<BaseCard>();
        public List<BaseCard> conflictDeck = new List<BaseCard>();
        public List<BaseCard> provinceDeck = new List<BaseCard>();
        public List<BaseCard> hand = new List<BaseCard>();
        public List<BaseCard> cardsInPlay = new List<BaseCard>();
        
        // Province locations
        public List<BaseCard> strongholdProvince = new List<BaseCard>();
        public List<BaseCard> provinceOne = new List<BaseCard>();
        public List<BaseCard> provinceTwo = new List<BaseCard>();
        public List<BaseCard> provinceThree = new List<BaseCard>();
        public List<BaseCard> provinceFour = new List<BaseCard>();
        
        // Discard and special locations
        public List<BaseCard> dynastyDiscardPile = new List<BaseCard>();
        public List<BaseCard> conflictDiscardPile = new List<BaseCard>();
        public List<BaseCard> removedFromGame = new List<BaseCard>();
        public List<BaseCard> underneathStronghold = new List<BaseCard>();
        
        public Dictionary<string, AdditionalPile> additionalPiles = new Dictionary<string, AdditionalPile>();

        [Header("Player Cards")]
        public Faction faction;
        public StrongholdCard stronghold;
        public RoleCard role;

        [Header("Phase Values")]
        public bool hideProvinceDeck = false;
        public bool takenDynastyMulligan = false;
        public bool takenConflictMulligan = false;
        public bool passedDynasty = false;
        public bool actionPhasePriority = false;
        public int honorBidModifier = 0;
        public int showBid = 0;
        public ConflictOpportunities conflictOpportunities = new ConflictOpportunities();
        public string imperialFavor = "";

        [Header("Game Resources")]
        public int fate = 0;
        public int honor = 0;
        public bool readyToStart = false;
        public int limitedPlayed = 0;
        public int maxLimited = 1;
        public bool firstPlayer = false;

        [Header("Game State")]
        public bool showConflict = false;
        public bool showDynasty = false;
        public bool resetTimerAtEndOfRound = false;

        // References
        public Player opponent;
        public Deck deck;
        public Game game;
        public ClockManager clock;
        public PreparedDeck preparedDeck;

        // Systems
        private List<CostReducer> costReducers = new List<CostReducer>();
        private List<PlayableLocation> playableLocations = new List<PlayableLocation>();
        private Dictionary<string, AbilityLimit> abilityMaxByIdentifier = new Dictionary<string, AbilityLimit>();
        private PlayerSettings settings = new PlayerSettings();
        private PlayerPromptState promptState;

        // Static location arrays for easy reference
        private static readonly string[] ProvinceLocations = {
            Locations.StrongholdProvince,
            Locations.ProvinceOne,
            Locations.ProvinceTwo,
            Locations.ProvinceThree,
            Locations.ProvinceFour
        };

        public void Initialize(string playerId, UserInfo userInfo, bool isOwner, Game gameInstance, ClockSettings clockSettings)
        {
            // Base initialization
            name = userInfo.username;
            
            id = playerId;
            user = userInfo;
            emailHash = userInfo.emailHash;
            owner = isOwner;
            game = gameInstance;
            
            // Initialize clock
            clock = gameObject.AddComponent<ClockManager>();
            clock.Initialize(this, clockSettings);
            
            // Initialize prompt state
            promptState = new PlayerPromptState(this);
            
            // Set up initial playable locations
            InitializePlayableLocations();
            
            Debug.Log($"🎮 Player {userInfo.username} initialized");
        }

        private void InitializePlayableLocations()
        {
            playableLocations = new List<PlayableLocation>
            {
                new PlayableLocation(PlayTypes.PlayFromHand, this, Locations.Hand),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceOne),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceTwo),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceThree),
                new PlayableLocation(PlayTypes.PlayFromProvince, this, Locations.ProvinceFour)
            };
        }

        // Clock management
        public void StartClock()
        {
            clock.Start();
            if (opponent != null)
            {
                opponent.clock.OpponentStart();
            }
        }

        public void StopClock()
        {
            clock.Stop();
        }

        public void ResetClock()
        {
            clock.Reset();
        }

        // Card searching and validation methods
        public bool IsCardUuidInList(List<BaseCard> list, BaseCard card)
        {
            return list.Any(c => c.uuid == card.uuid);
        }

        public bool IsCardNameInList(List<BaseCard> list, BaseCard card)
        {
            return list.Any(c => c.name == card.name);
        }

        public bool AreCardsSelected()
        {
            return cardsInPlay.Any(card => card.selected);
        }

        public List<BaseCard> RemoveCardByUuid(List<BaseCard> list, string uuid)
        {
            return list.Where(card => card.uuid != uuid).ToList();
        }

        public BaseCard FindCardByName(List<BaseCard> list, string name)
        {
            return FindCard(list, card => card.name == name);
        }

        public BaseCard FindCardByUuid(List<BaseCard> list, string uuid)
        {
            return FindCard(list, card => card.uuid == uuid);
        }

        public BaseCard FindCardInPlayByUuid(string uuid)
        {
            return FindCard(cardsInPlay, card => card.uuid == uuid);
        }

        public BaseCard FindCard(List<BaseCard> cardList, System.Func<BaseCard, bool> predicate)
        {
            var cards = FindCards(cardList, predicate);
            return cards.FirstOrDefault();
        }

        public List<BaseCard> FindCards(List<BaseCard> cardList, System.Func<BaseCard, bool> predicate)
        {
            if (cardList == null) return new List<BaseCard>();

            var cardsToReturn = new List<BaseCard>();

            foreach (var card in cardList)
            {
                if (predicate(card))
                {
                    cardsToReturn.Add(card);
                }

                // Check attachments
                if (card.attachments != null)
                {
                    cardsToReturn.AddRange(card.attachments.Where(predicate));
                }
            }

            return cardsToReturn;
        }

        public bool AreLocationsAdjacent(string location1, string location2)
        {
            int index1 = Array.IndexOf(ProvinceLocations, location1);
            int index2 = Array.IndexOf(ProvinceLocations, location2);
            return index1 > -1 && index2 > -1 && Mathf.Abs(index1 - index2) == 1;
        }

        // Province management
        public BaseCard GetDynastyCardInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.FirstOrDefault(card => card.isDynasty);
        }

        public List<BaseCard> GetDynastyCardsInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.Where(card => card.isDynasty).ToList();
        }

        public BaseCard GetProvinceCardInProvince(string location)
        {
            var province = GetSourceList(location);
            return province.FirstOrDefault(card => card.isProvince);
        }

        public bool AnyCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return game.allCards.Any(card => 
                card.controller == this && 
                card.location == Locations.PlayArea && 
                predicate(card));
        }

        public List<BaseCard> FilterCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            return game.allCards.Where(card => 
                card.controller == this && 
                card.location == Locations.PlayArea && 
                predicate(card)).ToList();
        }

        // Game state properties
        public bool HasComposure()
        {
            return opponent != null && opponent.showBid > showBid;
        }

        public List<string> GetLegalConflictTypes(ConflictProperties properties)
        {
            var types = properties.type ?? new List<string> { ConflictTypes.Military, ConflictTypes.Political };
            if (!types.GetType().IsArray && !(types is List<string>))
                types = new List<string> { types.ToString() };

            var forcedDeclaredType = properties.forcedDeclaredType ?? 
                                   (game.currentConflict?.forcedDeclaredType);

            if (!string.IsNullOrEmpty(forcedDeclaredType))
            {
                return new List<string> { forcedDeclaredType }.Where(type =>
                    types.Contains(type) &&
                    GetConflictOpportunities() > 0 &&
                    !GetEffects(EffectNames.CannotDeclareConflictsOfType).Contains(type)
                ).ToList();
            }

            return types.Where(type =>
                GetConflictOpportunities(type) > 0 &&
                !GetEffects(EffectNames.CannotDeclareConflictsOfType).Contains(type)
            ).ToList();
        }

        // Conflict management
        public void AddConflictOpportunity(string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                switch (type.ToLower())
                {
                    case "military":
                        conflictOpportunities.military++;
                        break;
                    case "political":
                        conflictOpportunities.political++;
                        break;
                }
            }
            conflictOpportunities.total++;
        }

        public int GetConflictOpportunities(string type = "total")
        {
            switch (type.ToLower())
            {
                case "military":
                    return conflictOpportunities.military;
                case "political":
                    return conflictOpportunities.political;
                default:
                    return conflictOpportunities.total;
            }
        }

        // Cost reduction system
        public CostReducer AddCostReducer(EffectSource source, CostReducerProperties properties)
        {
            var reducer = new CostReducer(game, source, properties);
            costReducers.Add(reducer);
            return reducer;
        }

        public void RemoveCostReducer(CostReducer reducer)
        {
            if (costReducers.Contains(reducer))
            {
                reducer.UnregisterEvents();
                costReducers.Remove(reducer);
            }
        }

        public PlayableLocation AddPlayableLocation(string type, Player player, string location, List<BaseCard> cards = null)
        {
            if (player == null) return null;
            
            var playableLocation = new PlayableLocation(type, player, location, cards ?? new List<BaseCard>());
            playableLocations.Add(playableLocation);
            return playableLocation;
        }

        public void RemovePlayableLocation(PlayableLocation location)
        {
            playableLocations.Remove(location);
        }

        // Ability limit management
        public void RegisterAbilityMax(string maxIdentifier, AbilityLimit limit)
        {
            if (abilityMaxByIdentifier.ContainsKey(maxIdentifier))
                return;

            abilityMaxByIdentifier[maxIdentifier] = limit;
            limit.RegisterEvents(game);
        }

        public bool IsAbilityAtMax(string maxIdentifier)
        {
            if (!abilityMaxByIdentifier.TryGetValue(maxIdentifier, out AbilityLimit limit))
                return false;

            return limit.IsAtMax(this);
        }

        public void IncrementAbilityMax(string maxIdentifier)
        {
            if (abilityMaxByIdentifier.TryGetValue(maxIdentifier, out AbilityLimit limit))
            {
                limit.Increment(this);
            }
        }

        // List management methods
        public List<BaseCard> GetSourceList(string source)
        {
            switch (source)
            {
                case Locations.Hand:
                    return hand;
                case Locations.ConflictDeck:
                    return conflictDeck;
                case Locations.DynastyDeck:
                    return dynastyDeck;
                case Locations.ConflictDiscardPile:
                    return conflictDiscardPile;
                case Locations.RemovedFromGame:
                    return removedFromGame;
                case Locations.PlayArea:
                    return cardsInPlay;
                case Locations.ProvinceOne:
                    return provinceOne;
                case Locations.ProvinceTwo:
                    return provinceTwo;
                case Locations.ProvinceThree:
                    return provinceThree;
                case Locations.ProvinceFour:
                    return provinceFour;
                case Locations.StrongholdProvince:
                    return strongholdProvince;
                case Locations.ProvinceDeck:
                    return provinceDeck;
                case Locations.UnderneathStronghold:
                    return underneathStronghold;
                default:
                    if (additionalPiles.ContainsKey(source))
                    {
                        return additionalPiles[source].cards;
                    }
                    break;
            }
            return new List<BaseCard>();
        }

        // Placeholder methods for missing functionality
        public List<object> GetEffects(string effectName)
        {
            // Placeholder implementation
            return new List<object>();
        }

        public bool AnyEffect(string effectName)
        {
            // Placeholder implementation
            return false;
        }

        public object MostRecentEffect(string effectName)
        {
            // Placeholder implementation
            return null;
        }

        public bool CheckRestrictions(string restriction, AbilityContext context)
        {
            // Placeholder implementation
            return true;
        }

        public void MoveCard(BaseCard card, string targetLocation)
        {
            // Placeholder implementation
        }
    }

    // Supporting classes
    [System.Serializable]
    public class ConflictProperties
    {
        public List<string> type;
        public object ring;
        public object province;
        public BaseCard attacker;
        public string forcedDeclaredType;
    }

    [System.Serializable]
    public class AdditionalPile
    {
        public List<BaseCard> cards = new List<BaseCard>();
        public AdditionalPileProperties properties;
    }

    [System.Serializable]
    public class AdditionalPileProperties
    {
        public string name;
        public bool isPrivate = true;
    }

    // Static classes for constants
    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
    }

    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
    }

    public static class Decks
    {
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
    }
}