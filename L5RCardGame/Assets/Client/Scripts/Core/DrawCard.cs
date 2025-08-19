using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IronPython.Runtime;

namespace L5RGame
{
    /// <summary>
    /// DrawCard represents playable cards that can be drawn from decks and played in the game.
    /// This includes Characters, Attachments, Events, and Holdings with complex skill calculations.
    /// </summary>
    [System.Serializable]
    public class DrawCard : BaseCard
    {
        [Header("Printed Stats")]
        public int printedMilitarySkill;
        public int printedPoliticalSkill;
        public int printedCost;
        public int printedGlory;
        public int printedStrengthBonus;

        [Header("Current State")]
        public int fate = 0;
        public bool bowed = false;
        public bool covert = false;
        public bool inConflict = false;
        public bool isNew = false;

        [Header("Card Properties")]
        public bool isConflict;
        public bool isDynasty;

        // Honor status
        private StatusToken personalHonor;
        private float pausedTime = 0f;

        // Cached skill modifiers for performance
        private SkillModifiers cachedSkillModifiers;
        private bool skillModifiersCacheValid = false;

        // Default controller (usually the owner)
        private Player defaultController;

        // Menu options for card interactions
        private List<CardMenuOption> menuOptions = new List<CardMenuOption>();

        public DrawCard(Player owner, CardData cardData) : base(owner.game, cardData)
        {
            this.owner = owner;
            this.defaultController = owner;
            this.controller = owner;
            this.parent = null;

            InitializePrintedStats(cardData);
            InitializeCardProperties(cardData);
            InitializeMenu();
            InitializeKeywordAbilities();
        }

        #region Initialization
        private void InitializePrintedStats(CardData cardData)
        {
            printedMilitarySkill = GetPrintedSkill("military", cardData);
            printedPoliticalSkill = GetPrintedSkill("political", cardData);
            printedCost = cardData.cost;
            printedGlory = cardData.glory;
            printedStrengthBonus = cardData.strengthBonus;
        }

        private void InitializeCardProperties(CardData cardData)
        {
            isConflict = cardData.side == "conflict";
            isDynasty = cardData.side == "dynasty";
            
            // Parse keywords from card text
            if (!string.IsNullOrEmpty(cardData.text))
            {
                string cleanText = cardData.text.Replace("<[^>]*>", "").ToLower();
                ParseKeywords(cleanText);
            }
        }

        private void InitializeMenu()
        {
            menuOptions = new List<CardMenuOption>
            {
                new CardMenuOption("bow", "Bow/Ready"),
                new CardMenuOption("honor", "Honor"),
                new CardMenuOption("dishonor", "Dishonor"),
                new CardMenuOption("addfate", "Add 1 fate"),
                new CardMenuOption("remfate", "Remove 1 fate"),
                new CardMenuOption("move", "Move into/out of conflict"),
                new CardMenuOption("control", "Give control")
            };
        }

        private void InitializeKeywordAbilities()
        {
            if (type == CardTypes.Character)
            {
                // Add keyword abilities - these would be implemented as separate classes
                // abilities.reactions.Add(new CourtesyAbility(game, this));
                // abilities.reactions.Add(new PrideAbility(game, this));
                // abilities.reactions.Add(new SincerityAbility(game, this));
            }
        }

        private int GetPrintedSkill(string skillType, CardData cardData)
        {
            int skillValue = 0;
            
            switch (skillType)
            {
                case "military":
                    if (cardData.military == null || !int.TryParse(cardData.military.ToString(), out skillValue))
                        return 0;
                    break;
                case "political":
                    if (cardData.political == null || !int.TryParse(cardData.political.ToString(), out skillValue))
                        return 0;
                    break;
            }
            
            return skillValue;
        }
        #endregion

        #region Keyword Properties
        public bool IsLimited() => HasKeyword("limited") || HasPrintedKeyword("limited");
        public bool IsRestricted() => HasKeyword("restricted");
        public bool IsAncestral() => HasKeyword("ancestral");
        public bool IsCovert() => HasKeyword("covert");
        public bool HasSincerity() => HasKeyword("sincerity");
        public bool HasPride() => HasKeyword("pride");
        public bool HasCourtesy() => HasKeyword("courtesy");
        #endregion

        #region Cost and Fate Management
        public int GetCost()
        {
            var copyEffect = MostRecentEffect(EffectNames.CopyCharacter);
            return copyEffect?.printedCost ?? printedCost;
        }

        public int GetFate() => fate;

        public bool CostLessThan(int num)
        {
            int cost = GetCost();
            return cost < num;
        }

        public void ModifyFate(int amount)
        {
            fate = Mathf.Max(0, fate + amount);
            ExecutePythonScript("on_fate_changed", new Dictionary<string, object>
            {
                {"old_fate", fate - amount},
                {"new_fate", fate},
                {"change", amount}
            });
        }
        #endregion

