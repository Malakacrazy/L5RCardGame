using System;

namespace L5RGame
{
    /// <summary>
    /// Card and game object locations within the game state
    /// </summary>
    public static class Locations
    {
        public const string Any = "any";
        public const string Hand = "hand";
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
        public const string ConflictDiscardPile = "conflict discard pile";
        public const string DynastyDiscardPile = "dynasty discard pile";
        public const string PlayArea = "play area";
        public const string Provinces = "province";
        public const string ProvinceOne = "province 1";
        public const string ProvinceTwo = "province 2";
        public const string ProvinceThree = "province 3";
        public const string ProvinceFour = "province 4";
        public const string StrongholdProvince = "stronghold province";
        public const string ProvinceDeck = "province deck";
        public const string RemovedFromGame = "removed from game";
        public const string UnderneathStronghold = "underneath stronghold";
        public const string BeingPlayed = "being played";
        public const string Role = "role";

        /// <summary>
        /// Get all province locations
        /// </summary>
        public static string[] GetAllProvinces()
        {
            return new string[] 
            { 
                ProvinceOne, ProvinceTwo, ProvinceThree, ProvinceFour, StrongholdProvince 
            };
        }

        /// <summary>
        /// Check if location is a province
        /// </summary>
        public static bool IsProvince(string location)
        {
            return location == ProvinceOne || location == ProvinceTwo || 
                   location == ProvinceThree || location == ProvinceFour || 
                   location == StrongholdProvince;
        }

        /// <summary>
        /// Check if location is a deck
        /// </summary>
        public static bool IsDeck(string location)
        {
            return location == ConflictDeck || location == DynastyDeck || location == ProvinceDeck;
        }

        /// <summary>
        /// Check if location is a discard pile
        /// </summary>
        public static bool IsDiscardPile(string location)
        {
            return location == ConflictDiscardPile || location == DynastyDiscardPile;
        }
    }

    /// <summary>
    /// Deck types for card categorization
    /// </summary>
    public static class Decks
    {
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
    }

    /// <summary>
    /// Effect names for the effect system
    /// </summary>
    public static class EffectNames
    {
        // Ability restrictions
        public const string AbilityRestrictions = "abilityRestrictions";
        
        // Card property modifications
        public const string AddElementAsAttacker = "addElementAsAttacker";
        public const string AddFaction = "addFaction";
        public const string AddKeyword = "addKeyword";
        public const string AddTrait = "addTrait";
        public const string LoseKeyword = "loseKeyword";
        
        // Attachment restrictions
        public const string AttachmentFactionRestriction = "attachmentFactionRestriction";
        public const string AttachmentLimit = "attachmentLimit";
        public const string AttachmentMyControlOnly = "attachmentMyControlOnly";
        public const string AttachmentRestrictTraitAmount = "attachmentRestrictTraitAmount";
        public const string AttachmentTraitRestriction = "attachmentTraitRestriction";
        public const string AttachmentUniqueRestriction = "attachmentUniqueRestriction";
        
        // Card state effects
        public const string Blank = "blank";
        public const string CopyCharacter = "copyCharacter";
        public const string ChangeType = "changeType";
        public const string TakeControl = "takeControl";
        
        // Visibility effects
        public const string CanBeSeenWhenFacedown = "canBeSeenWhenFacedown";
        public const string HideWhenFaceUp = "hideWhenFaceUp";
        
        // Participation restrictions
        public const string CanOnlyBeDeclaredAsAttackerWithElement = "canOnlyBeDeclaredAsAttackerWithElement";
        public const string CannotBeAttacked = "cannotBeAttacked";
        public const string CannotContribute = "cannotContribute";
        public const string CannotHaveConflictsDeclaredOfType = "cannotHaveConflictsDeclaredOfType";
        public const string CannotParticipateAsAttacker = "cannotParticipateAsAttacker";
        public const string CannotParticipateAsDefender = "cannotParticipateAsDefender";
        public const string MustBeDeclaredAsAttacker = "mustBeDeclaredAsAttacker";
        public const string MustBeDeclaredAsDefender = "mustBeDeclaredAsDefender";
        public const string MustBeChosen = "mustBeChosen";
        
        // Combat effects
        public const string ChangeContributionFunction = "changeContributionFunction";
        public const string ContributeToConflict = "contribute";
        public const string ChangeConflictSkillFunction = "skillFunction";
        
