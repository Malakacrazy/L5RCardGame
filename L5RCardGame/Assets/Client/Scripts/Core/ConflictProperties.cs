using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties that define how a conflict can be declared and resolved.
    /// Used for conflict declaration validation and special conflict effects.
    /// </summary>
    [System.Serializable]
    public class ConflictProperties
    {
        [Header("Conflict Type")]
        public List<string> type = new List<string>();
        public string forcedDeclaredType;
        public bool canSwitchType = true;

        [Header("Ring Selection")]
        public List<Ring> ring = new List<Ring>();
        public string forcedRing;
        public bool canSwitchRing = true;

        [Header("Province Target")]
        public List<BaseCard> province = new List<BaseCard>();
        public string forcedProvince;
        public bool canTargetAnyProvince = true;

        [Header("Attacker Requirements")]
        public BaseCard attacker;
        public List<BaseCard> forcedAttackers = new List<BaseCard>();
        public List<BaseCard> additionalAttackers = new List<BaseCard>();
        public int minAttackers = 0;
        public int maxAttackers = -1; // -1 means unlimited

        [Header("Defender Restrictions")]
        public List<BaseCard> forcedDefenders = new List<BaseCard>();
        public List<BaseCard> cannotDefend = new List<BaseCard>();
        public int minDefenders = 0;
        public int maxDefenders = -1; // -1 means unlimited

        [Header("Skill Calculation")]
        public bool unopposed = false;
        public bool forceUnopposed = false;
        public int skillModifier = 0;
        public System.Func<BaseCard, int> customSkillFunction;

        [Header("Special Rules")]
        public bool noSkillComparison = false;
        public bool cannotBeBroken = false;
        public bool mustBreak = false;
        public bool cannotResolveRingEffect = false;
        public bool mustResolveRingEffect = false;

        [Header("Timing Restrictions")]
        public bool cannotPass = false;
        public bool mustDeclare = false;
        public bool skipActionWindow = false;
        public bool skipDefenderChoice = false;

        [Header("Custom Properties")]
        public Dictionary<string, object> customProperties = new Dictionary<string, object>();
        public List<string> tags = new List<string>();

        /// <summary>
        /// Default constructor with standard conflict rules
        /// </summary>
        public ConflictProperties()
        {
            type = new List<string> { ConflictTypes.Military, ConflictTypes.Political };
            ring = new List<Ring>();
            province = new List<BaseCard>();
            forcedAttackers = new List<BaseCard>();
            additionalAttackers = new List<BaseCard>();
            forcedDefenders = new List<BaseCard>();
            cannotDefend = new List<BaseCard>();
            customProperties = new Dictionary<string, object>();
            tags = new List<string>();
        }

        /// <summary>
        /// Constructor with specific conflict type
        /// </summary>
        /// <param name=\"conflictType\">Type of conflict to allow</param>
        public ConflictProperties(string conflictType) : this()
        {
            type = new List<string> { conflictType };
        }

        /// <summary>
        /// Constructor with specific attacker
        /// </summary>
        /// <param name=\"attackingCard\">Card that must attack</param>
        public ConflictProperties(BaseCard attackingCard) : this()
        {
            attacker = attackingCard;
            if (attackingCard != null)
            {
                forcedAttackers.Add(attackingCard);
            }
        }

        /// <summary>
        /// Constructor with specific conflict type and attacker
        /// </summary>
        /// <param name=\"conflictType\">Type of conflict</param>
        /// <param name=\"attackingCard\">Attacking card</param>
        public ConflictProperties(string conflictType, BaseCard attackingCard) : this(conflictType)
        {
            attacker = attackingCard;
            if (attackingCard != null)
            {
                forcedAttackers.Add(attackingCard);
            }
        }

        /// <summary>
        /// Get all allowed conflict types
        /// </summary>
        /// <returns>List of allowed conflict types</returns>
        public List<string> GetAllowedTypes()
        {
            if (type == null || type.Count == 0)
            {
                return new List<string> { ConflictTypes.Military, ConflictTypes.Political };
            }
            return type.ToList();
        }

        /// <summary>
        /// Check if a specific conflict type is allowed
        /// </summary>
        /// <param name=\"conflictType\">Type to check</param>
        /// <returns>True if type is allowed</returns>
        public bool IsTypeAllowed(string conflictType)
        {
            if (!string.IsNullOrEmpty(forcedDeclaredType))
            {
                return forcedDeclaredType == conflictType;
            }
            return GetAllowedTypes().Contains(conflictType);
        }

        /// <summary>
        /// Get all allowed rings
        /// </summary>
        /// <param name=\"game\">Game instance for ring lookup</param>
        /// <returns>List of allowed rings</returns>
        public List<Ring> GetAllowedRings(Game game)
        {
            if (!string.IsNullOrEmpty(forcedRing))
            {
                var ring = game.rings.GetValueOrDefault(forcedRing);
                return ring != null ? new List<Ring> { ring } : new List<Ring>();
            }

            if (ring != null && ring.Count > 0)
            {
                return ring.ToList();
            }

            // Return all rings if none specified
            return game.rings.Values.ToList();
        }

        /// <summary>
        /// Check if a specific ring is allowed
        /// </summary>
        /// <param name=\"targetRing\">Ring to check</param>
        /// <param name=\"game\">Game instance</param>
        /// <returns>True if ring is allowed</returns>
        public bool IsRingAllowed(Ring targetRing, Game game)
        {
            if (targetRing == null) return false;

            if (!string.IsNullOrEmpty(forcedRing))
            {
                return targetRing.element == forcedRing;
            }

            if (ring != null && ring.Count > 0)
            {
                return ring.Contains(targetRing);
            }

            return true; // All rings allowed if none specified
        }

        /// <summary>
        /// Get all allowed provinces
        /// </summary>
        /// <param name=\"defendingPlayer\">Defending player</param>
        /// <returns>List of allowed provinces</returns>
        public List<BaseCard> GetAllowedProvinces(Player defendingPlayer)
        {
            if (!string.IsNullOrEmpty(forcedProvince))
            {
                var province = defendingPlayer.GetSourceList(forcedProvince).FirstOrDefault(c => c.isProvince);
                return province != null ? new List<BaseCard> { province } : new List<BaseCard>();
            }

            if (province != null && province.Count > 0)
            {
                return province.ToList();
            }

            // Return all provinces if none specified
            if (canTargetAnyProvince)
            {
                return defendingPlayer.GetProvinces();
            }

            return new List<BaseCard>();
        }

        /// <summary>
        /// Check if a specific province can be targeted
        /// </summary>
        /// <param name=\"targetProvince\">Province to check</param>
        /// <param name=\"defendingPlayer\">Defending player</param>
        /// <returns>True if province can be targeted</returns>
        public bool IsProvinceAllowed(BaseCard targetProvince, Player defendingPlayer)
        {
            if (targetProvince == null || !targetProvince.isProvince) return false;

            if (!string.IsNullOrEmpty(forcedProvince))
            {
                return targetProvince.location == forcedProvince;
            }

            if (province != null && province.Count > 0)
            {
                return province.Contains(targetProvince);
            }

            return canTargetAnyProvince;
        }

        /// <summary>
        /// Get all required attackers
        /// </summary>
        /// <returns>List of cards that must attack</returns>
        public List<BaseCard> GetRequiredAttackers()
        {
            var required = new List<BaseCard>();

            if (attacker != null)
            {
                required.Add(attacker);
            }

            if (forcedAttackers != null)
            {
                required.AddRange(forcedAttackers);
            }

            return required.Distinct().ToList();
        }

        /// <summary>
        /// Check if a card can attack in this conflict
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <returns>True if card can attack</returns>
        public bool CanCardAttack(BaseCard card)
        {
            if (card == null) return false;

            // Check if card is forced to attack
            var requiredAttackers = GetRequiredAttackers();
            if (requiredAttackers.Count > 0 && !requiredAttackers.Contains(card))
            {
                return false;
            }

            // Check if card is in additional attackers
            if (additionalAttackers != null && additionalAttackers.Count > 0)
            {
                return additionalAttackers.Contains(card);
            }

            return true; // Default: any character can attack
        }

        /// <summary>
        /// Check if a card can defend in this conflict
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <returns>True if card can defend</returns>
        public bool CanCardDefend(BaseCard card)
        {
            if (card == null) return false;

            // Check if card is forbidden from defending
            if (cannotDefend != null && cannotDefend.Contains(card))
            {
                return false;
            }

            // Check if card is forced to defend
            if (forcedDefenders != null && forcedDefenders.Count > 0)
            {
                return forcedDefenders.Contains(card);
            }

            return true; // Default: any character can defend
        }

        /// <summary>
        /// Get the minimum number of attackers required
        /// </summary>
        /// <returns>Minimum attacker count</returns>
        public int GetMinAttackers()
        {
            return Math.Max(minAttackers, GetRequiredAttackers().Count);
        }

        /// <summary>
        /// Get the maximum number of attackers allowed
        /// </summary>
        /// <returns>Maximum attacker count (-1 for unlimited)</returns>
        public int GetMaxAttackers()
        {
            return maxAttackers;
        }

        /// <summary>
        /// Get the minimum number of defenders required
        /// </summary>
        /// <returns>Minimum defender count</returns>
        public int GetMinDefenders()
        {
            return Math.Max(minDefenders, forcedDefenders?.Count ?? 0);
        }

        /// <summary>
        /// Get the maximum number of defenders allowed
        /// </summary>
        /// <returns>Maximum defender count (-1 for unlimited)</returns>
        public int GetMaxDefenders()
        {
            return maxDefenders;
        }

        /// <summary>
        /// Check if the conflict must be unopposed
        /// </summary>
        /// <returns>True if conflict must be unopposed</returns>
        public bool MustBeUnopposed()
        {
            return unopposed || forceUnopposed;
        }

        /// <summary>
        /// Check if the conflict can be declared with current parameters
        /// </summary>
        /// <param name=\"game\">Game instance</param>
        /// <param name=\"attackingPlayer\">Attacking player</param>
        /// <returns>True if conflict can be declared</returns>
        public bool CanDeclareConflict(Game game, Player attackingPlayer)
        {
            // Check if declaration is forced or forbidden
            if (cannotPass && mustDeclare)
            {
                return true; // Must declare regardless of other conditions
            }

            // Check type availability
            var allowedTypes = GetAllowedTypes();
            bool hasValidType = allowedTypes.Any(type => 
                attackingPlayer.GetConflictOpportunities(type) > 0);

            if (!hasValidType) return false;

            // Check ring availability
            var allowedRings = GetAllowedRings(game);
            bool hasValidRing = allowedRings.Any(ring => ring.CanDeclare(attackingPlayer));

            if (!hasValidRing) return false;

            // Check attacker availability
            var requiredAttackers = GetRequiredAttackers();
            bool hasValidAttackers = requiredAttackers.All(card => 
                card.CanDeclareAsAttacker(allowedTypes.First(), allowedRings.First()));

            if (!hasValidAttackers) return false;

            // Check province availability
            if (attackingPlayer.opponent != null)
            {
                var allowedProvinces = GetAllowedProvinces(attackingPlayer.opponent);
                bool hasValidProvince = allowedProvinces.Any(province => 
                    province.CanDeclare(allowedTypes.First(), allowedRings.First()));

                if (!hasValidProvince) return false;
            }

            return true;
        }

        /// <summary>
        /// Apply custom skill calculation if specified
        /// </summary>
        /// <param name=\"card\">Card to calculate skill for</param>
        /// <param name=\"defaultSkill\">Default skill value</param>
        /// <returns>Modified skill value</returns>
        public int ApplyCustomSkillCalculation(BaseCard card, int defaultSkill)
        {
            if (customSkillFunction != null)
            {
                return customSkillFunction(card);
            }

            return defaultSkill + skillModifier;
        }

        /// <summary>
        /// Check if conflict has a specific tag
        /// </summary>
        /// <param name=\"tag\">Tag to check for</param>
        /// <returns>True if conflict has the tag</returns>
        public bool HasTag(string tag)
        {
            return tags.Contains(tag);
        }

        /// <summary>
        /// Add a tag to the conflict
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
        /// Remove a tag from the conflict
        /// </summary>
        /// <param name=\"tag\">Tag to remove</param>
        public void RemoveTag(string tag)
        {
            tags.Remove(tag);
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
        /// Set a custom property value
        /// </summary>
        /// <param name=\"key\">Property key</param>
        /// <param name=\"value\">Property value</param>
        public void SetCustomProperty(string key, object value)
        {
            customProperties[key] = value;
        }

        /// <summary>
        /// Check if a custom property exists
        /// </summary>
        /// <param name=\"key\">Property key</param>
        /// <returns>True if property exists</returns>
        public bool HasCustomProperty(string key)
        {
            return customProperties.ContainsKey(key);
        }

        /// <summary>
        /// Remove a custom property
        /// </summary>
        /// <param name=\"key\">Property key</param>
        public void RemoveCustomProperty(string key)
        {
            customProperties.Remove(key);
        }

        /// <summary>
        /// Validate attacker selection
        /// </summary>
        /// <param name=\"selectedAttackers\">Cards selected to attack</param>
        /// <returns>Validation result</returns>
        public ConflictValidationResult ValidateAttackers(List<BaseCard> selectedAttackers)
        {
            var result = new ConflictValidationResult();
            
            // Check minimum attackers
            if (selectedAttackers.Count < GetMinAttackers())
            {
                result.AddError($"Must select at least {GetMinAttackers()} attacker(s)");
            }

            // Check maximum attackers
            if (maxAttackers >= 0 && selectedAttackers.Count > maxAttackers)
            {
                result.AddError($"Cannot select more than {maxAttackers} attacker(s)");
            }

            // Check required attackers
            var requiredAttackers = GetRequiredAttackers();
            foreach (var required in requiredAttackers)
            {
                if (!selectedAttackers.Contains(required))
                {
                    result.AddError($"{required.name} must be declared as an attacker");
                }
            }

            // Check if all selected attackers can attack
            foreach (var attacker in selectedAttackers)
            {
                if (!CanCardAttack(attacker))
                {
                    result.AddError($"{attacker.name} cannot attack in this conflict");
                }
            }

            return result;
        }

        /// <summary>
        /// Validate defender selection
        /// </summary>
        /// <param name=\"selectedDefenders\">Cards selected to defend</param>
        /// <returns>Validation result</returns>
        public ConflictValidationResult ValidateDefenders(List<BaseCard> selectedDefenders)
        {
            var result = new ConflictValidationResult();
            
            // Check minimum defenders
            if (selectedDefenders.Count < GetMinDefenders())
            {
                result.AddError($"Must select at least {GetMinDefenders()} defender(s)");
            }

            // Check maximum defenders
            if (maxDefenders >= 0 && selectedDefenders.Count > maxDefenders)
            {
                result.AddError($"Cannot select more than {maxDefenders} defender(s)");
            }

            // Check forced defenders
            if (forcedDefenders != null)
            {
                foreach (var forced in forcedDefenders)
                {
                    if (!selectedDefenders.Contains(forced))
                    {
                        result.AddError($"{forced.name} must be declared as a defender");
                    }
                }
            }

            // Check if all selected defenders can defend
            foreach (var defender in selectedDefenders)
            {
                if (!CanCardDefend(defender))
                {
                    result.AddError($"{defender.name} cannot defend in this conflict");
                }
            }

            return result;
        }

        /// <summary>
        /// Create a copy of these conflict properties
        /// </summary>
        /// <returns>Copied conflict properties</returns>
        public ConflictProperties CreateCopy()
        {
            var copy = new ConflictProperties
            {
                type = new List<string>(type),
                forcedDeclaredType = forcedDeclaredType,
                canSwitchType = canSwitchType,
                ring = new List<Ring>(ring),
                forcedRing = forcedRing,
                canSwitchRing = canSwitchRing,
                province = new List<BaseCard>(province),
                forcedProvince = forcedProvince,
                canTargetAnyProvince = canTargetAnyProvince,
                attacker = attacker,
                forcedAttackers = new List<BaseCard>(forcedAttackers),
                additionalAttackers = new List<BaseCard>(additionalAttackers),
                minAttackers = minAttackers,
                maxAttackers = maxAttackers,
                forcedDefenders = new List<BaseCard>(forcedDefenders),
                cannotDefend = new List<BaseCard>(cannotDefend),
                minDefenders = minDefenders,
                maxDefenders = maxDefenders,
                unopposed = unopposed,
                forceUnopposed = forceUnopposed,
                skillModifier = skillModifier,
                customSkillFunction = customSkillFunction,
                noSkillComparison = noSkillComparison,
                cannotBeBroken = cannotBeBroken,
                mustBreak = mustBreak,
                cannotResolveRingEffect = cannotResolveRingEffect,
                mustResolveRingEffect = mustResolveRingEffect,
                cannotPass = cannotPass,
                mustDeclare = mustDeclare,
                skipActionWindow = skipActionWindow,
                skipDefenderChoice = skipDefenderChoice,
                customProperties = new Dictionary<string, object>(customProperties),
                tags = new List<string>(tags)
            };

            return copy;
        }

        /// <summary>
        /// Get summary of conflict properties for display
        /// </summary>
        /// <returns>Summary string</returns>
        public string GetSummary()
        {
            var summary = new List<string>();

            if (type.Count > 0)
            {
                summary.Add($"Type: {string.Join(", ", type)}");
            }

            if (!string.IsNullOrEmpty(forcedDeclaredType))
            {
                summary.Add($"Forced Type: {forcedDeclaredType}");
            }

            if (ring.Count > 0)
            {
                summary.Add($"Ring: {string.Join(", ", ring.Select(r => r.element))}");
            }

            if (!string.IsNullOrEmpty(forcedRing))
            {
                summary.Add($"Forced Ring: {forcedRing}");
            }

            if (attacker != null)
            {
                summary.Add($"Attacker: {attacker.name}");
            }

            if (forcedAttackers.Count > 0)
            {
                summary.Add($"Forced Attackers: {string.Join(", ", forcedAttackers.Select(a => a.name))}");
            }

            if (minAttackers > 0)
            {
                summary.Add($"Min Attackers: {minAttackers}");
            }

            if (maxAttackers >= 0)
            {
                summary.Add($"Max Attackers: {maxAttackers}");
            }

            if (MustBeUnopposed())
            {
                summary.Add("Must be unopposed");
            }

            if (noSkillComparison)
            {
                summary.Add("No skill comparison");
            }

            if (cannotBeBroken)
            {
                summary.Add("Cannot be broken");
            }

            if (mustBreak)
            {
                summary.Add("Must break province");
            }

            if (tags.Count > 0)
            {
                summary.Add($"Tags: {string.Join(", ", tags)}");
            }

            return summary.Count > 0 ? string.Join("; ", summary) : "Standard conflict";
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
    /// Result of conflict properties validation
    /// </summary>
    public class ConflictValidationResult
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
    /// Factory class for creating common conflict properties
    /// </summary>
    public static class ConflictPropertiesFactory
    {
        /// <summary>
        /// Create standard conflict properties
        /// </summary>
        /// <returns>Standard conflict properties</returns>
        public static ConflictProperties CreateStandard()
        {
            return new ConflictProperties();
        }

        /// <summary>
        /// Create military-only conflict properties
        /// </summary>
        /// <returns>Military conflict properties</returns>
        public static ConflictProperties CreateMilitary()
        {
            return new ConflictProperties(ConflictTypes.Military);
        }

        /// <summary>
        /// Create political-only conflict properties
        /// </summary>
        /// <returns>Political conflict properties</returns>
        public static ConflictProperties CreatePolitical()
        {
            return new ConflictProperties(ConflictTypes.Political);
        }

        /// <summary>
        /// Create unopposed conflict properties
        /// </summary>
        /// <returns>Unopposed conflict properties</returns>
        public static ConflictProperties CreateUnopposed()
        {
            return new ConflictProperties
            {
                forceUnopposed = true,
                maxDefenders = 0
            };
        }

        /// <summary>
        /// Create forced conflict properties
        /// </summary>
        /// <param name=\"attacker\">Card that must attack</param>
        /// <returns>Forced conflict properties</returns>
        public static ConflictProperties CreateForced(BaseCard attacker)
        {
            return new ConflictProperties
            {
                attacker = attacker,
                forcedAttackers = new List<BaseCard> { attacker },
                mustDeclare = true,
                cannotPass = true
            };
        }

        /// <summary>
        /// Create conflict properties for a specific ring
        /// </summary>
        /// <param name=\"ring\">Ring that must be contested</param>
        /// <returns>Ring-specific conflict properties</returns>
        public static ConflictProperties CreateForRing(Ring ring)
        {
            return new ConflictProperties
            {
                ring = new List<Ring> { ring },
                forcedRing = ring.element,
                canSwitchRing = false
            };
        }

        /// <summary>
        /// Create conflict properties for a specific province
        /// </summary>
        /// <param name=\"province\">Province that must be attacked</param>
        /// <returns>Province-specific conflict properties</returns>
        public static ConflictProperties CreateForProvince(BaseCard province)
        {
            return new ConflictProperties
            {
                province = new List<BaseCard> { province },
                forcedProvince = province.location,
                canTargetAnyProvince = false
            };
        }

        /// <summary>
        /// Create duel conflict properties
        /// </summary>
        /// <param name=\"attacker\">Attacking character</param>
        /// <param name=\"defender\">Defending character</param>
        /// <returns>Duel conflict properties</returns>
        public static ConflictProperties CreateDuel(BaseCard attacker, BaseCard defender)
        {
            return new ConflictProperties
            {
                attacker = attacker,
                forcedAttackers = new List<BaseCard> { attacker },
                forcedDefenders = new List<BaseCard> { defender },
                minAttackers = 1,
                maxAttackers = 1,
                minDefenders = 1,
                maxDefenders = 1,
                skipActionWindow = true,
                mustDeclare = true,
                tags = new List<string> { "duel" }
            };
        }

        /// <summary>
        /// Create no-skill conflict properties
        /// </summary>
        /// <returns>No-skill conflict properties</returns>
        public static ConflictProperties CreateNoSkill()
        {
            return new ConflictProperties
            {
                noSkillComparison = true,
                skillModifier = 0,
                customSkillFunction = (card) => 0
            };
        }

        /// <summary>
        /// Create custom conflict properties with builder pattern
        /// </summary>
        /// <returns>Conflict properties builder</returns>
        public static ConflictPropertiesBuilder CreateCustom()
        {
            return new ConflictPropertiesBuilder();
        }
    }

    /// <summary>
    /// Builder class for creating custom conflict properties
    /// </summary>
    public class ConflictPropertiesBuilder
    {
        private ConflictProperties properties;

        public ConflictPropertiesBuilder()
        {
            properties = new ConflictProperties();
        }

        public ConflictPropertiesBuilder WithType(string conflictType)
        {
            properties.type = new List<string> { conflictType };
            return this;
        }

        public ConflictPropertiesBuilder WithTypes(params string[] conflictTypes)
        {
            properties.type = conflictTypes.ToList();
            return this;
        }

        public ConflictPropertiesBuilder WithForcedType(string forcedType)
        {
            properties.forcedDeclaredType = forcedType;
            return this;
        }

        public ConflictPropertiesBuilder WithRing(Ring ring)
        {
            properties.ring = new List<Ring> { ring };
            return this;
        }

        public ConflictPropertiesBuilder WithForcedRing(string ringElement)
        {
            properties.forcedRing = ringElement;
            return this;
        }

        public ConflictPropertiesBuilder WithAttacker(BaseCard attacker)
        {
            properties.attacker = attacker;
            properties.forcedAttackers.Add(attacker);
            return this;
        }

        public ConflictPropertiesBuilder WithForcedAttackers(params BaseCard[] attackers)
        {
            properties.forcedAttackers.AddRange(attackers);
            return this;
        }

        public ConflictPropertiesBuilder WithMinAttackers(int min)
        {
            properties.minAttackers = min;
            return this;
        }

        public ConflictPropertiesBuilder WithMaxAttackers(int max)
        {
            properties.maxAttackers = max;
            return this;
        }

        public ConflictPropertiesBuilder WithForcedDefenders(params BaseCard[] defenders)
        {
            properties.forcedDefenders.AddRange(defenders);
            return this;
        }

        public ConflictPropertiesBuilder WithMinDefenders(int min)
        {
            properties.minDefenders = min;
            return this;
        }

        public ConflictPropertiesBuilder WithMaxDefenders(int max)
        {
            properties.maxDefenders = max;
            return this;
        }

        public ConflictPropertiesBuilder AsUnopposed()
        {
            properties.forceUnopposed = true;
            properties.maxDefenders = 0;
            return this;
        }

        public ConflictPropertiesBuilder WithSkillModifier(int modifier)
        {
            properties.skillModifier = modifier;
            return this;
        }

        public ConflictPropertiesBuilder WithCustomSkillFunction(System.Func<BaseCard, int> skillFunction)
        {
            properties.customSkillFunction = skillFunction;
            return this;
        }

        public ConflictPropertiesBuilder AsForced()
        {
            properties.mustDeclare = true;
            properties.cannotPass = true;
            return this;
        }

        public ConflictPropertiesBuilder WithNoSkillComparison()
        {
            properties.noSkillComparison = true;
            return this;
        }

        public ConflictPropertiesBuilder WithTag(string tag)
        {
            properties.AddTag(tag);
            return this;
        }

        public ConflictPropertiesBuilder WithCustomProperty(string key, object value)
        {
            properties.SetCustomProperty(key, value);
            return this;
        }

        public ConflictProperties Build()
        {
            return properties.CreateCopy();
        }
    }

    /// <summary>
    /// Extension methods for conflict properties
    /// </summary>
    public static class ConflictPropertiesExtensions
    {
        /// <summary>
        /// Check if conflict properties allow a specific combination
        /// </summary>
        /// <param name="properties">Conflict properties</param>
        /// <param name="conflictType">Conflict type</param>
        /// <param name="ring">Ring</param>
        /// <param name="province">Province</param>
        /// <returns>True if combination is allowed</returns>
        public static bool AllowsCombination(this ConflictProperties properties, 
                                            string conflictType, Ring ring, BaseCard province)
        {
            return properties.IsTypeAllowed(conflictType) && 
                   properties.IsRingAllowed(ring, ring.game) && 
                   properties.IsProvinceAllowed(province, province.controller);
        }

        /// <summary>
        /// Get all valid combinations for conflict declaration
        /// </summary>
        /// <param name="properties">Conflict properties</param>
        /// <param name="game">Game instance</param>
        /// <param name="attackingPlayer">Attacking player</param>
        /// <returns>List of valid combinations</returns>
        public static List<ConflictCombination> GetValidCombinations(this ConflictProperties properties, 
                                                                     Game game, Player attackingPlayer)
        {
            var combinations = new List<ConflictCombination>();
            var allowedTypes = properties.GetAllowedTypes();
            var allowedRings = properties.GetAllowedRings(game);
            var allowedProvinces = attackingPlayer.opponent != null ? 
                properties.GetAllowedProvinces(attackingPlayer.opponent) : new List<BaseCard>();

            foreach (var type in allowedTypes)
            {
                if (attackingPlayer.GetConflictOpportunities(type) <= 0) continue;

                foreach (var ring in allowedRings)
                {
                    if (!ring.CanDeclare(attackingPlayer)) continue;

                    foreach (var province in allowedProvinces)
                    {
                        if (!province.CanDeclare(type, ring)) continue;

                        combinations.Add(new ConflictCombination
                        {
                            conflictType = type,
                            ring = ring,
                            province = province,
                            isValid = true
                        });
                    }
                }
            }

            return combinations;
        }

        /// <summary>
        /// Apply conflict properties to an existing conflict
        /// </summary>
        /// <param name="properties">Properties to apply</param>
        /// <param name="conflict">Conflict to modify</param>
        public static void ApplyToConflict(this ConflictProperties properties, Conflict conflict)
        {
            if (properties.forceUnopposed)
            {
                conflict.conflictUnopposed = true;
            }

            if (properties.skillModifier != 0)
            {
                // This would need to integrate with the conflict's skill calculation
            }

            if (properties.customSkillFunction != null)
            {
                // This would need to modify the conflict's skill calculation method
            }

            // Apply tags to conflict
            foreach (var tag in properties.tags)
            {
                // This would add tags to the conflict if it supported them
            }
        }

        /// <summary>
        /// Merge two conflict properties
        /// </summary>
        /// <param name="primary">Primary properties</param>
        /// <param name="secondary">Secondary properties to merge</param>
        /// <returns>Merged properties</returns>
        public static ConflictProperties MergeWith(this ConflictProperties primary, ConflictProperties secondary)
        {
            var merged = primary.CreateCopy();

            // Merge types (intersection)
            if (secondary.type.Count > 0)
            {
                merged.type = merged.type.Intersect(secondary.type).ToList();
            }

            // Override forced properties
            if (!string.IsNullOrEmpty(secondary.forcedDeclaredType))
            {
                merged.forcedDeclaredType = secondary.forcedDeclaredType;
            }

            if (!string.IsNullOrEmpty(secondary.forcedRing))
            {
                merged.forcedRing = secondary.forcedRing;
            }

            if (!string.IsNullOrEmpty(secondary.forcedProvince))
            {
                merged.forcedProvince = secondary.forcedProvince;
            }

            // Merge attacker requirements
            merged.forcedAttackers.AddRange(secondary.forcedAttackers);
            merged.additionalAttackers.AddRange(secondary.additionalAttackers);
            merged.minAttackers = Math.Max(merged.minAttackers, secondary.minAttackers);
            
            if (secondary.maxAttackers >= 0)
            {
                merged.maxAttackers = merged.maxAttackers >= 0 ? 
                    Math.Min(merged.maxAttackers, secondary.maxAttackers) : secondary.maxAttackers;
            }

            // Merge defender requirements
            merged.forcedDefenders.AddRange(secondary.forcedDefenders);
            merged.cannotDefend.AddRange(secondary.cannotDefend);
            merged.minDefenders = Math.Max(merged.minDefenders, secondary.minDefenders);
            
            if (secondary.maxDefenders >= 0)
            {
                merged.maxDefenders = merged.maxDefenders >= 0 ? 
                    Math.Min(merged.maxDefenders, secondary.maxDefenders) : secondary.maxDefenders;
            }

            // Merge special rules (logical OR for restrictions)
            merged.unopposed = merged.unopposed || secondary.unopposed;
            merged.forceUnopposed = merged.forceUnopposed || secondary.forceUnopposed;
            merged.noSkillComparison = merged.noSkillComparison || secondary.noSkillComparison;
            merged.cannotBeBroken = merged.cannotBeBroken || secondary.cannotBeBroken;
            merged.mustBreak = merged.mustBreak || secondary.mustBreak;
            merged.cannotResolveRingEffect = merged.cannotResolveRingEffect || secondary.cannotResolveRingEffect;
            merged.mustResolveRingEffect = merged.mustResolveRingEffect || secondary.mustResolveRingEffect;
            merged.cannotPass = merged.cannotPass || secondary.cannotPass;
            merged.mustDeclare = merged.mustDeclare || secondary.mustDeclare;
            merged.skipActionWindow = merged.skipActionWindow || secondary.skipActionWindow;
            merged.skipDefenderChoice = merged.skipDefenderChoice || secondary.skipDefenderChoice;

            // Merge skill modifier (additive)
            merged.skillModifier += secondary.skillModifier;

            // Override custom skill function with secondary if present
            if (secondary.customSkillFunction != null)
            {
                merged.customSkillFunction = secondary.customSkillFunction;
            }

            // Merge tags
            foreach (var tag in secondary.tags)
            {
                merged.AddTag(tag);
            }

            // Merge custom properties (secondary overrides primary)
            foreach (var kvp in secondary.customProperties)
            {
                merged.customProperties[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        /// <summary>
        /// Create a restricted version of conflict properties
        /// </summary>
        /// <param name="properties">Original properties</param>
        /// <param name="restriction">Restriction to apply</param>
        /// <returns>Restricted properties</returns>
        public static ConflictProperties RestrictBy(this ConflictProperties properties, ConflictRestriction restriction)
        {
            var restricted = properties.CreateCopy();

            switch (restriction.type)
            {
                case ConflictRestrictionType.ForbidType:
                    restricted.type.Remove(restriction.value as string);
                    break;

                case ConflictRestrictionType.RequireType:
                    restricted.type = new List<string> { restriction.value as string };
                    break;

                case ConflictRestrictionType.ForbidRing:
                    restricted.ring.RemoveAll(r => r.element == (restriction.value as string));
                    break;

                case ConflictRestrictionType.RequireRing:
                    var requiredRing = restriction.value as Ring;
                    if (requiredRing != null)
                    {
                        restricted.ring = new List<Ring> { requiredRing };
                    }
                    break;

                case ConflictRestrictionType.MaxAttackers:
                    restricted.maxAttackers = (int)restriction.value;
                    break;

                case ConflictRestrictionType.MinAttackers:
                    restricted.minAttackers = (int)restriction.value;
                    break;

                case ConflictRestrictionType.ForceUnopposed:
                    restricted.forceUnopposed = true;
                    restricted.maxDefenders = 0;
                    break;
            }

            return restricted;
        }
    }

    /// <summary>
    /// Represents a valid combination of conflict parameters
    /// </summary>
    [System.Serializable]
    public class ConflictCombination
    {
        public string conflictType;
        public Ring ring;
        public BaseCard province;
        public bool isValid;
        public List<string> invalidReasons = new List<string>();

        public override string ToString()
        {
            return $"{conflictType} conflict at {ring?.element} ring targeting {province?.name}";
        }
    }

    /// <summary>
    /// Types of conflict restrictions
    /// </summary>
    public enum ConflictRestrictionType
    {
        ForbidType,
        RequireType,
        ForbidRing,
        RequireRing,
        ForbidProvince,
        RequireProvince,
        MaxAttackers,
        MinAttackers,
        MaxDefenders,
        MinDefenders,
        ForceUnopposed,
        ForbidUnopposed
    }

    /// <summary>
    /// Represents a restriction on conflict declaration
    /// </summary>
    [System.Serializable]
    public class ConflictRestriction
    {
        public ConflictRestrictionType type;
        public object value;
        public string source;
        public int priority = 0;

        public ConflictRestriction(ConflictRestrictionType restrictionType, object restrictionValue, string restrictionSource = "")
        {
            type = restrictionType;
            value = restrictionValue;
            source = restrictionSource;
        }
    }

    /// <summary>
    /// Manager for conflict properties and restrictions
    /// </summary>
    public class ConflictPropertiesManager
    {
        private Game game;
        private List<ConflictRestriction> activeRestrictions = new List<ConflictRestriction>();
        private ConflictProperties baseProperties;

        public ConflictPropertiesManager(Game gameInstance)
        {
            game = gameInstance;
            baseProperties = new ConflictProperties();
        }

        /// <summary>
        /// Add a conflict restriction
        /// </summary>
        /// <param name="restriction">Restriction to add</param>
        public void AddRestriction(ConflictRestriction restriction)
        {
            activeRestrictions.Add(restriction);
            SortRestrictionsByPriority();
        }

        /// <summary>
        /// Remove a conflict restriction
        /// </summary>
        /// <param name="restriction">Restriction to remove</param>
        public void RemoveRestriction(ConflictRestriction restriction)
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
        /// Get the current effective conflict properties
        /// </summary>
        /// <param name="player">Player declaring the conflict</param>
        /// <returns>Effective conflict properties</returns>
        public ConflictProperties GetEffectiveProperties(Player player)
        {
            var effective = baseProperties.CreateCopy();

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
        public List<ConflictRestriction> GetActiveRestrictions()
        {
            return activeRestrictions.ToList();
        }
    }
}