        #region Unique Card Management
        public bool AnotherUniqueInPlay(Player player)
        {
            if (!IsUnique()) return false;

            return game.AllCards.Any(card =>
                card.IsInPlay() &&
                card.printedName == printedName &&
                card != this &&
                (card.owner == player || card.controller == player || card.owner == owner));
        }
        #endregion

        #region Skill Calculations
        public bool HasDash(string skillType = "")
        {
            if (skillType == "glory" || printedType != CardTypes.Character)
                return false;

            var baseSkillModifiers = GetBaseSkillModifiers();

            switch (skillType)
            {
                case "military":
                    return float.IsNaN(baseSkillModifiers.baseMilitarySkill);
                case "political":
                    return float.IsNaN(baseSkillModifiers.basePoliticalSkill);
                default:
                    return float.IsNaN(baseSkillModifiers.baseMilitarySkill) || 
                           float.IsNaN(baseSkillModifiers.basePoliticalSkill);
            }
        }

        public int GetContributionToConflict(string conflictType)
        {
            // Check for custom contribution function
            var skillFunction = MostRecentEffect(EffectNames.ChangeContributionFunction);
            if (skillFunction != null)
            {
                return ExecutePythonFunction<int>("calculate_contribution", this, conflictType);
            }
            return GetSkill(conflictType);
        }

        public int GetSkill(string skillType)
        {
            switch (skillType)
            {
                case "military":
                    return GetMilitarySkill();
                case "political":
                    return GetPoliticalSkill();
                default:
                    return 0;
            }
        }

        public int GetMilitarySkill(bool floor = true)
        {
            var modifiers = GetMilitaryModifiers();
            float skill = modifiers.Sum(m => m.amount);
            
            if (float.IsNaN(skill)) return 0;
            return floor ? Mathf.Max(0, (int)skill) : (int)skill;
        }

        public int GetPoliticalSkill(bool floor = true)
        {
            var modifiers = GetPoliticalModifiers();
            float skill = modifiers.Sum(m => m.amount);
            
            if (float.IsNaN(skill)) return 0;
            return floor ? Mathf.Max(0, (int)skill) : (int)skill;
        }

        public int GetBaseMilitarySkill()
        {
            float skill = GetBaseSkillModifiers().baseMilitarySkill;
            return float.IsNaN(skill) ? 0 : Mathf.Max(0, (int)skill);
        }

        public int GetBasePoliticalSkill()
        {
            float skill = GetBaseSkillModifiers().basePoliticalSkill;
            return float.IsNaN(skill) ? 0 : Mathf.Max(0, (int)skill);
        }
        #endregion

        #region Skill Modifiers
        [System.Serializable]
        public class SkillModifiers
        {
            public List<StatModifier> baseMilitaryModifiers = new List<StatModifier>();
            public List<StatModifier> basePoliticalModifiers = new List<StatModifier>();
            public float baseMilitarySkill;
            public float basePoliticalSkill;
        }

        private SkillModifiers GetBaseSkillModifiers()
        {
            if (skillModifiersCacheValid && cachedSkillModifiers != null)
                return cachedSkillModifiers;

            var baseModifierEffects = new[]
            {
                EffectNames.CopyCharacter,
                EffectNames.CalculatePrintedMilitarySkill,
                EffectNames.ModifyBaseMilitarySkillMultiplier,
                EffectNames.ModifyBasePoliticalSkillMultiplier,
                EffectNames.SetBaseMilitarySkill,
                EffectNames.SetBasePoliticalSkill,
                EffectNames.SetBaseDash,
                EffectNames.SwitchBaseSkills,
                EffectNames.SetBaseGlory
            };

            var baseEffects = GetRawEffects().Where(effect => baseModifierEffects.Contains(effect.type)).ToList();
            var result = new SkillModifiers();
            
            result.baseMilitaryModifiers.Add(StatModifier.FromCard(printedMilitarySkill, this, "Printed skill", false));
            result.basePoliticalModifiers.Add(StatModifier.FromCard(printedPoliticalSkill, this, "Printed skill", false));
            result.baseMilitarySkill = printedMilitarySkill;
            result.basePoliticalSkill = printedPoliticalSkill;

            foreach (var effect in baseEffects)
            {
                ProcessBaseSkillEffect(effect, result);
            }

            // Handle overriding modifiers
            ProcessOverridingModifiers(result);

            cachedSkillModifiers = result;
            skillModifiersCacheValid = true;
            return result;
        }