        // Skill modifications
        public const string CalculatePrintedMilitarySkill = "calculatePrintedMilitarySkill";
        public const string ModifyBaseMilitarySkillMultiplier = "modifyBaseMilitarySkillMultiplier";
        public const string ModifyBasePoliticalSkillMultiplier = "modifyBasePoliticalSkillMultiplier";
        public const string ModifyBothSkills = "modifyBothSkills";
        public const string ModifyMilitarySkill = "modifyMilitarySkill";
        public const string AttachmentMilitarySkillModifier = "attachmentMilitarySkillModifier";
        public const string ModifyMilitarySkillMultiplier = "modifyMilitarySkillMultiplier";
        public const string ModifyPoliticalSkill = "modifyPoliticalSkill";
        public const string AttachmentPoliticalSkillModifier = "attachmentPoliticalSkillModifier";
        public const string ModifyPoliticalSkillMultiplier = "modifyPoliticalSkillMultiplier";
        public const string SetBaseDash = "setBaseDash";
        public const string SetBaseMilitarySkill = "setBaseMilitarySkill";
        public const string SetBasePoliticalSkill = "setBasePoliticalSkill";
        public const string SetDash = "setDash";
        public const string SetMilitarySkill = "setMilitarySkill";
        public const string SetPoliticalSkill = "setPoliticalSkill";
        public const string SwitchBaseSkills = "switchBaseSkills";
        
        // Glory effects
        public const string ModifyGlory = "modifyGlory";
        public const string SetGlory = "setGlory";
        public const string SetBaseGlory = "setBaseGlory";
        
        // Province effects
        public const string ModifyBaseProvinceStrength = "modifyBaseProvinceStrength";
        public const string ModifyProvinceStrengthBonus = "modifyProvinceStrengthBonus";
        public const string ModifyProvinceStrength = "modifyProvinceStrength";
        public const string ModifyProvinceStrengthMultiplier = "modifyProvinceStrengthMultiplier";
        public const string SetBaseProvinceStrength = "setBaseProvinceStrength";
        public const string SetProvinceStrengthBonus = "setProvinceStrengthBonus";
        public const string SetProvinceStrength = "setProvinceStrength";
        
        // Status effects
        public const string DoesNotBow = "doesNotBow";
        public const string DoesNotReady = "doesNotReady";
        public const string HonorStatusDoesNotAffectLeavePlay = "honorStatusDoesNotAffectLeavePlay";
        public const string HonorStatusDoesNotModifySkill = "honorStatusDoesNotModifySkill";
        public const string HonorStatusReverseModifySkill = "honorStatusReverseModifySkill";
        
        // Cost effects
        public const string FateCostToAttack = "fateCostToAttack";
        public const string FateCostToTarget = "fateCostToTarget";
        public const string AdditionalTriggerCost = "additionalTriggerCost";
        public const string AdditionalPlayCost = "additionalPlayCost";
        public const string CostReducer = "costReducer";
        public const string AlternateFatePool = "alternateFatePool";
        
        // Ability effects
        public const string GainAbility = "gainAbility";
        public const string GainPlayAction = "gainPlayAction";
        public const string IncreaseLimitOnAbilities = "increaseLimitOnAbilities";
        public const string CannotApplyLastingEffects = "cannotApplyLastingEffects";
        public const string CannotBidInDuels = "cannotBidInDuels";
        public const string CannotHaveOtherRestrictedAttachments = "cannotHaveOtherRestrictedAttachments";
        
        // Fate effects
        public const string GainExtraFateWhenPlayed = "gainExtraFateWhenPlayed";
        
        // Conflict effects
        public const string SetConflictDeclarationType = "setConflictDeclarationType";
        public const string SetConflictTotalSkill = "setConflictTotalSkill";
        public const string ModifyConflictElementsToResolve = "modifyConflictElementsToResolve";
        public const string RestrictNumberOfDefenders = "restrictNumberOfDefenders";
        public const string ResolveConflictEarly = "resolveConflictEarly";
        public const string ForceConflictUnopposed = "forceConflictUnopposed";
        
        // Ring effects
        public const string AddElement = "addElement";
        public const string CannotDeclareRing = "cannotDeclare";
        public const string ConsiderRingAsClaimed = "considerAsClaimed";
        
        // Player effects
        public const string AdditionalAction = "additionalAction";
        public const string AdditionalCardPlayed = "additionalCardPlayed";
        public const string AdditionalCharactersInConflict = "additionalCharactersInConflict";
        public const string AdditionalConflict = "additionalConflict";
        public const string CannotDeclareConflictsOfType = "cannotDeclareConflictsOfType";
        public const string CanPlayFromOwn = "canPlayFromOwn";
        public const string CanPlayFromOpponents = "canPlayFromOpponents";
        public const string ChangePlayerGloryModifier = "gloryModifier";
        public const string ChangePlayerSkillModifier = "conflictSkillModifier";
        public const string GainActionPhasePriority = "actionPhasePriority";
        public const string ModifyCardsDrawnInDrawPhase = "modifyCardsDrawnInDrawPhase";
        public const string SetMaxConflicts = "maxConflicts";
        public const string ShowTopConflictCard = "showTopConflictCard";
        public const string ShowTopDynastyCard = "showTopDynastyCard";
        
