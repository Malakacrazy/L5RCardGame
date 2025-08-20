using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class HandlerMenuPromptProperties
    {
        public List<string> Choices { get; set; } = new List<string>();
        public List<Action> Handlers { get; set; } = new List<Action>();
        public Action<string> ChoiceHandler { get; set; }
        public string ActivePromptTitle { get; set; }
        public string WaitingPromptTitle { get; set; }
        public object Source { get; set; }
        public List<BaseCard> Cards { get; set; }
        public Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
        public Action<BaseCard> CardHandler { get; set; }
        public AbilityContext Context { get; set; }
        public object Controls { get; set; }
        public object Target { get; set; }
    }

    /// <summary>
    /// General purpose menu prompt. Takes a choices object with menu options and
    /// a handler for each. Handlers should return true in order to complete the
    /// prompt.
    /// 
    /// The properties option object may contain the following:
    /// choices            - an array of titles for menu buttons
    /// handlers           - an array of handlers corresponding to the menu buttons
    /// choiceHandler      - handler which is called when a choice button is clicked
    /// activePromptTitle  - the title that should be used in the prompt for the
    ///                      choosing player.
    /// waitingPromptTitle - the title to display for opponents.
    /// source             - what is at the origin of the user prompt, usually a card;
    ///                      used to provide a default waitingPromptTitle, if missing
    /// cards              - a list of cards to display as buttons with mouseover support
    /// cardCondition      - disables the prompt buttons for any cards which return false
    /// cardHandler        - handler which is called when a card button is clicked
    /// </summary>
    public class HandlerMenuPrompt : UiPrompt
    {
        protected Player player;
        protected HandlerMenuPromptProperties properties;
        protected Func<BaseCard, AbilityContext, bool> cardCondition;
        protected AbilityContext context;

        public HandlerMenuPrompt(Game game, Player player, HandlerMenuPromptProperties properties) : base(game)
        {
            this.player = player;
            
            if (properties.Source is string sourceName)
            {
                properties.Source = new EffectSource(game, sourceName);
            }
            else if (properties.Context?.Source != null)
            {
                properties.Source = properties.Context.Source;
            }
            
            if (properties.Source != null && string.IsNullOrEmpty(properties.WaitingPromptTitle))
            {
                properties.WaitingPromptTitle = $"Waiting for opponent to use {GetSourceName(properties.Source)}";
            }
            else if (properties.Source == null)
            {
                properties.Source = new EffectSource(game);
            }
            
            this.properties = properties;
            cardCondition = properties.CardCondition ?? ((card, ctx) => true);
            context = properties.Context ?? new AbilityContext(game, player, (EffectSource)properties.Source);
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
            return player == this.player;
        }

        public override object ActivePrompt(Player player)
        {
            var buttons = new List<object>();
            
            if (properties.Cards != null && properties.Cards.Count > 0)
            {
                var cardQuantities = new Dictionary<string, int>();
                foreach (var card in properties.Cards)
                {
                    if (cardQuantities.ContainsKey(card.Id))
                    {
                        cardQuantities[card.Id]++;
                    }
                    else
                    {
                        cardQuantities[card.Id] = 1;
                    }
                }
                
                var uniqueCards = properties.Cards.GroupBy(c => c.Id).Select(g => g.First()).ToList();
                var cardButtons = uniqueCards.Select(card =>
                {
                    var text = card.Name;
                    if (cardQuantities[card.Id] > 1)
                    {
                        text = $"{text} ({cardQuantities[card.Id]})";
                    }
                    return new
                    {
                        text = text,
                        arg = card.Id,
                        card = card,
                        disabled = !cardCondition(card, context)
                    };
                });
                buttons.AddRange(cardButtons);
            }
            
            var choiceButtons = properties.Choices.Select((choice, index) => new
            {
                text = choice,
                arg = index
            });
            buttons.AddRange(choiceButtons);

            return new
            {
                menuTitle = properties.ActivePromptTitle ?? "Select one",
                buttons = buttons,
                controls = GetAdditionalPromptControls(),
                promptTitle = GetSourceName(properties.Source)
            };
        }

        protected virtual List<object> GetAdditionalPromptControls()
        {
            if (properties.Controls != null)
            {
                var controlsType = properties.Controls.GetType();
                var typeProperty = controlsType.GetProperty("type");
                
                if (typeProperty?.GetValue(properties.Controls)?.ToString() == "targeting")
                {
                    var targetsProperty = controlsType.GetProperty("targets");
                    var targets = targetsProperty?.GetValue(properties.Controls) as IEnumerable<object>;
                    
                    return new List<object>
                    {
                        new
                        {
                            type = "targeting",
                            source = ((EffectSource)properties.Source).GetShortSummary(),
                            targets = targets?.Select(target => 
                                target is BaseCard card ? card.GetShortSummaryForControls(player) : target).ToList()
                        }
                    };
                }
            }
            
            if (((EffectSource)context.Source).Type == "")
            {
                return new List<object>();
            }
            
            var contextTargets = context.Targets?.Values.SelectMany(t => t).ToList() ?? new List<object>();
            
            if (properties.Target != null)
            {
                contextTargets = properties.Target is IEnumerable<object> targetList 
                    ? targetList.ToList() 
                    : new List<object> { properties.Target };
            }
            
            if (contextTargets.Count == 0 && context.Event?.Card != null)
            {
                contextTargets = new List<object> { context.Event.Card };
            }
            
            return new List<object>
            {
                new
                {
                    type = "targeting",
                    source = context.Source.GetShortSummary(),
                    targets = contextTargets.Select(target => 
                        target is BaseCard card ? card.GetShortSummaryForControls(player) : target).ToList()
                }
            };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = properties.WaitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            // Try to parse as card ID first
            if (properties.Cards != null)
            {
                var card = properties.Cards.FirstOrDefault(c => c.Id == arg);
                if (card != null && properties.CardHandler != null)
                {
                    properties.CardHandler(card);
                    Complete();
                    return true;
                }
            }
            
            // Try to parse as choice index
            if (int.TryParse(arg, out int choiceIndex))
            {
                if (properties.ChoiceHandler != null && choiceIndex >= 0 && choiceIndex < properties.Choices.Count)
                {
                    properties.ChoiceHandler(properties.Choices[choiceIndex]);
                    Complete();
                    return true;
                }
                
                if (properties.Handlers != null && choiceIndex >= 0 && choiceIndex < properties.Handlers.Count)
                {
                    properties.Handlers[choiceIndex]();
                    Complete();
                    return true;
                }
            }
            
            return false;
        }
    }
}
