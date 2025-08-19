using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Factory class for creating all types of game actions in L5R.
    /// Provides a fluent API for action creation with proper parameter validation.
    /// </summary>
    public static class GameActions
    {
        #region Card Actions

        /// <summary>
        /// Add token to a card
        /// </summary>
        public static AddTokenAction AddToken(object propertyFactoryOrProperties = null)
        {
            return CreateAction<AddTokenAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Attach one card to another
        /// </summary>
        public static AttachAction Attach(object propertyFactoryOrProperties = null)
        {
            return CreateAction<AttachAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Attach card to a ring
        /// </summary>
        public static AttachToRingAction AttachToRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<AttachToRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Bow a character
        /// </summary>
        public static BowAction Bow(object propertyFactoryOrProperties = null)
        {
            return CreateAction<BowAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Break a province
        /// </summary>
        public static BreakAction Break(object propertyFactoryOrProperties = null)
        {
            return CreateAction<BreakAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Apply lasting effect to a card
        /// </summary>
        public static LastingEffectCardAction CardLastingEffect(object propertyFactoryOrProperties)
        {
            return CreateAction<LastingEffectCardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Claim imperial favor
        /// </summary>
        public static ClaimFavorAction ClaimImperialFavor(object propertyFactoryOrProperties)
        {
            return CreateAction<ClaimFavorAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Create a status token
        /// </summary>
        public static CreateTokenAction CreateToken(object propertyFactoryOrProperties = null)
        {
            return CreateAction<CreateTokenAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Detach attachment from parent
        /// </summary>
        public static DetachAction Detach(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DetachAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Discard a card
        /// </summary>
        public static DiscardCardAction DiscardCard(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DiscardCardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Discard card from play
        /// </summary>
        public static DiscardFromPlayAction DiscardFromPlay(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DiscardFromPlayAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Dishonor a character
        /// </summary>
        public static DishonorAction Dishonor(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DishonorAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Initiate a duel between characters
        /// </summary>
        public static DuelAction Duel(object propertyFactoryOrProperties)
        {
            return CreateAction<DuelAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Flip dynasty card face up
        /// </summary>
        public static FlipDynastyAction FlipDynasty(object propertyFactoryOrProperties = null)
        {
            return CreateAction<FlipDynastyAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Honor a character
        /// </summary>
        public static HonorAction Honor(object propertyFactoryOrProperties = null)
        {
            return CreateAction<HonorAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Look at cards without revealing to opponent
        /// </summary>
        public static LookAtAction LookAt(object propertyFactoryOrProperties = null)
        {
            return CreateAction<LookAtAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Move card to a different location
        /// </summary>
        public static MoveCardAction MoveCard(object propertyFactoryOrProperties)
        {
            return CreateAction<MoveCardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Move character to current conflict
        /// </summary>
        public static MoveToConflictAction MoveToConflict(object propertyFactoryOrProperties = null)
        {
            return CreateAction<MoveToConflictAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Place fate on a card
        /// </summary>
        public static PlaceFateAction PlaceFate(object propertyFactoryOrProperties = null)
        {
            return CreateAction<PlaceFateAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Play a card from hand
        /// </summary>
        public static PlayCardAction PlayCard(object propertyFactoryOrProperties = null)
        {
            return CreateAction<PlayCardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Perform glory count for honor/dishonor resolution
        /// </summary>
        public static GloryCountAction PerformGloryCount(object propertyFactoryOrProperties)
        {
            return CreateAction<GloryCountAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Put card into play (participating in conflict)
        /// </summary>
        public static PutIntoPlayAction PutIntoConflict(object propertyFactoryOrProperties = null)
        {
            var action = CreateAction<PutIntoPlayAction>(propertyFactoryOrProperties);
            action.SetIntoConflict(true);
            return action;
        }

        /// <summary>
        /// Put card into play (not participating in conflict)
        /// </summary>
        public static PutIntoPlayAction PutIntoPlay(object propertyFactoryOrProperties = null)
        {
            var action = CreateAction<PutIntoPlayAction>(propertyFactoryOrProperties);
            action.SetIntoConflict(false);
            return action;
        }

        /// <summary>
        /// Ready a character
        /// </summary>
        public static ReadyAction Ready(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ReadyAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Remove fate from a card
        /// </summary>
        public static RemoveFateAction RemoveFate(object propertyFactoryOrProperties = null)
        {
            return CreateAction<RemoveFateAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Remove card from the game entirely
        /// </summary>
        public static RemoveFromGameAction RemoveFromGame(object propertyFactoryOrProperties = null)
        {
            return CreateAction<RemoveFromGameAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Resolve an ability
        /// </summary>
        public static ResolveAbilityAction ResolveAbility(object propertyFactoryOrProperties)
        {
            return CreateAction<ResolveAbilityAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Return card to deck
        /// </summary>
        public static ReturnToDeckAction ReturnToDeck(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ReturnToDeckAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Return card to hand
        /// </summary>
        public static ReturnToHandAction ReturnToHand(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ReturnToHandAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Reveal cards to all players
        /// </summary>
        public static RevealAction Reveal(object propertyFactoryOrProperties = null)
        {
            return CreateAction<RevealAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Send character home from conflict
        /// </summary>
        public static SendHomeAction SendHome(object propertyFactoryOrProperties = null)
        {
            return CreateAction<SendHomeAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Sacrifice a card (discard from play with sacrifice flag)
        /// </summary>
        public static DiscardFromPlayAction Sacrifice(object propertyFactoryOrProperties = null)
        {
            var action = CreateAction<DiscardFromPlayAction>(propertyFactoryOrProperties);
            action.SetSacrifice(true);
            return action;
        }

        /// <summary>
        /// Take control of opponent's card
        /// </summary>
        public static TakeControlAction TakeControl(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TakeControlAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Turn card face down
        /// </summary>
        public static TurnCardFacedownAction TurnFacedown(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TurnCardFacedownAction>(propertyFactoryOrProperties);
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Player chooses cards to discard
        /// </summary>
        public static ChosenDiscardAction ChosenDiscard(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ChosenDiscardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Search deck for cards
        /// </summary>
        public static DeckSearchAction DeckSearch(object propertyFactoryOrProperties)
        {
            return CreateAction<DeckSearchAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Discard random cards from hand
        /// </summary>
        public static RandomDiscardAction DiscardAtRandom(object propertyFactoryOrProperties = null)
        {
            return CreateAction<RandomDiscardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Draw cards from deck
        /// </summary>
        public static DrawAction Draw(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DrawAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Gain fate
        /// </summary>
        public static GainFateAction GainFate(object propertyFactoryOrProperties = null)
        {
            return CreateAction<GainFateAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Gain honor
        /// </summary>
        public static GainHonorAction GainHonor(object propertyFactoryOrProperties = null)
        {
            return CreateAction<GainHonorAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Initiate a conflict
        /// </summary>
        public static InitiateConflictAction InitiateConflict(object propertyFactoryOrProperties = null)
        {
            return CreateAction<InitiateConflictAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Lose fate
        /// </summary>
        public static LoseFateAction LoseFate(object propertyFactoryOrProperties = null)
        {
            return CreateAction<LoseFateAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Lose honor
        /// </summary>
        public static LoseHonorAction LoseHonor(object propertyFactoryOrProperties = null)
        {
            return CreateAction<LoseHonorAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Lose imperial favor
        /// </summary>
        public static DiscardFavorAction LoseImperialFavor(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DiscardFavorAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Modify bid value
        /// </summary>
        public static ModifyBidAction ModifyBid(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ModifyBidAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Apply lasting effect to a player
        /// </summary>
        public static LastingEffectAction PlayerLastingEffect(object propertyFactoryOrProperties)
        {
            return CreateAction<LastingEffectAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Refill face-up dynasty cards
        /// </summary>
        public static RefillFaceupAction RefillFaceup(object propertyFactoryOrProperties)
        {
            return CreateAction<RefillFaceupAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Set honor dial to specific value
        /// </summary>
        public static SetDialAction SetHonorDial(object propertyFactoryOrProperties)
        {
            return CreateAction<SetDialAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Shuffle deck
        /// </summary>
        public static ShuffleDeckAction ShuffleDeck(object propertyFactoryOrProperties)
        {
            return CreateAction<ShuffleDeckAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Take fate from opponent
        /// </summary>
        public static TransferFateAction TakeFate(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TransferFateAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Take honor from opponent
        /// </summary>
        public static TransferHonorAction TakeHonor(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TransferHonorAction>(propertyFactoryOrProperties);
        }

        #endregion

        #region Ring Actions

        /// <summary>
        /// Place fate on a ring
        /// </summary>
        public static PlaceFateRingAction PlaceFateOnRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<PlaceFateRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Resolve current conflict ring
        /// </summary>
        public static ResolveConflictRingAction ResolveConflictRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ResolveConflictRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Resolve ring element effect
        /// </summary>
        public static ResolveElementAction ResolveRingEffect(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ResolveElementAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Return ring to unclaimed state
        /// </summary>
        public static ReturnRingAction ReturnRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ReturnRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Apply lasting effect to a ring
        /// </summary>
        public static LastingEffectRingAction RingLastingEffect(object propertyFactoryOrProperties)
        {
            return CreateAction<LastingEffectRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Select a ring
        /// </summary>
        public static SelectRingAction SelectRing(object propertyFactoryOrProperties)
        {
            return CreateAction<SelectRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Switch conflict element
        /// </summary>
        public static SwitchConflictElementAction SwitchConflictElement(object propertyFactoryOrProperties = null)
        {
            return CreateAction<SwitchConflictElementAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Switch conflict type (military/political)
        /// </summary>
        public static SwitchConflictTypeAction SwitchConflictType(object propertyFactoryOrProperties = null)
        {
            return CreateAction<SwitchConflictTypeAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Take fate from ring
        /// </summary>
        public static TakeFateRingAction TakeFateFromRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TakeFateRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Take control of a ring
        /// </summary>
        public static TakeRingAction TakeRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TakeRingAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Claim a ring
        /// </summary>
        public static ClaimRingAction ClaimRing(object propertyFactoryOrProperties = null)
        {
            return CreateAction<ClaimRingAction>(propertyFactoryOrProperties);
        }

        #endregion

        #region Status Token Actions

        /// <summary>
        /// Discard status token
        /// </summary>
        public static DiscardStatusAction DiscardStatusToken(object propertyFactoryOrProperties = null)
        {
            return CreateAction<DiscardStatusAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Move status token between cards
        /// </summary>
        public static MoveTokenAction MoveStatusToken(object propertyFactoryOrProperties)
        {
            return CreateAction<MoveTokenAction>(propertyFactoryOrProperties);
        }

        #endregion

        #region General Actions

        /// <summary>
        /// Cancel an event or ability
        /// </summary>
        public static CancelAction Cancel(object propertyFactoryOrProperties = null)
        {
            return CreateAction<CancelAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Execute custom handler function
        /// </summary>
        public static HandlerAction Handler(object propertyFactoryOrProperties)
        {
            return CreateAction<HandlerAction>(propertyFactoryOrProperties);
        }

        #endregion

        #region Meta Actions

        /// <summary>
        /// Display card menu for selection
        /// </summary>
        public static CardMenuAction CardMenu(object propertyFactoryOrProperties)
        {
            return CreateAction<CardMenuAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Choose between multiple actions
        /// </summary>
        public static ChooseGameAction ChooseAction(object propertyFactoryOrProperties)
        {
            return CreateAction<ChooseGameAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Execute action conditionally
        /// </summary>
        public static ConditionalAction Conditional(object propertyFactoryOrProperties)
        {
            return CreateAction<ConditionalAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Execute action if able, otherwise do nothing
        /// </summary>
        public static IfAbleAction IfAble(object propertyFactoryOrProperties)
        {
            return CreateAction<IfAbleAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Execute multiple actions simultaneously
        /// </summary>
        public static JointGameAction Joint(params GameAction[] gameActions)
        {
            return new JointGameAction(gameActions.ToList());
        }

        /// <summary>
        /// Execute multiple actions (each may choose different targets)
        /// </summary>
        public static MultipleGameAction Multiple(params GameAction[] gameActions)
        {
            return new MultipleGameAction(gameActions.ToList());
        }

        /// <summary>
        /// Display menu prompt for player choice
        /// </summary>
        public static MenuPromptAction MenuPrompt(object propertyFactoryOrProperties)
        {
            return CreateAction<MenuPromptAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Select cards from a list
        /// </summary>
        public static SelectCardAction SelectCard(object propertyFactoryOrProperties)
        {
            return CreateAction<SelectCardAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Execute actions in sequence
        /// </summary>
        public static SequentialAction Sequential(params GameAction[] gameActions)
        {
            return new SequentialAction(gameActions.ToList());
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create action with properties or property factory
        /// </summary>
        private static T CreateAction<T>(object propertyFactoryOrProperties) where T : GameAction, new()
        {
            if (propertyFactoryOrProperties == null)
            {
                return new T();
            }

            // Check if it's a function (property factory)
            if (propertyFactoryOrProperties is System.Func<AbilityContext, GameAction.GameActionProperties> factory)
            {
                return (T)Activator.CreateInstance(typeof(T), factory);
            }

            // Otherwise treat as static properties
            if (propertyFactoryOrProperties is GameAction.GameActionProperties properties)
            {
                return (T)Activator.CreateInstance(typeof(T), properties);
            }

            // Try to convert to properties if it's a dictionary or similar
            try
            {
                var convertedProperties = ConvertToProperties(propertyFactoryOrProperties);
                return (T)Activator.CreateInstance(typeof(T), convertedProperties);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to create action {typeof(T).Name} with properties: {ex.Message}");
                return new T();
            }
        }

        /// <summary>
        /// Convert various property formats to GameActionProperties
        /// </summary>
        private static GameAction.GameActionProperties ConvertToProperties(object properties)
        {
            if (properties == null)
                return new GameAction.GameActionProperties();

            if (properties is GameAction.GameActionProperties gameActionProps)
                return gameActionProps;

            // Handle dictionary conversion
            if (properties is Dictionary<string, object> dict)
            {
                var result = new GameAction.GameActionProperties();

                if (dict.ContainsKey("target"))
                {
                    var target = dict["target"];
                    if (target is List<object> targetList)
                        result.target = targetList;
                    else if (target != null)
                        result.target = new List<object> { target };
                }

                if (dict.ContainsKey("cannotBeCancelled") && dict["cannotBeCancelled"] is bool cannotBeCancelled)
                    result.cannotBeCancelled = cannotBeCancelled;

                if (dict.ContainsKey("optional") && dict["optional"] is bool optional)
                    result.optional = optional;

                return result;
            }

            // Default fallback
            return new GameAction.GameActionProperties();
        }

        #endregion

        #region Fluent Builders

        /// <summary>
        /// Builder for creating actions with fluent syntax
        /// </summary>
        public static class Builder
        {
            /// <summary>
            /// Start building an action targeting specific objects
            /// </summary>
            public static ActionBuilder<T> Create<T>() where T : GameAction, new()
            {
                return new ActionBuilder<T>();
            }

            /// <summary>
            /// Quick bow action targeting specific character
            /// </summary>
            public static BowAction BowCharacter(BaseCard character)
            {
                return GameActions.Bow(new GameAction.GameActionProperties
                {
                    target = new List<object> { character }
                });
            }

            /// <summary>
            /// Quick honor action targeting specific character
            /// </summary>
            public static HonorAction HonorCharacter(BaseCard character)
            {
                return GameActions.Honor(new GameAction.GameActionProperties
                {
                    target = new List<object> { character }
                });
            }

            /// <summary>
            /// Quick gain fate action for specific player
            /// </summary>
            public static GainFateAction PlayerGainsFate(Player player, int amount = 1)
            {
                var action = GameActions.GainFate();
                action.SetDefaultTarget(context => player);
                return action;
            }

            /// <summary>
            /// Quick draw cards action for specific player
            /// </summary>
            public static DrawAction PlayerDrawsCards(Player player, int amount = 1)
            {
                var action = GameActions.Draw();
                action.SetDefaultTarget(context => player);
                return action;
            }
        }

        /// <summary>
        /// Fluent builder for configuring actions
        /// </summary>
        public class ActionBuilder<T> where T : GameAction, new()
        {
            private readonly GameAction.GameActionProperties properties = new GameAction.GameActionProperties();

            public ActionBuilder<T> Target(params object[] targets)
            {
                properties.target = targets.ToList();
                return this;
            }

            public ActionBuilder<T> Optional(bool optional = true)
            {
                properties.optional = optional;
                return this;
            }

            public ActionBuilder<T> CannotBeCancelled(bool cannotBeCancelled = true)
            {
                properties.cannotBeCancelled = cannotBeCancelled;
                return this;
            }

            public T Build()
            {
                return CreateAction<T>(properties);
            }
        }

        #endregion

        #region Convenient Combo Actions

        /// <summary>
        /// Character enters play and gains fate
        /// </summary>
        public static SequentialAction CharacterEntersPlayWithFate(BaseCard character, int fate = 0)
        {
            return Sequential(
                PutIntoPlay(new GameAction.GameActionProperties { target = new List<object> { character } }),
                PlaceFate(new GameAction.GameActionProperties
                {
                    target = new List<object> { character }
                })
            );
        }

        /// <summary>
        /// Player draws cards and gains honor
        /// </summary>
        public static SequentialAction DrawAndGainHonor(Player player, int cards = 1, int honor = 1)
        {
            return Sequential(
                Draw(new GameAction.GameActionProperties { target = new List<object> { player } }),
                GainHonor(new GameAction.GameActionProperties { target = new List<object> { player } })
            );
        }

        /// <summary>
        /// Bow character and remove fate
        /// </summary>
        public static SequentialAction BowAndRemoveFate(BaseCard character, int fate = 1)
        {
            return Sequential(
                Bow(new GameAction.GameActionProperties { target = new List<object> { character } }),
                RemoveFate(new GameAction.GameActionProperties { target = new List<object> { character } })
            );
        }

        /// <summary>
        /// Complex duel with resolution
        /// </summary>
        public static SequentialAction InitiateDuelWithResolution(
            BaseCard challenger,
            BaseCard target,
            System.Action<object> winnerEffect,
            System.Action<object> loserEffect)
        {
            var duelAction = Duel(new GameAction.GameActionProperties
            {
                target = new List<object> { challenger, target }
            });

            var resolutionAction = Handler(new GameAction.GameActionProperties());

            return Sequential(duelAction, resolutionAction);
        }

        #endregion
    }

    #region Action Class Definitions

    // Card Actions
    public class AddTokenAction : GameAction
    {
        public AddTokenAction() : base() { }
        public AddTokenAction(GameActionProperties properties) : base(properties) { }
        public AddTokenAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class AttachAction : GameAction
    {
        public AttachAction() : base() { }
        public AttachAction(GameActionProperties properties) : base(properties) { }
        public AttachAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class AttachToRingAction : GameAction
    {
        public AttachToRingAction() : base() { }
        public AttachToRingAction(GameActionProperties properties) : base(properties) { }
        public AttachToRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class BowAction : GameAction
    {
        public BowAction() : base() { }
        public BowAction(GameActionProperties properties) : base(properties) { }
        public BowAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class BreakAction : GameAction
    {
        public BreakAction() : base() { }
        public BreakAction(GameActionProperties properties) : base(properties) { }
        public BreakAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class LastingEffectCardAction : GameAction
    {
        public LastingEffectCardAction() : base() { }
        public LastingEffectCardAction(GameActionProperties properties) : base(properties) { }
        public LastingEffectCardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ClaimFavorAction : GameAction
    {
        public ClaimFavorAction() : base() { }
        public ClaimFavorAction(GameActionProperties properties) : base(properties) { }
        public ClaimFavorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class CreateTokenAction : GameAction
    {
        public CreateTokenAction() : base() { }
        public CreateTokenAction(GameActionProperties properties) : base(properties) { }
        public CreateTokenAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DetachAction : GameAction
    {
        public DetachAction() : base() { }
        public DetachAction(GameActionProperties properties) : base(properties) { }
        public DetachAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DiscardCardAction : GameAction
    {
        public DiscardCardAction() : base() { }
        public DiscardCardAction(GameActionProperties properties) : base(properties) { }
        public DiscardCardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DiscardFromPlayAction : GameAction
    {
        private bool isSacrifice = false;

        public DiscardFromPlayAction() : base() { }
        public DiscardFromPlayAction(GameActionProperties properties) : base(properties) { }
        public DiscardFromPlayAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }

        public virtual void SetSacrifice(bool sacrifice)
        {
            isSacrifice = sacrifice;
            if (sacrifice)
            {
                actionName = "Sacrifice";
                effectMessage = "sacrifices {0}";
            }
        }
    }

    public class DishonorAction : GameAction
    {
        public DishonorAction() : base() { }
        public DishonorAction(GameActionProperties properties) : base(properties) { }
        public DishonorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DuelAction : GameAction
    {
        public DuelAction() : base() { }
        public DuelAction(GameActionProperties properties) : base(properties) { }
        public DuelAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class FlipDynastyAction : GameAction
    {
        public FlipDynastyAction() : base() { }
        public FlipDynastyAction(GameActionProperties properties) : base(properties) { }
        public FlipDynastyAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class HonorAction : GameAction
    {
        public HonorAction() : base() { }
        public HonorAction(GameActionProperties properties) : base(properties) { }
        public HonorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class LookAtAction : GameAction
    {
        public LookAtAction() : base() { }
        public LookAtAction(GameActionProperties properties) : base(properties) { }
        public LookAtAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class MoveCardAction : GameAction
    {
        public MoveCardAction() : base() { }
        public MoveCardAction(GameActionProperties properties) : base(properties) { }
        public MoveCardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class MoveToConflictAction : GameAction
    {
        public MoveToConflictAction() : base() { }
        public MoveToConflictAction(GameActionProperties properties) : base(properties) { }
        public MoveToConflictAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class PlaceFateAction : GameAction
    {
        public PlaceFateAction() : base() { }
        public PlaceFateAction(GameActionProperties properties) : base(properties) { }
        public PlaceFateAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class PlayCardAction : GameAction
    {
        public PlayCardAction() : base() { }
        public PlayCardAction(GameActionProperties properties) : base(properties) { }
        public PlayCardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class GloryCountAction : GameAction
    {
        public GloryCountAction() : base() { }
        public GloryCountAction(GameActionProperties properties) : base(properties) { }
        public GloryCountAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class PutIntoPlayAction : GameAction
    {
        private bool intoConflict = false;

        public PutIntoPlayAction() : base() { }
        public PutIntoPlayAction(GameActionProperties properties) : base(properties) { }
        public PutIntoPlayAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }

        public virtual void SetIntoConflict(bool intoConflict)
        {
            this.intoConflict = intoConflict;
            if (intoConflict)
            {
                actionName = "Put Into Conflict";
                effectMessage = "puts {0} into play participating in the conflict";
            }
            else
            {
                actionName = "Put Into Play";
                effectMessage = "puts {0} into play";
            }
        }
    }

    public class ReadyAction : GameAction
    {
        public ReadyAction() : base() { }
        public ReadyAction(GameActionProperties properties) : base(properties) { }
        public ReadyAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class RemoveFateAction : GameAction
    {
        public RemoveFateAction() : base() { }
        public RemoveFateAction(GameActionProperties properties) : base(properties) { }
        public RemoveFateAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class RemoveFromGameAction : GameAction
    {
        public RemoveFromGameAction() : base() { }
        public RemoveFromGameAction(GameActionProperties properties) : base(properties) { }
        public RemoveFromGameAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ResolveAbilityAction : GameAction
    {
        public ResolveAbilityAction() : base() { }
        public ResolveAbilityAction(GameActionProperties properties) : base(properties) { }
        public ResolveAbilityAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ReturnToDeckAction : GameAction
    {
        public ReturnToDeckAction() : base() { }
        public ReturnToDeckAction(GameActionProperties properties) : base(properties) { }
        public ReturnToDeckAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ReturnToHandAction : GameAction
    {
        public ReturnToHandAction() : base() { }
        public ReturnToHandAction(GameActionProperties properties) : base(properties) { }
        public ReturnToHandAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class RevealAction : GameAction
    {
        public RevealAction() : base() { }
        public RevealAction(GameActionProperties properties) : base(properties) { }
        public RevealAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class SendHomeAction : GameAction
    {
        public SendHomeAction() : base() { }
        public SendHomeAction(GameActionProperties properties) : base(properties) { }
        public SendHomeAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class TakeControlAction : GameAction
    {
        public TakeControlAction() : base() { }
        public TakeControlAction(GameActionProperties properties) : base(properties) { }
        public TakeControlAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class TurnCardFacedownAction : GameAction
    {
        public TurnCardFacedownAction() : base() { }
        public TurnCardFacedownAction(GameActionProperties properties) : base(properties) { }
        public TurnCardFacedownAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    // Player Actions
    public class ChosenDiscardAction : GameAction
    {
        public ChosenDiscardAction() : base() { }
        public ChosenDiscardAction(GameActionProperties properties) : base(properties) { }
        public ChosenDiscardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DeckSearchAction : GameAction
    {
        public DeckSearchAction() : base() { }
        public DeckSearchAction(GameActionProperties properties) : base(properties) { }
        public DeckSearchAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class RandomDiscardAction : GameAction
    {
        public RandomDiscardAction() : base() { }
        public RandomDiscardAction(GameActionProperties properties) : base(properties) { }
        public RandomDiscardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DrawAction : GameAction
    {
        public DrawAction() : base() { }
        public DrawAction(GameActionProperties properties) : base(properties) { }
        public DrawAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class GainFateAction : GameAction
    {
        public GainFateAction() : base() { }
        public GainFateAction(GameActionProperties properties) : base(properties) { }
        public GainFateAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class GainHonorAction : GameAction
    {
        public GainHonorAction() : base() { }
        public GainHonorAction(GameActionProperties properties) : base(properties) { }
        public GainHonorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class InitiateConflictAction : GameAction
    {
        public InitiateConflictAction() : base() { }
        public InitiateConflictAction(GameActionProperties properties) : base(properties) { }
        public InitiateConflictAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class LoseFateAction : GameAction
    {
        public LoseFateAction() : base() { }
        public LoseFateAction(GameActionProperties properties) : base(properties) { }
        public LoseFateAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class LoseHonorAction : GameAction
    {
        public LoseHonorAction() : base() { }
        public LoseHonorAction(GameActionProperties properties) : base(properties) { }
        public LoseHonorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class DiscardFavorAction : GameAction
    {
        public DiscardFavorAction() : base() { }
        public DiscardFavorAction(GameActionProperties properties) : base(properties) { }
        public DiscardFavorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ModifyBidAction : GameAction
    {
        public ModifyBidAction() : base() { }
        public ModifyBidAction(GameActionProperties properties) : base(properties) { }
        public ModifyBidAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class LastingEffectAction : GameAction
    {
        public LastingEffectAction() : base() { }
        public LastingEffectAction(GameActionProperties properties) : base(properties) { }
        public LastingEffectAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class RefillFaceupAction : GameAction
    {
        public RefillFaceupAction() : base() { }
        public RefillFaceupAction(GameActionProperties properties) : base(properties) { }
        public RefillFaceupAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class SetDialAction : GameAction
    {
        public SetDialAction() : base() { }
        public SetDialAction(GameActionProperties properties) : base(properties) { }
        public SetDialAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ShuffleDeckAction : GameAction
    {
        public ShuffleDeckAction() : base() { }
        public ShuffleDeckAction(GameActionProperties properties) : base(properties) { }
        public ShuffleDeckAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class TransferFateAction : GameAction
    {
        public TransferFateAction() : base() { }
        public TransferFateAction(GameActionProperties properties) : base(properties) { }
        public TransferFateAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class TransferHonorAction : GameAction
    {
        public TransferHonorAction() : base() { }
        public TransferHonorAction(GameActionProperties properties) : base(properties) { }
        public TransferHonorAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    // Ring Actions
    public class PlaceFateRingAction : GameAction
    {
        public PlaceFateRingAction() : base() { }
        public PlaceFateRingAction(GameActionProperties properties) : base(properties) { }
        public PlaceFateRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ResolveConflictRingAction : GameAction
    {
        public ResolveConflictRingAction() : base() { }
        public ResolveConflictRingAction(GameActionProperties properties) : base(properties) { }
        public ResolveConflictRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ResolveElementAction : GameAction
    {
        public ResolveElementAction() : base() { }
        public ResolveElementAction(GameActionProperties properties) : base(properties) { }
        public ResolveElementAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ReturnRingAction : GameAction
    {
        public ReturnRingAction() : base() { }
        public ReturnRingAction(GameActionProperties properties) : base(properties) { }
        public ReturnRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class LastingEffectRingAction : GameAction
    {
        public LastingEffectRingAction() : base() { }
        public LastingEffectRingAction(GameActionProperties properties) : base(properties) { }
        public LastingEffectRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class SelectRingAction : GameAction
    {
        public SelectRingAction() : base() { }
        public SelectRingAction(GameActionProperties properties) : base(properties) { }
        public SelectRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class SwitchConflictElementAction : GameAction
    {
        public SwitchConflictElementAction() : base() { }
        public SwitchConflictElementAction(GameActionProperties properties) : base(properties) { }
        public SwitchConflictElementAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class SwitchConflictTypeAction : GameAction
    {
        public SwitchConflictTypeAction() : base() { }
        public SwitchConflictTypeAction(GameActionProperties properties) : base(properties) { }
        public SwitchConflictTypeAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class TakeFateRingAction : GameAction
    {
        public TakeFateRingAction() : base() { }
        public TakeFateRingAction(GameActionProperties properties) : base(properties) { }
        public TakeFateRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class TakeRingAction : GameAction
    {
        public TakeRingAction() : base() { }
        public TakeRingAction(GameActionProperties properties) : base(properties) { }
        public TakeRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ClaimRingAction : GameAction
    {
        public ClaimRingAction() : base() { }
        public ClaimRingAction(GameActionProperties properties) : base(properties) { }
        public ClaimRingAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    // Status Token Actions
    public class DiscardStatusAction : GameAction
    {
        public DiscardStatusAction() : base() { }
        public DiscardStatusAction(GameActionProperties properties) : base(properties) { }
        public DiscardStatusAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class MoveTokenAction : GameAction
    {
        public MoveTokenAction() : base() { }
        public MoveTokenAction(GameActionProperties properties) : base(properties) { }
        public MoveTokenAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    // General Actions
    public class CancelAction : GameAction
    {
        public CancelAction() : base() { }
        public CancelAction(GameActionProperties properties) : base(properties) { }
        public CancelAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class HandlerAction : GameAction
    {
        public HandlerAction() : base() { }
        public HandlerAction(GameActionProperties properties) : base(properties) { }
        public HandlerAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    // Meta Actions
    public class CardMenuAction : GameAction
    {
        public CardMenuAction() : base() { }
        public CardMenuAction(GameActionProperties properties) : base(properties) { }
        public CardMenuAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ChooseGameAction : GameAction
    {
        public ChooseGameAction() : base() { }
        public ChooseGameAction(GameActionProperties properties) : base(properties) { }
        public ChooseGameAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class ConditionalAction : GameAction
    {
        public ConditionalAction() : base() { }
        public ConditionalAction(GameActionProperties properties) : base(properties) { }
        public ConditionalAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class IfAbleAction : GameAction
    {
        public IfAbleAction() : base() { }
        public IfAbleAction(GameActionProperties properties) : base(properties) { }
        public IfAbleAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    /// <summary>
    /// Joint action executes multiple actions simultaneously with shared targeting
    /// </summary>
    public class JointGameAction : GameAction
    {
        private List<GameAction> actions;

        public JointGameAction(List<GameAction> gameActions) : base()
        {
            actions = gameActions ?? new List<GameAction>();
            actionName = "Joint Action";
            effectMessage = "executes {0} actions simultaneously";
        }

        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return actions.Any(action => action.HasLegalTarget(context, additionalProperties));
        }

        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            foreach (var action in actions)
            {
                action.AddEventsToArray(events, context, additionalProperties);
            }
        }

        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            // Joint actions execute all sub-actions
            LogExecution("Executing {0} joint actions", actions.Count);
        }

        public void AddAction(GameAction action)
        {
            if (action != null)
                actions.Add(action);
        }

        public List<GameAction> GetActions() => actions.ToList();
    }

    /// <summary>
    /// Multiple action allows each sub-action to choose different targets
    /// </summary>
    public class MultipleGameAction : GameAction
    {
        private List<GameAction> actions;

        public MultipleGameAction(List<GameAction> gameActions) : base()
        {
            actions = gameActions ?? new List<GameAction>();
            actionName = "Multiple Action";
            effectMessage = "executes {0} actions with separate targeting";
        }

        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return actions.Any(action => action.HasLegalTarget(context, additionalProperties));
        }

        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            foreach (var action in actions)
            {
                if (action.HasLegalTarget(context, additionalProperties))
                {
                    action.AddEventsToArray(events, context, additionalProperties);
                }
            }
        }

        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            LogExecution("Executing {0} multiple actions", actions.Count);
        }

        public void AddAction(GameAction action)
        {
            if (action != null)
                actions.Add(action);
        }

        public List<GameAction> GetActions() => actions.ToList();
    }

    /// <summary>
    /// Sequential action executes actions one after another
    /// </summary>
    public class SequentialAction : GameAction
    {
        private List<GameAction> actions;

        public SequentialAction(List<GameAction> gameActions) : base()
        {
            actions = gameActions ?? new List<GameAction>();
            actionName = "Sequential Action";
            effectMessage = "executes {0} actions in sequence";
        }

        public override bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            return actions.Any(action => action.HasLegalTarget(context, additionalProperties));
        }

        public override void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            // Sequential actions create a chain of events
            foreach (var action in actions)
            {
                if (action.HasLegalTarget(context, additionalProperties))
                {
                    action.AddEventsToArray(events, context, additionalProperties);
                }
            }
        }

        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            LogExecution("Executing {0} sequential actions", actions.Count);
        }

        public void AddAction(GameAction action)
        {
            if (action != null)
                actions.Add(action);
        }

        public List<GameAction> GetActions() => actions.ToList();
    }

    public class MenuPromptAction : GameAction
    {
        public MenuPromptAction() : base() { }
        public MenuPromptAction(GameActionProperties properties) : base(properties) { }
        public MenuPromptAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    public class SelectCardAction : GameAction
    {
        public SelectCardAction() : base() { }
        public SelectCardAction(GameActionProperties properties) : base(properties) { }
        public SelectCardAction(System.Func<AbilityContext, GameActionProperties> factory) : base(factory) { }
    }

    #endregion
}