        // Misc effects
        public const string CustomEffect = "customEffect";
        public const string DelayedEffect = "delayedEffect";
        public const string SuppressEffects = "suppressEffects";
        public const string TerminalCondition = "terminalCondition";
        public const string UnlessActionCost = "unlessActionCost";
        public const string EventsCannotBeCancelled = "eventsCannotBeCancelled";
    }

    /// <summary>
    /// Effect durations for temporary effects
    /// </summary>
    public static class Durations
    {
        public const string UntilEndOfDuel = "untilEndOfDuel";
        public const string UntilEndOfConflict = "untilEndOfConflict";
        public const string UntilEndOfPhase = "untilEndOfPhase";
        public const string UntilEndOfRound = "untilEndOfRound";
        public const string UntilPassPriority = "untilPassPriority";
        public const string UntilOpponentPassPriority = "untilOpponentPassPriority";
        public const string UntilNextPassPriority = "untilNextPassPriority";
        public const string Persistent = "persistent";
        public const string Custom = "lastingEffect";

        /// <summary>
        /// Check if duration is temporary
        /// </summary>
        public static bool IsTemporary(string duration)
        {
            return duration != Persistent && duration != Custom;
        }

        /// <summary>
        /// Get all temporary durations
        /// </summary>
        public static string[] GetTemporaryDurations()
        {
            return new string[]
            {
                UntilEndOfDuel, UntilEndOfConflict, UntilEndOfPhase, 
                UntilEndOfRound, UntilPassPriority, UntilOpponentPassPriority, 
                UntilNextPassPriority
            };
        }
    }

    /// <summary>
    /// Ability execution stages
    /// </summary>
    public static class Stages
    {
        public const string Cost = "cost";
        public const string Effect = "effect";
        public const string PreTarget = "pretarget";
        public const string Target = "target";
    }

    /// <summary>
    /// Player targeting for effects and abilities
    /// </summary>
    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
        public const string Any = "any";

