using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Manages ability usage limits (per round, per conflict, etc.)
    /// </summary>
    [Serializable]
    public class AbilityLimit
    {
        public int maxUses;
        public string limitType;
        public Dictionary<string, int> currentUses;
        public BaseAbility ability;

        public AbilityLimit()
        {
            currentUses = new Dictionary<string, int>();
        }

        public static AbilityLimit PerRound(int max)
        {
            return new AbilityLimit { limitType = "perRound", maxUses = max };
        }

        public static AbilityLimit PerConflict(int max)
        {
            return new AbilityLimit { limitType = "perConflict", maxUses = max };
        }

        public static AbilityLimit PerPhase(int max)
        {
            return new AbilityLimit { limitType = "perPhase", maxUses = max };
        }

        public void RegisterEvents()
        {
            // Register event handlers based on limit type
        }

        public bool IsAtMax()
        {
            // Check if limit has been reached
            return false;
        }

        public void Increment()
        {
            // Increment usage counter
        }
    }

    /// <summary>
    /// Base class for all abilities
    /// </summary>
    [Serializable]
    public abstract class BaseAbility
    {
        public string id;
        public string title;
        public AbilityLimit limit;
        public List<ICost> cost = new List<ICost>();

        public BaseAbility()
        {
            cost = new List<ICost>();
        }

        public virtual bool MeetsRequirements(AbilityContext context, List<string> errors = null)
        {
            return true;
        }

        public virtual List<ICost> GetCosts(AbilityContext context, bool isResolution = false, bool displayOnly = false)
        {
            return cost ?? new List<ICost>();
        }

        public virtual bool IsInValidLocation(AbilityContext context)
        {
            return true;
        }

        public virtual void DisplayMessage(AbilityContext context)
        {
            // Display ability message
        }

        public virtual bool IsTriggeredAbility()
        {
            return false;
        }

        public virtual bool IsCardAbility()
        {
            return true;
        }

        public virtual bool IsKeywordAbility()
        {
            return false;
        }
    }

    /// <summary>
    /// Interface for costs
    /// </summary>
    public interface ICost
    {
        bool CanPay(AbilityContext context);
        void Pay(AbilityContext context);
        string GetMessage(AbilityContext context);
    }

    /// <summary>
    /// Base selector for cards
    /// </summary>
    [Serializable]
    public abstract class BaseCardSelector
    {
        public int numCards = 1;
        public bool optional = false;
        public Players controller = Players.Any;
        public Locations location = Locations.Any;
        public List<CardTypes> cardTypes = new List<CardTypes>();
        
        public virtual bool CanSelect(BaseCard card, AbilityContext context)
        {
            return true;
        }
        
        public virtual List<BaseCard> GetDefaultTargets(AbilityContext context)
        {
            return new List<BaseCard>();
        }
    }

    /// <summary>
    /// Game step interface
    /// </summary>
    public interface IGameStep
    {
        bool Execute();
        bool IsComplete { get; }
        bool CanCancel { get; }
        string GetDebugInfo();
        void Continue();
        void CancelStep();
        void QueueStep(IGameStep step);
        void OnCardClicked(Player player, BaseCard card);
        void OnRingClicked(Player player, Ring ring);
        void OnMenuCommand(Player player, string command, string arg1, string arg2);
        string StepName { get; }
    }

    /// <summary>
    /// Simple step implementation
    /// </summary>
    [Serializable]
    public class SimpleStep : IGameStep
    {
        protected string name;
        protected System.Action executeAction;
        protected bool isComplete;

        public SimpleStep(string name, System.Action action)
        {
            this.name = name;
            this.executeAction = action;
        }

        public bool Execute()
        {
            executeAction?.Invoke();
            isComplete = true;
            return true;
        }

        public bool IsComplete => isComplete;
        public bool CanCancel => false;
        public string StepName => name;

        public string GetDebugInfo()
        {
            return $"SimpleStep: {name}";
        }

        public void Continue()
        {
            // Simple steps complete immediately
        }

        public void CancelStep()
        {
            // Cannot cancel simple steps
        }

        public void QueueStep(IGameStep step)
        {
            // Simple steps don't queue other steps
        }

        public void OnCardClicked(Player player, BaseCard card)
        {
            // No card interaction for simple steps
        }

        public void OnRingClicked(Player player, Ring ring)
        {
            // No ring interaction for simple steps
        }

        public void OnMenuCommand(Player player, string command, string arg1, string arg2)
        {
            // No menu commands for simple steps
        }
    }

    /// <summary>
    /// Base step implementation
    /// </summary>
    [Serializable]
    public abstract class BaseStep : IGameStep
    {
        protected string stepName;
        protected bool isComplete;
        protected bool canCancel;

        public virtual bool Execute()
        {
            return true;
        }

        public virtual bool IsComplete => isComplete;
        public virtual bool CanCancel => canCancel;
        public virtual string StepName => stepName;

        public virtual string GetDebugInfo()
        {
            return $"{GetType().Name}: {stepName}";
        }

        public virtual void Continue()
        {
            // Override in derived classes
        }

        public virtual void CancelStep()
        {
            // Override in derived classes
        }

        public virtual void QueueStep(IGameStep step)
        {
            // Override in derived classes
        }

        public virtual void OnCardClicked(Player player, BaseCard card)
        {
            // Override in derived classes
        }

        public virtual void OnRingClicked(Player player, Ring ring)
        {
            // Override in derived classes
        }

        public virtual void OnMenuCommand(Player player, string command, string arg1, string arg2)
        {
            // Override in derived classes
        }
    }

    /// <summary>
    /// Triggered ability context
    /// </summary>
    [Serializable]
    public class TriggeredAbilityContext : AbilityContext
    {
        public IGameEvent TriggeringEvent { get; set; }
    }

    /// <summary>
    /// When type for timing
    /// </summary>
    public enum WhenType
    {
        BeforeReaction,
        AfterReaction,
        Immediate,
        EndOfPhase,
        EndOfRound,
        Custom
    }

    /// <summary>
    /// Province reference (for StatusToken errors)
    /// </summary>
    [Serializable]
    public class Province
    {
        public string id;
        public string name;
        public ProvinceCard card;
        public int strength;
        public bool isBroken;
        
        public Province()
        {
            // Constructor
        }
    }

    /// <summary>
    /// Lasting effect general properties interface
    /// </summary>
    public interface ILastingEffectGeneralProperties
    {
        Durations Duration { get; set; }
        Func<AbilityContext, bool> Condition { get; set; }
        WhenType Until { get; set; }
        object Effect { get; set; }
    }

    /// <summary>
    /// Then event window for event chaining
    /// </summary>
    [Serializable]
    public class ThenEventWindow : EventWindow
    {
        public ThenEventWindow(Game game, string eventName) : base(game, eventName)
        {
        }
    }
}