        private void ProcessBaseSkillEffect(CardEffect effect, SkillModifiers result)
        {
            switch (effect.type)
            {
                case EffectNames.CopyCharacter:
                    ProcessCopyCharacterEffect(effect, result);
                    break;
                case EffectNames.SetBaseDash:
                    ProcessSetBaseDashEffect(effect, result);
                    break;
                case EffectNames.SetBaseMilitarySkill:
                    result.baseMilitarySkill = effect.GetValue<int>(this);
                    result.baseMilitaryModifiers.Add(StatModifier.FromEffect(result.baseMilitarySkill, effect, true, $"Base set by {effect.name}"));
                    break;
                case EffectNames.SetBasePoliticalSkill:
                    result.basePoliticalSkill = effect.GetValue<int>(this);
                    result.basePoliticalModifiers.Add(StatModifier.FromEffect(result.basePoliticalSkill, effect, true, $"Base set by {effect.name}"));
                    break;
                case EffectNames.SwitchBaseSkills:
                    ProcessSwitchBaseSkillsEffect(effect, result);
                    break;
                case EffectNames.ModifyBaseMilitarySkillMultiplier:
                    ProcessBaseMilitaryMultiplierEffect(effect, result);
                    break;
                case EffectNames.ModifyBasePoliticalSkillMultiplier:
                    ProcessBasePoliticalMultiplierEffect(effect, result);
                    break;
            }
        }

        private void ProcessCopyCharacterEffect(CardEffect effect, SkillModifiers result)
        {
            var copiedCard = effect.GetValue<DrawCard>(this);
            result.baseMilitarySkill = copiedCard.printedMilitarySkill;
            result.basePoliticalSkill = copiedCard.printedPoliticalSkill;
            
            result.baseMilitaryModifiers.RemoveAll(mod => mod.name.StartsWith("Printed skill"));
            result.basePoliticalModifiers.RemoveAll(mod => mod.name.StartsWith("Printed skill"));
            
            result.baseMilitaryModifiers.Add(StatModifier.FromEffect(result.baseMilitarySkill, effect, false, $"Printed skill from {copiedCard.name} due to {effect.name}"));
            result.basePoliticalModifiers.Add(StatModifier.FromEffect(result.basePoliticalSkill, effect, false, $"Printed skill from {copiedCard.name} due to {effect.name}"));
        }

        private void ProcessSetBaseDashEffect(CardEffect effect, SkillModifiers result)
        {
            string dashType = effect.GetValue<string>(this);
            if (dashType == "military")
            {
                result.baseMilitaryModifiers.Add(StatModifier.FromEffect(float.NaN, effect, true, effect.name));
                result.baseMilitarySkill = float.NaN;
            }
            if (dashType == "political")
            {
                result.basePoliticalModifiers.Add(StatModifier.FromEffect(float.NaN, effect, true, effect.name));
                result.basePoliticalSkill = float.NaN;
            }
        }

        private void ProcessSwitchBaseSkillsEffect(CardEffect effect, SkillModifiers result)
        {
            float milChange = Mathf.Max(result.basePoliticalSkill, 0) - Mathf.Max(result.baseMilitarySkill, 0);
            float polChange = Mathf.Max(result.baseMilitarySkill, 0) - Mathf.Max(result.basePoliticalSkill, 0);
            
            result.baseMilitarySkill += milChange;
            result.basePoliticalSkill += polChange;
            
            result.baseMilitaryModifiers.Add(StatModifier.FromEffect(milChange, effect, false, $"Base due to {effect.name}"));
            result.basePoliticalModifiers.Add(StatModifier.FromEffect(polChange, effect, false, $"Base due to {effect.name}"));
        }

        private void ProcessBaseMilitaryMultiplierEffect(CardEffect effect, SkillModifiers result)
        {
            float multiplier = effect.GetValue<float>(this);
            float milChange = (multiplier - 1) * result.baseMilitarySkill;
            result.baseMilitarySkill += milChange;
            result.baseMilitaryModifiers.Add(StatModifier.FromEffect(milChange, effect, false, $"Base due to {effect.name}"));
        }

        private void ProcessBasePoliticalMultiplierEffect(CardEffect effect, SkillModifiers result)
        {
            float multiplier = effect.GetValue<float>(this);
            float polChange = (multiplier - 1) * result.basePoliticalSkill;
            result.basePoliticalSkill += polChange;
            result.basePoliticalModifiers.Add(StatModifier.FromEffect(polChange, effect, false, $"Base due to {effect.name}"));
        }

        private void ProcessOverridingModifiers(SkillModifiers result)
        {
            var overridingMilModifiers = result.baseMilitaryModifiers.Where(mod => mod.overrides).ToList();
            if (overridingMilModifiers.Count > 0)
            {
                var lastModifier = overridingMilModifiers.Last();
                result.baseMilitaryModifiers = new List<StatModifier> { lastModifier };
                result.baseMilitarySkill = lastModifier.amount;
            }

            var overridingPolModifiers = result.basePoliticalModifiers.Where(mod => mod.overrides).ToList();
            if (overridingPolModifiers.Count > 0)
            {
                var lastModifier = overridingPolModifiers.Last();
                result.basePoliticalModifiers = new List<StatModifier> { lastModifier };
                result.basePoliticalSkill = lastModifier.amount;
            }
        }

