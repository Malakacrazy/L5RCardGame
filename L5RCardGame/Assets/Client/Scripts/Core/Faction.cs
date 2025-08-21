using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a faction/clan in Legend of the Five Rings
    /// Handles faction-specific rules, abilities, and restrictions
    /// </summary>
    [System.Serializable]
    public class Faction
    {
        [Header("Basic Information")]
        public string id;
        public string name;
        public string value;
        public string displayName;
        
        [Header("Faction Properties")]
        public string clanChampion;
        public List<string> traits = new List<string>();
        public List<string> keywords = new List<string>();
        public UnityEngine.Color primaryColor = UnityEngine.Color.white;
        public UnityEngine.Color secondaryColor = UnityEngine.Color.gray;
        
        [Header("Deck Building")]
        public List<string> alwaysAllowedCards = new List<string>();
        public List<string> restrictedCards = new List<string>();
        public int influencePool = 0;
        public int startingHonor = 10;
        
        [Header("Faction Abilities")]
        public List<FactionAbility> abilities = new List<FactionAbility>();
        public List<string> factionEffects = new List<string>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public Faction()
        {
            id = "";
            name = "";
            value = "";
            displayName = "";
        }

        /// <summary>
        /// Constructor with basic information
        /// </summary>
        /// <param name="factionId">Faction ID</param>
        /// <param name="factionName">Faction name</param>
        /// <param name="factionValue">Faction value (usually lowercase name)</param>
        public Faction(string factionId, string factionName, string factionValue = null)
        {
            id = factionId;
            name = factionName;
            value = factionValue ?? factionName.ToLower();
            displayName = factionName;
            
            // Set default colors and properties based on clan
            SetDefaultProperties();
        }

        /// <summary>
        /// Set default properties based on faction name
        /// </summary>
        private void SetDefaultProperties()
        {
            switch (name.ToLower())
            {
                case "crab":
                    primaryColor = new UnityEngine.Color(0.4f, 0.2f, 0.0f); // Brown
                    secondaryColor = new UnityEngine.Color(0.6f, 0.3f, 0.1f);
                    clanChampion = "Hida Kisada";
                    traits.AddRange(new[] { "bushi", "berserker", "engineer" });
                    startingHonor = 11;
                    break;
                    
                case "crane":
                    primaryColor = new UnityEngine.Color(0.0f, 0.6f, 0.9f); // Light Blue
                    secondaryColor = new UnityEngine.Color(0.4f, 0.8f, 1.0f);
                    clanChampion = "Doji Hotaru";
                    traits.AddRange(new[] { "courtier", "duelist", "magistrate" });
                    startingHonor = 12;
                    break;
                    
                case "dragon":
                    primaryColor = new UnityEngine.Color(0.0f, 0.5f, 0.0f); // Green
                    secondaryColor = new UnityEngine.Color(0.2f, 0.7f, 0.2f);
                    clanChampion = "Togashi Yokuni";
                    traits.AddRange(new[] { "monk", "tattooed", "kiho" });
                    startingHonor = 10;
                    break;
                    
                case "lion":
                    primaryColor = new UnityEngine.Color(0.9f, 0.7f, 0.0f); // Gold
                    secondaryColor = new UnityEngine.Color(1.0f, 0.8f, 0.2f);
                    clanChampion = "Akodo Toturi";
                    traits.AddRange(new[] { "bushi", "commander", "samurai" });
                    startingHonor = 10;
                    break;
                    
                case "phoenix":
                    primaryColor = new UnityEngine.Color(1.0f, 0.3f, 0.0f); // Orange
                    secondaryColor = new UnityEngine.Color(1.0f, 0.5f, 0.2f);
                    clanChampion = "Shiba Tsukune";
                    traits.AddRange(new[] { "shugenja", "scholar", "elemental" });
                    startingHonor = 11;
                    break;
                    
                case "scorpion":
                    primaryColor = new UnityEngine.Color(0.6f, 0.0f, 0.0f); // Dark Red
                    secondaryColor = new UnityEngine.Color(0.8f, 0.2f, 0.2f);
                    clanChampion = "Bayushi Shoju";
                    traits.AddRange(new[] { "shinobi", "courtier", "magistrate" });
                    startingHonor = 9;
                    break;
                    
                case "unicorn":
                    primaryColor = new UnityEngine.Color(0.6f, 0.0f, 0.6f); // Purple
                    secondaryColor = new UnityEngine.Color(0.8f, 0.2f, 0.8f);
                    clanChampion = "Shinjo Altansarnai";
                    traits.AddRange(new[] { "bushi", "cavalry", "scout" });
                    startingHonor = 10;
                    break;
                    
                case "neutral":
                    primaryColor = UnityEngine.Color.gray;
                    secondaryColor = UnityEngine.Color.white;
                    clanChampion = "";
                    traits.AddRange(new[] { "ronin", "imperial" });
                    startingHonor = 10;
                    break;
                    
                default:
                    primaryColor = UnityEngine.Color.white;
                    secondaryColor = UnityEngine.Color.gray;
                    startingHonor = 10;
                    break;
            }
        }

        /// <summary>
        /// Check if this faction can use a specific card
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if faction can use the card</returns>
        public bool CanUseCard(BaseCard card)
        {
            // Always allow faction cards
            if (card.IsFaction(value))
                return true;
                
            // Check explicitly allowed cards
            if (alwaysAllowedCards.Contains(card.id))
                return true;
                
            // Check explicitly restricted cards
            if (restrictedCards.Contains(card.id))
                return false;
                
            // Allow neutral cards
            if (card.IsFaction(Clans.Neutral))
                return true;
                
            return false;
        }

        /// <summary>
        /// Check if this faction can use a card with influence cost
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <param name="availableInfluence">Available influence points</param>
        /// <returns>True if faction can use the card</returns>
        public bool CanUseCardWithInfluence(BaseCard card, int availableInfluence)
        {
            if (CanUseCard(card))
                return true;
                
            // Check if card can be used with influence
            if (card.cardData != null && card.cardData.influenceCost <= availableInfluence)
                return true;
                
            return false;
        }

        /// <summary>
        /// Get the influence cost for a card for this faction
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>Influence cost (0 if faction card or neutral)</returns>
        public int GetInfluenceCost(BaseCard card)
        {
            if (CanUseCard(card))
                return 0;
                
            return card.cardData?.influenceCost ?? 0;
        }

        /// <summary>
        /// Add a faction-specific ability
        /// </summary>
        /// <param name="ability">Ability to add</param>
        public void AddAbility(FactionAbility ability)
        {
            if (!abilities.Contains(ability))
            {
                abilities.Add(ability);
            }
        }

        /// <summary>
        /// Remove a faction-specific ability
        /// </summary>
        /// <param name="ability">Ability to remove</param>
        public void RemoveAbility(FactionAbility ability)
        {
            abilities.Remove(ability);
        }

        /// <summary>
        /// Check if faction has a specific trait
        /// </summary>
        /// <param name="trait">Trait to check for</param>
        /// <returns>True if faction has the trait</returns>
        public bool HasTrait(string trait)
        {
            return traits.Any(t => t.Equals(trait, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Add a trait to the faction
        /// </summary>
        /// <param name="trait">Trait to add</param>
        public void AddTrait(string trait)
        {
            if (!HasTrait(trait))
            {
                traits.Add(trait.ToLower());
            }
        }

        /// <summary>
        /// Remove a trait from the faction
        /// </summary>
        /// <param name="trait">Trait to remove</param>
        public void RemoveTrait(string trait)
        {
            traits.RemoveAll(t => t.Equals(trait, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get display name with clan symbol
        /// </summary>
        /// <returns>Formatted display name</returns>
        public string GetDisplayNameWithSymbol()
        {
            string symbol = GetClanSymbol();
            return string.IsNullOrEmpty(symbol) ? displayName : $"{symbol} {displayName}";
        }

        /// <summary>
        /// Get clan symbol emoji
        /// </summary>
        /// <returns>Clan symbol</returns>
        private string GetClanSymbol()
        {
            switch (name.ToLower())
            {
                case "crab": return "🦀";
                case "crane": return "🕊️";
                case "dragon": return "🐉";
                case "lion": return "🦁";
                case "phoenix": return "🔥";
                case "scorpion": return "🦂";
                case "unicorn": return "🦄";
                case "neutral": return "⚪";
                default: return "";
            }
        }

        /// <summary>
        /// Get faction summary for UI
        /// </summary>
        /// <returns>Faction summary data</returns>
        public FactionSummary GetSummary()
        {
            return new FactionSummary
            {
                id = id,
                name = name,
                value = value,
                displayName = GetDisplayNameWithSymbol(),
                clanChampion = clanChampion,
                primaryColor = primaryColor,
                secondaryColor = secondaryColor,
                startingHonor = startingHonor,
                influencePool = influencePool,
                traitCount = traits.Count,
                abilityCount = abilities.Count
            };
        }

        /// <summary>
        /// Create faction from clan constant
        /// </summary>
        /// <param name="clan">Clan constant</param>
        /// <returns>Faction instance</returns>
        public static Faction FromClan(string clan)
        {
            return new Faction(clan, ConstantsHelper.ToDisplayCase(clan), clan);
        }

        /// <summary>
        /// Get all available factions
        /// </summary>
        /// <returns>List of all factions</returns>
        public static List<Faction> GetAllFactions()
        {
            return Clans.GetAllClans().Select(FromClan).ToList();
        }

        /// <summary>
        /// Validate deck for this faction
        /// </summary>
        /// <param name="deck">Deck to validate</param>
        /// <returns>Validation result</returns>
        public DeckValidationResult ValidateDeck(DeckData deck)
        {
            var result = new DeckValidationResult();
            int usedInfluence = 0;

            // Check all cards in the deck
            var allCards = deck.conflictCards.Concat(deck.dynastyCards)
                                             .Concat(deck.provinceCards)
                                             .ToList();

            foreach (var cardEntry in allCards)
            {
                var card = CreateTempCard(cardEntry.card);
                
                if (!CanUseCard(card))
                {
                    int influenceCost = GetInfluenceCost(card);
                    if (influenceCost > 0)
                    {
                        usedInfluence += influenceCost * cardEntry.count;
                    }
                    else
                    {
                        result.AddError($"Card {card.name} cannot be used by faction {name}");
                    }
                }
            }

            // Check influence limit
            if (usedInfluence > influencePool)
            {
                result.AddError($"Deck uses {usedInfluence} influence but faction {name} only has {influencePool}");
            }

            return result;
        }

        /// <summary>
        /// Create a temporary card for validation
        /// </summary>
        /// <param name="cardData">Card data</param>
        /// <returns>Temporary card instance</returns>
        private BaseCard CreateTempCard(CardData cardData)
        {
            var tempGO = new GameObject("TempCard");
            var tempCard = tempGO.AddComponent<BaseCard>();
            tempCard.cardData = cardData;
            tempCard.id = cardData.id;
            tempCard.printedFaction = cardData.clan;
            
            var result = tempCard;
            UnityEngine.Object.DestroyImmediate(tempGO);
            
            return result;
        }

        /// <summary>
        /// Apply faction effects to a player
        /// </summary>
        /// <param name="player">Player to apply effects to</param>
        /// <param name="game">Game instance</param>
        public void ApplyFactionEffects(Player player, Game game)
        {
            // Apply faction-specific starting honor
            player.honor = startingHonor;
            
            // Apply faction abilities
            foreach (var ability in abilities)
            {
                ability.Apply(player, game);
            }
            
            // Apply faction effects
            foreach (var effectName in factionEffects)
            {
                // This would apply ongoing faction effects
                // Implementation depends on effect system
            }
        }

        /// <summary>
        /// String representation of faction
        /// </summary>
        /// <returns>Faction name</returns>
        public override string ToString()
        {
            return displayName ?? name ?? id;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>True if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is Faction other)
            {
                return id == other.id || value == other.value;
            }
            if (obj is string str)
            {
                return id == str || value == str || name == str;
            }
            return false;
        }

        /// <summary>
        /// Hash code for dictionary usage
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return (id ?? value ?? name ?? "").GetHashCode();
        }
    }

    /// <summary>
    /// Faction-specific ability
    /// </summary>
    [System.Serializable]
    public class FactionAbility
    {
        public string name;
        public string description;
        public string type; // "passive", "triggered", "action"
        public List<string> conditions = new List<string>();
        public List<string> effects = new List<string>();

        /// <summary>
        /// Apply faction ability to player
        /// </summary>
        /// <param name="player">Player to apply to</param>
        /// <param name="game">Game instance</param>
        public virtual void Apply(Player player, Game game)
        {
            // Base implementation - override in specific abilities
        }
    }

    /// <summary>
    /// Faction summary for UI display
    /// </summary>
    [System.Serializable]
    public class FactionSummary
    {
        public string id;
        public string name;
        public string value;
        public string displayName;
        public string clanChampion;
        public UnityEngine.Color primaryColor;
        public UnityEngine.Color secondaryColor;
        public int startingHonor;
        public int influencePool;
        public int traitCount;
        public int abilityCount;
    }

    /// <summary>
    /// Extension methods for faction management
    /// </summary>
    public static class FactionExtensions
    {
        /// <summary>
        /// Check if card belongs to faction
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <param name="faction">Faction to check against</param>
        /// <returns>True if card belongs to faction</returns>
        public static bool BelongsToFaction(this BaseCard card, Faction faction)
        {
            return faction.CanUseCard(card);
        }

        /// <summary>
        /// Get faction from string
        /// </summary>
        /// <param name="factionString">Faction identifier</param>
        /// <returns>Faction instance or null</returns>
        public static Faction ToFaction(this string factionString)
        {
            return Faction.FromClan(factionString);
        }

        /// <summary>
        /// Check if two factions are the same
        /// </summary>
        /// <param name="faction1">First faction</param>
        /// <param name="faction2">Second faction</param>
        /// <returns>True if same faction</returns>
        public static bool IsSameFaction(this Faction faction1, Faction faction2)
        {
            return faction1?.Equals(faction2) ?? false;
        }

        /// <summary>
        /// Get faction color for UI elements
        /// </summary>
        /// <param name="faction">Faction to get color for</param>
        /// <param name="usePrimary">Whether to use primary color (default) or secondary</param>
        /// <returns>Faction color</returns>
        public static UnityEngine.Color GetColor(this Faction faction, bool usePrimary = true)
        {
            if (faction == null) return UnityEngine.Color.white;
            return usePrimary ? faction.primaryColor : faction.secondaryColor;
        }

        /// <summary>
        /// Format faction name for display
        /// </summary>
        /// <param name="faction">Faction to format</param>
        /// <param name="includeSymbol">Whether to include clan symbol</param>
        /// <returns>Formatted faction name</returns>
        public static string FormatForDisplay(this Faction faction, bool includeSymbol = true)
        {
            if (faction == null) return "Unknown";
            return includeSymbol ? faction.GetDisplayNameWithSymbol() : faction.displayName ?? faction.name;
        }
    }

    /// <summary>
    /// Constants for faction-related functionality
    /// </summary>
    public static class FactionConstants
    {
        /// <summary>
        /// Default influence pools for factions
        /// </summary>
        public static readonly Dictionary<string, int> DefaultInfluencePools = new Dictionary<string, int>
        {
            { Clans.Crab, 13 },
            { Clans.Crane, 13 },
            { Clans.Dragon, 13 },
            { Clans.Lion, 13 },
            { Clans.Phoenix, 13 },
            { Clans.Scorpion, 13 },
            { Clans.Unicorn, 13 },
            { Clans.Neutral, 0 }
        };

        /// <summary>
        /// Default starting honor for factions
        /// </summary>
        public static readonly Dictionary<string, int> DefaultStartingHonor = new Dictionary<string, int>
        {
            { Clans.Crab, 11 },
            { Clans.Crane, 12 },
            { Clans.Dragon, 10 },
            { Clans.Lion, 10 },
            { Clans.Phoenix, 11 },
            { Clans.Scorpion, 9 },
            { Clans.Unicorn, 10 },
            { Clans.Neutral, 10 }
        };

        /// <summary>
        /// Get default influence pool for faction
        /// </summary>
        /// <param name="faction">Faction name</param>
        /// <returns>Default influence pool</returns>
        public static int GetDefaultInfluencePool(string faction)
        {
            return DefaultInfluencePools.GetValueOrDefault(faction, 0);
        }

        /// <summary>
        /// Get default starting honor for faction
        /// </summary>
        /// <param name="faction">Faction name</param>
        /// <returns>Default starting honor</returns>
        public static int GetDefaultStartingHonor(string faction)
        {
            return DefaultStartingHonor.GetValueOrDefault(faction, 10);
        }
    }
}
