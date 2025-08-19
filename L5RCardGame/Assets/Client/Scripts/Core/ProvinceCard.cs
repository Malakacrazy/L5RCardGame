using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// ProvinceCard represents province cards that can be attacked and broken during conflicts.
    /// Provinces have strength values and elements, and can be defended by dynasty cards.
    /// </summary>
    [System.Serializable]
    public class ProvinceCard : BaseCard
    {
        [Header("Province Properties")]
        public bool isProvince = true;
        public bool isBroken = false;
        public int baseStrengthValue = 0;
        public string element = "";

        [Header("Province Settings")]
        public List<string> allowedAttachmentTraits = new List<string>();

        // Menu options for province interactions
        private List<CardMenuOption> provinceMenuOptions = new List<CardMenuOption>();

        public ProvinceCard(Player owner, CardData cardData) : base(owner.game, cardData)
        {
            this.owner = owner;
            this.controller = owner;
            
            InitializeProvinceProperties(cardData);
            InitializeProvinceMenu();
        }

        #region Initialization
        private void InitializeProvinceProperties(CardData cardData)
        {
            isProvince = true;
            isBroken = false;
            
            // Parse base strength from card data
            if (int.TryParse(cardData.strength?.ToString(), out int strengthValue))
            {
                baseStrengthValue = strengthValue;
            }
            
            // Set element
            element = cardData.element ?? "";
        }

        private void InitializeProvinceMenu()
        {
            provinceMenuOptions = new List<CardMenuOption>
            {
                new CardMenuOption("break", "Break/unbreak this province"),
                new CardMenuOption("hide", "Flip face down")
            };
        }
        #endregion

        #region Strength Calculations
        public int Strength => GetStrength();

        public int GetStrength()
        {
            // Check for set strength effects
            if (AnyEffect(EffectNames.SetProvinceStrength))
            {
                var setEffect = MostRecentEffect(EffectNames.SetProvinceStrength);
                return setEffect.GetValue<int>(this);
            }

            // Calculate modified strength
            int strength = BaseStrength + 
                          SumEffects(EffectNames.ModifyProvinceStrength) + 
                          GetDynastyOrStrongholdCardModifier();

            // Apply multipliers
            var multipliers = GetEffects(EffectNames.ModifyProvinceStrengthMultiplier);
            foreach (var multiplier in multipliers)
            {
                strength = (int)(strength * multiplier.GetValue<float>(this));
            }

            return Mathf.Max(0, strength);
        }

        public int BaseStrength => GetBaseStrength();

        public int GetBaseStrength()
        {
            // Check for set base strength effects
            if (AnyEffect(EffectNames.SetBaseProvinceStrength))
            {
                var setEffect = MostRecentEffect(EffectNames.SetBaseProvinceStrength);
                return setEffect.GetValue<int>(this);
            }

            return SumEffects(EffectNames.ModifyBaseProvinceStrength) + baseStrengthValue;
        }

        private int GetDynastyOrStrongholdCardModifier()
        {
            var province = controller.GetSourceList(location);
            return province.Sum(card => card.GetProvinceStrengthBonus());
        }
        #endregion

        #region Element System
        public string Element => GetElement();

        public string GetElement()
        {
            return element;
        }

        public bool IsElement(string elementType)
        {
            return element == "all" || element.Contains(elementType);
        }
        #endregion

        #region Province State Management
        public void FlipFaceup()
        {
            facedown = false;
            ExecutePythonScript("on_revealed", this);
        }

        public bool IsConflictProvince()
        {
            return game.currentConflict != null && game.currentConflict.conflictProvince == this;
        }

        public bool CanBeAttacked()
        {
            bool canAttack = !isBroken && 
                           !AnyEffect(EffectNames.CannotBeAttacked);

            // Stronghold province can only be attacked if more than 2 provinces are broken
            if (location == Locations.StrongholdProvince)
            {
                int brokenProvinces = controller.GetProvinces().Count(p => p.isBroken);
                canAttack = canAttack && brokenProvinces > 2;
            }

            return canAttack;
        }

        public bool CanDeclare(string conflictType, Ring ring)
        {
            if (!CanBeAttacked()) return false;

            var restrictedTypes = GetEffects(EffectNames.CannotHaveConflictsDeclaredOfType)
                                    .Select(effect => effect.GetValue<string>(this))
                                    .ToList();

            return !restrictedTypes.Contains(conflictType);
        }

        public override bool IsBlank()
        {
            return isBroken || base.IsBlank();
        }
        #endregion

        #region Breaking Provinces
        public void BreakProvince()
        {
            if (isBroken) return;

            isBroken = true;
            ExecutePythonScript("on_province_broken", this);

            if (controller.opponent != null)
            {
                game.AddMessage("{0} has broken {1}!", controller.opponent.name, name);

                if (location == Locations.StrongholdProvince)
                {
                    // Breaking stronghold province wins the game
                    game.RecordWinner(controller.opponent, "conquest");
                    ExecutePythonScript("on_stronghold_broken", controller.opponent);
                }
                else
                {
                    // Handle dynasty card in province
                    HandleDynastyCardOnBreak();
                }
            }
        }

        private void HandleDynastyCardOnBreak()
        {
            var dynastyCard = controller.GetDynastyCardInProvince(location);
            if (dynastyCard != null)
            {
                string cardName = dynastyCard.facedown ? "the facedown card" : dynastyCard.name;
                string promptTitle = $"Do you wish to discard {cardName}?";

                game.PromptWithHandlerMenu(controller.opponent, new MenuPrompt
                {
                    activePromptTitle = promptTitle,
                    source = $"Break {name}",
                    choices = new List<string> { "Yes", "No" },
                    handlers = new List<System.Action>
                    {
                        () => {
                            game.AddMessage("{0} chooses to discard {1}", 
                                          controller.opponent.name, cardName);
                            
                            // Apply discard action
                            var context = game.GetFrameworkContext();
                            var discardAction = new DiscardCardAction(dynastyCard);
                            game.ApplyGameAction(context, discardAction);
                            
                            ExecutePythonScript("on_dynasty_card_discarded", dynastyCard);
                        },
                        () => {
                            game.AddMessage("{0} chooses not to discard {1}", 
                                          controller.opponent.name, cardName);
                            
                            ExecutePythonScript("on_dynasty_card_kept", dynastyCard);
                        }
                    }
                });
            }
        }

        public void RepairProvince()
        {
            if (!isBroken) return;

            isBroken = false;
            ExecutePythonScript("on_province_repaired", this);
            game.AddMessage("{0} has been repaired!", name);
        }
        #endregion

        #region Province Restrictions
        public virtual bool CannotBeStrongholdProvince()
        {
            return false;
        }

        public virtual bool StartsGameFaceup()
        {
            return false;
        }

        public override bool HideWhenFacedown()
        {
            return false;
        }
        #endregion

        #region Attachment System
        public override bool AllowAttachment(BaseCard attachment)
        {
            // Check for specific allowed traits
            if (allowedAttachmentTraits.Any(trait => attachment.HasTrait(trait)))
            {
                return true;
            }

            // Generally allow attachments to unbroken provinces
            return !isBroken;
        }

        protected override bool CanAttach(BaseCard parent, AbilityContext context, bool ignoreType = false)
        {
            var province = parent as ProvinceCard;
            if (province == null) return false;

            return province.AllowAttachment(this) && 
                   base.CanAttach(parent, context, ignoreType);
        }
        #endregion

        #region Menu System
        public override List<CardMenuOption> GetMenuOptions()
        {
            var options = new List<CardMenuOption>(base.GetMenuOptions());
            options.AddRange(provinceMenuOptions);
            return options;
        }

        public override void ExecuteMenuCommand(string command, Player player)
        {
            switch (command)
            {
                case "break":
                    if (isBroken)
                        RepairProvince();
                    else
                        BreakProvince();
                    break;
                    
                case "hide":
                    facedown = !facedown;
                    game.AddMessage("{0} flips {1} {2}", 
                                  player.name, name, facedown ? "face down" : "face up");
                    break;
                    
                default:
                    base.ExecuteMenuCommand(command, player);
                    break;
            }

            ExecutePythonScript("on_menu_command", command, player);
        }
        #endregion

        #region Conflict Integration
        public List<DrawCard> GetDefendingCards()
        {
            var defendingCards = new List<DrawCard>();
            
            // Get dynasty card in this province
            var dynastyCard = controller.GetDynastyCardInProvince(location);
            if (dynastyCard != null && !dynastyCard.bowed)
            {
                defendingCards.Add(dynastyCard);
            }

            // Get attachments that can defend
            defendingCards.AddRange(attachments.OfType<DrawCard>()
                                              .Where(card => !card.bowed && card.CanParticipateAsDefender()));

            return defendingCards;
        }

        public int GetTotalDefenseStrength(string conflictType)
        {
            int provinceStrength = Strength;
            int cardStrength = GetDefendingCards().Sum(card => card.GetSkill(conflictType));
            
            return provinceStrength + cardStrength;
        }

        public bool IsVulnerable(string conflictType)
        {
            return GetTotalDefenseStrength(conflictType) == 0;
        }
        #endregion

        #region IronPython Integration
        protected override void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}_province.py";
                game.ExecuteCardScript(scriptName, methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for province {name}: {ex.Message}");
            }
        }

        // Province-specific Python events
        public void OnConflictDeclared(string conflictType, Player attacker)
        {
            ExecutePythonScript("on_conflict_declared", conflictType, attacker);
        }

        public void OnConflictResolved(bool wasWon, Player winner)
        {
            ExecutePythonScript("on_conflict_resolved", wasWon, winner);
        }

        public void OnCardPlayed(DrawCard card, Player player)
        {
            ExecutePythonScript("on_card_played", card, player);
        }
        #endregion

        #region Summary for Network/UI
        public override Dictionary<string, object> GetSummary(Player activePlayer, bool hideWhenFaceup = false)
        {
            var baseSummary = base.GetSummary(activePlayer, hideWhenFaceup);
            
            var summary = new Dictionary<string, object>(baseSummary)
            {
                {"isProvince", isProvince},
                {"isBroken", isBroken},
                {"strength", Strength},
                {"baseStrength", BaseStrength},
                {"element", element},
                {"canBeAttacked", CanBeAttacked()},
                {"isConflictProvince", IsConflictProvince()},
                {"defendingCards", GetDefendingCards().Select(card => card.GetSummary(activePlayer, hideWhenFaceup)).ToList()},
                {"attachments", attachments.Select(att => att.GetSummary(activePlayer, hideWhenFaceup)).ToList()}
            };

            return summary;
        }
        #endregion

        #region Special Province Types
        // Virtual methods for specific province types to override
        public virtual void OnEnterPlay()
        {
            ExecutePythonScript("on_enter_play", this);
        }

        public virtual void OnBecomesConflictProvince()
        {
            ExecutePythonScript("on_becomes_conflict_province", this);
        }

        public virtual void OnConflictEnds()
        {
            ExecutePythonScript("on_conflict_ends", this);
        }

        public virtual int GetBonusStrength(string conflictType)
        {
            return ExecutePythonFunction<int>("get_bonus_strength", conflictType);
        }

        protected T ExecutePythonFunction<T>(string functionName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}_province.py";
                return game.ExecuteCardFunction<T>(scriptName, functionName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python function {functionName} for province {name}: {ex.Message}");
                return default(T);
            }
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogProvinceStatus()
        {
            Debug.Log($"Province Status for {name}:\n" +
                     $"Broken: {isBroken}\n" +
                     $"Strength: {Strength} (Base: {BaseStrength})\n" +
                     $"Element: {element}\n" +
                     $"Can Be Attacked: {CanBeAttacked()}\n" +
                     $"Is Conflict Province: {IsConflictProvince()}\n" +
                     $"Defending Cards: {GetDefendingCards().Count}");
        }
        #endregion
    }

    #region Supporting Classes
    [System.Serializable]
    public class MenuPrompt
    {
        public string activePromptTitle;
        public string source;
        public List<string> choices;
        public List<System.Action> handlers;
    }

    [System.Serializable] 
    public class DiscardCardAction
    {
        public BaseCard targetCard;

        public DiscardCardAction(BaseCard card)
        {
            targetCard = card;
        }
    }
    #endregion
}