        public List<StatModifier> GetMilitaryModifiers(List<string> exclusions = null)
        {
            exclusions = exclusions ?? new List<string>();
            var baseSkillModifiers = GetBaseSkillModifiers();
            
            if (float.IsNaN(baseSkillModifiers.baseMilitarySkill))
                return baseSkillModifiers.baseMilitaryModifiers;

            var rawEffects = GetRawEffects().Where(effect => !exclusions.Contains(effect.type)).ToList();

            // Check for set effects
            var setEffects = rawEffects.Where(effect => effect.type == EffectNames.SetMilitarySkill || effect.type == EffectNames.SetDash).ToList();
            if (setEffects.Count > 0)
            {
                var latestSetEffect = setEffects.Last();
                float setAmount = latestSetEffect.type == EffectNames.SetDash ? float.NaN : latestSetEffect.GetValue<float>(this);
                return new List<StatModifier> { StatModifier.FromEffect(setAmount, latestSetEffect, true, $"Set by {latestSetEffect.name}") };
            }

            var modifiers = new List<StatModifier>(baseSkillModifiers.baseMilitaryModifiers);

            // Add skill modifier effects
            var modifierEffects = rawEffects.Where(effect => 
                effect.type == EffectNames.AttachmentMilitarySkillModifier ||
                effect.type == EffectNames.ModifyMilitarySkill ||
                effect.type == EffectNames.ModifyBothSkills).ToList();

            foreach (var modifierEffect in modifierEffects)
            {
                float value = modifierEffect.GetValue<float>(this);
                modifiers.Add(StatModifier.FromEffect(value, modifierEffect));
            }

            // Adjust honor status effects
            AdjustHonorStatusModifiers(modifiers);

            // Apply multipliers
            var multiplierEffects = rawEffects.Where(effect => effect.type == EffectNames.ModifyMilitarySkillMultiplier).ToList();
            foreach (var multiplierEffect in multiplierEffects)
            {
                float multiplier = multiplierEffect.GetValue<float>(this);
                float currentTotal = modifiers.Sum(modifier => modifier.amount);
                float amount = (multiplier - 1) * currentTotal;
                modifiers.Add(StatModifier.FromEffect(amount, multiplierEffect));
            }

            return modifiers;
        }

        public List<StatModifier> GetPoliticalModifiers(List<string> exclusions = null)
        {
            exclusions = exclusions ?? new List<string>();
            var baseSkillModifiers = GetBaseSkillModifiers();
            
            if (float.IsNaN(baseSkillModifiers.basePoliticalSkill))
                return baseSkillModifiers.basePoliticalModifiers;

            var rawEffects = GetRawEffects().Where(effect => !exclusions.Contains(effect.type)).ToList();

            // Check for set effects
            var setEffects = rawEffects.Where(effect => effect.type == EffectNames.SetPoliticalSkill).ToList();
            if (setEffects.Count > 0)
            {
                var latestSetEffect = setEffects.Last();
                float setAmount = latestSetEffect.GetValue<float>(this);
                return new List<StatModifier> { StatModifier.FromEffect(setAmount, latestSetEffect, true, $"Set by {latestSetEffect.name}") };
            }

            var modifiers = new List<StatModifier>(baseSkillModifiers.basePoliticalModifiers);

            // Add skill modifier effects
            var modifierEffects = rawEffects.Where(effect => 
                effect.type == EffectNames.AttachmentPoliticalSkillModifier ||
                effect.type == EffectNames.ModifyPoliticalSkill ||
                effect.type == EffectNames.ModifyBothSkills).ToList();

            foreach (var modifierEffect in modifierEffects)
            {
                float value = modifierEffect.GetValue<float>(this);
                modifiers.Add(StatModifier.FromEffect(value, modifierEffect));
            }

            // Adjust honor status effects
            AdjustHonorStatusModifiers(modifiers);

            // Apply multipliers
            var multiplierEffects = rawEffects.Where(effect => effect.type == EffectNames.ModifyPoliticalSkillMultiplier).ToList();
            foreach (var multiplierEffect in multiplierEffects)
            {
                float multiplier = multiplierEffect.GetValue<float>(this);
                float currentTotal = modifiers.Sum(modifier => modifier.amount);
                float amount = (multiplier - 1) * currentTotal;
                modifiers.Add(StatModifier.FromEffect(amount, multiplierEffect));
            }

            return modifiers;
        }

        private void AdjustHonorStatusModifiers(List<StatModifier> modifiers)
        {
            var doesNotModifyEffects = GetRawEffects().Where(effect => effect.type == EffectNames.HonorStatusDoesNotModifySkill).ToList();
            if (doesNotModifyEffects.Count > 0)
            {
                foreach (var modifier in modifiers.Where(m => m.type == "token" && m.amount != 0))
                {
                    modifier.amount = 0;
                    modifier.name += $" ({doesNotModifyEffects[0].name})";
                }
            }

            var reverseEffects = GetRawEffects().Where(effect => effect.type == EffectNames.HonorStatusReverseModifySkill).ToList();
            if (reverseEffects.Count > 0)
            {
                foreach (var modifier in modifiers.Where(m => m.type == "token" && m.amount != 0))
                {
                    modifier.amount = -modifier.amount;
                    modifier.name += $" ({reverseEffects[0].name})";
                }
            }
        }
        #endregion

