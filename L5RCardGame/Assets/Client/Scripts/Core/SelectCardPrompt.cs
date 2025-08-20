using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectCardProperties
    {
        public List<object> Buttons { get; set; } = new List<object>();
        public List<object> Controls { get; set; }
        public bool SelectCard { get; set; } = true;
        public Func<BaseCard, AbilityContext, bool> CardCondition { get; set; } = (card, context) => true;
        public Func<Player, object, bool> OnSelect { get; set; } = (player, cards) => true;
        public Func<Player, string, bool> OnMenuCommand { get; set; } = (player, arg) => true;
        public Func<Player, bool> OnCancel { get; set; } = player => true;
        public string ActivePromptTitle { get; set; }
        public string WaitingPromptTitle { get; set; }
        public object Source { get; set; }
        public AbilityContext Context { get; set; }
        public object GameAction { get; set; }
        public bool Ordered { get; set; }
        public List<BaseCard> MustSelect { get; set; }
        public BaseCardSelector Selector { get; set; }
        public Action<Player, BaseCard> OnCardToggle { get; set; }
    }

    /// <summary>
    /// General purpose prompt that asks the user to select 1 or more cards.
    /// </summary>
    public class SelectCardPrompt : UiPrompt
    {
        protected Player choosingPlayer;
        protected SelectCardProperties properties;
        protected AbilityContext context;
        protected BaseCardSelector selector;
        protected List<BaseCard> selectedCards;
        protected List<BaseCard> previouslySelectedCards;
        protected bool onlyMustSelectMayBeChosen;
        protected bool cannotUnselectMustSelect;

        public SelectCardPrompt(Game game, Player choosingPlayer, SelectCardProperties properties) : base(game)
        {
            this.choosingPlayer = choosingPlayer;
            
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
                properties.WaitingPromptTitle = $"Waiting for opponent to use {((EffectSource)properties.Source).Name}";
            }
            
            if (properties.Source == null)
            {
                properties.Source = new EffectSource(game);
            }

            this.properties = properties;
            context = properties.Context ?? new AbilityContext(game, choosingPlayer, (EffectSource)properties.Source);
            ApplyDefaultProperties();
            
            if (properties.GameAction != null)
            {
                if (properties.GameAction is GameAction singleAction)
                {
                    properties.GameAction = new List<GameAction> { singleAction };
                }
                else if (properties.GameAction is IEnumerable<GameAction> actions)
                {
                    var originalCondition = properties.CardCondition;
                    properties.CardCondition = (card, ctx) =>
                        originalCondition(card, ctx) && actions.Any(gameAction => gameAction.CanAffect(card, ctx));
                }
            }
            
            selector = properties.Selector ?? CardSelector.For(properties);
            selectedCards = new List<BaseCard>();
            
            if (properties.MustSelect != null && properties.MustSelect.Count > 0)
            {
                if (selector.HasEnoughSelected(properties.MustSelect) && properties.MustSelect.Count >= selector.NumCards)
                {
                    onlyMustSelectMayBeChosen = true;
                }
                else
                {
                    selectedCards = new List<BaseCard>(properties.MustSelect);
                    cannotUnselectMustSelect = true;
                }
            }
            
            SavePreviouslySelectedCards();
        }

        protected virtual void ApplyDefaultProperties()
        {
            if (properties.Controls == null)
            {
                properties.Controls = GetDefaultControls();
            }
        }

        protected virtual List<object> GetDefaultControls()
        {
            var targets = context.Targets != null ? context.Targets.Values.SelectMany(t => t).ToList() : new List<object>();
            
            if (targets.Count == 0 && context.Event?.Card != null)
            {
                targets = new List<object> { context.Event.Card };
            }
            
            return new List<object>
            {
                new
                {
                    type = "targeting",
                    source = ((EffectSource)context.Source).GetShortSummary(),
                    targets = targets.Select(target => 
                        target is BaseCard card ? card.GetShortSummaryForControls(choosingPlayer) : target).ToList()
                }
            };
        }

        protected virtual void SavePreviouslySelectedCards()
        {
            previouslySelectedCards = choosingPlayer.SelectedCards.ToList();
            choosingPlayer.ClearSelectedCards();
            choosingPlayer.SetSelectedCards(selectedCards);
        }

        public override bool Continue()
        {
            if (!IsComplete())
            {
                HighlightSelectableCards();
            }

            return base.Continue();
        }

        protected virtual void HighlightSelectableCards()
        {
            var selectableCards = selector.FindPossibleCards(context).Where(card => CheckCardCondition(card)).ToList();
            choosingPlayer.SetSelectableCards(selectableCards);
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer;
        }

        public override object ActivePrompt(Player player)
        {
            var buttons = new List<object>(properties.Buttons);
            
            if (!selector.AutomaticFireOnSelect() && selector.HasEnoughSelected(selectedCards) || selector.Optional)
            {
                if (!buttons.Any(b => GetButtonArg(b) == "done"))
                {
                    buttons.Insert(0, new { text = "Done", arg = "done" });
                }
            }
            
            if (Game.ManualMode && !buttons.Any(b => GetButtonArg(b) == "cancel"))
            {
                buttons.Add(new { text = "Cancel Prompt", arg = "cancel" });
            }

            return new
            {
                selectCard = properties.SelectCard,
                selectRing = true,
                selectOrder = properties.Ordered,
                menuTitle = properties.ActivePromptTitle ?? selector.DefaultActivePromptTitle(),
                buttons = buttons,
                promptTitle = ((EffectSource)properties.Source)?.Name,
                controls = properties.Controls
            };
        }

        private string GetButtonArg(object button)
        {
            var buttonType = button.GetType();
            var argProperty = buttonType.GetProperty("arg");
            return argProperty?.GetValue(button)?.ToString();
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = properties.WaitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player != choosingPlayer)
            {
                return false;
            }

            if (!CheckCardCondition(card))
            {
                return false;
            }

            if (!SelectCard(card))
            {
                return false;
            }

            if (selector.AutomaticFireOnSelect() && selector.HasReachedLimit(selectedCards))
            {
                FireOnSelect();
            }

            return true;
        }

        protected virtual bool CheckCardCondition(BaseCard card)
        {
            if (onlyMustSelectMayBeChosen && !properties.MustSelect.Contains(card))
            {
                return false;
            }
            else if (selectedCards.Contains(card))
            {
                return true;
            }

            return selector.CanTarget(card, context, choosingPlayer) && !selector.WouldExceedLimit(selectedCards, card);
        }

        protected virtual bool SelectCard(BaseCard card)
        {
            if (selector.HasReachedLimit(selectedCards) && !selectedCards.Contains(card))
            {
                return false;
            }
            else if (cannotUnselectMustSelect && properties.MustSelect.Contains(card))
            {
                return false;
            }

            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
            }
            else
            {
                selectedCards = selectedCards.Where(c => c != card).ToList();
            }
            
            choosingPlayer.SetSelectedCards(selectedCards);

            properties.OnCardToggle?.Invoke(choosingPlayer, card);

            return true;
        }

        protected virtual bool FireOnSelect()
        {
            var cardParam = selector.FormatSelectParam(selectedCards);
            if (properties.OnSelect(choosingPlayer, cardParam))
            {
                Complete();
                return true;
            }
            ClearSelection();
            return false;
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "cancel")
            {
                properties.OnCancel(player);
                Complete();
                return true;
            }
            else if (arg == "done" && selector.HasEnoughSelected(selectedCards))
            {
                return FireOnSelect();
            }
            else if (properties.OnMenuCommand(player, arg))
            {
                Complete();
                return true;
            }
            return false;
        }

        public override void Complete()
        {
            ClearSelection();
            base.Complete();
        }

        protected virtual void ClearSelection()
        {
            selectedCards = new List<BaseCard>();
            choosingPlayer.ClearSelectedCards();
            choosingPlayer.ClearSelectableCards();
            choosingPlayer.ClearSelectableRings();

            // Restore previous selections.
            choosingPlayer.SetSelectedCards(previouslySelectedCards);
        }
    }
}
