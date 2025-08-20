using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class ChoiceOption
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public Action<Player> Handler { get; set; }
        public bool Disabled { get; set; }
        public object Data { get; set; }

        public ChoiceOption(string text, string value = null, Action<Player> handler = null)
        {
            Text = text;
            Value = value ?? text;
            Handler = handler;
            Disabled = false;
        }
    }

    public class ChoicePromptProperties
    {
        public string ActivePromptTitle { get; set; } = "Make a choice";
        public string WaitingPromptTitle { get; set; } = "Waiting for opponent to make a choice";
        public string PromptTitle { get; set; }
        public List<ChoiceOption> Choices { get; set; } = new List<ChoiceOption>();
        public Action<Player, string, ChoiceOption> OnChoiceMade { get; set; }
        public Action<Player> OnCancel { get; set; }
        public bool AllowCancel { get; set; } = false;
        public object Source { get; set; }
        public AbilityContext Context { get; set; }
        public int? MaxChoices { get; set; }
        public int? MinChoices { get; set; }
        public bool MultiSelect { get; set; } = false;
    }

    /// <summary>
    /// General-purpose choice prompt that presents a list of options to a player.
    /// Can be used for single or multiple choice scenarios with custom handlers.
    /// </summary>
    public class ChoicePrompt : UiPrompt
    {
        private Player choosingPlayer;
        private ChoicePromptProperties properties;
        private List<string> selectedChoices;
        private bool choiceCompleted;

        public ChoicePrompt(Game game, Player choosingPlayer, ChoicePromptProperties properties) : base(game)
        {
            this.choosingPlayer = choosingPlayer;
            this.properties = properties ?? new ChoicePromptProperties();
            selectedChoices = new List<string>();
            choiceCompleted = false;

            // Set default prompt title from source if available
            if (string.IsNullOrEmpty(this.properties.PromptTitle) && this.properties.Source != null)
            {
                this.properties.PromptTitle = GetSourceName(this.properties.Source);
            }
        }

        private string GetSourceName(object source)
        {
            if (source is EffectSource effectSource)
                return effectSource.Name;
            if (source is BaseCard card)
                return card.Name;
            return source?.ToString() ?? "";
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer && !choiceCompleted;
        }

        public override bool IsComplete()
        {
            return choiceCompleted;
        }

        public override object ActivePrompt(Player player)
        {
            var buttons = new List<object>();

            // Add choice buttons
            foreach (var choice in properties.Choices)
            {
                buttons.Add(new
                {
                    text = choice.Text,
                    arg = choice.Value,
                    disabled = choice.Disabled
                });
            }

            // Add control buttons
            if (properties.MultiSelect)
            {
                var minChoices = properties.MinChoices ?? 0;
                var maxChoices = properties.MaxChoices ?? properties.Choices.Count;
                
                if (selectedChoices.Count >= minChoices)
                {
                    buttons.Add(new { text = "Done", arg = "done" });
                }
                
                if (selectedChoices.Count > 0)
                {
                    buttons.Add(new { text = "Clear Selection", arg = "clear" });
                }
            }

            if (properties.AllowCancel)
            {
                buttons.Add(new { text = "Cancel", arg = "cancel" });
            }

            return new
            {
                promptTitle = properties.PromptTitle,
                menuTitle = properties.ActivePromptTitle + 
                           (properties.MultiSelect && selectedChoices.Count > 0 ? 
                            $" (Selected: {selectedChoices.Count})" : ""),
                buttons = buttons,
                selectCard = false,
                selectRing = false
            };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = properties.WaitingPromptTitle };
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (player != choosingPlayer || choiceCompleted)
            {
                return false;
            }

            // Handle control commands
            switch (arg)
            {
                case "cancel":
                    if (properties.AllowCancel)
                    {
                        properties.OnCancel?.Invoke(player);
                        choiceCompleted = true;
                        Complete();
                        return true;
                    }
                    return false;

                case "done":
                    if (properties.MultiSelect && IsValidSelection())
                    {
                        ExecuteMultipleChoices(player);
                        return true;
                    }
                    return false;

                case "clear":
                    if (properties.MultiSelect)
                    {
                        selectedChoices.Clear();
                        return true;
                    }
                    return false;
            }

            // Handle choice selection
            var choice = properties.Choices.FirstOrDefault(c => c.Value == arg);
            if (choice == null || choice.Disabled)
            {
                return false;
            }

            if (properties.MultiSelect)
            {
                return HandleMultiSelectChoice(player, choice);
            }
            else
            {
                return HandleSingleChoice(player, choice);
            }
        }

        private bool HandleSingleChoice(Player player, ChoiceOption choice)
        {
            // Execute the choice handler
            choice.Handler?.Invoke(player);
            properties.OnChoiceMade?.Invoke(player, choice.Value, choice);
            
            Game.AddMessage("{0} chooses: {1}", player, choice.Text);
            choiceCompleted = true;
            Complete();
            return true;
        }

        private bool HandleMultiSelectChoice(Player player, ChoiceOption choice)
        {
            var maxChoices = properties.MaxChoices ?? properties.Choices.Count;
            
            if (selectedChoices.Contains(choice.Value))
            {
                // Deselect
                selectedChoices.Remove(choice.Value);
            }
            else if (selectedChoices.Count < maxChoices)
            {
                // Select
                selectedChoices.Add(choice.Value);
            }
            else
            {
                // Can't select more
                return false;
            }

            return true;
        }

        private bool IsValidSelection()
        {
            var minChoices = properties.MinChoices ?? 0;
            var maxChoices = properties.MaxChoices ?? properties.Choices.Count;
            
            return selectedChoices.Count >= minChoices && selectedChoices.Count <= maxChoices;
        }

        private void ExecuteMultipleChoices(Player player)
        {
            var selectedOptions = properties.Choices.Where(c => selectedChoices.Contains(c.Value)).ToList();
            
            // Execute handlers for all selected choices
            foreach (var choice in selectedOptions)
            {
                choice.Handler?.Invoke(player);
                properties.OnChoiceMade?.Invoke(player, choice.Value, choice);
            }
            
            var choiceTexts = selectedOptions.Select(c => c.Text);
            Game.AddMessage("{0} chooses: {1}", player, string.Join(", ", choiceTexts));
            
            choiceCompleted = true;
            Complete();
        }
    }
}