        /// <summary>
        /// Get all player targets
        /// </summary>
        public static string[] GetAllPlayers()
        {
            return new string[] { Self, Opponent, Any };
        }
    }

    /// <summary>
    /// Targeting modes for abilities
    /// </summary>
    public static class TargetModes
    {
        public const string Ring = "ring";
        public const string Select = "select";
        public const string Ability = "ability";
        public const string Token = "token";
        public const string AutoSingle = "autoSingle";
        public const string Exactly = "exactly";
        public const string ExactlyVariable = "exactlyVariable";
        public const string MaxStat = "maxStat";
        public const string Single = "single";
        public const string Unlimited = "unlimited";
        public const string UpTo = "upTo";
        public const string UpToVariable = "upToVariable";
    }

    /// <summary>
    /// Game phases
    /// </summary>
    public static class Phases
    {
        public const string Dynasty = "dynasty";
        public const string Draw = "draw";
        public const string Conflict = "conflict";
        public const string Fate = "fate";
        public const string Regroup = "regroup";

        /// <summary>
        /// Get all phases in order
        /// </summary>
        public static string[] GetAllPhases()
        {
            return new string[] { Dynasty, Draw, Conflict, Fate, Regroup };
        }

        /// <summary>
        /// Get next phase
        /// </summary>
        public static string GetNextPhase(string currentPhase)
        {
            var phases = GetAllPhases();
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i] == currentPhase)
                {
                    return i < phases.Length - 1 ? phases[i + 1] : phases[0];
                }
            }
            return Dynasty; // Default fallback
        }

        /// <summary>
        /// Check if phase is before another phase
        /// </summary>
        public static bool IsBefore(string phase1, string phase2)
        {
            var phases = GetAllPhases();
            int index1 = Array.IndexOf(phases, phase1);
            int index2 = Array.IndexOf(phases, phase2);
            return index1 >= 0 && index2 >= 0 && index1 < index2;
        }
    }

    /// <summary>
    /// Card types
    /// </summary>
    public static class CardTypes
    {
        public const string Stronghold = "stronghold";
        public const string Role = "role";
        public const string Province = "province";
        public const string Character = "character";
        public const string Holding = "holding";
        public const string Event = "event";
        public const string Attachment = "attachment";

        /// <summary>
        /// Get all card types
        /// </summary>
        public static string[] GetAllCardTypes()
        {
            return new string[] 
            { 
                Stronghold, Role, Province, Character, Holding, Event, Attachment 
            };
        }

        /// <summary>
        /// Check if card type is playable from hand
        /// </summary>
        public static bool IsPlayableFromHand(string cardType)
        {
            return cardType == Character || cardType == Event || cardType == Attachment;
        }

        /// <summary>
        /// Check if card type is playable from province
        /// </summary>
        public static bool IsPlayableFromProvince(string cardType)
        {
            return cardType == Character || cardType == Holding || cardType == Attachment;
        }

        /// <summary>
        /// Check if card type can participate in conflicts
        /// </summary>
        public static bool CanParticipateInConflicts(string cardType)
        {
            return cardType == Character;
        }
    }

    /// <summary>
    /// Play types for determining how cards are played
    /// </summary>
    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
        public const string Other = "other";
    }

    /// <summary>
    /// Game event names for the event system
    /// </summary>
    public static class EventNames
    {
        // Round and phase events
        public const string OnBeginRound = "onBeginRound";
        public const string OnPhaseCreated = "onPhaseCreated";
        public const string OnPhaseStarted = "onPhaseStarted";
        public const string OnPhaseEnded = "onPhaseEnded";
        public const string OnRoundEnded = "onRoundEnded";
        public const string OnPassFirstPlayer = "onPassFirstPlayer";
        
        // Card events
        public const string OnCharacterEntersPlay = "onCharacterEntersPlay";
        public const string OnCardPlayed = "onCardPlayed";
        public const string OnCardLeavesPlay = "onCardLeavesPlay";
        public const string OnCardRevealed = "onCardRevealed";
        public const string OnCardTurnedFacedown = "onCardTurnedFacedown";
        public const string OnDynastyCardTurnedFaceup = "onDynastyCardTurnedFaceup";
        public const string OnCardMoved = "onCardMoved";
        public const string OnCardsDrawn = "onCardsDrawn";
        public const string OnCardsDiscarded = "onCardsDiscarded";
        public const string OnCardsDiscardedFromHand = "onCardsDiscardedFromHand";
        public const string OnDeckShuffled = "onDeckShuffled";
        public const string OnDeckSearch = "onDeckSearch";
        public const string OnLookAtCards = "onLookAtCards";
        
        // Card state events
        public const string OnCardAttached = "onCardAttached";
        public const string OnCardDetached = "onCardDetached";
        public const string OnCardHonored = "onCardHonored";
        public const string OnCardDishonored = "onCardDishonored";
        public const string OnCardBowed = "onCardBowed";
        public const string OnCardReadied = "onCardReadied";
        public const string OnAddTokenToCard = "onAddTokenToCard";
        
        // Ability events
        public const string OnInitiateAbilityEffects = "onInitiateAbilityEffects";
        public const string OnCardAbilityInitiated = "onCardAbilityInitiated";
        public const string OnCardAbilityTriggered = "onCardAbilityTriggered";
        public const string OnAbilityResolved = "onAbilityResolved";
        
        // Conflict events
        public const string OnConflictInitiated = "onConflictInitiated";
        public const string OnConflictDeclared = "onConflictDeclared";
        public const string OnCovertResolved = "onCovertResolved";
        public const string OnDefendersDeclared = "onDefendersDeclared";
        public const string OnMoveToConflict = "onMoveToConflict";
        public const string OnSendHome = "onSendHome";
        public const string AfterConflict = "afterConflict";
        public const string OnConflictFinished = "onConflictFinished";
        public const string OnConflictPass = "onConflictPass";
        public const string OnBreakProvince = "onBreakProvince";
        public const string OnReturnHome = "onReturnHome";
        public const string OnParticipantsReturnHome = "onParticipantsReturnHome";
        public const string OnSwitchConflictElement = "onSwitchConflictElement";
        public const string OnSwitchConflictType = "onSwitchConflictType";
        
        // Ring events
        public const string OnResolveConflictRing = "onResolveConflictRing";
        public const string OnResolveRingElement = "onResolveRingElement";
        public const string OnClaimRing = "onClaimRing";
        public const string OnReturnRing = "onReturnRing";
        public const string OnTakeRing = "onTakeRing";
        public const string OnPlaceFateOnUnclaimedRings = "onPlaceFateOnUnclaimedRings";
        
        // Duel events
        public const string OnDuelInitiated = "onDuelInitiated";
        public const string AfterDuel = "afterDuel";
        public const string OnDuelResolution = "onDuelResolution";
        public const string OnDuelFinished = "onDuelFinished";
        
        // Honor and fate events
        public const string OnHonorDialsRevealed = "onHonorDialsRevealed";
        public const string OnFavorGloryTied = "onFavorGloryTied";
        public const string OnTransferHonor = "onTransferHonor";
        public const string OnModifyHonor = "onModifyHonor";
        public const string OnModifyBid = "onModifyBid";
        public const string OnSetHonorDial = "onSetHonorDial";
        public const string OnGloryCount = "onGloryCount";
        public const string OnClaimFavor = "onClaimFavor";
        public const string OnDiscardFavor = "onDiscardFavor";
        public const string OnMoveFate = "onMoveFate";
        public const string OnModifyFate = "onModifyFate";
        public const string OnSpendFate = "onSpendFate";
        public const string OnResolveFateCost = "onResolveFateCost";
        public const string OnFateCollected = "onFateCollected";
        
        // Dynasty events
        public const string OnPassDuringDynasty = "onPassDuringDynasty";
        
        // Action events
        public const string OnPassActionPhasePriority = "onPassActionPhasePriority";
        
        // Token events
        public const string OnStatusTokenDiscarded = "onStatusTokenDiscarded";
        public const string OnStatusTokenMoved = "onStatusTokenMoved";
        
        // Effect events
        public const string OnEffectApplied = "onEffectApplied";
        
        // Unnamed events
        public const string Unnamed = "unnamedEvent";

        /// <summary>
        /// Get all event names
        /// </summary>
        public static string[] GetAllEventNames()
        {
            return new string[]
            {
                OnBeginRound, OnPhaseCreated, OnPhaseStarted, OnPhaseEnded, OnRoundEnded,
                OnCharacterEntersPlay, OnCardPlayed, OnCardLeavesPlay, OnCardRevealed,
                OnConflictDeclared, OnDefendersDeclared, AfterConflict, OnConflictFinished,
                OnClaimRing, OnDuelInitiated, OnHonorDialsRevealed, OnMoveFate
                // Add more as needed
            };
        }

        /// <summary>
        /// Check if event is conflict-related
        /// </summary>
        public static bool IsConflictEvent(string eventName)
        {
            return eventName.Contains("Conflict") || eventName.Contains("Defender") || 
                   eventName.Contains("Attacker") || eventName == OnMoveToConflict || 
                   eventName == OnSendHome || eventName == OnReturnHome;
        }

        /// <summary>
        /// Check if event is card-related
        /// </summary>
        public static bool IsCardEvent(string eventName)
        {
            return eventName.Contains("Card") || eventName == OnCharacterEntersPlay;
        }
    }

    /// <summary>
    /// Ability types for triggered abilities
    /// </summary>
    public static class AbilityTypes
    {
        public const string Action = "action";
        public const string WouldInterrupt = "cancelinterrupt";
        public const string CancelInterrupt = "cancelinterrupt"; // Alias for WouldInterrupt
        public const string ForcedInterrupt = "forcedinterrupt";
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedreaction";
        public const string Reaction = "reaction";
        public const string Persistent = "persistent";
        public const string OtherEffects = "OtherEffects";

        /// <summary>
        /// Get all ability types
        /// </summary>
        public static string[] GetAllAbilityTypes()
        {
            return new string[]
            {
                Action, WouldInterrupt, ForcedInterrupt, Interrupt, 
                ForcedReaction, Reaction, Persistent, OtherEffects
            };
        }

        /// <summary>
        /// Check if ability type is triggered
        /// </summary>
        public static bool IsTriggered(string abilityType)
        {
            return abilityType == Interrupt || abilityType == Reaction || 
                   abilityType == ForcedInterrupt || abilityType == ForcedReaction ||
                   abilityType == WouldInterrupt;
        }

        /// <summary>
        /// Check if ability type is forced
        /// </summary>
        public static bool IsForced(string abilityType)
        {
            return abilityType == ForcedInterrupt || abilityType == ForcedReaction;
        }

        /// <summary>
        /// Get ability type priority (lower number = higher priority)
        /// </summary>
        public static int GetPriority(string abilityType)
        {
            switch (abilityType)
            {
                case WouldInterrupt: return 0;
                case ForcedInterrupt: return 1;
                case Interrupt: return 2;
                case ForcedReaction: return 3;
                case Reaction: return 4;
                case Action: return 5;
                default: return 99;
            }
        }
    }

    /// <summary>
    /// Duel types
    /// </summary>
    public static class DuelTypes
    {
        public const string Military = "military";
        public const string Political = "political";
        public const string Glory = "glory";

        /// <summary>
        /// Get all duel types
        /// </summary>
        public static string[] GetAllDuelTypes()
        {
            return new string[] { Military, Political, Glory };
        }

        /// <summary>
        /// Check if duel type is skill-based
        /// </summary>
        public static bool IsSkillBased(string duelType)
        {
            return duelType == Military || duelType == Political;
        }
    }

    /// <summary>
    /// Ring elements
    /// </summary>
    public static class Elements
    {
        public const string Fire = "fire";
        public const string Earth = "earth";
        public const string Air = "air";
        public const string Water = "water";
        public const string Void = "void";

        /// <summary>
        /// Get all elements
        /// </summary>
        public static string[] GetAllElements()
        {
            return new string[] { Fire, Earth, Air, Water, Void };
        }

        /// <summary>
        /// Get element display name
        /// </summary>
        public static string GetDisplayName(string element)
        {
            switch (element)
            {
                case Fire: return "🔥 Fire";
                case Earth: return "🌍 Earth";
                case Air: return "💨 Air";
                case Water: return "💧 Water";
                case Void: return "🌌 Void";
                default: return element;
            }
        }

        /// <summary>
        /// Get element color for UI
        /// </summary>
        public static UnityEngine.Color GetElementColor(string element)
        {
            switch (element)
            {
                case Fire: return UnityEngine.Color.red;
                case Earth: return new UnityEngine.Color(0.6f, 0.4f, 0.2f); // Brown
                case Air: return UnityEngine.Color.cyan;
                case Water: return UnityEngine.Color.blue;
                case Void: return new UnityEngine.Color(0.3f, 0.0f, 0.6f); // Purple
                default: return UnityEngine.Color.gray;
            }
        }
    }

    /// <summary>
    /// Conflict types
    /// </summary>
    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";

        /// <summary>
        /// Get all conflict types
        /// </summary>
        public static string[] GetAllConflictTypes()
        {
            return new string[] { Military, Political };
        }

        /// <summary>
        /// Get opposite conflict type
        /// </summary>
        public static string GetOpposite(string conflictType)
        {
            return conflictType == Military ? Political : Military;
        }

        /// <summary>
        /// Get conflict type display name
        /// </summary>
        public static string GetDisplayName(string conflictType)
        {
            switch (conflictType)
            {
                case Military: return "⚔️ Military";
                case Political: return "🎭 Political";
                default: return conflictType;
            }
        }

        /// <summary>
        /// Get conflict type color for UI
        /// </summary>
        public static UnityEngine.Color GetConflictTypeColor(string conflictType)
        {
            switch (conflictType)
            {
                case Military: return new UnityEngine.Color(0.8f, 0.2f, 0.2f); // Dark Red
                case Political: return new UnityEngine.Color(0.2f, 0.2f, 0.8f); // Dark Blue
                default: return UnityEngine.Color.gray;
            }
        }
    }

    /// <summary>
    /// Token types for status tokens
    /// </summary>
    public static class TokenTypes
    {
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string Fate = "fate";
        public const string Military = "military";
        public const string Political = "political";
        public const string Strength = "strength";

        /// <summary>
        /// Get all token types
        /// </summary>
        public static string[] GetAllTokenTypes()
        {
            return new string[] { Honor, Dishonor, Fate, Military, Political, Strength };
        }

        /// <summary>
        /// Check if token type affects skill
        /// </summary>
        public static bool AffectsSkill(string tokenType)
        {
            return tokenType == Military || tokenType == Political;
        }

        /// <summary>
        /// Get token display name
        /// </summary>
        public static string GetDisplayName(string tokenType)
        {
            switch (tokenType)
            {
                case Honor: return "🌟 Honor";
                case Dishonor: return "💀 Dishonor";
                case Fate: return "💰 Fate";
                case Military: return "⚔️ Military";
                case Political: return "🎭 Political";
                case Strength: return "🏰 Strength";
                default: return tokenType;
            }
        }
    }

    /// <summary>
    /// L5R clans and factions
    /// </summary>
    public static class Clans
    {
        public const string Crab = "crab";
        public const string Crane = "crane";
        public const string Dragon = "dragon";
        public const string Lion = "lion";
        public const string Phoenix = "phoenix";
        public const string Scorpion = "scorpion";
        public const string Unicorn = "unicorn";
        public const string Neutral = "neutral";

        /// <summary>
        /// Get all clans
        /// </summary>
        public static string[] GetAllClans()
        {
            return new string[] 
            { 
                Crab, Crane, Dragon, Lion, Phoenix, Scorpion, Unicorn, Neutral 
            };
        }

        /// <summary>
        /// Get clan display name
        /// </summary>
        public static string GetDisplayName(string clan)
        {
            switch (clan)
            {
                case Crab: return "🦀 Crab Clan";
                case Crane: return "🕊️ Crane Clan";
                case Dragon: return "🐉 Dragon Clan";
                case Lion: return "🦁 Lion Clan";
                case Phoenix: return "🔥 Phoenix Clan";
                case Scorpion: return "🦂 Scorpion Clan";
                case Unicorn: return "🦄 Unicorn Clan";
                case Neutral: return "⚪ Neutral";
                default: return clan;
            }
        }

        /// <summary>
        /// Get clan color for UI
        /// </summary>
        public static UnityEngine.Color GetClanColor(string clan)
        {
            switch (clan)
            {
                case Crab: return new UnityEngine.Color(0.4f, 0.2f, 0.0f); // Brown
                case Crane: return new UnityEngine.Color(0.0f, 0.6f, 0.9f); // Light Blue
                case Dragon: return new UnityEngine.Color(0.0f, 0.5f, 0.0f); // Green
                case Lion: return new UnityEngine.Color(0.9f, 0.7f, 0.0f); // Gold
                case Phoenix: return new UnityEngine.Color(1.0f, 0.3f, 0.0f); // Orange
                case Scorpion: return new UnityEngine.Color(0.6f, 0.0f, 0.0f); // Dark Red
                case Unicorn: return new UnityEngine.Color(0.6f, 0.0f, 0.6f); // Purple
                case Neutral: return UnityEngine.Color.gray;
                default: return UnityEngine.Color.white;
            }
        }
    }

    /// <summary>
    /// Common keywords in L5R
    /// </summary>
    public static class Keywords
    {
        public const string Covert = "covert";
        public const string Courtesy = "courtesy";
        public const string Sincerity = "sincerity";
        public const string Pride = "pride";
        public const string Limited = "limited";
        public const string Restricted = "restricted";
        public const string Unique = "unique";
        public const string Composure = "composure";

        /// <summary>
        /// Get all keywords
        /// </summary>
        public static string[] GetAllKeywords()
        {
            return new string[] 
            { 
                Covert, Courtesy, Sincerity, Pride, Limited, Restricted, Unique, Composure 
            };
        }

        /// <summary>
        /// Check if keyword is a status keyword
        /// </summary>
        public static bool IsStatusKeyword(string keyword)
        {
            return keyword == Courtesy || keyword == Sincerity || keyword == Pride || keyword == Composure;
        }

        /// <summary>
        /// Check if keyword is a play restriction
        /// </summary>
        public static bool IsPlayRestriction(string keyword)
        {
            return keyword == Limited || keyword == Restricted || keyword == Unique;
        }
    }

    /// <summary>
    /// Game actions for the action system
    /// </summary>
    public static class GameActions
    {
        public const string Bow = "bow";
        public const string Ready = "ready";
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string MoveCard = "moveCard";
        public const string DiscardCard = "discardCard";
        public const string DrawCards = "drawCards";
        public const string GainFate = "gainFate";
        public const string LoseFate = "loseFate";
        public const string GainHonor = "gainHonor";
        public const string LoseHonor = "loseHonor";
        public const string BreakProvince = "breakProvince";
        public const string ClaimRing = "claimRing";
        public const string TakeFateFromRing = "takeFateFromRing";
        public const string PlaceFateOnRing = "placeFateOnRing";
        public const string Duel = "duel";
        public const string LookAt = "lookAt";
        public const string Reveal = "reveal";
        public const string Shuffle = "shuffle";
        public const string Search = "search";
        public const string Attach = "attach";
        public const string Detach = "detach";

        /// <summary>
        /// Get all game actions
        /// </summary>
        public static string[] GetAllGameActions()
        {
            return new string[]
            {
                Bow, Ready, Honor, Dishonor, MoveCard, DiscardCard, DrawCards,
                GainFate, LoseFate, GainHonor, LoseHonor, BreakProvince, ClaimRing,
                TakeFateFromRing, PlaceFateOnRing, Duel, LookAt, Reveal, Shuffle,
                Search, Attach, Detach
            };
        }

        /// <summary>
        /// Check if action affects card state
        /// </summary>
        public static bool AffectsCardState(string action)
        {
            return action == Bow || action == Ready || action == Honor || 
                   action == Dishonor || action == Attach || action == Detach;
        }

        /// <summary>
        /// Check if action moves cards
        /// </summary>
        public static bool MovesCards(string action)
        {
            return action == MoveCard || action == DiscardCard || action == DrawCards;
        }
    }

    /// <summary>
    /// Game win conditions
    /// </summary>
    public static class WinConditions
    {
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string BreakStronghold = "breakStronghold";
        public const string Concession = "concession";
        public const string Clock = "clock";
        public const string Disconnection = "disconnection";

        /// <summary>
        /// Get all win conditions
        /// </summary>
        public static string[] GetAllWinConditions()
        {
            return new string[] 
            { 
                Honor, Dishonor, BreakStronghold, Concession, Clock, Disconnection 
            };
        }

        /// <summary>
        /// Get win condition display name
        /// </summary>
        public static string GetDisplayName(string winCondition)
        {
            switch (winCondition)
            {
                case Honor: return "🌟 Honor Victory";
                case Dishonor: return "💀 Dishonor Victory";
                case BreakStronghold: return "🏰 Conquest Victory";
                case Concession: return "🏳️ Concession";
                case Clock: return "⏰ Time Victory";
                case Disconnection: return "📡 Disconnection";
                default: return winCondition;
            }
        }
    }

    /// <summary>
    /// Utility class for constants validation and conversion
    /// </summary>
    public static class ConstantsHelper
    {
        /// <summary>
        /// Validate if a location is valid
        /// </summary>
        public static bool IsValidLocation(string location)
        {
            return !string.IsNullOrEmpty(location) && (
                location == Locations.Any || location == Locations.Hand ||
                location == Locations.ConflictDeck || location == Locations.DynastyDeck ||
                location == Locations.ConflictDiscardPile || location == Locations.DynastyDiscardPile ||
                location == Locations.PlayArea || Locations.IsProvince(location) ||
                location == Locations.RemovedFromGame || location == Locations.BeingPlayed ||
                location == Locations.Role || location == Locations.UnderneathStronghold
            );
        }

        /// <summary>
        /// Validate if a card type is valid
        /// </summary>
        public static bool IsValidCardType(string cardType)
        {
            return CardTypes.GetAllCardTypes().Contains(cardType);
        }

        /// <summary>
        /// Validate if an element is valid
        /// </summary>
        public static bool IsValidElement(string element)
        {
            return Elements.GetAllElements().Contains(element);
        }

        /// <summary>
        /// Validate if a conflict type is valid
        /// </summary>
        public static bool IsValidConflictType(string conflictType)
        {
            return ConflictTypes.GetAllConflictTypes().Contains(conflictType);
        }

        /// <summary>
        /// Validate if a phase is valid
        /// </summary>
        public static bool IsValidPhase(string phase)
        {
            return Phases.GetAllPhases().Contains(phase);
        }

        /// <summary>
        /// Validate if an ability type is valid
        /// </summary>
        public static bool IsValidAbilityType(string abilityType)
        {
            return AbilityTypes.GetAllAbilityTypes().Contains(abilityType);
        }

        /// <summary>
        /// Get default value for a constant type
        /// </summary>
        public static string GetDefaultLocation() => Locations.PlayArea;
        public static string GetDefaultCardType() => CardTypes.Character;
        public static string GetDefaultElement() => Elements.Void;
        public static string GetDefaultConflictType() => ConflictTypes.Military;
        public static string GetDefaultPhase() => Phases.Dynasty;
        public static string GetDefaultAbilityType() => AbilityTypes.Action;
        public static string GetDefaultClan() => Clans.Neutral;
        public static string GetDefaultDuration() => Durations.Persistent;

        /// <summary>
        /// Convert string to proper case for display
        /// </summary>
        public static string ToDisplayCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }

        /// <summary>
        /// Get all constants of a specific type
        /// </summary>
        public static Dictionary<string, string[]> GetAllConstants()
        {
            return new Dictionary<string, string[]>
            {
                { "Locations", new string[] { Locations.Hand, Locations.PlayArea, Locations.ConflictDeck } },
                { "CardTypes", CardTypes.GetAllCardTypes() },
                { "Elements", Elements.GetAllElements() },
                { "ConflictTypes", ConflictTypes.GetAllConflictTypes() },
                { "Phases", Phases.GetAllPhases() },
                { "AbilityTypes", AbilityTypes.GetAllAbilityTypes() },
                { "Clans", Clans.GetAllClans() },
                { "Keywords", Keywords.GetAllKeywords() }
            };
        }

        /// <summary>
        /// Parse comma-separated constants
        /// </summary>
        public static string[] ParseConstants(string constantsString)
        {
            if (string.IsNullOrEmpty(constantsString))
                return new string[0];

            return constantsString.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        /// <summary>
        /// Format constants for display
        /// </summary>
        public static string FormatConstantsForDisplay(string[] constants)
        {
            if (constants == null || constants.Length == 0)
                return "None";

            if (constants.Length == 1)
                return ToDisplayCase(constants[0]);

            if (constants.Length == 2)
                return $"{ToDisplayCase(constants[0])} and {ToDisplayCase(constants[1])}";

            var formatted = constants.Take(constants.Length - 1)
                .Select(ToDisplayCase)
                .ToArray();
            
            return $"{string.Join(", ", formatted)}, and {ToDisplayCase(constants.Last())}";
        }
    }

    /// <summary>
    /// Extension methods for working with constants
    /// </summary>
    public static class ConstantsExtensions
    {
        /// <summary>
        /// Check if location contains cards
        /// </summary>
        public static bool ContainsCards(this string location)
        {
            return location != Locations.RemovedFromGame;
        }

        /// <summary>
        /// Check if location is hidden from opponent
        /// </summary>
        public static bool IsHiddenFromOpponent(this string location)
        {
            return location == Locations.Hand || 
                   location == Locations.ConflictDeck || 
                   location == Locations.DynastyDeck;
        }

        /// <summary>
        /// Check if card type has skill values
        /// </summary>
        public static bool HasSkillValues(this string cardType)
        {
            return cardType == CardTypes.Character;
        }

        /// <summary>
        /// Check if card type has strength
        /// </summary>
        public static bool HasStrength(this string cardType)
        {
            return cardType == CardTypes.Province || cardType == CardTypes.Stronghold;
        }

        /// <summary>
        /// Check if element is a basic element
        /// </summary>
        public static bool IsBasicElement(this string element)
        {
            return element == Elements.Fire || element == Elements.Earth || 
                   element == Elements.Air || element == Elements.Water;
        }

        /// <summary>
        /// Check if clan is a Great Clan
        /// </summary>
        public static bool IsGreatClan(this string clan)
        {
            return clan != Clans.Neutral;
        }

        /// <summary>
        /// Get skill type for conflict type
        /// </summary>
        public static string GetSkillType(this string conflictType)
        {
            return conflictType == ConflictTypes.Military ? "military" : "political";
        }
    }
}