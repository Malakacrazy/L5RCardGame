using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a status token that can be placed on cards to modify their properties.
    /// Status tokens include honor/dishonor tokens, fate tokens, and various other markers.
    /// </summary>
    [System.Serializable]
    public class StatusToken
    {
        [Header("Token Identity")]
        public string uuid;
        public string tokenType;
        public string name;
        public string description;

        [Header("Token State")]
        public BaseCard attachedCard;
        public Player attachedPlayer;
        public Ring attachedRing;
        public Province attachedProvince;
        public int value = 1;

        [Header("Token Properties")]
        public bool isPersistent = false;
        public bool isVisible = true;
        public bool canBeRemoved = true;
        public bool stackable = true;
        public UnityEngine.Color tokenColor = UnityEngine.Color.white;

        [Header("Effects")]
        public List<TokenEffect> effects = new List<TokenEffect>();
        public Dictionary<string, object> modifiers = new Dictionary<string, object>();

        [Header("Lifecycle")]
        public Player owner;
        public Game game;
        public DateTime createdAt;
        public DateTime lastModified;
        public int timesActivated = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public StatusToken()
        {
            uuid = Guid.NewGuid().ToString();
            createdAt = DateTime.Now;
            lastModified = DateTime.Now;
            effects = new List<TokenEffect>();
            modifiers = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor with token type
        /// </summary>
        /// <param name=\"type\">Type of token</param>
        /// <param name=\"tokenOwner\">Player who owns the token</param>
        /// <param name=\"gameInstance\">Game instance</param>
        public StatusToken(string type, Player tokenOwner, Game gameInstance)
        {
            uuid = Guid.NewGuid().ToString();
            tokenType = type;
            name = GetTokenName(type);
            description = GetTokenDescription(type);
            owner = tokenOwner;
            game = gameInstance;
            createdAt = DateTime.Now;
            lastModified = DateTime.Now;
            effects = new List<TokenEffect>();
            modifiers = new Dictionary<string, object>();

            // Set token properties based on type
            SetTokenProperties(type);

            Debug.Log($"🏷️ StatusToken '{name}' created by {tokenOwner?.name ?? "unknown"}");
        }

        /// <summary>
        /// Constructor with specific value
        /// </summary>
        /// <param name=\"type\">Type of token</param>
        /// <param name=\"tokenValue\">Value of the token</param>
        /// <param name=\"tokenOwner\">Player who owns the token</param>
        /// <param name=\"gameInstance\">Game instance</param>
        public StatusToken(string type, int tokenValue, Player tokenOwner, Game gameInstance) 
            : this(type, tokenOwner, gameInstance)
        {
            value = tokenValue;
        }

        /// <summary>
        /// Get the display name for a token type
        /// </summary>
        /// <param name=\"type\">Token type</param>
        /// <returns>Display name</returns>
        private string GetTokenName(string type)
        {
            switch (type?.ToLower())
            {
                case TokenTypes.Honor: return "Honor Token";
                case TokenTypes.Dishonor: return "Dishonor Token";
                case TokenTypes.Fate: return "Fate Token";
                case TokenTypes.Military: return "Military Token";
                case TokenTypes.Political: return "Political Token";
                case TokenTypes.Strength: return "Strength Token";
                default: return $"{type} Token";
            }
        }

        /// <summary>
        /// Get the description for a token type
        /// </summary>
        /// <param name=\"type\">Token type</param>
        /// <returns>Description</returns>
        private string GetTokenDescription(string type)
        {
            switch (type?.ToLower())
            {
                case TokenTypes.Honor: 
                    return "This character gains +1 to both military and political skill";
                case TokenTypes.Dishonor: 
                    return "This character gets -1 to both military and political skill and cannot be honored";
                case TokenTypes.Fate: 
                    return "Fate token that prevents character from being discarded during fate phase";
                case TokenTypes.Military: 
                    return "This character gains +1 military skill";
                case TokenTypes.Political: 
                    return "This character gains +1 political skill";
                case TokenTypes.Strength: 
                    return "This province gains +1 strength";
                default: 
                    return $"A {type} token with various effects";
            }
        }

        /// <summary>
        /// Set token properties based on type
        /// </summary>
        /// <param name=\"type\">Token type</param>
        private void SetTokenProperties(string type)
        {
            switch (type?.ToLower())
            {
                case TokenTypes.Honor:
                    tokenColor = new UnityEngine.Color(1.0f, 0.84f, 0.0f); // Gold
                    isPersistent = true;
                    canBeRemoved = false;
                    AddEffect(new TokenEffect
                    {
                        effectType = "modifySkill",
                        militaryModifier = 1,
                        politicalModifier = 1,
                        description = "+1 military and political skill"
                    });
                    AddEffect(new TokenEffect
                    {
                        effectType = "preventDishonor",
                        description = "Cannot be dishonored while honored"
                    });
                    break;

                case TokenTypes.Dishonor:
                    tokenColor = new UnityEngine.Color(0.5f, 0.0f, 0.5f); // Purple
                    isPersistent = true;
                    canBeRemoved = false;
                    AddEffect(new TokenEffect
                    {
                        effectType = "modifySkill",
                        militaryModifier = -1,
                        politicalModifier = -1,
                        description = "-1 military and political skill"
                    });
                    AddEffect(new TokenEffect
                    {
                        effectType = "preventHonor",
                        description = "Cannot be honored while dishonored"
                    });
                    break;

                case TokenTypes.Fate:
                    tokenColor = new UnityEngine.Color(0.0f, 0.8f, 1.0f); // Cyan
                    isPersistent = false;
                    canBeRemoved = true;
                    stackable = true;
                    AddEffect(new TokenEffect
                    {
                        effectType = "preventDiscard",
                        description = "Prevents character from being discarded during fate phase"
                    });
                    break;

                case TokenTypes.Military:
                    tokenColor = new UnityEngine.Color(0.8f, 0.2f, 0.2f); // Red
                    isPersistent = false;
                    canBeRemoved = true;
                    stackable = true;
                    AddEffect(new TokenEffect
                    {
                        effectType = "modifyMilitarySkill",
                        militaryModifier = 1,
                        description = "+1 military skill"
                    });
                    break;

                case TokenTypes.Political:
                    tokenColor = new UnityEngine.Color(0.2f, 0.2f, 0.8f); // Blue
                    isPersistent = false;
                    canBeRemoved = true;
                    stackable = true;
                    AddEffect(new TokenEffect
                    {
                        effectType = "modifyPoliticalSkill",
                        politicalModifier = 1,
                        description = "+1 political skill"
                    });
                    break;

                case TokenTypes.Strength:
                    tokenColor = new UnityEngine.Color(0.6f, 0.4f, 0.2f); // Brown
                    isPersistent = false;
                    canBeRemoved = true;
                    stackable = true;
                    AddEffect(new TokenEffect
                    {
                        effectType = "modifyProvinceStrength",
                        strengthModifier = 1,
                        description = "+1 province strength"
                    });
                    break;

                default:
                    tokenColor = UnityEngine.Color.gray;
                    break;
            }
        }

        /// <summary>
        /// Add an effect to this token
        /// </summary>
        /// <param name=\"effect\">Effect to add</param>
        public void AddEffect(TokenEffect effect)
        {
            if (effect != null)
            {
                effects.Add(effect);
                lastModified = DateTime.Now;
            }
        }

        /// <summary>
        /// Remove an effect from this token
        /// </summary>
        /// <param name=\"effect\">Effect to remove</param>
        public void RemoveEffect(TokenEffect effect)
        {
            if (effects.Remove(effect))
            {
                lastModified = DateTime.Now;
            }
        }

        /// <summary>
        /// Attach this token to a card
        /// </summary>
        /// <param name=\"card\">Card to attach to</param>
        /// <returns>True if successfully attached</returns>
        public bool AttachToCard(BaseCard card)
        {
            if (card == null) return false;

            // Check if already attached to this card
            if (attachedCard == card) return true;

            // Detach from current target
            Detach();

            // Check if card can accept this token type
            if (!CanAttachToCard(card)) return false;

            // Handle non-stackable tokens
            if (!stackable)
            {
                var existingToken = card.GetTokens(tokenType).FirstOrDefault();
                if (existingToken != null)
                {
                    existingToken.Remove();
                }
            }

            // Attach to card
            attachedCard = card;
            card.AddStatusToken(this);
            ApplyEffects();

            // Trigger events
            game?.EmitEvent(EventNames.OnAddTokenToCard, new Dictionary<string, object>
            {
                {"card", card},
                {"token", this},
                {"player", owner}
            });

            lastModified = DateTime.Now;
            Debug.Log($"🏷️ Token '{name}' attached to {card.name}");
            return true;
        }

        /// <summary>
        /// Attach this token to a player
        /// </summary>
        /// <param name=\"player\">Player to attach to</param>
        /// <returns>True if successfully attached</returns>
        public bool AttachToPlayer(Player player)
        {
            if (player == null) return false;

            // Check if already attached to this player
            if (attachedPlayer == player) return true;

            // Detach from current target
            Detach();

            // Attach to player
            attachedPlayer = player;
            player.AddStatusToken(this);
            ApplyEffects();

            lastModified = DateTime.Now;
            Debug.Log($"🏷️ Token '{name}' attached to player {player.name}");
            return true;
        }

        /// <summary>
        /// Attach this token to a ring
        /// </summary>
        /// <param name=\"ring\">Ring to attach to</param>
        /// <returns>True if successfully attached</returns>
        public bool AttachToRing(Ring ring)
        {
            if (ring == null) return false;

            // Check if already attached to this ring
            if (attachedRing == ring) return true;

            // Detach from current target
            Detach();

            // Attach to ring
            attachedRing = ring;
            ring.AddStatusToken(this);
            ApplyEffects();

            lastModified = DateTime.Now;
            Debug.Log($"🏷️ Token '{name}' attached to {ring.element} ring");
            return true;
        }

        /// <summary>
        /// Detach this token from whatever it's attached to
        /// </summary>
        public void Detach()
        {
            if (attachedCard != null)
            {
                attachedCard.RemoveStatusToken(this);
                attachedCard = null;
            }

            if (attachedPlayer != null)
            {
                attachedPlayer.RemoveStatusToken(this);
                attachedPlayer = null;
            }

            if (attachedRing != null)
            {
                attachedRing.RemoveStatusToken(this);
                attachedRing = null;
            }

            if (attachedProvince != null)
            {
                attachedProvince.RemoveStatusToken(this);
                attachedProvince = null;
            }

            UnapplyEffects();
            lastModified = DateTime.Now;
        }

        /// <summary>
        /// Remove this token completely
        /// </summary>
        public void Remove()
        {
            if (!canBeRemoved)
            {
                Debug.LogWarning($"🏷️ Cannot remove token '{name}' - removal not allowed");
                return;
            }

            // Trigger events before removal
            game?.EmitEvent(EventNames.OnStatusTokenDiscarded, new Dictionary<string, object>
            {
                {"token", this},
                {"card", attachedCard},
                {"player", attachedPlayer},
                {"ring", attachedRing},
                {"owner", owner}
            });

            Detach();

            Debug.Log($"🏷️ Token '{name}' removed");
        }

        /// <summary>
        /// Check if this token can attach to a specific card
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <returns>True if can attach</returns>
        public bool CanAttachToCard(BaseCard card)
        {
            if (card == null) return false;

            // Check card type restrictions
            switch (tokenType?.ToLower())
            {
                case TokenTypes.Honor:
                case TokenTypes.Dishonor:
                case TokenTypes.Military:
                case TokenTypes.Political:
                case TokenTypes.Fate:
                    return card.type == CardTypes.Character;

                case TokenTypes.Strength:
                    return card.type == CardTypes.Province || card.type == CardTypes.Stronghold;

                default:
                    return true; // Custom tokens can attach to any card by default
            }
        }

        /// <summary>
        /// Apply the effects of this token
        /// </summary>
        private void ApplyEffects()
        {
            foreach (var effect in effects)
            {
                effect.Apply(this);
            }
            timesActivated++;
        }

        /// <summary>
        /// Unapply the effects of this token
        /// </summary>
        private void UnapplyEffects()
        {
            foreach (var effect in effects)
            {
                effect.Unapply(this);
            }
        }

        /// <summary>
        /// Get the current target of this token
        /// </summary>
        /// <returns>Target object or null</returns>
        public object GetTarget()
        {
            if (attachedCard != null) return attachedCard;
            if (attachedPlayer != null) return attachedPlayer;
            if (attachedRing != null) return attachedRing;
            if (attachedProvince != null) return attachedProvince;
            return null;
        }

        /// <summary>
        /// Check if this token is attached to something
        /// </summary>
        /// <returns>True if attached</returns>
        public bool IsAttached()
        {
            return GetTarget() != null;
        }

        /// <summary>
        /// Get the skill modifier this token provides
        /// </summary>
        /// <param name=\"skillType\">Type of skill to check</param>
        /// <returns>Skill modifier value</returns>
        public int GetSkillModifier(string skillType)
        {
            int modifier = 0;
            foreach (var effect in effects)
            {
                modifier += effect.GetSkillModifier(skillType);
            }
            return modifier * value; // Multiply by token value for stacking
        }

        /// <summary>
        /// Get the strength modifier this token provides
        /// </summary>
        /// <returns>Strength modifier value</returns>
        public int GetStrengthModifier()
        {
            int modifier = 0;
            foreach (var effect in effects)
            {
                modifier += effect.strengthModifier;
            }
            return modifier * value;
        }

        /// <summary>
        /// Check if this token prevents a specific action
        /// </summary>
        /// <param name=\"action\">Action to check</param>
        /// <returns>True if token prevents the action</returns>
        public bool PreventsAction(string action)
        {
            return effects.Any(effect => effect.PreventsAction(action));
        }

        /// <summary>
        /// Move this token to another target
        /// </summary>
        /// <param name=\"newTarget\">New target for the token</param>
        /// <returns>True if successfully moved</returns>
        public bool MoveTo(object newTarget)
        {
            var oldTarget = GetTarget();

            bool success = newTarget switch
            {
                BaseCard card => AttachToCard(card),
                Player player => AttachToPlayer(player),
                Ring ring => AttachToRing(ring),
                Province province => AttachToProvince(province),
                _ => false
            };

            if (success)
            {
                game?.EmitEvent(EventNames.OnStatusTokenMoved, new Dictionary<string, object>
                {
                    {"token", this},
                    {"oldTarget", oldTarget},
                    {"newTarget", newTarget},
                    {"player", owner}
                });
            }

            return success;
        }

        /// <summary>
        /// Attach this token to a province
        /// </summary>
        /// <param name=\"province\">Province to attach to</param>
        /// <returns>True if successfully attached</returns>
        public bool AttachToProvince(Province province)
        {
            if (province == null) return false;

            // Check if already attached to this province
            if (attachedProvince == province) return true;

            // Detach from current target
            Detach();

            // Attach to province
            attachedProvince = province;
            province.AddStatusToken(this);
            ApplyEffects();

            lastModified = DateTime.Now;
            Debug.Log($"🏷️ Token '{name}' attached to province {province.name}");
            return true;
        }

        /// <summary>
        /// Create a copy of this token
        /// </summary>
        /// <param name=\"newOwner\">Owner of the new token</param>
        /// <returns>Copied token</returns>
        public StatusToken CreateCopy(Player newOwner = null)
        {
            var copy = new StatusToken(tokenType, newOwner ?? owner, game)
            {
                value = value,
                isPersistent = isPersistent,
                isVisible = isVisible,
                canBeRemoved = canBeRemoved,
                stackable = stackable,
                tokenColor = tokenColor,
                modifiers = new Dictionary<string, object>(modifiers)
            };

            // Copy effects
            foreach (var effect in effects)
            {
                copy.AddEffect(effect.CreateCopy());
            }

            return copy;
        }

        /// <summary>
        /// Get token summary for UI display
        /// </summary>
        /// <param name=\"viewingPlayer\">Player viewing the token</param>
        /// <returns>Token summary</returns>
        public StatusTokenSummary GetSummary(Player viewingPlayer)
        {
            bool canView = isVisible || viewingPlayer == owner;

            var summary = new StatusTokenSummary
            {
                uuid = uuid,
                tokenType = tokenType,
                name = name,
                description = description,
                value = canView ? value : 0,
                isVisible = isVisible,
                canBeRemoved = canBeRemoved,
                stackable = stackable,
                tokenColor = tokenColor,
                isPersistent = isPersistent,
                isAttached = IsAttached(),
                targetType = GetTarget()?.GetType().Name,
                ownerName = owner?.name ?? "Unknown",
                createdAt = createdAt,
                timesActivated = timesActivated
            };

            if (canView)
            {
                summary.effects = effects.Select(e => e.GetSummary()).ToList();
                summary.modifiers = modifiers;
            }

            return summary;
        }

        /// <summary>
        /// Get token statistics
        /// </summary>
        /// <returns>Token statistics</returns>
        public StatusTokenStatistics GetStatistics()
        {
            return new StatusTokenStatistics
            {
                tokenType = tokenType,
                value = value,
                timesActivated = timesActivated,
                effectCount = effects.Count,
                militaryModifier = GetSkillModifier("military"),
                politicalModifier = GetSkillModifier("political"),
                strengthModifier = GetStrengthModifier(),
                isPersistent = isPersistent,
                canBeRemoved = canBeRemoved,
                createdAt = createdAt,
                lastModified = lastModified
            };
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>Token description</returns>
        public override string ToString()
        {
            string target = "";
            if (IsAttached())
            {
                var targetObj = GetTarget();
                target = $" on {targetObj}";
            }

            string valueStr = value > 1 ? $" (x{value})" : "";
            return $"{name}{valueStr}{target}";
        }
    }

    /// <summary>
    /// Represents an effect that a status token can have
    /// </summary>
    [System.Serializable]
    public class TokenEffect
    {
        public string effectType;
        public string description;
        public int militaryModifier = 0;
        public int politicalModifier = 0;
        public int strengthModifier = 0;
        public int honorModifier = 0;
        public int fateModifier = 0;
        public List<string> preventedActions = new List<string>();
        public List<string> grantedAbilities = new List<string>();
        public Dictionary<string, object> customProperties = new Dictionary<string, object>();

        /// <summary>
        /// Apply this effect
        /// </summary>
        /// <param name=\"token\">Token applying the effect</param>
        public virtual void Apply(StatusToken token)
        {
            var target = token.GetTarget();
            
            if (target is BaseCard card)
            {
                ApplyToCard(card, token);
            }
            else if (target is Player player)
            {
                ApplyToPlayer(player, token);
            }
            else if (target is Ring ring)
            {
                ApplyToRing(ring, token);
            }
        }

        /// <summary>
        /// Unapply this effect
        /// </summary>
        /// <param name=\"token\">Token unapplying the effect</param>
        public virtual void Unapply(StatusToken token)
        {
            var target = token.GetTarget();
            
            if (target is BaseCard card)
            {
                UnapplyFromCard(card, token);
            }
            else if (target is Player player)
            {
                UnapplyFromPlayer(player, token);
            }
            else if (target is Ring ring)
            {
                UnapplyFromRing(ring, token);
            }
        }

        /// <summary>
        /// Apply effect to a card
        /// </summary>
        /// <param name=\"card\">Card to apply to</param>
        /// <param name=\"token\">Token applying the effect</param>
        protected virtual void ApplyToCard(BaseCard card, StatusToken token)
        {
            // This would integrate with the existing effect system
            // For now, we'll use the token tracking on cards
        }

        /// <summary>
        /// Unapply effect from a card
        /// </summary>
        /// <param name=\"card\">Card to unapply from</param>
        /// <param name=\"token\">Token unapplying the effect</param>
        protected virtual void UnapplyFromCard(BaseCard card, StatusToken token)
        {
            // This would integrate with the existing effect system
        }

        /// <summary>
        /// Apply effect to a player
        /// </summary>
        /// <param name=\"player\">Player to apply to</param>
        /// <param name=\"token\">Token applying the effect</param>
        protected virtual void ApplyToPlayer(Player player, StatusToken token)
        {
            // Apply player-level effects
        }

        /// <summary>
        /// Unapply effect from a player
        /// </summary>
        /// <param name=\"player\">Player to unapply from</param>
        /// <param name=\"token\">Token unapplying the effect</param>
        protected virtual void UnapplyFromPlayer(Player player, StatusToken token)
        {
            // Unapply player-level effects
        }

        /// <summary>
        /// Apply effect to a ring
        /// </summary>
        /// <param name=\"ring\">Ring to apply to</param>
        /// <param name=\"token\">Token applying the effect</param>
        protected virtual void ApplyToRing(Ring ring, StatusToken token)
        {
            // Apply ring-level effects
        }

        /// <summary>
        /// Unapply effect from a ring
        /// </summary>
        /// <param name=\"ring\">Ring to unapply from</param>
        /// <param name=\"token\">Token unapplying the effect</param>
        protected virtual void UnapplyFromRing(Ring ring, StatusToken token)
        {
            // Unapply ring-level effects
        }

        /// <summary>
        /// Get skill modifier for a specific skill type
        /// </summary>
        /// <param name=\"skillType\">Type of skill</param>
        /// <returns>Modifier value</returns>
        public int GetSkillModifier(string skillType)
        {
            return skillType?.ToLower() switch
            {
                "military" => militaryModifier,
                "political" => politicalModifier,
                _ => 0
            };
        }

        /// <summary>
        /// Check if this effect prevents a specific action
        /// </summary>
        /// <param name=\"action\">Action to check</param>
        /// <returns>True if prevents the action</returns>
        public bool PreventsAction(string action)
        {
            return preventedActions.Contains(action);
        }

        /// <summary>
        /// Create a copy of this effect
        /// </summary>
        /// <returns>Copied effect</returns>
        public TokenEffect CreateCopy()
        {
            return new TokenEffect
            {
                effectType = effectType,
                description = description,
                militaryModifier = militaryModifier,
                politicalModifier = politicalModifier,
                strengthModifier = strengthModifier,
                honorModifier = honorModifier,
                fateModifier = fateModifier,
                preventedActions = new List<string>(preventedActions),
                grantedAbilities = new List<string>(grantedAbilities),
                customProperties = new Dictionary<string, object>(customProperties)
            };
        }

        /// <summary>
        /// Get effect summary
        /// </summary>
        /// <returns>Effect summary</returns>
        public object GetSummary()
        {
            return new
            {
                effectType = effectType,
                description = description,
                militaryModifier = militaryModifier,
                politicalModifier = politicalModifier,
                strengthModifier = strengthModifier,
                preventedActions = preventedActions,
                grantedAbilities = grantedAbilities
            };
        }
    }

    /// <summary>
    /// Summary data for status token UI display
    /// </summary>
    [System.Serializable]
    public class StatusTokenSummary
    {
        public string uuid;
        public string tokenType;
        public string name;
        public string description;
        public int value;
        public bool isVisible;
        public bool canBeRemoved;
        public bool stackable;
        public UnityEngine.Color tokenColor;
        public bool isPersistent;
        public bool isAttached;
        public string targetType;
        public string ownerName;
        public DateTime createdAt;
        public int timesActivated;
        public List<object> effects;
        public Dictionary<string, object> modifiers;
    }

    /// <summary>
    /// Statistics for status token analysis
    /// </summary>
    [System.Serializable]
    public class StatusTokenStatistics
    {
        public string tokenType;
        public int value;
        public int timesActivated;
        public int effectCount;
        public int militaryModifier;
        public int politicalModifier;
        public int strengthModifier;
        public bool isPersistent;
        public bool canBeRemoved;
        public DateTime createdAt;
        public DateTime lastModified;
    }

    /// <summary>
    /// Extension methods for status token management
    /// </summary>
    public static class StatusTokenExtensions
    {
        /// <summary>
        /// Add a status token to a card
        /// </summary>
        /// <param name=\"card\">Card to add token to</param>
        /// <param name=\"token\">Token to add</param>
        public static void AddStatusToken(this BaseCard card, StatusToken token)
        {
            if (!card.tokens.ContainsKey(token.tokenType))
            {
                card.tokens[token.tokenType] = 0;
            }
            card.tokens[token.tokenType] += token.value;
        }

        /// <summary>
        /// Remove a status token from a card
        /// </summary>
        /// <param name=\"card\">Card to remove token from</param>
        /// <param name=\"token\">Token to remove</param>
        public static void RemoveStatusToken(this BaseCard card, StatusToken token)
        {
            if (card.tokens.ContainsKey(token.tokenType))
            {
                card.tokens[token.tokenType] -= token.value;
                if (card.tokens[token.tokenType] <= 0)
                {
                    card.tokens.Remove(token.tokenType);
                }
            }
        }

        /// <summary>
        /// Get tokens of a specific type on a card
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <param name=\"tokenType\">Type of token to get</param>
        /// <returns>List of matching tokens</returns>
        public static List<StatusToken> GetTokens(this BaseCard card, string tokenType)
        {
            // This would need to be implemented with a proper token tracking system
            // For now, return empty list
            return new List<StatusToken>();
        }

        /// <summary>
        /// Check if card has a specific token type
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <param name=\"tokenType\">Type of token to check for</param>
        /// <returns>True if card has the token</returns>
        public static bool HasToken(this BaseCard card, string tokenType)
        {
            return card.tokens.ContainsKey(tokenType) && card.tokens[tokenType] > 0;
        }

        /// <summary>
        /// Get token count of a specific type on a card
        /// </summary>
        /// <param name=\"card\">Card to check</param>
        /// <param name=\"tokenType\">Type of token to count</param>
        /// <returns>Number of tokens</returns>
        public static int GetTokenCount(this BaseCard card, string tokenType)
        {
            return card.tokens.GetValueOrDefault(tokenType, 0);
        }

        /// <summary>
        /// Add status token to player
        /// </summary>
        /// <param name=\"player\">Player to add token to</param>
        /// <param name=\"token\">Token to add</param>
        public static void AddStatusToken(this Player player, StatusToken token)
        {
            // This would need to be implemented with a proper player token tracking system
        }

        /// <summary>
        /// Remove status token from player
        /// </summary>
        /// <param name="player">Player to remove token from</param>
        /// <param name="token">Token to remove</param>
        public static void RemoveStatusToken(this Player player, StatusToken token)
        {
            // This would need to be implemented with a proper player token tracking system
        }

        /// <summary>
        /// Add status token to ring
        /// </summary>
        /// <param name="ring">Ring to add token to</param>
        /// <param name="token">Token to add</param>
        public static void AddStatusToken(this Ring ring, StatusToken token)
        {
            // This would need to be implemented with a proper ring token tracking system
        }

        /// <summary>
        /// Remove status token from ring
        /// </summary>
        /// <param name="ring">Ring to remove token from</param>
        /// <param name="token">Token to remove</param>
        public static void RemoveStatusToken(this Ring ring, StatusToken token)
        {
            // This would need to be implemented with a proper ring token tracking system
        }

        /// <summary>
        /// Add status token to province
        /// </summary>
        /// <param name="province">Province to add token to</param>
        /// <param name="token">Token to add</param>
        public static void AddStatusToken(this Province province, StatusToken token)
        {
            // This would need to be implemented with a proper province token tracking system
        }

        /// <summary>
        /// Remove status token from province
        /// </summary>
        /// <param name="province">Province to remove token from</param>
        /// <param name="token">Token to remove</param>
        public static void RemoveStatusToken(this Province province, StatusToken token)
        {
            // This would need to be implemented with a proper province token tracking system
        }

        /// <summary>
        /// Create an honor token
        /// </summary>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <returns>Honor token</returns>
        public static StatusToken CreateHonorToken(Player owner, Game game)
        {
            return new StatusToken(TokenTypes.Honor, owner, game);
        }

        /// <summary>
        /// Create a dishonor token
        /// </summary>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <returns>Dishonor token</returns>
        public static StatusToken CreateDishonorToken(Player owner, Game game)
        {
            return new StatusToken(TokenTypes.Dishonor, owner, game);
        }

        /// <summary>
        /// Create fate tokens
        /// </summary>
        /// <param name="owner">Owner of the tokens</param>
        /// <param name="game">Game instance</param>
        /// <param name="amount">Number of fate tokens</param>
        /// <returns>Fate token</returns>
        public static StatusToken CreateFateToken(Player owner, Game game, int amount = 1)
        {
            return new StatusToken(TokenTypes.Fate, amount, owner, game);
        }

        /// <summary>
        /// Create a military token
        /// </summary>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <param name="amount">Bonus amount</param>
        /// <returns>Military token</returns>
        public static StatusToken CreateMilitaryToken(Player owner, Game game, int amount = 1)
        {
            return new StatusToken(TokenTypes.Military, amount, owner, game);
        }

        /// <summary>
        /// Create a political token
        /// </summary>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <param name="amount">Bonus amount</param>
        /// <returns>Political token</returns>
        public static StatusToken CreatePoliticalToken(Player owner, Game game, int amount = 1)
        {
            return new StatusToken(TokenTypes.Political, amount, owner, game);
        }

        /// <summary>
        /// Create a strength token
        /// </summary>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <param name="amount">Bonus amount</param>
        /// <returns>Strength token</returns>
        public static StatusToken CreateStrengthToken(Player owner, Game game, int amount = 1)
        {
            return new StatusToken(TokenTypes.Strength, amount, owner, game);
        }

        /// <summary>
        /// Honor a character
        /// </summary>
        /// <param name="card">Character to honor</param>
        /// <param name="player">Player performing the action</param>
        /// <param name="game">Game instance</param>
        /// <returns>True if character was honored</returns>
        public static bool Honor(this BaseCard card, Player player, Game game)
        {
            if (card.type != CardTypes.Character)
            {
                Debug.LogWarning($"Cannot honor non-character card: {card.name}");
                return false;
            }

            if (card.HasToken(TokenTypes.Dishonor))
            {
                Debug.LogWarning($"Cannot honor dishonored character: {card.name}");
                return false;
            }

            if (card.HasToken(TokenTypes.Honor))
            {
                Debug.LogWarning($"Character {card.name} is already honored");
                return false;
            }

            var honorToken = StatusTokenExtensions.CreateHonorToken(player, game);
            bool success = honorToken.AttachToCard(card);

            if (success)
            {
                game?.EmitEvent(EventNames.OnCardHonored, new Dictionary<string, object>
                {
                    {"card", card},
                    {"player", player},
                    {"token", honorToken}
                });

                Debug.Log($"🌟 {card.name} has been honored");
            }

            return success;
        }

        /// <summary>
        /// Dishonor a character
        /// </summary>
        /// <param name="card">Character to dishonor</param>
        /// <param name="player">Player performing the action</param>
        /// <param name="game">Game instance</param>
        /// <returns>True if character was dishonored</returns>
        public static bool Dishonor(this BaseCard card, Player player, Game game)
        {
            if (card.type != CardTypes.Character)
            {
                Debug.LogWarning($"Cannot dishonor non-character card: {card.name}");
                return false;
            }

            if (card.HasToken(TokenTypes.Honor))
            {
                Debug.LogWarning($"Cannot dishonor honored character: {card.name}");
                return false;
            }

            if (card.HasToken(TokenTypes.Dishonor))
            {
                Debug.LogWarning($"Character {card.name} is already dishonored");
                return false;
            }

            var dishonorToken = StatusTokenExtensions.CreateDishonorToken(player, game);
            bool success = dishonorToken.AttachToCard(card);

            if (success)
            {
                game?.EmitEvent(EventNames.OnCardDishonored, new Dictionary<string, object>
                {
                    {"card", card},
                    {"player", player},
                    {"token", dishonorToken}
                });

                Debug.Log($"💀 {card.name} has been dishonored");
            }

            return success;
        }

        /// <summary>
        /// Check if character is honored
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if honored</returns>
        public static bool IsHonored(this BaseCard card)
        {
            return card.HasToken(TokenTypes.Honor);
        }

        /// <summary>
        /// Check if character is dishonored
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if dishonored</returns>
        public static bool IsDishonored(this BaseCard card)
        {
            return card.HasToken(TokenTypes.Dishonor);
        }

        /// <summary>
        /// Get total fate tokens on a character
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>Number of fate tokens</returns>
        public static int GetFateTokens(this BaseCard card)
        {
            return card.GetTokenCount(TokenTypes.Fate);
        }

        /// <summary>
        /// Add fate tokens to a character
        /// </summary>
        /// <param name="card">Card to add fate to</param>
        /// <param name="amount">Amount of fate to add</param>
        /// <param name="player">Player performing the action</param>
        /// <param name="game">Game instance</param>
        /// <returns>True if fate was added</returns>
        public static bool AddFateTokens(this BaseCard card, int amount, Player player, Game game)
        {
            if (amount <= 0) return false;

            var fateToken = StatusTokenExtensions.CreateFateToken(player, game, amount);
            bool success = fateToken.AttachToCard(card);

            if (success)
            {
                Debug.Log($"💰 Added {amount} fate to {card.name}");
            }

            return success;
        }

        /// <summary>
        /// Remove fate tokens from a character
        /// </summary>
        /// <param name="card">Card to remove fate from</param>
        /// <param name="amount">Amount of fate to remove</param>
        /// <returns>True if fate was removed</returns>
        public static bool RemoveFateTokens(this BaseCard card, int amount)
        {
            if (amount <= 0) return false;

            int currentFate = card.GetFateTokens();
            if (currentFate < amount)
            {
                amount = currentFate;
            }

            if (amount > 0)
            {
                card.RemoveToken(TokenTypes.Fate, amount);
                Debug.Log($"💰 Removed {amount} fate from {card.name}");
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Factory class for creating status tokens
    /// </summary>
    public static class StatusTokenFactory
    {
        /// <summary>
        /// Create a custom status token
        /// </summary>
        /// <param name="tokenType">Type of token</param>
        /// <param name="name">Display name</param>
        /// <param name="description">Token description</param>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <param name="value">Token value</param>
        /// <returns>Custom status token</returns>
        public static StatusToken CreateCustomToken(string tokenType, string name, string description, 
                                                  Player owner, Game game, int value = 1)
        {
            var token = new StatusToken(tokenType, value, owner, game)
            {
                name = name,
                description = description
            };

            return token;
        }

        /// <summary>
        /// Create a temporary token that expires after a certain duration
        /// </summary>
        /// <param name="tokenType">Type of token</param>
        /// <param name="duration">Duration in rounds</param>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <returns>Temporary status token</returns>
        public static StatusToken CreateTemporaryToken(string tokenType, int duration, Player owner, Game game)
        {
            var token = new StatusToken(tokenType, owner, game)
            {
                isPersistent = false,
                canBeRemoved = true
            };

            // Add a custom effect that removes the token after the specified duration
            token.AddEffect(new TemporaryTokenEffect(duration));

            return token;
        }

        /// <summary>
        /// Create a token with custom effects
        /// </summary>
        /// <param name="tokenType">Type of token</param>
        /// <param name="effects">List of effects</param>
        /// <param name="owner">Owner of the token</param>
        /// <param name="game">Game instance</param>
        /// <returns>Status token with custom effects</returns>
        public static StatusToken CreateTokenWithEffects(string tokenType, List<TokenEffect> effects, 
                                                        Player owner, Game game)
        {
            var token = new StatusToken(tokenType, owner, game);

            foreach (var effect in effects)
            {
                token.AddEffect(effect);
            }

            return token;
        }
    }

    /// <summary>
    /// Temporary token effect that removes the token after a certain duration
    /// </summary>
    public class TemporaryTokenEffect : TokenEffect
    {
        private int roundsRemaining;
        private int initialDuration;

        public TemporaryTokenEffect(int duration)
        {
            effectType = "temporary";
            description = $"Expires after {duration} round(s)";
            roundsRemaining = duration;
            initialDuration = duration;
        }

        public override void Apply(StatusToken token)
        {
            base.Apply(token);

            // Subscribe to round end events to track duration
            if (token.game != null)
            {
                token.game.EmitEvent("onRoundEnd", null); // This would need proper event subscription
            }
        }

        public void OnRoundEnd()
        {
            roundsRemaining--;
            if (roundsRemaining <= 0)
            {
                // Remove the token
                // This would need access to the token instance
            }
        }
    }

    /// <summary>
    /// Manager class for handling status tokens in the game
    /// </summary>
    public class StatusTokenManager
    {
        private Game game;
        private List<StatusToken> allTokens = new List<StatusToken>();
        private Dictionary<string, List<StatusToken>> tokensByType = new Dictionary<string, List<StatusToken>>();

        public StatusTokenManager(Game gameInstance)
        {
            game = gameInstance;
        }

        /// <summary>
        /// Register a token with the manager
        /// </summary>
        /// <param name="token">Token to register</param>
        public void RegisterToken(StatusToken token)
        {
            if (!allTokens.Contains(token))
            {
                allTokens.Add(token);

                if (!tokensByType.ContainsKey(token.tokenType))
                {
                    tokensByType[token.tokenType] = new List<StatusToken>();
                }
                tokensByType[token.tokenType].Add(token);
            }
        }

        /// <summary>
        /// Unregister a token from the manager
        /// </summary>
        /// <param name="token">Token to unregister</param>
        public void UnregisterToken(StatusToken token)
        {
            allTokens.Remove(token);

            if (tokensByType.ContainsKey(token.tokenType))
            {
                tokensByType[token.tokenType].Remove(token);
                if (tokensByType[token.tokenType].Count == 0)
                {
                    tokensByType.Remove(token.tokenType);
                }
            }
        }

        /// <summary>
        /// Get all tokens of a specific type
        /// </summary>
        /// <param name="tokenType">Type of token</param>
        /// <returns>List of matching tokens</returns>
        public List<StatusToken> GetTokensByType(string tokenType)
        {
            return tokensByType.GetValueOrDefault(tokenType, new List<StatusToken>());
        }

        /// <summary>
        /// Get all tokens owned by a player
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>List of tokens owned by the player</returns>
        public List<StatusToken> GetTokensByOwner(Player player)
        {
            return allTokens.Where(token => token.owner == player).ToList();
        }

        /// <summary>
        /// Get all active tokens in the game
        /// </summary>
        /// <returns>List of all active tokens</returns>
        public List<StatusToken> GetAllActiveTokens()
        {
            return allTokens.Where(token => token.IsAttached()).ToList();
        }

        /// <summary>
        /// Clean up expired or invalid tokens
        /// </summary>
        public void CleanupTokens()
        {
            var tokensToRemove = allTokens.Where(token => !token.IsAttached() && !token.isPersistent).ToList();

            foreach (var token in tokensToRemove)
            {
                UnregisterToken(token);
            }

            Debug.Log($"🏷️ Cleaned up {tokensToRemove.Count} expired tokens");
        }

        /// <summary>
        /// Get statistics about all tokens in the game
        /// </summary>
        /// <returns>Token statistics</returns>
        public Dictionary<string, object> GetTokenStatistics()
        {
            return new Dictionary<string, object>
            {
                {"totalTokens", allTokens.Count},
                {"activeTokens", GetAllActiveTokens().Count},
                {"tokensByType", tokensByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)},
                {"persistentTokens", allTokens.Count(t => t.isPersistent)},
                {"temporaryTokens", allTokens.Count(t => !t.isPersistent)}
            };
        }
    }
}