        #region Glory System
        public int GetGlory()
        {
            var gloryModifiers = GetGloryModifiers();
            float glory = gloryModifiers.Sum(modifier => modifier.amount);
            return float.IsNaN(glory) ? 0 : Mathf.Max(0, (int)glory);
        }

        public List<StatModifier> GetGloryModifiers()
        {
            if (printedGlory == 0 && type != CardTypes.Character) // Holdings etc.
                return new List<StatModifier>();

            var gloryModifierEffects = new[]
            {
                EffectNames.CopyCharacter,
                EffectNames.SetGlory,
                EffectNames.ModifyGlory,
                EffectNames.SetBaseGlory
            };

            var gloryEffects = GetRawEffects().Where(effect => gloryModifierEffects.Contains(effect.type)).ToList();
            var gloryModifiers = new List<StatModifier>();

            // Check for set effects
            var setEffects = gloryEffects.Where(effect => effect.type == EffectNames.SetGlory).ToList();
            if (setEffects.Count > 0)
            {
                var latestSetEffect = setEffects.Last();
                float setAmount = latestSetEffect.GetValue<float>(this);
                return new List<StatModifier> { StatModifier.FromEffect(setAmount, latestSetEffect, true, $"Set by {latestSetEffect.name}") };
            }

            // Base effects/copy effects/printed glory
            var baseEffects = gloryEffects.Where(effect => effect.type == EffectNames.SetBaseGlory).ToList();
            var copyEffects = gloryEffects.Where(effect => effect.type == EffectNames.CopyCharacter).ToList();
            
            if (baseEffects.Count > 0)
            {
                var latestBaseEffect = baseEffects.Last();
                float baseAmount = latestBaseEffect.GetValue<float>(this);
                gloryModifiers.Add(StatModifier.FromEffect(baseAmount, latestBaseEffect, true, $"Base set by {latestBaseEffect.name}"));
            }
            else if (copyEffects.Count > 0)
            {
                var latestCopyEffect = copyEffects.Last();
                var copiedCard = latestCopyEffect.GetValue<DrawCard>(this);
                gloryModifiers.Add(StatModifier.FromEffect(copiedCard.printedGlory, latestCopyEffect, false, $"Printed glory from {copiedCard.name} due to {latestCopyEffect.name}"));
            }
            else
            {
                gloryModifiers.Add(StatModifier.FromCard(printedGlory, this, "Printed glory", false));
            }

            // Add modifier effects
            var modifierEffects = gloryEffects.Where(effect => effect.type == EffectNames.ModifyGlory).ToList();
            foreach (var modifierEffect in modifierEffects)
            {
                float value = modifierEffect.GetValue<float>(this);
                gloryModifiers.Add(StatModifier.FromEffect(value, modifierEffect));
            }

            return gloryModifiers;
        }

        public int GetContributionToImperialFavor()
        {
            return !bowed ? GetGlory() : 0;
        }
        #endregion

        #region Province Strength
        public int GetProvinceStrengthBonus()
        {
            if (printedStrengthBonus == 0 || facedown) return 0;
            
            var modifiers = GetProvinceStrengthBonusModifiers();
            return (int)modifiers.Sum(modifier => modifier.amount);
        }

        public List<StatModifier> GetProvinceStrengthBonusModifiers()
        {
            if (printedStrengthBonus == 0) // Not a holding
                return new List<StatModifier>();

            var strengthModifierEffects = new[]
            {
                EffectNames.SetProvinceStrengthBonus,
                EffectNames.ModifyProvinceStrengthBonus
            };

            var strengthEffects = GetRawEffects().Where(effect => strengthModifierEffects.Contains(effect.type)).ToList();
            var strengthModifiers = new List<StatModifier>();

            // Check for set effects
            var setEffects = strengthEffects.Where(effect => effect.type == EffectNames.SetProvinceStrengthBonus).ToList();
            if (setEffects.Count > 0)
            {
                var latestSetEffect = setEffects.Last();
                float setAmount = latestSetEffect.GetValue<float>(this);
                return new List<StatModifier> { StatModifier.FromEffect(setAmount, latestSetEffect, true, $"Set by {latestSetEffect.name}") };
            }

            // Add printed and modifier effects
            strengthModifiers.Add(StatModifier.FromCard(printedStrengthBonus, this, "Printed province strength bonus", false));
            
            var modifierEffects = strengthEffects.Where(effect => effect.type == EffectNames.ModifyProvinceStrengthBonus).ToList();
            foreach (var modifierEffect in modifierEffects)
            {
                float value = modifierEffect.GetValue<float>(this);
                strengthModifiers.Add(StatModifier.FromEffect(value, modifierEffect));
            }

            return strengthModifiers;
        }
        #endregion

