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
        /// Create a status token
        /// </summary>
        public static CreateTokenAction CreateToken(object propertyFactoryOrProperties = null)
        {
            return CreateAction<CreateTokenAction>(propertyFactoryOrProperties);
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
        /// Transfer fate from one player to another
        /// </summary>
        public static TransferFateAction TransferFate(object propertyFactoryOrProperties = null)
        {
            return CreateAction<TransferFateAction>(propertyFactoryOrProperties);
        }

        /// <summary>
        /// Transfer honor from one player to another
        /// </summary>
        public static TransferHonorAction TransferHonor(object propertyFactoryOrProperties = null)
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
                        result.Target = targetList;
                    else if (target != null)
                        result.Target = new List<object> { target };
                }

                if (dict.ContainsKey("cannotBeCancelled") && dict["cannotBeCancelled"] is bool cannotBeCancelled)
                    result.CannotBeCancelled = cannotBeCancelled;

                if (dict.ContainsKey("optional") && dict["optional"] is bool optional)
                    result.Optional = optional;

                return result;
            }

            // Default fallback
            return new GameAction.GameActionProperties();
        }

        #endregion
    }
}
