using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// StrongholdCard represents the player's home stronghold which provides starting resources,
    /// honor, influence, and province strength. It can be bowed to generate additional effects.
    /// </summary>
    [System.Serializable]
    public class StrongholdCard : BaseCard
    {
        [Header("Stronghold Properties")]
        public bool isStronghold = true;
        public bool bowed = false;

        [Header("Starting Resources")]
        public int startingFate = 0;
        public int startingHonor = 0;
        public int influencePool = 0;
        public int provinceStrengthBonus = 0;

        [Header("Stronghold Abilities")]
        public bool canProduceResources = true;
        public List<string> strongholdTraits = new List<string>();

        // Menu options for stronghold interactions
        private List<CardMenuOption> strongholdMenuOptions = new List<CardMenuOption>();

        public StrongholdCard(Player owner, CardData cardData) : base(owner.game, cardData)
        {
            this.owner = owner;
            this.controller = owner;
            
            InitializeStrongholdProperties(cardData);
            InitializeStrongholdMenu();
        }

        #region Initialization
        private void InitializeStrongholdProperties(CardData cardData)
        {
            isStronghold = true;
            bowed = false;
            
            // Parse starting resources from card data
            if (int.TryParse(cardData.fate?.ToString(), out int fate))
            {
                startingFate = fate;
            }
            
            if (int.TryParse(cardData.honor?.ToString(), out int honor))
            {
                startingHonor = honor;
            }
            
            if (int.TryParse(cardData.influencePool?.ToString(), out int influence))
            {
                influencePool = influence;
            }
            
            if (int.TryParse(cardData.strengthBonus?.ToString(), out int strengthBonus))
            {
                provinceStrengthBonus = strengthBonus;
            }

            // Initialize stronghold traits
            if (cardData.traits != null)
            {
                strongholdTraits = cardData.traits.ToList();
            }
        }

        private void InitializeStrongholdMenu()
        {
            strongholdMenuOptions = new List<CardMenuOption>
            {
                new CardMenuOption("bow", "Bow/Ready")
            };
        }
        #endregion

        #region Resource Properties
        public int GetFate()
        {
            int fate = startingFate;
            
            // Apply fate modification effects
            fate += SumEffects(EffectNames.ModifyStrongholdFate);
            
            return Mathf.Max(0, fate);
        }

        public int GetStartingHonor()
        {
            int honor = startingHonor;
            
            // Apply honor modification effects
            honor += SumEffects(EffectNames.ModifyStrongholdHonor);
            
            return Mathf.Max(0, honor);
        }

        public int GetInfluence()
        {
            int influence = influencePool;
            
            // Apply influence modification effects
            influence += SumEffects(EffectNames.ModifyStrongholdInfluence);
            
            return Mathf.Max(0, influence);
        }

        public int GetProvinceStrengthBonus()
        {
            if (bowed) return 0; // Bowed strongholds don't provide strength bonus
            
            int bonus = provinceStrengthBonus;
            
            // Apply strength bonus modification effects
            bonus += SumEffects(EffectNames.ModifyStrongholdStrengthBonus);
            
            return Mathf.Max(0, bonus);
        }
        #endregion

        #region Bow/Ready System
        public void Bow()
        {
            if (!bowed && CanBow())
            {
                bowed = true;
                OnBowed();
                ExecutePythonScript("on_bowed", this);
                
                game?.AddMessage("{0} bows {1}", owner?.name, name);
            }
        }

        public void Ready()
        {
            if (bowed && CanReady())
            {
                bowed = false;
                OnReadied();
                ExecutePythonScript("on_readied", this);
                
                game?.AddMessage("{0} readies {1}", owner?.name, name);
            }
        }

        public virtual bool CanBow()
        {
            return !bowed && !AnyEffect(EffectNames.CannotBow);
        }

        public virtual bool CanReady()
        {
            return bowed && !AnyEffect(EffectNames.CannotReady);
        }

        protected virtual void OnBowed()
        {
            // Override in derived classes for specific stronghold effects
            TriggerStrongholdAbility();
        }

        protected virtual void OnReadied()
        {
            // Override in derived classes for specific stronghold effects
        }
        #endregion

        #region Stronghold Abilities
        public virtual void TriggerStrongholdAbility()
        {
            if (bowed)
            {
                ExecutePythonScript("on_stronghold_ability_triggered", this, owner);
                
                // Apply any triggered effects
                ApplyBowedEffects();
            }
        }

        protected virtual void ApplyBowedEffects()
        {
            // Apply effects that only work when stronghold is bowed
            var bowedEffects = GetEffects(EffectNames.StrongholdBowedEffect);
            foreach (var effect in bowedEffects)
            {
                effect.Apply(game.GetFrameworkContext());
            }
        }

        public virtual bool CanUseAbility()
        {
            return !bowed && canProduceResources && !AnyEffect(EffectNames.CannotUseStrongholdAbility);
        }

        public virtual void UseStrongholdAbility(AbilityContext context = null)
        {
            if (CanUseAbility())
            {
                Bow(); // Most stronghold abilities require bowing
                ExecutePythonScript("on_ability_used", context);
            }
        }
        #endregion

        #region Game Setup
        public virtual void OnGameSetup()
        {
            // Provide starting resources to owner
            if (owner != null)
            {
                owner.fate += GetFate();
                owner.honor = GetStartingHonor();
                
                game?.AddMessage("{0} starts with {1} fate and {2} honor from {3}", 
                               owner.name, GetFate(), GetStartingHonor(), name);
                
                ExecutePythonScript("on_game_setup", owner, GetFate(), GetStartingHonor());
            }
        }

        public virtual void OnDynastyPhase()
        {
            if (!bowed)
            {
                ExecutePythonScript("on_dynasty_phase", this);
            }
        }

        public virtual void OnConflictPhase()
        {
            if (!bowed)
            {
                ExecutePythonScript("on_conflict_phase", this);
            }
        }

        public virtual void OnFatePhase()
        {
            // Strongholds typically ready during fate phase
            if (bowed)
            {
                Ready();
            }
            
            ExecutePythonScript("on_fate_phase", this);
        }
        #endregion

        #region State Management
        public void FlipFaceup()
        {
            if (facedown)
            {
                facedown = false;
                ExecutePythonScript("on_stronghold_revealed", this);
            }
        }

        public override bool IsBlank()
        {
            // Strongholds are rarely blanked, but check for effects
            return base.IsBlank();
        }

        public bool IsDestroyed()
        {
            // Check if stronghold province is broken (game end condition)
            var strongholdProvince = owner?.GetStrongholdProvince();
            return strongholdProvince?.isBroken ?? false;
        }
        #endregion

        #region Attachment System
        public override bool AllowAttachment(BaseCard attachment)
        {
            // Strongholds can typically accept certain attachments
            var allowedTypes = new HashSet<string> { "fortification", "holding", "stronghold" };
            
            if (allowedTypes.Contains(attachment.type))
            {
                return true;
            }

            // Check for specific traits
            if (strongholdTraits.Any(trait => attachment.HasTrait(trait)))
            {
                return true;
            }

            return base.AllowAttachment(attachment);
        }

        protected override bool CanAttach(BaseCard parent, AbilityContext context, bool ignoreType = false)
        {
            // Strongholds typically cannot attach to other cards
            return false;
        }
        #endregion

        #region Menu System
        public override List<CardMenuOption> GetMenuOptions()
        {
            var options = new List<CardMenuOption>(base.GetMenuOptions());
            options.AddRange(strongholdMenuOptions);

            // Add conditional options
            if (CanUseAbility())
            {
                options.Add(new CardMenuOption("useAbility", "Use Stronghold Ability"));
            }

            #if UNITY_EDITOR
            options.Add(new CardMenuOption("addFate", "Add 1 Fate"));
            options.Add(new CardMenuOption("addHonor", "Add 1 Honor"));
            options.Add(new CardMenuOption("resetBowed", "Reset Bowed State"));
            #endif

            return options;
        }

        public override void ExecuteMenuCommand(string command, Player player)
        {
            switch (command)
            {
                case "bow":
                    if (bowed)
                        Ready();
                    else
                        Bow();
                    break;
                    
                case "useAbility":
                    UseStrongholdAbility();
                    break;
                    
                case "addFate":
                    startingFate++;
                    game?.AddMessage("{0} adds 1 fate to {1}", player.name, name);
                    break;
                    
                case "addHonor":
                    startingHonor++;
                    game?.AddMessage("{0} adds 1 honor to {1}", player.name, name);
                    break;
                    
                case "resetBowed":
                    bowed = false;
                    game?.AddMessage("{0} resets {1}'s bowed state", player.name, name);
                    break;
                    
                default:
                    base.ExecuteMenuCommand(command, player);
                    break;
            }

            ExecutePythonScript("on_menu_command", command, player);
        }
        #endregion

        #region Stronghold Validation
        public virtual bool CanBeStronghold()
        {
            return true; // Most cards that are strongholds can be strongholds
        }

        public virtual List<string> GetStrongholdRequirements()
        {
            var requirements = new List<string>();
            
            // Execute Python to get dynamic requirements
            var pythonRequirements = ExecutePythonFunction<List<string>>("get_stronghold_requirements") ?? new List<string>();
            requirements.AddRange(pythonRequirements);
            
            return requirements;
        }

        public virtual bool ValidateStrongholdSetup(Player player)
        {
            // Basic validation
            if (player.stronghold != this)
            {
                return false;
            }

            // Execute Python validation
            return ExecutePythonFunction<bool>("validate_stronghold_setup", player);
        }
        #endregion

        #region IronPython Integration
        protected override void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}_stronghold.py";
                game.ExecuteCardScript(scriptName, methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for stronghold {name}: {ex.Message}");
            }
        }

        protected T ExecutePythonFunction<T>(string functionName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}_stronghold.py";
                return game.ExecuteCardFunction<T>(scriptName, functionName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python function {functionName} for stronghold {name}: {ex.Message}");
                return default(T);
            }
        }

        // Stronghold-specific Python events
        public void OnProvinceAttacked(ProvinceCard province, Player attacker)
        {
            ExecutePythonScript("on_province_attacked", province, attacker);
        }

        public void OnProvinceDefended(ProvinceCard province, bool wasSuccessful)
        {
            ExecutePythonScript("on_province_defended", province, wasSuccessful);
        }

        public void OnResourcesGained(int fate, int honor)
        {
            ExecutePythonScript("on_resources_gained", fate, honor);
        }

        public void OnConflictAtStronghold(Conflict conflict)
        {
            ExecutePythonScript("on_conflict_at_stronghold", conflict);
        }
        #endregion

        #region Special Stronghold Types
        // Virtual methods for specific stronghold types to override
        public virtual void OnTurnStart()
        {
            ExecutePythonScript("on_turn_start", this);
        }

        public virtual void OnTurnEnd()
        {
            ExecutePythonScript("on_turn_end", this);
        }

        public virtual int GetBonusInfluence()
        {
            return ExecutePythonFunction<int>("get_bonus_influence");
        }

        public virtual int GetBonusFate()
        {
            return ExecutePythonFunction<int>("get_bonus_fate");
        }

        public virtual bool ProvidesElementalBonus(string element)
        {
            return ExecutePythonFunction<bool>("provides_elemental_bonus", element);
        }

        public virtual void OnClanCardPlayed(DrawCard card)
        {
            if (card.GetFaction() == owner?.clan)
            {
                ExecutePythonScript("on_clan_card_played", card);
            }
        }
        #endregion

        #region Summary for Network/UI
        public override Dictionary<string, object> GetSummary(Player activePlayer, bool hideWhenFaceup = false)
        {
            var baseSummary = base.GetSummary(activePlayer, hideWhenFaceup);
            
            var summary = new Dictionary<string, object>(baseSummary)
            {
                {"isStronghold", isStronghold},
                {"bowed", bowed},
                {"startingFate", GetFate()},
                {"startingHonor", GetStartingHonor()},
                {"influence", GetInfluence()},
                {"provinceStrengthBonus", GetProvinceStrengthBonus()},
                {"canUseAbility", CanUseAbility()},
                {"canBow", CanBow()},
                {"canReady", CanReady()},
                {"isDestroyed", IsDestroyed()},
                {"strongholdTraits", strongholdTraits.ToList()},
                {"childCards", childCards.Select(card => card.GetSummary(activePlayer, hideWhenFaceup)).ToList()}
            };

            return summary;
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogStrongholdStatus()
        {
            Debug.Log($"Stronghold Status for {name}:\n" +
                     $"Bowed: {bowed}\n" +
                     $"Starting Fate: {GetFate()}\n" +
                     $"Starting Honor: {GetStartingHonor()}\n" +
                     $"Influence: {GetInfluence()}\n" +
                     $"Province Strength Bonus: {GetProvinceStrengthBonus()}\n" +
                     $"Can Use Ability: {CanUseAbility()}\n" +
                     $"Is Destroyed: {IsDestroyed()}\n" +
                     $"Owner: {owner?.name ?? "None"}");
        }
        #endregion
    }

    #region Supporting Classes and Enums
    [System.Serializable]
    public static class StrongholdTypes
    {
        public const string Standard = "standard";
        public const string Tower = "tower";
        public const string Palace = "palace";
        public const string Fortress = "fortress";
        public const string Temple = "temple";
    }

    [System.Serializable]
    public class StrongholdAbility
    {
        public string name;
        public string description;
        public bool requiresBowing;
        public int fateCost;
        public bool isOptional;

        public StrongholdAbility(string name, string description, bool requiresBowing = true)
        {
            this.name = name;
            this.description = description;
            this.requiresBowing = requiresBowing;
            this.fateCost = 0;
            this.isOptional = true;
        }
    }
    #endregion
}