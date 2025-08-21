using System;
using System.Collections.Generic;

namespace L5RGame
{
    // Enums instead of static classes to avoid CS0721/CS0722 errors
    public enum Locations
    {
        PlayArea,
        Hand,
        ConflictDeck,
        DynastyDeck,
        ConflictDiscardPile,
        DynastyDiscardPile,
        ProvinceOne,
        ProvinceTwo,
        ProvinceThree,
        ProvinceFour,
        StrongholdProvince,
        Provinces,
        RemovedFromGame,
        BeingPlayed,
        Any,
        Role,
        ConflictDeckTop
    }

    public partial class EffectNames
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
        // Add more as needed
    }

    public enum Durations
    {
        UntilEndOfConflict,
        UntilEndOfPhase,
        UntilEndOfRound,
        UntilEndOfDuel,
        Persistent,
        UntilPassPriority,
        UntilOpponentPassPriority,
        UntilNextPassPriority,
        Custom
    }

    public enum Stages
    {
        PreConflict,
        DuringConflict,
        PostConflict,
        Any
    }

    public enum Players
    {
        Self,
        Opponent,
        Any,
        Both,
        Attacker,
        Defender
    }

    public enum PlayTypes
    {
        PlayFromHand,
        PlayFromProvince,
        PlayFromOwned,
        Other
    }

    public enum CardTypes
    {
        Character,
        Event,
        Attachment,
        Holding,
        Province,
        Stronghold,
        Role
    }

    public enum AbilityTypes
    {
        Action,
        Reaction,
        Interrupt,
        ForcedReaction,
        ForcedInterrupt,
        Persistent,
        WouldInterrupt,
        OtherEffects
    }

    public partial class EventNames
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
        // Add more as needed
    }

    public enum ConflictTypes
    {
        Military,
        Political
    }

    public enum DuelTypes
    {
        Military,
        Political
    }

    public enum Decks
    {
        ConflictDeck,
        DynastyDeck
    }

    public static class GameActions
    {
        // Define any game action constants here
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
        public const string Fate = "fate";
        public const string Status = "status";
        public const string Conflict = "conflict";
    }
}