        #region Honor Status Management
        public bool IsHonored => personalHonor != null && personalHonor.honored;
        public bool IsDishonored => personalHonor != null && personalHonor.dishonored;

        public void SetPersonalHonor(StatusToken token)
        {
            if (personalHonor != null && token != personalHonor)
            {
                personalHonor.SetCard(null);
            }
            
            personalHonor = token;
            
            if (personalHonor != null)
            {
                personalHonor.SetCard(this);
            }

            InvalidateSkillCache();
            ExecutePythonScript("on_honor_status_changed", new Dictionary<string, object>
            {
                {"is_honored", IsHonored},
                {"is_dishonored", IsDishonored}
            });
        }

        public void Honor()
        {
            if (IsHonored) return;
            
            if (IsDishonored)
            {
                MakeOrdinary();
            }
            else
            {
                SetPersonalHonor(new StatusToken(game, this, true));
            }
        }

        public void Dishonor()
        {
            if (IsDishonored) return;
            
            if (IsHonored)
            {
                MakeOrdinary();
            }
            else
            {
                SetPersonalHonor(new StatusToken(game, this, false));
            }
        }

        public void MakeOrdinary()
        {
            SetPersonalHonor(null);
        }
        #endregion

        #region Bow/Ready States
        public void Bow()
        {
            if (!bowed)
            {
                bowed = true;
                ExecutePythonScript("on_bowed", this);
            }
        }

        public void Ready()
        {
            if (bowed)
            {
                bowed = false;
                ExecutePythonScript("on_readied", this);
            }
        }

        public bool BowsOnReturnHome()
        {
            return !AnyEffect(EffectNames.DoesNotBow);
        }
        #endregion

        #region Conflict Participation
        public bool CanDeclareAsAttacker(string conflictType, Ring ring, Province province)
        {
            var attackers = game.IsDuringConflict() ? game.currentConflict.attackers : new List<DrawCard>();
            var totalFateCost = attackers.Concat(new[] { this }).Sum(card => card.SumEffects(EffectNames.FateCostToAttack));
            
            if (totalFateCost > controller.fate) return false;

            // Check element restrictions
            if (AnyEffect(EffectNames.CanOnlyBeDeclaredAsAttackerWithElement))
            {
                var elementsAdded = attachments.SelectMany(attachment => attachment.GetEffects(EffectNames.AddElementAsAttacker))
                                             .Concat(GetEffects(EffectNames.AddElementAsAttacker))
                                             .ToList();

                foreach (string element in GetEffects(EffectNames.CanOnlyBeDeclaredAsAttackerWithElement))
                {
                    if (!ring.HasElement(element) && !elementsAdded.Contains(element))
                        return false;
                }
            }

            return CheckRestrictions("declareAsAttacker", game.GetFrameworkContext()) &&
                   CanParticipateAsAttacker(conflictType) &&
                   location == Locations.PlayArea && 
                   !bowed;
        }

        public bool CanDeclareAsDefender(string conflictType = null)
        {
            conflictType = conflictType ?? game.currentConflict?.conflictType;
            if (conflictType == null) return false;

            return CheckRestrictions("declareAsDefender", game.GetFrameworkContext()) &&
                   CanParticipateAsDefender(conflictType) &&
                   location == Locations.PlayArea && 
                   !bowed && 
                   !covert;
        }

        public bool CanParticipateAsAttacker(string conflictType = null)
        {
            conflictType = conflictType ?? game.currentConflict?.conflictType;
            if (conflictType == null) return false;

            var effects = GetEffects(EffectNames.CannotParticipateAsAttacker);
            return !effects.Any(value => value == "both" || value == conflictType) && 
                   !HasDash(conflictType);
        }

        public bool CanParticipateAsDefender(string conflictType = null)
        {
            conflictType = conflictType ?? game.currentConflict?.conflictType;
            if (conflictType == null) return false;

            var effects = GetEffects(EffectNames.CannotParticipateAsDefender);
            return !effects.Any(value => value == "both" || value == conflictType) && 
                   !HasDash(conflictType);
        }

        public bool CanBeBypassedByCovert(AbilityContext context)
        {
            return !IsCovert() && CheckRestrictions("applyCovert", context);
        }

        public void ResetForConflict()
        {
            covert = false;
            inConflict = false;
        }

        public bool IsParticipating()
        {
            return inConflict && game.currentConflict != null && 
                   (game.currentConflict.attackers.Contains(this) || 
                    game.currentConflict.defenders.Contains(this));
        }
        #endregion

