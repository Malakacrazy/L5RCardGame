using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Results from cost resolution
    /// </summary>
    [System.Serializable]
    public class CostResults
    {
        public bool cancelled = false;
        public bool canCancel = true;
        public List<object> events = new List<object>();
        public bool playCosts = true;
        public bool triggerCosts = true;
    }

    /// <summary>
    /// Results from target resolution
    /// </summary>
    [System.Serializable]
    public class TargetResults
    {
        public bool cancelled = false;
        public bool delayTargeting = false;
        public bool payCostsFirst = false;
        public Dictionary<string, object> targets = new Dictionary<string, object>();
    }

    /// <summary>
    /// Handles the complete resolution pipeline for card abilities.
    /// Manages targeting, cost payment, and execution in proper order.
    /// </summary>
    public class AbilityResolver : BaseStepWithPipeline
    {
        public AbilityContext context;
        public bool canCancel = true;
        public bool initiateAbility = false;
        public bool passPriority = false;
        public List<object> events = new List<object>();
        public List<ProvinceRefill> provincesToRefill = new List<ProvinceRefill>();
        public TargetResults targetResults = new TargetResults();
        public CostResults costResults;

        // State tracking
        public bool cancelled = false;

        public AbilityResolver(Game game, AbilityContext context) : base(game)
        {
            this.context = context;
            this.costResults = GetCostResults();
            Initialize();
        }

        protected override void Initialize()
        {
            pipeline.Initialize(new List<IGameStep>
            {
                new SimpleStep(game, CreateSnapshot),
                new SimpleStep(game, ResolveEarlyTargets),
                new SimpleStep(game, CheckForCancel),
                new SimpleStep(game, OpenInitiateAbilityEventWindow),
                new SimpleStep(game, RefillProvinces)
            });
        }

        /// <summary>
        /// Create snapshot of card state when ability was initiated
        /// </summary>
        public bool CreateSnapshot()
        {
            var cardTypes = new List<string> { CardTypes.Character, CardTypes.Holding, CardTypes.Attachment };
            
            if (context.source is BaseCard card && cardTypes.Contains(card.GetCardType()))
            {
                context.cardStateWhenInitiated = card.CreateSnapshot();
            }
            
            return true;
        }

        /// <summary>
        /// Opens the initiate ability event window and queues subsequent steps
        /// </summary>
        public bool OpenInitiateAbilityEventWindow()
        {
            if (cancelled)
            {
                return true;
            }

            string eventName = EventNames.Unnamed;
            var eventProps = new Dictionary<string, object>();

            if (context.ability.IsCardAbility())
            {
                eventName = EventNames.OnCardAbilityInitiated;
                eventProps = new Dictionary<string, object>
                {
                    { "card", context.source },
                    { "ability", context.ability },
                    { "context", context }
                };

                // Handle card played events
                if (context.ability.IsCardPlayed())
                {
                    var cardPlayedEvent = game.GetEvent(EventNames.OnCardPlayed, new Dictionary<string, object>
                    {
                        { "player", context.player },
                        { "card", context.source },
                        { "context", context },
                        { "originalLocation", ((BaseCard)context.source).location },
                        { "playType", context.playType },
                        { "resolver", this }
                    });
                    events.Add(cardPlayedEvent);
                }

                // Handle triggered ability events
                if (context.ability.IsTriggeredAbility())
                {
                    var triggeredEvent = game.GetEvent(EventNames.OnCardAbilityTriggered, new Dictionary<string, object>
                    {
                        { "player", context.player },
                        { "card", context.source },
                        { "context", context }
                    });
                    events.Add(triggeredEvent);
                }
            }

            // Add main initiate event
            var initiateEvent = game.GetEvent(eventName, eventProps, QueueInitiateAbilitySteps);
            events.Add(initiateEvent);

            // Queue the event window
            game.QueueStep(new InitiateAbilityEventWindow(game, events));
            
            return true;
        }

        /// <summary>
        /// Queues the main ability resolution steps
        /// </summary>
        public void QueueInitiateAbilitySteps()
        {
            QueueStep(new SimpleStep(game, ResolveCosts));
            QueueStep(new SimpleStep(game, PayCosts));
            QueueStep(new SimpleStep(game, CheckCostsWerePaid));
            QueueStep(new SimpleStep(game, ResolveTargets));
            QueueStep(new SimpleStep(game, CheckForCancel));
            QueueStep(new SimpleStep(game, InitiateAbilityEffects));
            QueueStep(new SimpleStep(game, ExecuteHandler));
            QueueStep(new SimpleStep(game, MoveEventCardToDiscard));
        }

        /// <summary>
        /// Resolve early targets (pre-cost targets)
        /// </summary>
        public bool ResolveEarlyTargets()
        {
            context.SetStage(Stages.PreTarget);
            
            if (!context.ability.cannotTargetFirst)
            {
                targetResults = context.ability.ResolveTargets(context);
            }
            
            return true;
        }

        /// <summary>
        /// Check if the ability should be cancelled
        /// </summary>
        public bool CheckForCancel()
        {
            if (cancelled)
            {
                return true;
            }

            cancelled = targetResults.cancelled;
            return true;
        }

        /// <summary>
        /// Resolve ability costs
        /// </summary>
        public bool ResolveCosts()
        {
            if (cancelled)
            {
                return true;
            }

            costResults.canCancel = canCancel;
            context.SetStage(Stages.Cost);
            context.ability.ResolveCosts(context, costResults);
            
            return true;
        }

        /// <summary>
        /// Get initial cost results structure
        /// </summary>
        public CostResults GetCostResults()
        {
            return new CostResults
            {
                cancelled = false,
                canCancel = canCancel,
                events = new List<object>(),
                playCosts = true,
                triggerCosts = true
            };
        }

        /// <summary>
        /// Pay the resolved costs
        /// </summary>
        public bool PayCosts()
        {
            if (cancelled)
            {
                return true;
            }
            
            if (costResults.cancelled)
            {
                cancelled = true;
                return true;
            }

            passPriority = true;
            
            if (costResults.events.Count > 0)
            {
                game.OpenEventWindow(costResults.events);
            }
            
            return true;
        }

        /// <summary>
        /// Check that all costs were successfully paid
        /// </summary>
        public bool CheckCostsWerePaid()
        {
            if (cancelled)
            {
                return true;
            }

            // Check if any cost events were cancelled
            cancelled = costResults.events.Any(eventObj => 
            {
                if (eventObj is IGameEvent gameEvent)
                {
                    return gameEvent.GetResolutionEvent()?.cancelled ?? false;
                }
                return false;
            });

            if (cancelled)
            {
                game.AddMessage("{0} attempted to use {1}, but did not successfully pay the required costs", 
                               context.player.name, GetSourceName());
            }
            
            return true;
        }

        /// <summary>
        /// Resolve ability targets
        /// </summary>
        public bool ResolveTargets()
        {
            if (cancelled)
            {
                return true;
            }

            context.SetStage(Stages.Target);

            if (!context.ability.HasLegalTargets(context))
            {
                // Ability cannot resolve, so display a message and cancel it
                game.AddMessage("{0} attempted to use {1}, but there are insufficient legal targets", 
                               context.player.name, GetSourceName());
                cancelled = true;
            }
            else if (targetResults.delayTargeting)
            {
                // Targeting was delayed due to an opponent needing to choose targets
                targetResults = context.ability.ResolveRemainingTargets(context, targetResults);
            }
            else if (targetResults.payCostsFirst || !context.ability.CheckAllTargets(context))
            {
                // Targeting was stopped by the player choosing to pay costs first, or one of the chosen targets is no longer legal
                targetResults = context.ability.ResolveTargets(context);
            }
            
            return true;
        }

        /// <summary>
        /// Initiate ability effects and handle limits
        /// </summary>
        public bool InitiateAbilityEffects()
        {
            if (cancelled)
            {
                // Cancel all events if ability is cancelled
                foreach (var eventObj in events)
                {
                    if (eventObj is IGameEvent gameEvent)
                    {
                        gameEvent.Cancel();
                    }
                }
                return true;
            }

            // Handle card played effects
            if (context.ability.IsCardPlayed())
            {
                if (context.source is BaseCard card && card.IsLimited())
                {
                    context.player.limitedPlayed += 1;
                }

                if (game.currentConflict != null)
                {
                    game.currentConflict.AddCardPlayed(context.player, (BaseCard)context.source);
                }
            }

            // Increment limits (limits aren't used up on cards in hand)
            if (context.ability.limit != null && 
                context.source is BaseCard sourceCard && 
                sourceCard.location != Locations.Hand &&
                (context.cardStateWhenInitiated == null || 
                 context.cardStateWhenInitiated.location == sourceCard.location))
            {
                context.ability.limit.Increment(context.player);
            }

            if (context.ability.max != null)
            {
                context.player.IncrementAbilityMax(context.ability.maxIdentifier);
            }

            // Display ability message
            context.ability.DisplayMessage(context);

            // Handle triggered abilities
            if (context.ability.IsTriggeredAbility())
            {
                // If this is an event, move it to 'being played'
                if (context.ability.IsCardPlayed() && context.source is BaseCard eventCard)
                {
                    var moveAction = game.actions.MoveCard(new Dictionary<string, object>
                    {
                        { "destination", Locations.BeingPlayed }
                    });
                    moveAction.Resolve(eventCard, context);
                }

                // Open event window for card ability initiation
                var initiateEvent = new InitiateCardAbilityEvent(
                    new Dictionary<string, object>
                    {
                        { "card", context.source },
                        { "context", context }
                    },
                    () => initiateAbility = true
                );
                
                game.OpenThenEventWindow(initiateEvent);
            }
            else
            {
                initiateAbility = true;
            }
            
            return true;
        }

        /// <summary>
        /// Execute the ability handler
        /// </summary>
        public bool ExecuteHandler()
        {
            if (cancelled || !initiateAbility)
            {
                return true;
            }

            context.SetStage(Stages.Effect);
            context.ability.ExecuteHandler(context);
            
            return true;
        }

        /// <summary>
        /// Move event cards to discard pile after resolution
        /// </summary>
        public bool MoveEventCardToDiscard()
        {
            if (context.source is BaseCard card && card.location == Locations.BeingPlayed)
            {
                context.player.MoveCard(card, Locations.ConflictDiscardPile);
            }
            
            return true;
        }

        /// <summary>
        /// Refill provinces that were marked for refill
        /// </summary>
        public bool RefillProvinces()
        {
            context.Refill();
            return true;
        }

        /// <summary>
        /// Get a displayable name for the source
        /// </summary>
        private string GetSourceName()
        {
            if (context.source is BaseCard card)
            {
                return card.name;
            }
            if (context.source is Ring ring)
            {
                return ring.name;
            }
            return context.source?.ToString() ?? "Unknown Source";
        }

        /// <summary>
        /// Extension methods for easy ability resolver creation
        /// </summary>
        public static class AbilityResolverExtensions
        {
            /// <summary>
            /// Create and execute an ability resolver
            /// </summary>
            public static AbilityResolver ResolveAbility(this Game game, AbilityContext context)
            {
                var resolver = new AbilityResolver(game, context);
                game.QueueStep(resolver);
                return resolver;
            }

            /// <summary>
            /// Create ability resolver for card action
            /// </summary>
            public static AbilityResolver ResolveCardAction(this Game game, BaseCard card, Player player, object ability)
            {
                var context = AbilityContext.CreateCardContext(game, card, player, ability);
                return game.ResolveAbility(context);
            }

            /// <summary>
            /// Create ability resolver for ring effect
            /// </summary>
            public static AbilityResolver ResolveRingEffect(this Game game, Ring ring, Player player)
            {
                var context = AbilityContext.CreateRingContext(game, ring, player);
                return game.ResolveAbility(context);
            }
        }
    }

    /// <summary>
    /// Represents a province that needs to be refilled
    /// </summary>
    [System.Serializable]
    public class ProvinceRefill
    {
        public Player player;
        public string location;

        public ProvinceRefill(Player player, string location)
        {
            this.player = player;
            this.location = location;
        }
    }

    /// <summary>
    /// Interface for game events
    /// </summary>
    public interface IGameEvent
    {
        void Cancel();
        IGameEvent GetResolutionEvent();
        bool cancelled { get; set; }
    }

    /// <summary>
    /// Simple step implementation for pipeline
    /// </summary>
    public class SimpleStep : IGameStep
    {
        private Game game;
        private Func<bool> action;

        public SimpleStep(Game game, Func<bool> action)
        {
            this.game = game;
            this.action = action;
        }

        public bool Execute()
        {
            return action();
        }

        public bool IsComplete => true;
        public bool CanCancel => false;
    }

    /// <summary>
    /// Event window for ability initiation
    /// </summary>
    public class InitiateAbilityEventWindow : IGameStep
    {
        private Game game;
        private List<object> events;

        public InitiateAbilityEventWindow(Game game, List<object> events)
        {
            this.game = game;
            this.events = events;
        }

        public bool Execute()
        {
            // Process all initiate events
            foreach (var eventObj in events)
            {
                if (eventObj is IGameEvent gameEvent)
                {
                    // Process the event
                    Debug.Log($"🎯 Processing initiate event: {eventObj.GetType().Name}");
                }
            }
            return true;
        }

        public bool IsComplete => true;
        public bool CanCancel => false;
    }

    /// <summary>
    /// Card ability initiation event
    /// </summary>
    public class InitiateCardAbilityEvent : IGameEvent
    {
        public Dictionary<string, object> properties;
        public Action callback;
        public bool cancelled { get; set; }

        public InitiateCardAbilityEvent(Dictionary<string, object> properties, Action callback)
        {
            this.properties = properties;
            this.callback = callback;
        }

        public void Cancel()
        {
            cancelled = true;
        }

        public IGameEvent GetResolutionEvent()
        {
            return this;
        }
    }
}