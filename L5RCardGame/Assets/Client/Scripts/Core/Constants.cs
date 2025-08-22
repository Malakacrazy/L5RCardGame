using System;
using System.Collections.Generic;

namespace L5RGame
{
    // Location constants
    public static class Locations
    {
        public const string PlayArea = "play area";
        public const string Hand = "hand";
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
        public const string ConflictDiscardPile = "conflict discard pile";
        public const string DynastyDiscardPile = "dynasty discard pile";
        public const string ProvinceOne = "province 1";
        public const string ProvinceTwo = "province 2";
        public const string ProvinceThree = "province 3";
        public const string ProvinceFour = "province 4";
        public const string StrongholdProvince = "stronghold province";
        public const string Provinces = "provinces";
        public const string RemovedFromGame = "removed from game";
        public const string BeingPlayed = "being played";
        public const string Any = "any";
        public const string Role = "role";
        public const string ConflictDeckTop = "conflict deck top";
    }

    public static class EffectNames
    {
        public const string ModifyMilitarySkill = "modifyMilitarySkill";
        public const string ModifyPoliticalSkill = "modifyPoliticalSkill";
        public const string ModifyBaseMilitarySkill = "modifyBaseMilitarySkill";
        public const string ModifyBasePoliticalSkill = "modifyBasePoliticalSkill";
        public const string ModifyGlory = "modifyGlory";
        public const string ModifyBothSkills = "modifyBothSkills";
        public const string ModifyBaseBothSkills = "modifyBaseBothSkills";
        public const string ModifyProvinceStrength = "modifyProvinceStrength";
        public const string ModifyBaseProvinceStrength = "modifyBaseProvinceStrength";
        public const string AddFaction = "addFaction";
        public const string AddTrait = "addTrait";
        public const string RemoveTrait = "removeTrait";
        public const string CannotParticipateAsAttacker = "cannotParticipateAsAttacker";
        public const string CannotParticipateAsDefender = "cannotParticipateAsDefender";
        public const string AttachmentLimit = "attachmentLimit";
        public const string AttachmentMyControlOnly = "attachmentMyControlOnly";
        public const string AttachmentRestrictTraitAmount = "attachmentRestrictTraitAmount";
        public const string AttachmentTraitRestriction = "attachmentTraitRestriction";
        public const string AttachmentUniqueRestriction = "attachmentUniqueRestriction";
        public const string AttachmentFactionRestriction = "attachmentFactionRestriction";
        public const string Blank = "blank";
        public const string CopyCharacter = "copyCharacter";
        public const string TakeControl = "takeControl";
        public const string CanBeSeenWhenFacedown = "canBeSeenWhenFacedown";
        public const string HideWhenFaceUp = "hideWhenFaceUp";
        public const string CannotContribute = "cannotContribute";
        public const string ContributeToConflict = "contributeToConflict";
        public const string ChangeConflictSkillFunction = "changeConflictSkillFunction";
        public const string DoesNotReady = "doesNotReady";
        public const string GainAbility = "gainAbility";
        public const string IncreaseLimitOnAbilities = "increaseLimitOnAbilities";
        public const string CannotHaveOtherRestrictedAttachments = "cannotHaveOtherRestrictedAttachments";
        public const string SetConflictTotalSkill = "setConflictTotalSkill";
        public const string ModifyConflictElementsToResolve = "modifyConflictElementsToResolve";
        public const string RestrictNumberOfDefenders = "restrictNumberOfDefenders";
        public const string ForceConflictUnopposed = "forceConflictUnopposed";
        public const string AdditionalCardPlayed = "additionalCardPlayed";
        public const string AdditionalCharactersInConflict = "additionalCharactersInConflict";
    }

    public static class Durations
    {
        public const string UntilEndOfConflict = "untilEndOfConflict";
        public const string UntilEndOfPhase = "untilEndOfPhase";
        public const string UntilEndOfRound = "untilEndOfRound";
        public const string UntilEndOfDuel = "untilEndOfDuel";
        public const string Persistent = "persistent";
        public const string UntilPassPriority = "untilPassPriority";
        public const string UntilOpponentPassPriority = "untilOpponentPassPriority";
        public const string UntilNextPassPriority = "untilNextPassPriority";
        public const string Custom = "custom";
    }

    public static class Players
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
        public const string Any = "any";
        public const string Both = "both";
        public const string Attacker = "attacker";
        public const string Defender = "defender";
    }

    public static class PlayTypes
    {
        public const string PlayFromHand = "playFromHand";
        public const string PlayFromProvince = "playFromProvince";
        public const string PlayFromOwned = "playFromOwned";
        public const string Other = "other";
    }

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
        public const string Interrupt = "interrupt";
        public const string ForcedReaction = "forcedReaction";
        public const string ForcedInterrupt = "forcedInterrupt";
        public const string Persistent = "persistent";
        public const string WouldInterrupt = "wouldInterrupt";
        public const string OtherEffects = "otherEffects";
    }

    public static class EventNames
    {
        public const string Unnamed = "Unnamed";
        public const string OnPhaseStarted = "onPhaseStarted";
        public const string OnPhaseEnded = "onPhaseEnded";
        public const string OnRoundEnded = "onRoundEnded";
        public const string OnCardPlayed = "onCardPlayed";
        public const string OnCardMoved = "onCardMoved";
        public const string OnDeckShuffled = "onDeckShuffled";
        public const string OnConflictDeclared = "onConflictDeclared";
        public const string OnConflictFinished = "onConflictFinished";
        public const string OnConflictPass = "onConflictPass";
        public const string OnDuelFinished = "onDuelFinished";
        public const string OnFateCollected = "onFateCollected";
        public const string OnPassActionPhasePriority = "onPassActionPhasePriority";
        public const string OnEffectApplied = "onEffectApplied";
        public const string OnCardBowed = "onCardBowed";
        public const string OnCardHonored = "onCardHonored";
        public const string OnCardDishonored = "onCardDishonored";
        public const string OnBreakProvince = "onBreakProvince";
        public const string OnAddTokenToCard = "onAddTokenToCard";
        public const string OnCardAttached = "onCardAttached";
        public const string OnCreateToken = "onCreateToken";
        public const string OnDynastyCardTurnedFaceup = "onDynastyCardTurnedFaceup";
        public const string OnDuelInitiated = "onDuelInitiated";
        public const string OnCancel = "onCancel";
    }

    public static class ConflictTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    public static class DuelTypes
    {
        public const string Military = "military";
        public const string Political = "political";
    }

    public static class Decks
    {
        public const string ConflictDeck = "conflict deck";
        public const string DynastyDeck = "dynasty deck";
    }

    public static class TargetModes
    {
        public const string Exactly = "exactly";
        public const string UpTo = "upTo";
        public const string MaxStat = "maxStat";
        public const string MinStat = "minStat";
        public const string Single = "single";
        public const string Unlimited = "unlimited";
        public const string Auto = "auto";
    }

    public static class TokenTypes
    {
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string Fate = "fate";
        public const string Status = "status";
        public const string Conflict = "conflict";
    }

    public static class Stages
    {
        public const string PreConflict = "preConflict";
        public const string DuringConflict = "duringConflict";
        public const string PostConflict = "postConflict";
        public const string Any = "any";
    }

    public static class Effects
    {
        // Dynamic effects container
        public static Dictionary<string, object> AllEffects = new Dictionary<string, object>();
    }
}