        #region Card Movement and State
        public override void LeavesPlay()
        {
            // Remove from parent if attached
            if (parent != null && parent.attachments != null)
            {
                parent.RemoveAttachment(this);
                parent = null;
            }

            // Remove from conflict if participating
            if (IsParticipating())
            {
                game.currentConflict.RemoveFromConflict(this);
            }

            // Handle honor status effects on leaving play
            if (IsDishonored && !AnyEffect(EffectNames.HonorStatusDoesNotAffectLeavePlay))
            {
                game.AddMessage("{0} loses 1 honor due to {1}'s personal dishonor", controller.name, name);
                controller.LoseHonor(1);
            }
            else if (IsHonored && !AnyEffect(EffectNames.HonorStatusDoesNotAffectLeavePlay))
            {
                game.AddMessage("{0} gains 1 honor due to {1}'s personal honor", controller.name, name);
                controller.GainHonor(1);
            }

            // Reset card state
            MakeOrdinary();
            bowed = false;
            covert = false;
            isNew = false;
            fate = 0;

            // Call base implementation
            base.LeavesPlay();

            // Execute Python script
            ExecutePythonScript("on_leaves_play", this);
        }

        public override void UpdateEffects(string from, string to)
        {
            base.UpdateEffects(from, to);
            SetPersonalHonor(personalHonor); // Refresh honor token
            InvalidateSkillCache();
        }

        public void SetDefaultController(Player player)
        {
            defaultController = player;
        }

        public override Player GetModifiedController()
        {
            if (location == Locations.PlayArea || 
                (type == CardTypes.Holding && location.Contains("province")))
            {
                var takeControlEffect = MostRecentEffect(EffectNames.TakeControl);
                return takeControlEffect?.GetValue<Player>(this) ?? defaultController;
            }
            return owner;
        }
        #endregion

        #region Disguise Mechanics
        public bool CanDisguise(DrawCard card, AbilityContext context, bool intoConflictOnly = false)
        {
            return disguisedKeywordTraits.Any(trait => card.HasTrait(trait)) &&
                   card.AllowGameAction("discardFromPlay", context) &&
                   !card.IsUnique() &&
                   (!intoConflictOnly || card.IsParticipating());
        }
        #endregion

        #region Play Actions and Restrictions
        public bool CanPlay(AbilityContext context, string playType)
        {
            return CheckRestrictions(playType, context) && 
                   context.player.CheckRestrictions(playType, context) &&
                   CheckRestrictions("play", context) && 
                   context.player.CheckRestrictions("play", context);
        }

        public override List<CardAction> GetActions(string location = null)
        {
            location = location ?? this.location;
            
            if (location == Locations.PlayArea || type == CardTypes.Event)
            {
                return base.GetActions();
            }

            var actions = new List<CardAction>();
            if (type == CardTypes.Character)
            {
                // actions.Add(new DuplicateUniqueAction(this));
            }

            return actions.Concat(GetPlayActions()).Concat(base.GetActions()).ToList();
        }

        public virtual void Play()
        {
            // Empty function so PlayCardAction doesn't crash the game
            ExecutePythonScript("on_play", this);
        }
        #endregion

        #region Snapshots for Undo/Rollback
        public DrawCard CreateSnapshot()
        {
            var clone = new DrawCard(owner, cardData);

            // Copy attachments
            clone.attachments = new List<BaseCard>(attachments.Select(attachment => attachment.CreateSnapshot()));
            clone.childCards = childCards.Select(card => card.CreateSnapshot()).ToList();
            clone.effects = new List<CardEffect>(effects);
            
            // Copy state
            clone.controller = controller;
            clone.bowed = bowed;
            clone.personalHonor = personalHonor;
            clone.location = location;
            clone.parent = parent;
            clone.fate = fate;
            clone.inConflict = inConflict;
            clone.traits = GetTraits();
            
            return clone;
        }
        #endregion

        #region Cache Management
        private void InvalidateSkillCache()
        {
            skillModifiersCacheValid = false;
            cachedSkillModifiers = null;
        }

        protected override void OnEffectsChanged()
        {
            base.OnEffectsChanged();
            InvalidateSkillCache();
        }
        #endregion

        #region Skill Summaries for UI
        public bool ShowStats => location == Locations.PlayArea && type == CardTypes.Character;

        public SkillSummary MilitarySkillSummary
        {
            get
            {
                if (!ShowStats) return new SkillSummary();
                
                var modifiers = GetMilitaryModifiers().Select(m => new StatModifier(m)).ToList();
                int skill = (int)modifiers.Sum(m => m.amount);
                
                return new SkillSummary
                {
                    stat = float.IsNaN(skill) ? "-" : Mathf.Max(skill, 0).ToString(),
                    modifiers = modifiers
                };
            }
        }

        public SkillSummary PoliticalSkillSummary
        {
            get
            {
                if (!ShowStats) return new SkillSummary();
                
                var modifiers = GetPoliticalModifiers().Select(m => new StatModifier(m)).ToList();
                int skill = (int)modifiers.Sum(m => m.amount);
                
                return new SkillSummary
                {
                    stat = float.IsNaN(skill) ? "-" : Mathf.Max(skill, 0).ToString(),
                    modifiers = modifiers
                };
            }
        }

