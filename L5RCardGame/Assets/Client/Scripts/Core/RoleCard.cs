using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// RoleCard represents special role cards that define player strategies and provide influence pools.
    /// Role cards have restricted actions and cannot be manipulated like normal cards.
    /// </summary>
    [System.Serializable]
    public class RoleCard : BaseCard
    {
        [Header("Role Properties")]
        public bool isRole = true;
        public int influenceModifier = 0;
        public int baseInfluencePool = 0;

        [Header("Role Restrictions")]
        public List<string> restrictedElements = new List<string>();
        public List<string> allowedClans = new List<string>();

        // Illegal actions that cannot be performed on role cards
        private static readonly HashSet<string> IllegalActions = new HashSet<string>
        {
            "bow", "ready", "dishonor", "honor", "sacrifice",
            "discardFromPlay", "moveToConflict", "sendHome", "putIntoPlay", "putIntoConflict",
            "break", "returnToHand", "placeFate", "removeFate"
        };

        public RoleCard(Player owner, CardData cardData) : base(owner.game, cardData)
        {
            this.owner = owner;
            this.controller = owner;
            
            InitializeRoleProperties(cardData);
        }

        #region Initialization
        private void InitializeRoleProperties(CardData cardData)
        {
            isRole = true;
            influenceModifier = 0;
            
            // Parse base influence pool from card data
            if (int.TryParse(cardData.influencePool?.ToString(), out int influence))
            {
                baseInfluencePool = influence;
            }

            // Initialize restricted elements and allowed clans from card data
            if (cardData.restrictedElements != null)
            {
                restrictedElements = cardData.restrictedElements.ToList();
            }

            if (cardData.allowedClans != null)
            {
                allowedClans = cardData.allowedClans.ToList();
            }
        }
        #endregion

        #region Influence System
        public int GetInfluence()
        {
            int totalInfluence = baseInfluencePool + influenceModifier;
            
            // Apply influence modification effects
            totalInfluence += SumEffects(EffectNames.ModifyInfluence);
            
            // Apply influence multiplier effects
            var multipliers = GetEffects(EffectNames.ModifyInfluenceMultiplier);
            foreach (var multiplier in multipliers)
            {
                totalInfluence = (int)(totalInfluence * multiplier.GetValue<float>(this));
            }

            return Mathf.Max(0, totalInfluence);
        }

        public void ModifyInfluence(int amount)
        {
            int oldInfluence = GetInfluence();
            influenceModifier += amount;
            int newInfluence = GetInfluence();

            if (oldInfluence != newInfluence)
            {
                ExecutePythonScript("on_influence_changed", oldInfluence, newInfluence, amount);
                
                // Notify the player of influence change
                owner?.OnInfluenceChanged(this, oldInfluence, newInfluence);
            }
        }

        public void SetInfluenceModifier(int modifier)
        {
            int oldInfluence = GetInfluence();
            influenceModifier = modifier;
            int newInfluence = GetInfluence();

            if (oldInfluence != newInfluence)
            {
                ExecutePythonScript("on_influence_set", oldInfluence, newInfluence);
                owner?.OnInfluenceChanged(this, oldInfluence, newInfluence);
            }
        }
        #endregion

        #region Role State Management
        public void FlipFaceup()
        {
            if (facedown)
            {
                facedown = false;
                ExecutePythonScript("on_role_revealed", this);
                
                // Apply role effects when revealed
                ApplyRoleEffects();
            }
        }

        private void ApplyRoleEffects()
        {
            // Apply any persistent effects from the role
            ExecutePythonScript("on_role_effects_applied", this);
            
            // Notify game of role being active
            game?.OnRoleActivated(owner, this);
        }

        public override void FlipFacedown()
        {
            if (!facedown)
            {
                facedown = true;
                ExecutePythonScript("on_role_hidden", this);
                
                // Remove role effects when hidden
                RemoveRoleEffects();
            }
        }

        private void RemoveRoleEffects()
        {
            ExecutePythonScript("on_role_effects_removed", this);
            game?.OnRoleDeactivated(owner, this);
        }
        #endregion

        #region Action Restrictions
        public override bool AllowGameAction(string actionType, AbilityContext context = null)
        {
            // Role cards cannot perform most standard card actions
            if (IllegalActions.Contains(actionType))
            {
                return false;
            }

            // Check for TakeControl effect name specifically
            if (actionType == EffectNames.TakeControl)
            {
                return false;
            }

            // Allow specific role-related actions
            var allowedRoleActions = new HashSet<string>
            {
                "reveal", "flip", "modifyInfluence", "applyRoleEffect"
            };

            if (allowedRoleActions.Contains(actionType))
            {
                return base.AllowGameAction(actionType, context);
            }

            return base.AllowGameAction(actionType, context);
        }

        public override bool CanBeTargeted(string actionType, AbilityContext context = null)
        {
            // Role cards generally cannot be targeted by effects
            var prohibitedTargeting = new HashSet<string>
            {
                "damage", "destroy", "discard", "move", "attach", "detach"
            };

            if (prohibitedTargeting.Contains(actionType))
            {
                return false;
            }

            return base.CanBeTargeted(actionType, context);
        }
        #endregion

        #region Element System
        public override List<string> GetElement()
        {
            // Role cards typically don't provide elements
            return new List<string>();
        }

        public bool IsElementRestricted(string element)
        {
            return restrictedElements.Contains(element);
        }

        public bool CanUseElement(string element)
        {
            return !IsElementRestricted(element);
        }
        #endregion

        #region Clan Restrictions
        public bool IsClanAllowed(string clan)
        {
            if (allowedClans.Count == 0) return true; // No restrictions
            return allowedClans.Contains(clan);
        }

        public bool CanPlayCard(BaseCard card)
        {
            // Check clan restrictions
            if (!IsClanAllowed(card.GetFaction()))
            {
                return false;
            }

            // Check influence cost if applicable
            if (card is DrawCard drawCard)
            {
                int influenceCost = drawCard.GetInfluenceCost();
                if (influenceCost > 0 && owner.GetAvailableInfluence() < influenceCost)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Special Role Abilities
        public virtual void OnGameStart()
        {
            ExecutePythonScript("on_game_start", this);
        }

        public virtual void OnTurnStart()
        {
            ExecutePythonScript("on_turn_start", this);
        }

        public virtual void OnTurnEnd()
        {
            ExecutePythonScript("on_turn_end", this);
        }

        public virtual void OnConflictDeclared(Conflict conflict)
        {
            ExecutePythonScript("on_conflict_declared", conflict);
        }

        public virtual void OnConflictResolved(Conflict conflict)
        {
            ExecutePythonScript("on_conflict_resolved", conflict);
        }

        public virtual void OnCardPlayed(BaseCard card)
        {
            if (CanPlayCard(card))
            {
                ExecutePythonScript("on_card_played", card);
            }
        }

        public virtual void OnHonorGained(int amount)
        {
            ExecutePythonScript("on_honor_gained", amount);
        }

        public virtual void OnHonorLost(int amount)
        {
            ExecutePythonScript("on_honor_lost", amount);
        }
        #endregion

        #region Menu System
        public override List<CardMenuOption> GetMenuOptions()
        {
            var options = new List<CardMenuOption>();

            // Role-specific menu options
            if (facedown)
            {
                options.Add(new CardMenuOption("reveal", "Reveal Role"));
            }
            else
            {
                options.Add(new CardMenuOption("hide", "Hide Role"));
            }

            if (influenceModifier != 0)
            {
                options.Add(new CardMenuOption("resetInfluence", "Reset Influence Modifier"));
            }

            // Debug options in editor
            #if UNITY_EDITOR
            options.Add(new CardMenuOption("addInfluence", "Add 1 Influence"));
            options.Add(new CardMenuOption("removeInfluence", "Remove 1 Influence"));
            #endif

            return options;
        }

        public override void ExecuteMenuCommand(string command, Player player)
        {
            switch (command)
            {
                case "reveal":
                    FlipFaceup();
                    game.AddMessage("{0} reveals their role: {1}", player.name, name);
                    break;
                    
                case "hide":
                    FlipFacedown();
                    game.AddMessage("{0} hides their role", player.name);
                    break;
                    
                case "resetInfluence":
                    SetInfluenceModifier(0);
                    game.AddMessage("{0} resets {1}'s influence modifier", player.name, name);
                    break;
                    
                case "addInfluence":
                    ModifyInfluence(1);
                    game.AddMessage("{0} adds 1 influence to {1}", player.name, name);
                    break;
                    
                case "removeInfluence":
                    ModifyInfluence(-1);
                    game.AddMessage("{0} removes 1 influence from {1}", player.name, name);
                    break;
                    
                default:
                    base.ExecuteMenuCommand(command, player);
                    break;
            }

            ExecutePythonScript("on_menu_command", command, player);
        }
        #endregion

        #region IronPython Integration
        protected override void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}_role.py";
                game.ExecuteCardScript(scriptName, methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for role {name}: {ex.Message}");
            }
        }

        protected T ExecutePythonFunction<T>(string functionName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}_role.py";
                return game.ExecuteCardFunction<T>(scriptName, functionName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python function {functionName} for role {name}: {ex.Message}");
                return default(T);
            }
        }

        // Role-specific Python events
        public void OnRoleSelected()
        {
            ExecutePythonScript("on_role_selected", this, owner);
        }

        public void OnInfluenceSpent(int amount, BaseCard targetCard)
        {
            ExecutePythonScript("on_influence_spent", amount, targetCard);
        }

        public void OnDeckBuilding(List<BaseCard> deck)
        {
            ExecutePythonScript("on_deck_building", deck);
        }
        #endregion

        #region Validation and Restrictions
        public bool ValidateRoleDeck(List<BaseCard> deck)
        {
            // Check clan restrictions
            foreach (var card in deck)
            {
                if (!CanPlayCard(card))
                {
                    Debug.LogWarning($"Card {card.name} violates role restrictions for {name}");
                    return false;
                }
            }

            // Check influence limits
            int totalInfluenceCost = deck.OfType<DrawCard>()
                                       .Sum(card => card.GetInfluenceCost());
            
            if (totalInfluenceCost > GetInfluence())
            {
                Debug.LogWarning($"Deck influence cost ({totalInfluenceCost}) exceeds role influence ({GetInfluence()})");
                return false;
            }

            // Execute Python validation
            return ExecutePythonFunction<bool>("validate_role_deck", deck);
        }

        public List<string> GetDeckValidationErrors(List<BaseCard> deck)
        {
            var errors = new List<string>();

            foreach (var card in deck)
            {
                if (!IsClanAllowed(card.GetFaction()))
                {
                    errors.Add($"{card.name} is not allowed by {name} (clan restriction)");
                }
            }

            int totalInfluenceCost = deck.OfType<DrawCard>()
                                       .Sum(card => card.GetInfluenceCost());
            
            if (totalInfluenceCost > GetInfluence())
            {
                errors.Add($"Total influence cost ({totalInfluenceCost}) exceeds available influence ({GetInfluence()})");
            }

            // Get Python validation errors
            var pythonErrors = ExecutePythonFunction<List<string>>("get_deck_validation_errors", deck) ?? new List<string>();
            errors.AddRange(pythonErrors);

            return errors;
        }
        #endregion

        #region Summary for Network/UI
        public override Dictionary<string, object> GetSummary(Player activePlayer, bool hideWhenFaceup = false)
        {
            var baseSummary = base.GetSummary(activePlayer, hideWhenFaceup);
            
            var summary = new Dictionary<string, object>(baseSummary)
            {
                {"isRole", isRole},
                {"location", location},
                {"influence", GetInfluence()},
                {"baseInfluence", baseInfluencePool},
                {"influenceModifier", influenceModifier},
                {"restrictedElements", restrictedElements.ToList()},
                {"allowedClans", allowedClans.ToList()},
                {"isRevealed", !facedown},
                {"canUseElements", GetAvailableElements()}
            };

            return summary;
        }

        private List<string> GetAvailableElements()
        {
            var allElements = new List<string> { "air", "earth", "fire", "water", "void" };
            return allElements.Where(element => !IsElementRestricted(element)).ToList();
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogRoleStatus()
        {
            Debug.Log($"Role Status for {name}:\n" +
                     $"Revealed: {!facedown}\n" +
                     $"Influence: {GetInfluence()} (Base: {baseInfluencePool}, Modifier: {influenceModifier})\n" +
                     $"Allowed Clans: {string.Join(", ", allowedClans)}\n" +
                     $"Restricted Elements: {string.Join(", ", restrictedElements)}\n" +
                     $"Owner: {owner?.name ?? "None"}");
        }

        public static List<string> GetAllIllegalActions()
        {
            return IllegalActions.ToList();
        }
        #endregion

        #region Role-Specific Effects
        public virtual bool CanPlayOutOfClan(BaseCard card)
        {
            return ExecutePythonFunction<bool>("can_play_out_of_clan", card);
        }

        public virtual int GetInfluenceDiscount(BaseCard card)
        {
            return ExecutePythonFunction<int>("get_influence_discount", card);
        }

        public virtual void ApplyRoleBonus(string bonusType, Dictionary<string, object> parameters)
        {
            ExecutePythonScript("apply_role_bonus", bonusType, parameters);
        }

        public virtual bool HasRoleAbility(string abilityName)
        {
            return ExecutePythonFunction<bool>("has_role_ability", abilityName);
        }
        #endregion
    }

    #region Supporting Enums and Classes
    [System.Serializable]
    public static class RoleTypes
    {
        public const string Keeper = "keeper";
        public const string Seeker = "seeker";
        public const string Support = "support";
    }

    [System.Serializable]
    public class RoleAbility
    {
        public string name;
        public string description;
        public bool isActive;
        public Dictionary<string, object> parameters;

        public RoleAbility(string name, string description)
        {
            this.name = name;
            this.description = description;
            this.isActive = false;
            this.parameters = new Dictionary<string, object>();
        }
    }
    #endregion
}