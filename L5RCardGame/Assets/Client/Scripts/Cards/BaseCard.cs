using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    [System.Serializable]
    public class CardData
    {
        public string id;
        public string name;
        public string type;
        public List<string> traits = new List<string>();
        public string clan;
        public int military_bonus;
        public int political_bonus;
        public int fate;
        public bool unicity;
        public string text;
        public string flavor;
        public int glory;
        public int military;
        public int political;
        public int strength;
        public int influencePool;
        public int influenceCost;
        public string side;
        public string pack_id;
    }

    [System.Serializable]
    public class CardAbilities
    {
        public List<CardAction> actions = new List<CardAction>();
        public List<TriggeredAbility> reactions = new List<TriggeredAbility>();
        public List<PersistentEffect> persistentEffects = new List<PersistentEffect>();
        public List<CustomPlayAction> playActions = new List<CustomPlayAction>();
    }

    [System.Serializable]
    public class CardMenuOption
    {
        public string command;
        public string text;
        public string arg;
        public bool disabled;
    }

    public class BaseCard : EffectSource
    {
        [Header("Card Identity")]
        public Player owner;
        public Player controller;
        public Game game;
        public CardData cardData;

        [Header("Card Properties")]
        public string id;
        public string printedName;
        public string printedType;
        public bool inConflict = false;
        public string type;
        public bool facedown = false;

        [Header("Card State")]
        public Dictionary<string, int> tokens = new Dictionary<string, int>();
        public List<CardMenuOption> menu = new List<CardMenuOption>();
        public bool showPopup = false;
        public string popupMenuText = "";
        public List<string> traits = new List<string>();
        public string printedFaction;
        public string location;

        [Header("Card Type Flags")]
        public bool isProvince = false;
        public bool isConflict = false;
        public bool isDynasty = false;
        public bool isStronghold = false;
        public bool isNew = false;
        public bool selected = false;

        [Header("Card Relationships")]
        public List<BaseCard> attachments = new List<BaseCard>();
        public List<BaseCard> childCards = new List<BaseCard>();
        public BaseCard parent;

        [Header("Card Abilities")]
        public CardAbilities abilities = new CardAbilities();

        [Header("Keywords and Restrictions")]
        public List<string> printedKeywords = new List<string>();
        public List<string> allowedAttachmentTraits = new List<string>();
        public List<string> disguisedKeywordTraits = new List<string>();

        [Header("IronPython Integration")]
        public string scriptName;
        public bool hasCustomScript = false;

        // Static keyword validation
        private static readonly string[] ValidKeywords = {
            "ancestral", "restricted", "limited", "sincerity", 
            "courtesy", "pride", "covert"
        };

        public virtual void Initialize(CardData data, Player cardOwner)
        {
            owner = cardOwner;
            controller = cardOwner;
            game = cardOwner.game;
            cardData = data;

            // Set basic properties
            id = data.id;
            printedName = data.name;
            printedType = data.type;
            type = data.type;
            traits = data.traits ?? new List<string>();
            printedFaction = data.clan;

            // Set script name for IronPython integration
            scriptName = GenerateScriptName();

            // Initialize as EffectSource
            base.Initialize(game, printedName);

            // Set up card abilities
            SetupCardAbilities();
            ApplyAttachmentBonus();
            ParseKeywords(data.text);

            Debug.Log($"🃏 Card {printedName} initialized with script: {scriptName}");
        }

        private string GenerateScriptName()
        {
            // Convert card name to snake_case for Python script filename
            return printedName.ToLower()
                .Replace(" ", "_")
                .Replace("'", "")
                .Replace("-", "_")
                .Replace(",", "");
        }

        // Properties with effect consideration
        public virtual string Name
        {
            get
            {
                var copyEffect = MostRecentEffect(EffectNames.CopyCharacter);
                return copyEffect != null ? ((dynamic)copyEffect).printedName : printedName;
            }
            set { printedName = value; }
        }

        public virtual List<CardAction> Actions
        {
            get
            {
                var actions = abilities.actions.ToList();
                
                if (AnyEffect(EffectNames.CopyCharacter))
                {
                    var mostRecentEffect = GetRawEffects()
                        .Where(effect => effect.type == EffectNames.CopyCharacter)
                        .LastOrDefault();
                    if (mostRecentEffect != null)
                    {
                        actions = ((dynamic)mostRecentEffect.value).GetActions(this);
                    }
                }

                var effectActions = GetEffects(EffectNames.GainAbility)
                    .Where(ability => ((dynamic)ability).abilityType == AbilityTypes.Action)
                    .Cast<CardAction>()
                    .ToList();

                return actions.Concat(effectActions).ToList();
            }
        }

        public virtual List<TriggeredAbility> Reactions
        {
            get
            {
                var triggeredAbilityTypes = new[]
                {
                    AbilityTypes.ForcedInterrupt,
                    AbilityTypes.ForcedReaction,
                    AbilityTypes.Interrupt,
                    AbilityTypes.Reaction,
                    AbilityTypes.WouldInterrupt
                };

                var reactions = abilities.reactions.ToList();

                if (AnyEffect(EffectNames.CopyCharacter))
                {
                    var mostRecentEffect = GetRawEffects()
                        .Where(effect => effect.type == EffectNames.CopyCharacter)
                        .LastOrDefault();
                    if (mostRecentEffect != null)
                    {
                        reactions = ((dynamic)mostRecentEffect.value).GetReactions(this);
                    }
                }

                var effectReactions = GetEffects(EffectNames.GainAbility)
                    .Where(ability => triggeredAbilityTypes.Contains(((dynamic)ability).abilityType))
                    .Cast<TriggeredAbility>()
                    .ToList();

                return reactions.Concat(effectReactions).ToList();
            }
        }

        public virtual List<PersistentEffect> PersistentEffects
        {
            get
            {
                var gainedPersistentEffects = GetEffects(EffectNames.GainAbility)
                    .Where(ability => ((dynamic)ability).abilityType == AbilityTypes.Persistent)
                    .Cast<PersistentEffect>()
                    .ToList();

                if (AnyEffect(EffectNames.CopyCharacter))
                {
                    var mostRecentEffect = GetRawEffects()
                        .Where(effect => effect.type == EffectNames.CopyCharacter)
                        .LastOrDefault();
                    if (mostRecentEffect != null)
                    {
                        return gainedPersistentEffects
                            .Concat(((dynamic)mostRecentEffect.value).GetPersistentEffects())
                            .ToList();
                    }
                }

                return IsBlank() ? gainedPersistentEffects : 
                    abilities.persistentEffects.Concat(gainedPersistentEffects).ToList();
            }
        }

        // Card ability setup (to be overridden by specific cards)
        protected virtual void SetupCardAbilities()
        {
            // Base implementation - specific cards will override this
            // This is where card-specific abilities are defined
        }

        // Ability creation methods
        public void Action(ActionProperties properties)
        {
            abilities.actions.Add(CreateAction(properties));
        }

        public virtual CardAction CreateAction(ActionProperties properties)
        {
            return new CardAction(game, this, properties);
        }

        public void TriggeredAbility(string abilityType, TriggeredAbilityProperties properties)
        {
            abilities.reactions.Add(CreateTriggeredAbility(abilityType, properties));
        }

        public virtual TriggeredAbility CreateTriggeredAbility(string abilityType, TriggeredAbilityProperties properties)
        {
            return new TriggeredAbility(game, this, abilityType, properties);
        }

        public void Reaction(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.Reaction, properties);
        }

        public void ForcedReaction(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.ForcedReaction, properties);
        }

        public void WouldInterrupt(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.WouldInterrupt, properties);
        }

        public void Interrupt(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.Interrupt, properties);
        }

        public void ForcedInterrupt(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.ForcedInterrupt, properties);
        }

        public void PlayAction(CustomPlayActionProperties properties)
        {
            abilities.playActions.Add(new CustomPlayAction(properties));
        }

        public void PersistentEffect(PersistentEffectProperties properties)
        {
            var allowedLocations = new[]
            {
                Locations.Any, Locations.ConflictDiscardPile, 
                Locations.PlayArea, Locations.Provinces
            };

            var defaultLocationForType = new Dictionary<string, string>
            {
                {"province", Locations.Provinces},
                {"holding", Locations.Provinces},
                {"stronghold", Locations.Provinces}
            };

            string location = properties.location ?? 
                             defaultLocationForType.GetValueOrDefault(GetCardType(), Locations.PlayArea);

            if (!allowedLocations.Contains(location))
            {
                throw new System.Exception($"'{location}' is not a supported effect location.");
            }

            var effect = new PersistentEffect
            {
                duration = Durations.Persistent,
                location = location,
                effect = properties.effect,
                condition = properties.condition,
                match = properties.match,
                targetController = properties.targetController
            };

            abilities.persistentEffects.Add(effect);
        }

        public void AttachmentConditions(AttachmentConditionProperties properties)
        {
            var effects = new List<object>();

            if (properties.limit > 0)
            {
                effects.Add(Effects.AttachmentLimit(properties.limit));
            }

            if (properties.myControl)
            {
                effects.Add(Effects.AttachmentMyControlOnly());
            }

            if (properties.unique)
            {
                effects.Add(Effects.AttachmentUniqueRestriction());
            }

            if (properties.faction != null)
            {
                var factions = properties.faction is List<string> ? 
                    (List<string>)properties.faction : new List<string> { (string)properties.faction };
                effects.Add(Effects.AttachmentFactionRestriction(factions));
            }

            if (properties.trait != null)
            {
                var traits = properties.trait is List<string> ? 
                    (List<string>)properties.trait : new List<string> { (string)properties.trait };
                effects.Add(Effects.AttachmentTraitRestriction(traits));
            }

            if (properties.limitTrait != null)
            {
                var traitLimits = properties.limitTrait is List<Dictionary<string, int>> ? 
                    (List<Dictionary<string, int>>)properties.limitTrait : 
                    new List<Dictionary<string, int>> { (Dictionary<string, int>)properties.limitTrait };

                foreach (var traitLimit in traitLimits)
                {
                    foreach (var kvp in traitLimit)
                    {
                        effects.Add(Effects.AttachmentRestrictTraitAmount(new Dictionary<string, int> { {kvp.Key, kvp.Value} }));
                    }
                }
            }

            if (effects.Count > 0)
            {
                PersistentEffect(new PersistentEffectProperties
                {
                    location = Locations.Any,
                    effect = effects
                });
            }
        }

        public void Composure(PersistentEffectProperties properties)
        {
            var composureProperties = new PersistentEffectProperties
            {
                condition = (context) => ((Player)context.player).HasComposure(),
                effect = properties.effect,
                match = properties.match,
                targetController = properties.targetController,
                location = properties.location
            };

            PersistentEffect(composureProperties);
        }

        // Trait and faction management
        public bool HasTrait(string trait)
        {
            trait = trait.ToLower();
            return GetTraits().Contains(trait) || GetEffects(EffectNames.AddTrait).Contains(trait);
        }

        public List<string> GetTraits()
        {
            var copyEffect = MostRecentEffect(EffectNames.CopyCharacter);
            var cardTraits = copyEffect != null ? 
                ((dynamic)copyEffect).traits : 
                (GetEffects(EffectNames.Blank).Any() ? new List<string>() : traits);

            var additionalTraits = GetEffects(EffectNames.AddTrait).Cast<string>().ToList();
            return cardTraits.Concat(additionalTraits).Distinct().ToList();
        }

        public bool IsFaction(string faction)
        {
            faction = faction.ToLower();
            if (faction == "neutral")
            {
                return printedFaction == faction && !AnyEffect(EffectNames.AddFaction);
            }
            return printedFaction == faction || GetEffects(EffectNames.AddFaction).Contains(faction);
        }

        // Location and state checks
        public bool IsInProvince()
        {
            var provinceLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree,
                Locations.ProvinceFour, Locations.StrongholdProvince
            };
            return provinceLocations.Contains(location);
        }

        public bool IsInPlay()
        {
            if (facedown) return false;

            var inProvinceTypes = new[] { CardTypes.Holding, CardTypes.Province, CardTypes.Stronghold };
            if (inProvinceTypes.Contains(type))
            {
                return IsInProvince();
            }

            return location == Locations.PlayArea;
        }

        public void ApplyAnyLocationPersistentEffects()
        {
            foreach (var effect in PersistentEffects.Where(e => e.location == Locations.Any))
            {
                effect.reference = AddEffectToEngine(effect);
            }
        }

        // Card lifecycle events
        public virtual void LeavesPlay()
        {
            tokens.Clear();
            
            foreach (var action in abilities.actions)
            {
                action.limit?.Reset();
            }
            
            foreach (var reaction in abilities.reactions)
            {
                reaction.limit?.Reset();
            }

            controller = owner;
            inConflict = false;

            // Execute Python script for leaving play
            ExecutePythonScript("on_leave_play");
        }

        public virtual void MoveTo(string targetLocation)
        {
            string originalLocation = location;
            location = targetLocation;

            var visibleLocations = new[]
            {
                Locations.PlayArea, Locations.ConflictDiscardPile, 
                Locations.DynastyDiscardPile, Locations.Hand
            };

            if (visibleLocations.Contains(targetLocation))
            {
                facedown = false;
            }

            if (originalLocation != targetLocation)
            {
                UpdateAbilityEvents(originalLocation, targetLocation);
                UpdateEffects(originalLocation, targetLocation);
                
                game.EmitEvent(EventNames.OnCardMoved, new Dictionary<string, object>
                {
                    {"card", this},
                    {"originalLocation", originalLocation},
                    {"newLocation", targetLocation}
                });

                // Execute Python script for movement
                ExecutePythonScript("on_move", originalLocation, targetLocation);
            }
        }

        public void UpdateAbilityEvents(string from, string to)
        {
            foreach (var reaction in Reactions)
            {
                reaction.limit?.Reset();
                
                if (type == CardTypes.Event)
                {
                    if (to == Locations.ConflictDeck || 
                        controller.IsCardInPlayableLocation(this, null) || 
                        (controller.opponent?.IsCardInPlayableLocation(this, null) ?? false))
                    {
                        reaction.RegisterEvents();
                    }
                    else
                    {
                        reaction.UnregisterEvents();
                    }
                }
                else if (reaction.location.Contains(to) && !reaction.location.Contains(from))
                {
                    reaction.RegisterEvents();
                }
                else if (!reaction.location.Contains(to) && reaction.location.Contains(from))
                {
                    reaction.UnregisterEvents();
                }
            }

            foreach (var action in abilities.actions)
            {
                action.limit?.Reset();
            }
        }

        public void UpdateEffects(string from, string to)
        {
            var activeLocations = new Dictionary<string, string[]>
            {
                {"conflict discard pile", new[] { Locations.ConflictDiscardPile }},
                {"play area", new[] { Locations.PlayArea }},
                {"province", new[] { 
                    Locations.ProvinceOne, Locations.ProvinceTwo, 
                    Locations.ProvinceThree, Locations.ProvinceFour, 
                    Locations.StrongholdProvince 
                }}
            };

            if (!activeLocations[Locations.Provinces].Contains(from) || 
                !activeLocations[Locations.Provinces].Contains(to))
            {
                RemoveLastingEffects();
            }

            foreach (var effect in PersistentEffects.Where(e => e.location != Locations.Any))
            {
                if (activeLocations.ContainsKey(effect.location))
                {
                    var locations = activeLocations[effect.location];
                    
                    if (locations.Contains(to) && !locations.Contains(from))
                    {
                        effect.reference = AddEffectToEngine(effect);
                    }
                    else if (!locations.Contains(to) && locations.Contains(from))
                    {
                        RemoveEffectFromEngine(effect.reference);
                        effect.reference = null;
                    }
                }
            }
        }

        public void UpdateEffectContexts()
        {
            foreach (var effect in PersistentEffects.Where(e => e.reference != null))
            {
                foreach (var e in (List<object>)effect.reference)
                {
                    ((dynamic)e).RefreshContext();
                }
            }
        }

        // Ability triggers and restrictions
        public bool CanTriggerAbilities(AbilityContext context)
        {
            return !facedown && CheckRestrictions("triggerAbilities", context);
        }

        public bool CanInitiateKeywords(AbilityContext context)
        {
            return !facedown && CheckRestrictions("initiateKeywords", context);
        }

        public int GetModifiedLimitMax(Player player, CardAbility ability, int max)
        {
            var effects = GetRawEffects()
                .Where(effect => effect.type == EffectNames.IncreaseLimitOnAbilities);

            return effects.Aggregate(max, (total, effect) =>
            {
                var value = effect.GetValue(this);
                if ((value is bool && (bool)value) || value == ability)
                {
                    if (((dynamic)effect.context).player == player)
                        return total + 1;
                }
                return total;
            });
        }

        // Menu system
        public List<CardMenuOption> GetMenu()
        {
            var cardMenu = new List<CardMenuOption>();

            var validLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree,
                Locations.ProvinceFour, Locations.StrongholdProvince, Locations.PlayArea
            };

            if (menu.Count == 0 || !game.manualMode || !validLocations.Contains(location))
            {
                return null;
            }

            if (facedown)
            {
                return new List<CardMenuOption>
                {
                    new CardMenuOption { command = "reveal", text = "Reveal" }
                };
            }

            cardMenu.Add(new CardMenuOption { command = "click", text = "Select Card" });
            
            if (location == Locations.PlayArea || isProvince || isStronghold)
            {
                cardMenu.AddRange(menu);
            }

            return cardMenu;
        }

        // Combat state checks
        public virtual bool IsConflictProvince()
        {
            return false;
        }

        public bool IsAttacking()
        {
            return game.currentConflict?.IsAttacking(this) ?? false;
        }

        public bool IsDefending()
        {
            return game.currentConflict?.IsDefending(this) ?? false;
        }

        public bool IsParticipating()
        {
            return game.currentConflict?.IsParticipating(this) ?? false;
        }

        // Card properties
        public bool IsUnique()
        {
            return cardData.unicity;
        }

        public bool IsBlank()
        {
            return AnyEffect(EffectNames.Blank) || AnyEffect(EffectNames.CopyCharacter);
        }

        public string GetPrintedFaction()
        {
            return cardData.clan;
        }

        public virtual string GetCardType()
        {
            return type;
        }

        public virtual int GetCost()
        {
            return cardData.fate;
        }

        public override bool CheckRestrictions(string actionType, AbilityContext context)
        {
            return base.CheckRestrictions(actionType, context) && 
                   controller.CheckRestrictions(actionType, context);
        }

        // Token management
        public void AddToken(string tokenType, int number = 1)
        {
            if (!tokens.ContainsKey(tokenType))
            {
                tokens[tokenType] = 0;
            }
            tokens[tokenType] += number;
        }

        public int GetTokenCount(string tokenType)
        {
            return tokens.GetValueOrDefault(tokenType, 0);
        }

        public bool HasToken(string tokenType)
        {
            return tokens.ContainsKey(tokenType) && tokens[tokenType] > 0;
        }

        public void RemoveToken(string tokenType, int number)
        {
            if (!tokens.ContainsKey(tokenType)) return;

            tokens[tokenType] -= number;

            if (tokens[tokenType] <= 0)
            {
                tokens.Remove(tokenType);
            }
        }

        // Ability getters
        public List<CardAction> GetActions()
        {
            return Actions.ToList();
        }

        public List<TriggeredAbility> GetReactions()
        {
            return Reactions.ToList();
        }

        public virtual int GetProvinceStrengthBonus()
        {
            return 0;
        }

        public bool ReadiesDuringReadyPhase()
        {
            return !AnyEffect(EffectNames.DoesNotReady);
        }

        public bool HideWhenFacedown()
        {
            return !AnyEffect(EffectNames.CanBeSeenWhenFacedown);
        }

        public virtual Dictionary<string, object> CreateSnapshot()
        {
            return new Dictionary<string, object>();
        }

        // Keyword parsing
        public void ParseKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split('\n');
            var potentialKeywords = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('.');
                foreach (var keyword in trimmedLine.Split(new[] { ". " }, StringSplitOptions.None))
                {
                    potentialKeywords.Add(keyword);
                }
            }

            printedKeywords.Clear();
            allowedAttachmentTraits.Clear();
            disguisedKeywordTraits.Clear();

            foreach (var keyword in potentialKeywords)
            {
                if (ValidKeywords.Contains(keyword))
                {
                    printedKeywords.Add(keyword);
                }
                else if (keyword.StartsWith("disguised "))
                {
                    disguisedKeywordTraits.Add(keyword.Replace("disguised ", ""));
                }
                else if (keyword.StartsWith("no attachments except"))
                {
                    var traits = keyword.Replace("no attachments except ", "");
                    allowedAttachmentTraits = traits.Split(new[] { " or " }, StringSplitOptions.None).ToList();
                }
                else if (keyword.StartsWith("no attachments"))
                {
                    allowedAttachmentTraits = new List<string> { "none" };
                }
            }

            // Apply keyword effects
            foreach (var keyword in printedKeywords)
            {
                PersistentEffect(new PersistentEffectProperties
                {
                    effect = new List<object> { Effects.AddKeyword(keyword) }
                });
            }
        }

        // Attachment bonus application
        public void ApplyAttachmentBonus()
        {
            int militaryBonus = cardData.military_bonus;
            if (militaryBonus != 0)
            {
                PersistentEffect(new PersistentEffectProperties
                {
                    match = (card) => card == parent,
                    targetController = Players.Any,
                    effect = new List<object> { Effects.AttachmentMilitarySkillModifier(militaryBonus) }
                });
            }

            int politicalBonus = cardData.political_bonus;
            if (politicalBonus != 0)
            {
                PersistentEffect(new PersistentEffectProperties
                {
                    match = (card) => card == parent,
                    targetController = Players.Any,
                    effect = new List<object> { Effects.AttachmentPoliticalSkillModifier(politicalBonus) }
                });
            }
        }

        // IronPython Integration
        public void ExecutePythonScript(string eventType, params object[] parameters)
        {
            if (hasCustomScript && !string.IsNullOrEmpty(scriptName))
            {
                var allParams = new List<object> { this }.Concat(parameters).ToArray();
                game.ExecuteCardScript(scriptName, eventType, allParams);
            }
        }

        public virtual void OnCardPlayed()
        {
            ExecutePythonScript("on_card_played", controller, new Dictionary<string, object>());
        }

        public virtual void OnEnterPlay()
        {
            ExecutePythonScript("on_enter_play", controller);
        }

        public virtual void OnConflict(Conflict conflict)
        {
            ExecutePythonScript("on_conflict", conflict);
        }

        public virtual bool CanTrigger(string eventName, Dictionary<string, object> eventData)
        {
            if (hasCustomScript)
            {
                var result = game.ExecuteCardScript(scriptName, "can_trigger", this, eventName, eventData);
                return result is bool ? (bool)result : false;
            }
            return false;
        }

        public virtual void OnTrigger(string eventName, Dictionary<string, object> eventData)
        {
            ExecutePythonScript("on_trigger", eventName, eventData);
        }

        // Reset for conflict
        public virtual void ResetForConflict()
        {
            // Override in derived classes if needed
        }

        // Attachment system
        public void CheckForIllegalAttachments()
        {
            var context = game.GetFrameworkContext(controller);
            var illegalAttachments = attachments.Where(attachment =>
                !AllowAttachment(attachment) || 
                !attachment.CanAttach(this, context, false)).ToList();

            // Check restricted attachment limits and other restrictions
            foreach (var effectCard in GetEffects(EffectNames.CannotHaveOtherRestrictedAttachments))
            {
                illegalAttachments.AddRange(attachments.Where(card => 
                    card.IsRestricted() && card != effectCard));
            }

            // Handle attachment limits
            foreach (var card in attachments.Where(card => card.AnyEffect(EffectNames.AttachmentLimit)))
            {
                var limit = card.GetEffects(EffectNames.AttachmentLimit).Cast<int>().Max();
                var matchingAttachments = attachments.Where(attachment => attachment.id == card.id).ToList();
                if (matchingAttachments.Count > limit)
                {
                    illegalAttachments.AddRange(matchingAttachments.Skip(limit));
                }
            }

            illegalAttachments = illegalAttachments.Distinct().ToList();

            // Handle too many restricted attachments
            var restrictedAttachments = attachments.Where(card => card.IsRestricted()).ToList();
            if (restrictedAttachments.Count > 2)
            {
                game.PromptForSelect(controller, new SelectCardPromptProperties
                {
                    activePromptTitle = "Choose an attachment to discard",
                    waitingPromptTitle = "Waiting for opponent to choose an attachment to discard",
                    controller = Players.Self,
                    cardCondition = (card) => card.parent == this && card.IsRestricted(),
                    onSelect = (player, card) =>
                    {
                        game.AddMessage("{0} discards {1} from {2} due to too many Restricted attachments", 
                                       player, card, card.parent);
                        
                        if (!illegalAttachments.Contains(card))
                        {
                            illegalAttachments.Add(card);
                        }
                        
                        game.ApplyGameAction(context, new Dictionary<string, object>
                        {
                            {"discardFromPlay", illegalAttachments}
                        });
                        return true;
                    },
                    source = "Too many Restricted attachments"
                });
            }
            else if (illegalAttachments.Count > 0)
            {
                game.AddMessage("{0} {1} discarded from {2} as {3} {1} no longer legally attached", 
                               illegalAttachments, 
                               illegalAttachments.Count > 1 ? "are" : "is",
                               this,
                               illegalAttachments.Count > 1 ? "they" : "it");
                               
                game.ApplyGameAction(context, new Dictionary<string, object>
                {
                    {"discardFromPlay", illegalAttachments}
                });
            }
        }

        public virtual bool MustAttachToRing()
        {
            return false;
        }

        public virtual bool CanPlayOn(BaseCard card)
        {
            return true;
        }

        public bool AllowAttachment(BaseCard attachment)
        {
            if (allowedAttachmentTraits.Any(trait => attachment.HasTrait(trait)))
            {
                return true;
            }

            return IsBlank() || allowedAttachmentTraits.Count == 0;
        }

        public void WhileAttached(WhileAttachedProperties properties)
        {
            PersistentEffect(new PersistentEffectProperties
            {
                condition = properties.condition ?? ((context) => true),
                match = (card, context) => card == parent && 
                                          (properties.match == null || properties.match(card, context)),
                targetController = Players.Any,
                effect = properties.effect
            });
        }

        public bool CanAttach(BaseCard parent, AbilityContext context, bool ignoreType = false)
        {
            if (parent == null || parent.GetCardType() != CardTypes.Character || 
                (!ignoreType && GetCardType() != CardTypes.Attachment))
            {
                return false;
            }

            if (AnyEffect(EffectNames.AttachmentMyControlOnly) && 
                context.player != parent.controller && controller != parent.controller)
            {
                return false;
            }

            if (AnyEffect(EffectNames.AttachmentUniqueRestriction) && !parent.IsUnique())
            {
                return false;
            }

            var factionRestrictions = GetEffects(EffectNames.AttachmentFactionRestriction);
            if (factionRestrictions.Any(factions => 
                !((List<string>)factions).Any(faction => parent.IsFaction(faction))))
            {
                return false;
            }

            var traitRestrictions = GetEffects(EffectNames.AttachmentTraitRestriction);
            if (traitRestrictions.Any(traits => 
                !((List<string>)traits).Any(trait => parent.HasTrait(trait))))
            {
                return false;
            }

            return true;
        }

        public List<object> GetPlayActions()
        {
            if (type == CardTypes.Event)
            {
                return GetActions().Cast<object>().ToList();
            }

            var actions = abilities.playActions.Cast<object>().ToList();

            if (type == CardTypes.Character)
            {
                if (disguisedKeywordTraits.Count > 0)
                {
                    actions.Add(new PlayDisguisedCharacterAction(this));
                }

                if (isDynasty)
                {
                    actions.Add(new DynastyCardAction(this));
                }
                else
                {
                    actions.Add(new PlayCharacterAction(this));
                }
            }
            else if (type == CardTypes.Attachment && MustAttachToRing())
            {
                actions.Add(new PlayAttachmentOnRingAction(this));
            }
            else if (type == CardTypes.Attachment)
            {
                actions.Add(new PlayAttachmentAction(this));
            }

            return actions;
        }

        public void RemoveAttachment(BaseCard attachment)
        {
            attachments = attachments.Where(card => card.uuid != attachment.uuid).ToList();
        }

        public void AddChildCard(BaseCard card, string location)
        {
            childCards.Add(card);
            controller.MoveCard(card, location);
        }

        public void RemoveChildCard(BaseCard card, string location)
        {
            if (card == null) return;

            childCards.Remove(card);
            controller.MoveCard(card, location);
        }

        // Derived card types can override these methods
        public virtual bool IsRestricted()
        {
            return HasTrait("restricted") || printedKeywords.Contains("restricted");
        }

        public virtual Player GetModifiedController()
        {
            // Check for control-changing effects
            var controlEffects = GetEffects(EffectNames.TakeControl);
            if (controlEffects.Any())
            {
                return (Player)controlEffects.Last();
            }
            return controller;
        }

        public virtual void SetDefaultController(Player player)
        {
            controller = player;
        }

        public virtual bool AllowGameAction(string actionType)
        {
            // Check if this card allows the specified game action
            return !GetEffects($"cannot{actionType}").Any();
        }

        public virtual int GetContributionToImperialFavor()
        {
            // Override in character cards to return glory value
            return 0;
        }

        // Summary methods for UI
        public Dictionary<string, object> GetShortSummaryForControls(Player activePlayer)
        {
            if (facedown && (activePlayer != controller || HideWhenFacedown()))
            {
                return new Dictionary<string, object>
                {
                    {"facedown", true},
                    {"isDynasty", isDynasty},
                    {"isConflict", isConflict}
                };
            }

            return base.GetShortSummaryForControls(activePlayer);
        }

        public virtual Dictionary<string, object> GetSummary(Player activePlayer, bool hideWhenFaceup = false)
        {
            bool isActivePlayer = activePlayer == controller;
            var selectionState = activePlayer.GetCardSelectionState(this);

            // Handle facedown or hidden cards
            if (isActivePlayer ? (facedown && HideWhenFacedown()) : 
                (facedown || hideWhenFaceup || AnyEffect(EffectNames.HideWhenFaceUp)))
            {
                var hiddenState = new Dictionary<string, object>
                {
                    {"controller", controller.GetShortSummary()},
                    {"facedown", true},
                    {"inConflict", inConflict},
                    {"location", location}
                };

                foreach (var kvp in selectionState)
                {
                    hiddenState[kvp.Key] = kvp.Value;
                }

                return hiddenState;
            }

            var state = new Dictionary<string, object>
            {
                {"id", cardData.id},
                {"controlled", owner != controller},
                {"inConflict", inConflict},
                {"facedown", facedown},
                {"location", location},
                {"menu", GetMenu()},
                {"name", cardData.name},
                {"popupMenuText", popupMenuText},
                {"showPopup", showPopup},
                {"tokens", tokens},
                {"type", GetCardType()},
                {"uuid", uuid},
                {"selected", selected},
                {"traits", GetTraits()},
                {"faction", printedFaction},
                {"cost", GetCost()},
                {"scriptName", scriptName},
                {"hasCustomScript", hasCustomScript}
            };

            foreach (var kvp in selectionState)
            {
                state[kvp.Key] = kvp.Value;
            }

            return state;
        }

        // Cleanup when destroyed
        protected virtual void OnDestroy()
        {
            // Clean up any remaining effects
            RemoveLastingEffects();
            
            // Clear references
            attachments.Clear();
            childCards.Clear();
            
            Debug.Log($"🃏 Card {printedName} destroyed");
        }
    }

    // Supporting classes and interfaces
    [System.Serializable]
    public class ActionProperties
    {
        public string title;
        public int cost;
        public object condition;
        public object target;
        public object effect;
        public object limit;
        public int phase;
        public string location;
    }

    [System.Serializable]
    public class TriggeredAbilityProperties
    {
        public string title;
        public object when;
        public int cost;
        public object condition;
        public object target;
        public object effect;
        public object limit;
        public List<string> location;
    }

    [System.Serializable]
    public class PersistentEffectProperties
    {
        public string location;
        public object effect;
        public object condition;
        public object match;
        public string targetController;
    }

    [System.Serializable]
    public class AttachmentConditionProperties
    {
        public int limit;
        public bool myControl;
        public bool unique;
        public object faction;
        public object trait;
        public object limitTrait;
    }

    [System.Serializable]
    public class CustomPlayActionProperties
    {
        public string title;
        public object condition;
        public object target;
        public object effect;
        public string location;
    }

    [System.Serializable]
    public class WhileAttachedProperties
    {
        public object condition;
        public object match;
        public object effect;
    }

    [System.Serializable]
    public class PersistentEffect
    {
        public string duration;
        public string location;
        public object effect;
        public object condition;
        public object match;
        public string targetController;
        public object reference;
    }

    [System.Serializable]
    public class SelectCardPromptProperties
    {
        public string activePromptTitle;
        public string waitingPromptTitle;
        public string controller;
        public System.Func<BaseCard, bool> cardCondition;
        public System.Func<Player, BaseCard, bool> onSelect;
        public string source;
    }

    // Static classes for constants
    public static class CardTypes
    {
        public const string Character = "character";
        public const string Event = "event";
        public const string Attachment = "attachment";
        public const string Holding = "holding";
        public const string Province = "province";
        public const string Stronghold = "stronghold";
        public const string Role = "role";
    }

    public static class AbilityTypes
    {
        public const string Action = "action";
        public const string Reaction = "reaction";
        public const string ForcedReaction = "forcedreaction";
        public const string Interrupt = "interrupt";
        public const string ForcedInterrupt = "forcedinterrupt";
        public const string WouldInterrupt = "wouldinterrupt";
        public const string Persistent = "persistent";
    }

    public static class Durations
    {
        public const string Persistent = "persistent";
        public const string UntilEndOfRound = "untilEndOfRound";
        public const string UntilEndOfPhase = "untilEndOfPhase";
        public const string UntilEndOfConflict = "untilEndOfConflict";
    }

    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
        public const string Any = "any";
    }

    public static class Locations
    {
        public const string Hand = "hand";
        public const string PlayArea = "play area";
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
        public const string ConflictDiscardPile = "conflict discard pile";
        public const string DynastyDiscardPile = "dynasty discard pile";
        public const string RemovedFromGame = "removed from game";
        public const string ProvinceOne = "province 1";
        public const string ProvinceTwo = "province 2";
        public const string ProvinceThree = "province 3";
        public const string ProvinceFour = "province 4";
        public const string StrongholdProvince = "stronghold province";
        public const string Provinces = "province";
        public const string ProvinceDeck = "province deck";
        public const string BeingPlayed = "being played";
        public const string Any = "any";
        public const string Role = "role";
    }

    public static class EventNames
    {
        public const string OnCardMoved = "onCardMoved";
        public const string OnCardPlayed = "onCardPlayed";
        public const string OnFateCollected = "onFateCollected";
        public const string OnDeckShuffled = "onDeckShuffled";
    }

    public static class EffectNames
    {
        public const string CopyCharacter = "copyCharacter";
        public const string GainAbility = "gainAbility";
        public const string Blank = "blank";
        public const string AddTrait = "addTrait";
        public const string AddFaction = "addFaction";
        public const string DoesNotReady = "doesNotReady";
        public const string CanBeSeenWhenFacedown = "canBeSeenWhenFacedown";
        public const string HideWhenFaceUp = "hideWhenFaceUp";
        public const string AttachmentLimit = "attachmentLimit";
        public const string AttachmentMyControlOnly = "attachmentMyControlOnly";
        public const string AttachmentUniqueRestriction = "attachmentUniqueRestriction";
        public const string AttachmentFactionRestriction = "attachmentFactionRestriction";
        public const string AttachmentTraitRestriction = "attachmentTraitRestriction";
        public const string AttachmentRestrictTraitAmount = "attachmentRestrictTraitAmount";
        public const string CannotHaveOtherRestrictedAttachments = "cannotHaveOtherRestrictedAttachments";
        public const string TakeControl = "takeControl";
        public const string IncreaseLimitOnAbilities = "increaseLimitOnAbilities";
    }

    // Effects static class for creating effects
    public static class Effects
    {
        public static object AttachmentLimit(int limit)
        {
            return new { type = EffectNames.AttachmentLimit, value = limit };
        }

        public static object AttachmentMyControlOnly()
        {
            return new { type = EffectNames.AttachmentMyControlOnly, value = true };
        }

        public static object AttachmentUniqueRestriction()
        {
            return new { type = EffectNames.AttachmentUniqueRestriction, value = true };
        }

        public static object AttachmentFactionRestriction(List<string> factions)
        {
            return new { type = EffectNames.AttachmentFactionRestriction, value = factions };
        }

        public static object AttachmentTraitRestriction(List<string> traits)
        {
            return new { type = EffectNames.AttachmentTraitRestriction, value = traits };
        }

        public static object AttachmentRestrictTraitAmount(Dictionary<string, int> traitLimits)
        {
            return new { type = EffectNames.AttachmentRestrictTraitAmount, value = traitLimits };
        }

        public static object AttachmentMilitarySkillModifier(int bonus)
        {
            return new { type = "attachmentMilitarySkillModifier", value = bonus };
        }

        public static object AttachmentPoliticalSkillModifier(int bonus)
        {
            return new { type = "attachmentPoliticalSkillModifier", value = bonus };
        }

        public static object AddKeyword(string keyword)
        {
            return new { type = "addKeyword", value = keyword };
        }
    }

    // Card-specific action classes (placeholders - will be implemented separately)
    public class CardAction
    {
        public object limit;
        public Game game;
        public BaseCard source;
        public ActionProperties properties;

        public CardAction(Game gameInstance, BaseCard card, ActionProperties props)
        {
            game = gameInstance;
            source = card;
            properties = props;
        }
    }

    public class TriggeredAbility
    {
        public object limit;
        public List<string> location = new List<string>();
        public Game game;
        public BaseCard source;
        public string abilityType;
        public TriggeredAbilityProperties properties;

        public TriggeredAbility(Game gameInstance, BaseCard card, string type, TriggeredAbilityProperties props)
        {
            game = gameInstance;
            source = card;
            abilityType = type;
            properties = props;
            location = props.location ?? new List<string> { Locations.PlayArea };
        }

        public void RegisterEvents() { }
        public void UnregisterEvents() { }
    }

    public class CustomPlayAction
    {
        public CustomPlayActionProperties properties;

        public CustomPlayAction(CustomPlayActionProperties props)
        {
            properties = props;
        }
    }

    // Placeholder action classes
    public class PlayDisguisedCharacterAction { public PlayDisguisedCharacterAction(BaseCard card) { } }
    public class DynastyCardAction { public DynastyCardAction(BaseCard card) { } }
    public class PlayCharacterAction { public PlayCharacterAction(BaseCard card) { } }
    public class PlayAttachmentAction { public PlayAttachmentAction(BaseCard card) { } }
    public class PlayAttachmentOnRingAction { public PlayAttachmentOnRingAction(BaseCard card) { } }
}