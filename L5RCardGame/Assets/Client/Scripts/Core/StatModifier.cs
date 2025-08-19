using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a modification to a card's statistics (skills, glory, etc.).
    /// Handles both temporary effects and permanent modifiers from various sources.
    /// </summary>
    [System.Serializable]
    public class StatModifier : IEquatable<StatModifier>, IComparable<StatModifier>
    {
        #region Fields

        [Header("Modifier Properties")]
        [SerializeField] private float amount;
        [SerializeField] private string name;
        [SerializeField] private bool countsAsBase;
        [SerializeField] private string type;
        [SerializeField] private bool overrides;

        [Header("Source Information")]
        [SerializeField] private object sourceEffect;
        [SerializeField] private BaseCard sourceCard;
        [SerializeField] private string sourceId;

        [Header("Application Details")]
        [SerializeField] private StatModifierCategory category;
        [SerializeField] private int priority;
        [SerializeField] private bool isTemporary;
        [SerializeField] private DateTime createdAt;

        #endregion

        #region Properties

        /// <summary>
        /// The numeric amount of the modification (can be positive or negative)
        /// </summary>
        public float Amount
        {
            get => amount;
            set => amount = value;
        }

        /// <summary>
        /// Human-readable name describing this modifier
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value ?? "";
        }

        /// <summary>
        /// Whether this modifier counts as part of the base value
        /// </summary>
        public bool CountsAsBase
        {
            get => countsAsBase;
            set => countsAsBase = value;
        }

        /// <summary>
        /// The type/category of this modifier (matches card types or special categories)
        /// </summary>
        public string Type
        {
            get => type;
            set => type = value ?? "";
        }

        /// <summary>
        /// Whether this modifier overrides other modifiers (typically used for "set" effects)
        /// </summary>
        public bool Overrides
        {
            get => overrides;
            set => overrides = value;
        }

        /// <summary>
        /// The effect that created this modifier
        /// </summary>
        public object SourceEffect
        {
            get => sourceEffect;
            set => sourceEffect = value;
        }

        /// <summary>
        /// The card that is the source of this modifier
        /// </summary>
        public BaseCard SourceCard
        {
            get => sourceCard;
            set => sourceCard = value;
        }

        /// <summary>
        /// Unique identifier for the source of this modifier
        /// </summary>
        public string SourceId
        {
            get => sourceId;
            set => sourceId = value ?? "";
        }

        /// <summary>
        /// Category of this modifier for grouping and priority purposes
        /// </summary>
        public StatModifierCategory Category
        {
            get => category;
            set => category = value;
        }

        /// <summary>
        /// Priority for applying modifiers (higher priority applied later)
        /// </summary>
        public int Priority
        {
            get => priority;
            set => priority = value;
        }

        /// <summary>
        /// Whether this is a temporary modifier that should be cleaned up
        /// </summary>
        public bool IsTemporary
        {
            get => isTemporary;
            set => isTemporary = value;
        }

        /// <summary>
        /// When this modifier was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => createdAt;
            set => createdAt = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public StatModifier()
        {
            name = "";
            type = "";
            sourceId = "";
            createdAt = DateTime.UtcNow;
            category = StatModifierCategory.Effect;
        }

        /// <summary>
        /// Primary constructor
        /// </summary>
        /// <param name="modifierAmount">Numeric amount of modification</param>
        /// <param name="modifierName">Human-readable name</param>
        /// <param name="doesOverride">Whether this modifier overrides others</param>
        /// <param name="modifierType">Type/category of modifier</param>
        public StatModifier(float modifierAmount, string modifierName, bool doesOverride = false, string modifierType = "")
        {
            amount = modifierAmount;
            name = modifierName ?? "";
            overrides = doesOverride;
            type = modifierType ?? "";
            sourceId = "";
            createdAt = DateTime.UtcNow;
            category = StatModifierCategory.Effect;
        }

        /// <summary>
        /// Constructor with category
        /// </summary>
        /// <param name="modifierAmount">Numeric amount of modification</param>
        /// <param name="modifierName">Human-readable name</param>
        /// <param name="modifierCategory">Category of modifier</param>
        /// <param name="doesOverride">Whether this modifier overrides others</param>
        /// <param name="modifierType">Type/category of modifier</param>
        public StatModifier(float modifierAmount, string modifierName, StatModifierCategory modifierCategory, bool doesOverride = false, string modifierType = "")
            : this(modifierAmount, modifierName, doesOverride, modifierType)
        {
            category = modifierCategory;
            priority = GetDefaultPriority(modifierCategory);
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a modifier from an effect (matches TypeScript fromEffect method)
        /// </summary>
        /// <param name="modifierAmount">Amount of modification</param>
        /// <param name="effect">Source effect</param>
        /// <param name="doesOverride">Whether this overrides other modifiers</param>
        /// <param name="modifierName">Optional custom name</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromEffect(float modifierAmount, object effect, bool doesOverride = false, string modifierName = null)
        {
            var effectName = GetEffectName(effect);
            var effectType = GetEffectType(effect);
            var finalName = modifierName ?? effectName;

            var modifier = new StatModifier(modifierAmount, finalName, doesOverride, effectType)
            {
                sourceEffect = effect,
                sourceId = GetEffectId(effect),
                category = StatModifierCategory.Effect
            };

            // Set source card if available
            var sourceCard = GetEffectSourceCard(effect);
            if (sourceCard != null)
            {
                modifier.sourceCard = sourceCard;
            }

            return modifier;
        }

        /// <summary>
        /// Create a modifier from a card (matches TypeScript fromCard method)
        /// </summary>
        /// <param name="modifierAmount">Amount of modification</param>
        /// <param name="card">Source card</param>
        /// <param name="modifierName">Name of the modifier</param>
        /// <param name="doesOverride">Whether this overrides other modifiers</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromCard(float modifierAmount, BaseCard card, string modifierName, bool doesOverride = false)
        {
            var cardType = GetCardType(card);
            
            var modifier = new StatModifier(modifierAmount, modifierName, doesOverride, cardType)
            {
                sourceCard = card,
                sourceId = card?.uuid ?? "",
                category = StatModifierCategory.Card
            };

            return modifier;
        }

        /// <summary>
        /// Create a modifier from a status token (matches TypeScript fromStatusToken method)
        /// </summary>
        /// <param name="modifierAmount">Amount of modification</param>
        /// <param name="modifierName">Name of the modifier</param>
        /// <param name="doesOverride">Whether this overrides other modifiers</param>
        /// <returns>New stat modifier</returns>
        public static StatModifier FromStatusToken(float modifierAmount, string modifierName, bool doesOverride = false)
        {
            var modifier = new StatModifier(modifierAmount, modifierName, doesOverride, "token")
            {
                category = StatModifierCategory.StatusToken,
                sourceId = $"token_{modifierName}_{DateTime.UtcNow.Ticks}"
            };

            return modifier;
        }

        /// <summary>
        /// Create a base/printed value modifier
        /// </summary>
        /// <param name="baseValue">Base value amount</param>
        /// <param name="card">Source card</param>
        /// <param name="statName">Name of the stat</param>
        /// <returns>New base stat modifier</returns>
        public static StatModifier FromBaseValue(float baseValue, BaseCard card, string statName)
        {
            var modifier = new StatModifier(baseValue, $"Printed {statName}", false, GetCardType(card))
            {
                sourceCard = card,
                sourceId = card?.uuid ?? "",
                category = StatModifierCategory.Base,
                countsAsBase = true
            };

            return modifier;
        }

        /// <summary>
        /// Create an equipment/attachment modifier
        /// </summary>
        /// <param name="modifierAmount">Amount of modification</param>
        /// <param name="equipment">Equipment/attachment card</param>
        /// <param name="modifierName">Name of the modifier</param>
        /// <returns>New equipment modifier</returns>
        public static StatModifier FromEquipment(float modifierAmount, BaseCard equipment, string modifierName)
        {
            var modifier = new StatModifier(modifierAmount, modifierName, false, CardTypes.Attachment)
            {
                sourceCard = equipment,
                sourceId = equipment?.uuid ?? "",
                category = StatModifierCategory.Equipment
            };

            return modifier;
        }

        /// <summary>
        /// Create a temporary modifier that expires
        /// </summary>
        /// <param name="modifierAmount">Amount of modification</param>
        /// <param name="modifierName">Name of the modifier</param>
        /// <param name="duration">How long the modifier lasts</param>
        /// <returns>New temporary modifier</returns>
        public static StatModifier CreateTemporary(float modifierAmount, string modifierName, TimeSpan? duration = null)
        {
            var modifier = new StatModifier(modifierAmount, modifierName, false, "temporary")
            {
                category = StatModifierCategory.Temporary,
                isTemporary = true,
                sourceId = $"temp_{DateTime.UtcNow.Ticks}"
            };

            return modifier;
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Get the name from an effect object (matches TypeScript getEffectName)
        /// </summary>
        /// <param name="effect">Effect object</param>
        /// <returns>Effect name or "Unknown"</returns>
        public static string GetEffectName(object effect)
        {
            if (effect == null) return "Unknown";

            try
            {
                // Try to get name from effect.context.source.name
                var effectType = effect.GetType();
                var contextProperty = effectType.GetProperty("context");
                
                if (contextProperty != null)
                {
                    var context = contextProperty.GetValue(effect);
                    if (context != null)
                    {
                        var contextType = context.GetType();
                        var sourceProperty = contextType.GetProperty("source");
                        
                        if (sourceProperty != null)
                        {
                            var source = sourceProperty.GetValue(context);
                            if (source != null)
                            {
                                var sourceType = source.GetType();
                                var nameProperty = sourceType.GetProperty("name") ?? sourceType.GetProperty("Name");
                                
                                if (nameProperty != null)
                                {
                                    var name = nameProperty.GetValue(source)?.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                        return name;
                                }
                            }
                        }
                    }
                }

                // Fallback: try direct name property
                var directNameProperty = effectType.GetProperty("name") ?? effectType.GetProperty("Name");
                if (directNameProperty != null)
                {
                    var name = directNameProperty.GetValue(effect)?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get effect name: {ex.Message}");
            }

            return "Unknown";
        }

        /// <summary>
        /// Get the type from an effect object (matches TypeScript getEffectType)
        /// </summary>
        /// <param name="effect">Effect object</param>
        /// <returns>Effect type or empty string</returns>
        public static string GetEffectType(object effect)
        {
            if (effect == null) return "";

            try
            {
                // Try to get type from effect.context.source.type
                var effectType = effect.GetType();
                var contextProperty = effectType.GetProperty("context");
                
                if (contextProperty != null)
                {
                    var context = contextProperty.GetValue(effect);
                    if (context != null)
                    {
                        var contextType = context.GetType();
                        var sourceProperty = contextType.GetProperty("source");
                        
                        if (sourceProperty != null)
                        {
                            var source = sourceProperty.GetValue(context);
                            if (source != null)
                            {
                                var sourceType = source.GetType();
                                var typeProperty = sourceType.GetProperty("type") ?? sourceType.GetProperty("Type");
                                
                                if (typeProperty != null)
                                {
                                    var type = typeProperty.GetValue(source)?.ToString();
                                    if (!string.IsNullOrEmpty(type))
                                        return type;
                                }
                            }
                        }
                    }
                }

                // Fallback: use the effect's class name
                return effectType.Name;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get effect type: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Get the card type from a card object (matches TypeScript getCardType)
        /// </summary>
        /// <param name="card">Card object</param>
        /// <returns>Card type or empty string</returns>
        public static string GetCardType(BaseCard card)
        {
            if (card == null) return "";

            try
            {
                return card.type ?? card.GetCardType() ?? "";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get card type: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Get unique ID from an effect
        /// </summary>
        /// <param name="effect">Effect object</param>
        /// <returns>Effect ID</returns>
        private static string GetEffectId(object effect)
        {
            if (effect == null) return "";

            try
            {
                var effectType = effect.GetType();
                var idProperty = effectType.GetProperty("id") ?? effectType.GetProperty("Id");
                
                if (idProperty != null)
                {
                    var id = idProperty.GetValue(effect)?.ToString();
                    if (!string.IsNullOrEmpty(id))
                        return id;
                }

                // Fallback: use hash code
                return effect.GetHashCode().ToString();
            }
            catch
            {
                return effect.GetHashCode().ToString();
            }
        }

        /// <summary>
        /// Get source card from an effect
        /// </summary>
        /// <param name="effect">Effect object</param>
        /// <returns>Source card or null</returns>
        private static BaseCard GetEffectSourceCard(object effect)
        {
            if (effect == null) return null;

            try
            {
                var effectType = effect.GetType();
                var contextProperty = effectType.GetProperty("context");
                
                if (contextProperty != null)
                {
                    var context = contextProperty.GetValue(effect);
                    if (context != null)
                    {
                        var contextType = context.GetType();
                        var sourceProperty = contextType.GetProperty("source");
                        
                        if (sourceProperty != null)
                        {
                            return sourceProperty.GetValue(context) as BaseCard;
                        }
                    }
                }

                // Try direct source property
                var directSourceProperty = effectType.GetProperty("source") ?? effectType.GetProperty("Source");
                if (directSourceProperty != null)
                {
                    return directSourceProperty.GetValue(effect) as BaseCard;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get effect source card: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get default priority for a modifier category
        /// </summary>
        /// <param name="modifierCategory">Category</param>
        /// <returns>Default priority</returns>
        private static int GetDefaultPriority(StatModifierCategory modifierCategory)
        {
            return modifierCategory switch
            {
                StatModifierCategory.Base => 0,
                StatModifierCategory.Card => 10,
                StatModifierCategory.Equipment => 20,
                StatModifierCategory.Effect => 30,
                StatModifierCategory.StatusToken => 40,
                StatModifierCategory.Temporary => 50,
                StatModifierCategory.Override => 100,
                _ => 30
            };
        }

        #endregion

        #region Application and Calculation

        /// <summary>
        /// Apply a list of modifiers to a base value
        /// </summary>
        /// <param name="baseValue">Starting value</param>
        /// <param name="modifiers">List of modifiers to apply</param>
        /// <param name="respectOverrides">Whether to respect override modifiers</param>
        /// <returns>Final calculated value</returns>
        public static float ApplyModifiers(float baseValue, IEnumerable<StatModifier> modifiers, bool respectOverrides = true)
        {
            if (modifiers == null) return baseValue;

            var modifierList = modifiers.ToList();

            // Handle overrides first
            if (respectOverrides)
            {
                var overrideModifiers = modifierList.Where(m => m.overrides).ToList();
                if (overrideModifiers.Count > 0)
                {
                    // Use the last override modifier
                    var lastOverride = overrideModifiers.OrderBy(m => m.createdAt).Last();
                    return lastOverride.amount;
                }
            }

            // Apply all non-override modifiers
            var applicableModifiers = modifierList.Where(m => !m.overrides || !respectOverrides);
            var sortedModifiers = applicableModifiers.OrderBy(m => m.priority).ThenBy(m => m.createdAt);

            float result = baseValue;
            foreach (var modifier in sortedModifiers)
            {
                result += modifier.amount;
            }

            return result;
        }

        /// <summary>
        /// Apply modifiers and return integer result (common for game stats)
        /// </summary>
        /// <param name="baseValue">Starting value</param>
        /// <param name="modifiers">List of modifiers to apply</param>
        /// <param name="minimumValue">Minimum allowed result</param>
        /// <param name="respectOverrides">Whether to respect override modifiers</param>
        /// <returns>Final calculated integer value</returns>
        public static int ApplyModifiersInt(float baseValue, IEnumerable<StatModifier> modifiers, int minimumValue = 0, bool respectOverrides = true)
        {
            var result = ApplyModifiers(baseValue, modifiers, respectOverrides);
            return Mathf.Max(minimumValue, Mathf.RoundToInt(result));
        }

        /// <summary>
        /// Get the total modifier amount from a list of modifiers
        /// </summary>
        /// <param name="modifiers">List of modifiers</param>
        /// <param name="excludeOverrides">Whether to exclude override modifiers</param>
        /// <returns>Total modifier amount</returns>
        public static float GetTotalModifierAmount(IEnumerable<StatModifier> modifiers, bool excludeOverrides = false)
        {
            if (modifiers == null) return 0f;

            var applicableModifiers = excludeOverrides ? 
                modifiers.Where(m => !m.overrides) : 
                modifiers;

            return applicableModifiers.Sum(m => m.amount);
        }

        #endregion

        #region Filtering and Grouping

        /// <summary>
        /// Filter modifiers by category
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="modifierCategory">Category to filter by</param>
        /// <returns>Filtered modifiers</returns>
        public static IEnumerable<StatModifier> FilterByCategory(IEnumerable<StatModifier> modifiers, StatModifierCategory modifierCategory)
        {
            return modifiers?.Where(m => m.category == modifierCategory) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Filter modifiers by type
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="modifierType">Type to filter by</param>
        /// <returns>Filtered modifiers</returns>
        public static IEnumerable<StatModifier> FilterByType(IEnumerable<StatModifier> modifiers, string modifierType)
        {
            return modifiers?.Where(m => m.type == modifierType) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Filter modifiers by source card
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="sourceCard">Source card to filter by</param>
        /// <returns>Filtered modifiers</returns>
        public static IEnumerable<StatModifier> FilterBySourceCard(IEnumerable<StatModifier> modifiers, BaseCard sourceCard)
        {
            return modifiers?.Where(m => m.sourceCard == sourceCard) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Group modifiers by their source
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <returns>Grouped modifiers</returns>
        public static Dictionary<string, List<StatModifier>> GroupBySource(IEnumerable<StatModifier> modifiers)
        {
            return modifiers?.GroupBy(m => m.sourceId)
                .ToDictionary(g => g.Key, g => g.ToList()) ?? new Dictionary<string, List<StatModifier>>();
        }

        #endregion

        #region IEquatable and IComparable Implementation

        /// <summary>
        /// Check equality with another StatModifier
        /// </summary>
        /// <param name="other">Other modifier</param>
        /// <returns>True if equal</returns>
        public bool Equals(StatModifier other)
        {
            if (other == null) return false;
            
            return amount.Equals(other.amount) &&
                   name == other.name &&
                   type == other.type &&
                   overrides == other.overrides &&
                   sourceId == other.sourceId;
        }

        /// <summary>
        /// Check equality with object
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>True if equal</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as StatModifier);
        }

        /// <summary>
        /// Get hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(amount, name, type, overrides, sourceId);
        }

        /// <summary>
        /// Compare with another StatModifier for sorting
        /// </summary>
        /// <param name="other">Other modifier</param>
        /// <returns>Comparison result</returns>
        public int CompareTo(StatModifier other)
        {
            if (other == null) return 1;
            
            // Compare by priority first
            var priorityComparison = priority.CompareTo(other.priority);
            if (priorityComparison != 0)
                return priorityComparison;
            
            // Then by creation time
            return createdAt.CompareTo(other.createdAt);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if this modifier affects a specific stat type
        /// </summary>
        /// <param name="statType">Stat type to check</param>
        /// <returns>True if this modifier applies</returns>
        public bool AppliesTo(string statType)
        {
            // This could be extended based on your specific stat system
            return !string.IsNullOrEmpty(statType);
        }

        /// <summary>
        /// Check if this modifier is still valid
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            // Basic validation - can be extended
            return !string.IsNullOrEmpty(name);
        }

        /// <summary>
        /// Create a copy of this modifier
        /// </summary>
        /// <returns>Copied modifier</returns>
        public StatModifier Clone()
        {
            return new StatModifier(amount, name, overrides, type)
            {
                sourceEffect = sourceEffect,
                sourceCard = sourceCard,
                sourceId = sourceId,
                category = category,
                priority = priority,
                isTemporary = isTemporary,
                countsAsBase = countsAsBase,
                createdAt = createdAt
            };
        }

        /// <summary>
        /// Get a human-readable description of this modifier
        /// </summary>
        /// <returns>Description string</returns>
        public string GetDescription()
        {
            var sign = amount >= 0 ? "+" : "";
            var overrideText = overrides ? " (overrides)" : "";
            return $"{sign}{amount} {name}{overrideText}";
        }

        /// <summary>
        /// Convert to string for debugging
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetDescription();
        }

        #endregion
    }

    #region Supporting Enums and Classes

    /// <summary>
    /// Categories for stat modifiers to control application order and grouping
    /// </summary>
    public enum StatModifierCategory
    {
        /// <summary>
        /// Base/printed values from cards
        /// </summary>
        Base = 0,
        
        /// <summary>
        /// Modifiers from the card itself
        /// </summary>
        Card = 1,
        
        /// <summary>
        /// Modifiers from equipment/attachments
        /// </summary>
        Equipment = 2,
        
        /// <summary>
        /// Modifiers from ongoing effects
        /// </summary>
        Effect = 3,
        
        /// <summary>
        /// Modifiers from status tokens (honored/dishonored)
        /// </summary>
        StatusToken = 4,
        
        /// <summary>
        /// Temporary modifiers (until end of turn, etc.)
        /// </summary>
        Temporary = 5,
        
        /// <summary>
        /// Override modifiers that replace other values
        /// </summary>
        Override = 10
    }

    /// <summary>
    /// Extension methods for working with StatModifier collections
    /// </summary>
    public static class StatModifierExtensions
    {
        /// <summary>
        /// Sum all modifier amounts
        /// </summary>
        /// <param name="modifiers">Modifiers to sum</param>
        /// <returns>Total amount</returns>
        public static float Sum(this IEnumerable<StatModifier> modifiers)
        {
            return modifiers?.Sum(m => m.Amount) ?? 0f;
        }

        /// <summary>
        /// Get modifiers from a specific source
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="sourceCard">Source card</param>
        /// <returns>Filtered modifiers</returns>
        public static IEnumerable<StatModifier> FromSource(this IEnumerable<StatModifier> modifiers, BaseCard sourceCard)
        {
            return StatModifier.FilterBySourceCard(modifiers, sourceCard);
        }

        /// <summary>
        /// Get modifiers of a specific category
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="category">Category to filter by</param>
        /// <returns>Filtered modifiers</returns>
        public static IEnumerable<StatModifier> OfCategory(this IEnumerable<StatModifier> modifiers, StatModifierCategory category)
        {
            return StatModifier.FilterByCategory(modifiers, category);
        }

        /// <summary>
        /// Get modifiers of a specific type
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="type">Type to filter by</param>
        /// <returns>Filtered modifiers</returns>
        public static IEnumerable<StatModifier> OfType(this IEnumerable<StatModifier> modifiers, string type)
        {
            return StatModifier.FilterByType(modifiers, type);
        }

        /// <summary>
        /// Get only override modifiers
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <returns>Override modifiers</returns>
        public static IEnumerable<StatModifier> Overrides(this IEnumerable<StatModifier> modifiers)
        {
            return modifiers?.Where(m => m.Overrides) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Get non-override modifiers
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <returns>Non-override modifiers</returns>
        public static IEnumerable<StatModifier> NonOverrides(this IEnumerable<StatModifier> modifiers)
        {
            return modifiers?.Where(m => !m.Overrides) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Apply all modifiers to a base value
        /// </summary>
        /// <param name="modifiers">Modifiers to apply</param>
        /// <param name="baseValue">Base value</param>
        /// <returns>Modified value</returns>
        public static float ApplyTo(this IEnumerable<StatModifier> modifiers, float baseValue)
        {
            return StatModifier.ApplyModifiers(baseValue, modifiers);
        }

        /// <summary>
        /// Apply all modifiers to a base value and return integer
        /// </summary>
        /// <param name="modifiers">Modifiers to apply</param>
        /// <param name="baseValue">Base value</param>
        /// <param name="minimumValue">Minimum allowed result</param>
        /// <returns>Modified integer value</returns>
        public static int ApplyToInt(this IEnumerable<StatModifier> modifiers, float baseValue, int minimumValue = 0)
        {
            return StatModifier.ApplyModifiersInt(baseValue, modifiers, minimumValue);
        }

        /// <summary>
        /// Remove expired temporary modifiers
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <param name="currentTime">Current time for comparison</param>
        /// <returns>Non-expired modifiers</returns>
        public static IEnumerable<StatModifier> RemoveExpired(this IEnumerable<StatModifier> modifiers, DateTime? currentTime = null)
        {
            var checkTime = currentTime ?? DateTime.UtcNow;
            
            return modifiers?.Where(m => 
                !m.IsTemporary || 
                (checkTime - m.CreatedAt).TotalMinutes < 60 // Example: temporary modifiers last 1 hour
            ) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Sort modifiers by application order
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <returns>Sorted modifiers</returns>
        public static IEnumerable<StatModifier> SortByApplicationOrder(this IEnumerable<StatModifier> modifiers)
        {
            return modifiers?.OrderBy(m => m.Priority).ThenBy(m => m.CreatedAt) ?? Enumerable.Empty<StatModifier>();
        }

        /// <summary>
        /// Get debug information for a collection of modifiers
        /// </summary>
        /// <param name="modifiers">Source modifiers</param>
        /// <returns>Debug string</returns>
        public static string GetDebugInfo(this IEnumerable<StatModifier> modifiers)
        {
            if (modifiers == null) return "No modifiers";

            var modifierList = modifiers.ToList();
            if (modifierList.Count == 0) return "No modifiers";

            var lines = new List<string>
            {
                $"Stat Modifiers ({modifierList.Count}):"
            };

            foreach (var modifier in modifierList.SortByApplicationOrder())
            {
                var categoryName = modifier.Category.ToString();
                var typeInfo = !string.IsNullOrEmpty(modifier.Type) ? $" ({modifier.Type})" : "";
                var overrideInfo = modifier.Overrides ? " [OVERRIDE]" : "";
                var tempInfo = modifier.IsTemporary ? " [TEMP]" : "";
                
                lines.Add($"  {modifier.GetDescription()}{typeInfo}{overrideInfo}{tempInfo} [{categoryName}]");
            }

            var total = modifierList.Sum();
            lines.Add($"Total Modifier: {(total >= 0 ? "+" : "")}{total}");

            return string.Join("\n", lines);
        }
    }

    #endregion

    #region Specialized Modifier Types

    /// <summary>
    /// Specialized modifier for skill values with military/political distinctions
    /// </summary>
    [System.Serializable]
    public class SkillModifier : StatModifier
    {
        [SerializeField] private SkillType skillType;
        [SerializeField] private bool affectsBothSkills;

        /// <summary>
        /// Type of skill this modifier affects
        /// </summary>
        public SkillType SkillType
        {
            get => skillType;
            set => skillType = value;
        }

        /// <summary>
        /// Whether this modifier affects both military and political skills
        /// </summary>
        public bool AffectsBothSkills
        {
            get => affectsBothSkills;
            set => affectsBothSkills = value;
        }

        /// <summary>
        /// Constructor for skill modifier
        /// </summary>
        public SkillModifier(float amount, string name, SkillType type, bool affectsBoth = false)
            : base(amount, name)
        {
            skillType = type;
            affectsBothSkills = affectsBoth;
        }

        /// <summary>
        /// Check if this modifier applies to a specific skill type
        /// </summary>
        /// <param name="targetSkillType">Skill type to check</param>
        /// <returns>True if applies</returns>
        public bool AppliesTo(SkillType targetSkillType)
        {
            return affectsBothSkills || skillType == targetSkillType;
        }

        /// <summary>
        /// Create a military skill modifier
        /// </summary>
        public static SkillModifier Military(float amount, string name, bool overrides = false)
        {
            return new SkillModifier(amount, name, SkillType.Military)
            {
                Overrides = overrides
            };
        }

        /// <summary>
        /// Create a political skill modifier
        /// </summary>
        public static SkillModifier Political(float amount, string name, bool overrides = false)
        {
            return new SkillModifier(amount, name, SkillType.Political)
            {
                Overrides = overrides
            };
        }

        /// <summary>
        /// Create a modifier that affects both skills
        /// </summary>
        public static SkillModifier Both(float amount, string name, bool overrides = false)
        {
            return new SkillModifier(amount, name, SkillType.Military, true)
            {
                Overrides = overrides
            };
        }
    }

    /// <summary>
    /// Specialized modifier for honor status effects
    /// </summary>
    [System.Serializable]
    public class HonorStatusModifier : StatModifier
    {
        [SerializeField] private bool isHonored;
        [SerializeField] private bool isDishonored;
        [SerializeField] private bool canBeReversed;
        [SerializeField] private bool canBeNullified;

        /// <summary>
        /// Whether this is from honored status
        /// </summary>
        public bool IsHonored
        {
            get => isHonored;
            set => isHonored = value;
        }

        /// <summary>
        /// Whether this is from dishonored status
        /// </summary>
        public bool IsDishonored
        {
            get => isDishonored;
            set => isDishonored = value;
        }

        /// <summary>
        /// Whether this modifier can be reversed by effects
        /// </summary>
        public bool CanBeReversed
        {
            get => canBeReversed;
            set => canBeReversed = value;
        }

        /// <summary>
        /// Whether this modifier can be nullified by effects
        /// </summary>
        public bool CanBeNullified
        {
            get => canBeNullified;
            set => canBeNullified = value;
        }

        /// <summary>
        /// Constructor for honor status modifier
        /// </summary>
        public HonorStatusModifier(float amount, bool honored, bool dishonored)
            : base(amount, honored ? "Honored" : "Dishonored", false, "token")
        {
            isHonored = honored;
            isDishonored = dishonored;
            canBeReversed = true;
            canBeNullified = true;
            Category = StatModifierCategory.StatusToken;
        }

        /// <summary>
        /// Create honored status modifier
        /// </summary>
        public static HonorStatusModifier Honored(float amount = 1)
        {
            return new HonorStatusModifier(amount, true, false);
        }

        /// <summary>
        /// Create dishonored status modifier
        /// </summary>
        public static HonorStatusModifier Dishonored(float amount = -1)
        {
            return new HonorStatusModifier(amount, false, true);
        }

        /// <summary>
        /// Apply honor status effects (nullification, reversal)
        /// </summary>
        /// <param name="hasNullifyEffect">Whether nullification is active</param>
        /// <param name="hasReverseEffect">Whether reversal is active</param>
        /// <param name="effectName">Name of the effect causing the change</param>
        public void ApplyHonorStatusEffects(bool hasNullifyEffect, bool hasReverseEffect, string effectName = "")
        {
            if (hasNullifyEffect && canBeNullified)
            {
                var originalAmount = Amount;
                Amount = 0;
                Name += $" ({effectName})";
                Debug.Log($"Honor status modifier nullified: {originalAmount} -> {Amount}");
            }
            else if (hasReverseEffect && canBeReversed)
            {
                Amount = -Amount;
                Name += $" ({effectName})";
                Debug.Log($"Honor status modifier reversed: {Name}");
            }
        }
    }

    /// <summary>
    /// Modifier that applies differently based on game conditions
    /// </summary>
    [System.Serializable]
    public class ConditionalModifier : StatModifier
    {
        [SerializeField] private string conditionDescription;
        
        private Func<bool> condition;
        private float conditionalAmount;
        private float baseAmount;

        /// <summary>
        /// Description of the condition
        /// </summary>
        public string ConditionDescription
        {
            get => conditionDescription;
            set => conditionDescription = value;
        }

        /// <summary>
        /// Constructor for conditional modifier
        /// </summary>
        public ConditionalModifier(float baseAmt, float conditionalAmt, string name, Func<bool> conditionFunc, string conditionDesc)
            : base(baseAmt, name)
        {
            baseAmount = baseAmt;
            conditionalAmount = conditionalAmt;
            condition = conditionFunc;
            conditionDescription = conditionDesc;
        }

        /// <summary>
        /// Get the current amount based on condition
        /// </summary>
        /// <returns>Effective amount</returns>
        public float GetEffectiveAmount()
        {
            if (condition?.Invoke() == true)
            {
                return conditionalAmount;
            }
            return baseAmount;
        }

        /// <summary>
        /// Update the stored amount based on current conditions
        /// </summary>
        public void UpdateAmount()
        {
            Amount = GetEffectiveAmount();
        }
    }

    /// <summary>
    /// Modifier that decays over time
    /// </summary>
    [System.Serializable]
    public class DecayingModifier : StatModifier
    {
        [SerializeField] private float initialAmount;
        [SerializeField] private float decayRate;
        [SerializeField] private DateTime startTime;

        /// <summary>
        /// Initial amount when created
        /// </summary>
        public float InitialAmount
        {
            get => initialAmount;
            set => initialAmount = value;
        }

        /// <summary>
        /// Rate of decay per minute
        /// </summary>
        public float DecayRate
        {
            get => decayRate;
            set => decayRate = value;
        }

        /// <summary>
        /// When decay started
        /// </summary>
        public DateTime StartTime
        {
            get => startTime;
            set => startTime = value;
        }

        /// <summary>
        /// Constructor for decaying modifier
        /// </summary>
        public DecayingModifier(float initial, float decay, string name)
            : base(initial, name)
        {
            initialAmount = initial;
            decayRate = decay;
            startTime = DateTime.UtcNow;
            IsTemporary = true;
        }

        /// <summary>
        /// Update amount based on time elapsed
        /// </summary>
        public void UpdateDecay()
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMinutes;
            var decayedAmount = initialAmount - (decayRate * (float)elapsed);
            Amount = Mathf.Max(0, decayedAmount);
        }

        /// <summary>
        /// Check if this modifier has completely decayed
        /// </summary>
        /// <returns>True if fully decayed</returns>
        public bool IsFullyDecayed()
        {
            UpdateDecay();
            return Amount <= 0;
        }
    }

    #endregion

    #region Supporting Enums

    /// <summary>
    /// Types of skills in the L5R card game
    /// </summary>
    public enum SkillType
    {
        Military,
        Political
    }

    #endregion

    #region Modifier Calculation Utilities

    /// <summary>
    /// Utility class for complex modifier calculations
    /// </summary>
    public static class ModifierCalculator
    {
        /// <summary>
        /// Calculate final skill value with L5R-specific rules
        /// </summary>
        /// <param name="baseSkill">Base skill value</param>
        /// <param name="modifiers">All applicable modifiers</param>
        /// <param name="skillType">Type of skill being calculated</param>
        /// <returns>Final skill value</returns>
        public static int CalculateSkill(float baseSkill, IEnumerable<StatModifier> modifiers, SkillType skillType)
        {
            if (modifiers == null) return Mathf.Max(0, (int)baseSkill);

            var modifierList = modifiers.ToList();

            // Handle skill-specific filtering
            var skillModifiers = modifierList.OfType<SkillModifier>()
                .Where(sm => sm.AppliesTo(skillType))
                .Cast<StatModifier>()
                .Concat(modifierList.Where(m => !(m is SkillModifier)));

            // Apply honor status effects
            var honorModifiers = skillModifiers.OfType<HonorStatusModifier>().ToList();
            ApplyHonorStatusEffects(honorModifiers);

            // Calculate final value
            var result = StatModifier.ApplyModifiersInt(baseSkill, skillModifiers);
            
            // Handle dash (null) values
            if (float.IsNaN(baseSkill))
                return 0;

            return result;
        }

        /// <summary>
        /// Apply honor status effects to honor modifiers
        /// </summary>
        /// <param name="honorModifiers">Honor status modifiers</param>
        private static void ApplyHonorStatusEffects(List<HonorStatusModifier> honorModifiers)
        {
            // This would integrate with your game's effect system
            // For now, it's a placeholder for the actual implementation
            foreach (var modifier in honorModifiers)
            {
                // Check for nullify/reverse effects and apply them
                // modifier.ApplyHonorStatusEffects(hasNullify, hasReverse, effectName);
            }
        }

        /// <summary>
        /// Calculate glory value with L5R-specific rules
        /// </summary>
        /// <param name="baseGlory">Base glory value</param>
        /// <param name="modifiers">All applicable modifiers</param>
        /// <returns>Final glory value</returns>
        public static int CalculateGlory(float baseGlory, IEnumerable<StatModifier> modifiers)
        {
            if (float.IsNaN(baseGlory) || baseGlory == 0)
                return 0;

            var result = StatModifier.ApplyModifiersInt(baseGlory, modifiers);
            return Mathf.Max(0, result);
        }

        /// <summary>
        /// Calculate province strength with L5R-specific rules
        /// </summary>
        /// <param name="baseStrength">Base province strength</param>
        /// <param name="strengthModifiers">Strength modifiers</param>
        /// <param name="isBroken">Whether province is broken</param>
        /// <returns>Final province strength</returns>
        public static int CalculateProvinceStrength(float baseStrength, IEnumerable<StatModifier> strengthModifiers, bool isBroken)
        {
            if (isBroken)
                return 0;

            return StatModifier.ApplyModifiersInt(baseStrength, strengthModifiers);
        }
    }

    #endregion

    #region Debug and Testing Utilities

    /// <summary>
    /// Utilities for debugging and testing modifiers
    /// </summary>
    public static class ModifierDebugUtils
    {
        /// <summary>
        /// Create a test suite of modifiers for debugging
        /// </summary>
        /// <returns>Test modifiers</returns>
        public static List<StatModifier> CreateTestModifiers()
        {
            return new List<StatModifier>
            {
                StatModifier.FromBaseValue(3, null, "military skill"),
                StatModifier.FromStatusToken(1, "Honored"),
                StatModifier.FromStatusToken(-1, "Dishonored"),
                StatModifier.CreateTemporary(2, "Temporary boost"),
                new StatModifier(5, "Override effect", true, "effect"),
                SkillModifier.Both(1, "Both skills bonus"),
                HonorStatusModifier.Honored(1),
                new ConditionalModifier(0, 2, "Conditional", () => true, "When condition is met")
            };
        }

        /// <summary>
        /// Test modifier application with various scenarios
        /// </summary>
        public static void RunModifierTests()
        {
            Debug.Log("=== Modifier Tests ===");
            
            var testModifiers = CreateTestModifiers();
            var baseValue = 2f;
            
            Debug.Log($"Base Value: {baseValue}");
            Debug.Log("Modifiers:");
            foreach (var modifier in testModifiers)
            {
                Debug.Log($"  {modifier}");
            }
            
            var result = StatModifier.ApplyModifiersInt(baseValue, testModifiers);
            Debug.Log($"Final Result: {result}");
            
            // Test with overrides
            var resultWithOverrides = StatModifier.ApplyModifiersInt(baseValue, testModifiers, respectOverrides: true);
            Debug.Log($"Result with Overrides: {resultWithOverrides}");
            
            // Test without overrides
            var resultWithoutOverrides = StatModifier.ApplyModifiersInt(baseValue, testModifiers, respectOverrides: false);
            Debug.Log($"Result without Overrides: {resultWithoutOverrides}");
            
            Debug.Log("=== End Tests ===");
        }

        /// <summary>
        /// Validate modifier consistency
        /// </summary>
        /// <param name="modifiers">Modifiers to validate</param>
        /// <returns>Validation results</returns>
        public static List<string> ValidateModifiers(IEnumerable<StatModifier> modifiers)
        {
            var issues = new List<string>();
            
            if (modifiers == null)
            {
                issues.Add("Modifier list is null");
                return issues;
            }
            
            var modifierList = modifiers.ToList();
            
            // Check for duplicate source IDs
            var duplicateSources = modifierList
                .Where(m => !string.IsNullOrEmpty(m.SourceId))
                .GroupBy(m => m.SourceId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            
            foreach (var sourceId in duplicateSources)
            {
                issues.Add($"Duplicate source ID: {sourceId}");
            }
            
            // Check for invalid modifiers
            var invalidModifiers = modifierList.Where(m => !m.IsValid()).ToList();
            foreach (var invalid in invalidModifiers)
            {
                issues.Add($"Invalid modifier: {invalid.Name}");
            }
            
            // Check for conflicting overrides
            var overrides = modifierList.Where(m => m.Overrides).ToList();
            if (overrides.Count > 1)
            {
                issues.Add($"Multiple override modifiers found: {string.Join(", ", overrides.Select(o => o.Name))}");
            }
            
            return issues;
        }
    }

    #endregion
}