        public SkillSummary GlorySummary
        {
            get
            {
                if (!ShowStats) return new SkillSummary();
                
                var modifiers = GetGloryModifiers().Select(m => new StatModifier(m)).ToList();
                int glory = (int)modifiers.Sum(m => m.amount);
                
                return new SkillSummary
                {
                    stat = Mathf.Max(glory, 0).ToString(),
                    modifiers = modifiers
                };
            }
        }
        #endregion

        #region IronPython Integration
        protected override void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}.py";
                game.ExecuteCardScript(scriptName, methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for card {name}: {ex.Message}");
            }
        }

        protected T ExecutePythonFunction<T>(string functionName, params object[] parameters)
        {
            try
            {
                var scriptName = $"{id}.py";
                return game.ExecuteCardFunction<T>(scriptName, functionName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python function {functionName} for card {name}: {ex.Message}");
                return default(T);
            }
        }
        #endregion

        #region Summary for Network/UI
        public override Dictionary<string, object> GetSummary(Player activePlayer, bool hideWhenFaceup = false)
        {
            var baseSummary = base.GetSummary(activePlayer, hideWhenFaceup);
            
            var summary = new Dictionary<string, object>(baseSummary)
            {
                {"attached", parent != null},
                {"attachments", attachments.Select(att => att.GetSummary(activePlayer, hideWhenFaceup)).ToList()},
                {"childCards", childCards.Select(card => card.GetSummary(activePlayer, hideWhenFaceup)).ToList()},
                {"inConflict", inConflict},
                {"isConflict", isConflict},
                {"isDynasty", isDynasty},
                {"isDishonored", IsDishonored},
                {"isHonored", IsHonored},
                {"isPlayableByMe", isConflict && controller.IsCardInPlayableLocation(this, PlayTypes.PlayFromHand)},
                {"isPlayableByOpponent", isConflict && controller.opponent != null && controller.opponent.IsCardInPlayableLocation(this, PlayTypes.PlayFromHand)},
                {"bowed", bowed},
                {"fate", fate},
                {"new", isNew},
                {"covert", covert},
                {"showStats", ShowStats},
                {"militarySkillSummary", MilitarySkillSummary},
                {"politicalSkillSummary", PoliticalSkillSummary},
                {"glorySummary", GlorySummary},
                {"controller", controller.GetShortSummary()}
            };

            return summary;
        }
        #endregion

        #region Menu System
        [System.Serializable]
        public class CardMenuOption
        {
            public string command;
            public string text;

            public CardMenuOption(string command, string text)
            {
                this.command = command;
                this.text = text;
            }
        }

        public List<CardMenuOption> GetMenuOptions()
        {
            return new List<CardMenuOption>(menuOptions);
        }

        public void ExecuteMenuCommand(string command, Player player)
        {
            switch (command)
            {
                case "bow":
                    if (bowed) Ready(); else Bow();
                    break;
                case "honor":
                    Honor();
                    break;
                case "dishonor":
                    Dishonor();
                    break;
                case "addfate":
                    ModifyFate(1);
                    break;
                case "remfate":
                    ModifyFate(-1);
                    break;
                case "move":
                    ToggleConflictParticipation();
                    break;
                case "control":
                    TransferControl(player);
                    break;
            }
            
            ExecutePythonScript("on_menu_command", command, player);
        }

        private void ToggleConflictParticipation()
        {
            if (game.currentConflict != null)
            {
                if (inConflict)
                {
                    game.currentConflict.RemoveFromConflict(this);
                }
                else if (CanParticipateAsAttacker(game.currentConflict.conflictType) || 
                         CanParticipateAsDefender(game.currentConflict.conflictType))
                {
                    game.currentConflict.AddToConflict(this);
                }
            }
        }

        private void TransferControl(Player newController)
        {
            if (newController != controller && location == Locations.PlayArea)
            {
                var oldController = controller;
                controller = newController;
                game.AddMessage("{0} takes control of {1}", newController.name, name);
                ExecutePythonScript("on_control_changed", oldController, newController);
            }
        }
        #endregion
    }

    #region Supporting Data Structures
    [System.Serializable]
    public class SkillSummary
    {
        public string stat = "0";
        public List<StatModifier> modifiers = new List<StatModifier>();
    }

    [System.Serializable]
    public class StatusToken
    {
        public Game game;
        public DrawCard card;
        public bool honored;
        public bool dishonored;

        public StatusToken(Game game, DrawCard card, bool isHonored)
        {
            this.game = game;
            this.card = card;
            this.honored = isHonored;
            this.dishonored = !isHonored;
        }

        public void SetCard(DrawCard newCard)
        {
            card = newCard;
        }
    }
    #endregion
}