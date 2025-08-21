using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a choice option for simultaneous effects
    /// </summary>
    public class SimultaneousEffectChoice
    {
        public string Title { get; set; }
        public Action Handler { get; set; }
        public Func<bool> Condition { get; set; }

        public SimultaneousEffectChoice(string title, Action handler, Func<bool> condition = null)
        {
            Title = title;
            Handler = handler;
            Condition = condition ?? (() => true);
        }
    }

    /// <summary>
    /// Window for handling simultaneous effects that need to be ordered by players
    /// </summary>
    public class SimultaneousEffectWindow : ForcedTriggeredAbilityWindow
    {
        protected List<SimultaneousEffectChoice> choices;

        public SimultaneousEffectWindow(Game game) : base(game, "delayedeffects")
        {
            choices = new List<SimultaneousEffectChoice>();
        }

        /// <summary>
        /// Add a choice to be resolved
        /// </summary>
        public void AddChoice(SimultaneousEffectChoice choice)
        {
            if (choice.Condition == null)
            {
                choice.Condition = () => true;
            }
            choices.Add(choice);
        }

        /// <summary>
        /// Add a choice with simple parameters
        /// </summary>
        public void AddChoice(string title, Action handler, Func<bool> condition = null)
        {
            AddChoice(new SimultaneousEffectChoice(title, handler, condition));
        }

        protected override bool FilterChoices()
        {
            var validChoices = choices.Where(choice => choice.Condition()).ToList();
            
            if (validChoices.Count == 0)
            {
                return true;
            }
            
            if (validChoices.Count == 1 || !CurrentPlayer.OptionSettings.OrderForcedAbilities)
            {
                ResolveEffect(validChoices[0]);
            }
            else
            {
                PromptBetweenChoices(validChoices);
            }
            
            return false;
        }

        private void PromptBetweenChoices(List<SimultaneousEffectChoice> validChoices)
        {
            var promptProperties = new HandlerMenuPromptProperties
            {
                Source = "Order Simultaneous effects",
                ActivePromptTitle = "Choose an effect to be resolved",
                WaitingPromptTitle = "Waiting for opponent",
                Choices = validChoices.Select(choice => choice.Title).ToList(),
                Handlers = validChoices.Select<SimultaneousEffectChoice, Action>(choice => () => ResolveEffect(choice)).ToList()
            };

            Game.PromptWithHandlerMenu(CurrentPlayer, promptProperties);
        }

        private void ResolveEffect(SimultaneousEffectChoice choice)
        {
            choices.Remove(choice);
            choice.Handler?.Invoke();
        }

        /// <summary>
        /// Clear all pending choices
        /// </summary>
        public void ClearChoices()
        {
            choices.Clear();
        }

        /// <summary>
        /// Get the number of pending choices
        /// </summary>
        public int GetChoiceCount()
        {
            return choices.Count(choice => choice.Condition());
        }

        /// <summary>
        /// Check if there are any valid choices remaining
        /// </summary>
        public bool HasValidChoices()
        {
            return choices.Any(choice => choice.Condition());
        }
    }
}
