using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectRingProperties
    {
        public List<object> Buttons { get; set; } = new List<object>();
        public List<object> Controls { get; set; }
        public string ActivePromptTitle { get; set; }
        public string WaitingPromptTitle { get; set; }
        public Func<Ring, AbilityContext, bool> RingCondition { get; set; } = (ring, context) => true;
        public Func<Player, Ring, bool> OnSelect { get; set; } = (player, ring) => true;
        public Func<Player, string, bool> OnMenuCommand { get; set; } = (player, arg) => true;
        public Func<Player, bool> OnCancel { get; set; } = player => true;
        public object Source { get; set; }
        public AbilityContext Context { get; set; }
        public bool Optional { get; set; }
        public bool Ordered { get; set; }
    }

    /// <summary>
    /// General purpose prompt that asks the user to select a ring.
    /// 
    /// The properties option object has the following properties:
    /// additionalButtons  - array of additional buttons for the prompt.
    /// activePromptTitle  - the title that should be used in the prompt for the
    ///                      choosing player.
    /// waitingPromptTitle - the title that should be used in the prompt for the
    ///                      opponent players.
    /// ringCondition      - a function that takes a ring and should return a boolean
    ///                      on whether that ring is elligible to be selected.
    /// onSelect           - a callback that is called as soon as an elligible ring
    ///                      is clicked. If the callback does not return true, the
    ///                      prompt is not marked as complete.
    /// onMenuCommand      - a callback that is called when one of the additional
    ///                      buttons is clicked.
    /// onCancel           - a callback that is called when the player clicks the
    ///                      done button without selecting any rings.
    /// source             - what is at the origin of the user prompt, usually a card;
    ///                      used to provide a default waitingPromptTitle, if missing
    /// </summary>
    public class SelectRingPrompt : UiPrompt
    {
        protected Player choosingPlayer;
        protected SelectRingProperties properties;
        protected AbilityContext context;
        protected Ring selectedRing;

        public SelectRingPrompt(Game game, Player choosingPlayer, SelectRingProperties properties) : base(game)
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
            else if (properties.Source == null)
            {
                properties.Source = new EffectSource(game);
            }

            this.properties = properties;
            context = properties.Context ?? new AbilityContext(game, choosingPlayer, (EffectSource)properties.Source);
            ApplyDefaultProperties();
            selectedRing = null;
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
            if (properties.Context == null)
            {
                return new List<object>();
            }
            
            var targets = properties.Context.Targets?.Values
                .Select(target => target is BaseCard card ? card.GetShortSummaryForControls(choosingPlayer) : target)
                .ToList() ?? new List<object>();
                
            if (targets.Count == 0 && properties.Context.Event?.Card != null)
            {
                targets = new List<object> { properties.Context.Event.Card.GetShortSummaryForControls(choosingPlayer) };
            }
            
            return new List<object>
            {
                new
                {
                    type = "targeting",
                    source = properties.Context.Source.GetShortSummary(),
                    targets = targets
                }
            };
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer;
        }

        public override bool Continue()
        {
            if (!IsComplete())
            {
                HighlightSelectableRings();
            }

            return base.Continue();
        }

        protected virtual void HighlightSelectableRings()
        {
            var selectableRings = Game.Rings.Values
                .Where(ring => properties.RingCondition(ring, context))
                .ToList();
            choosingPlayer.SetSelectableRings(selectableRings);
        }

        public override object ActivePrompt(Player player)
        {
            var buttons = new List<object>(properties.Buttons);
            
            if (properties.Optional)
            {
                buttons.Add(new { text = "Done", arg = "done" });
            }
            
            if (Game.ManualMode && !buttons.Any(b => GetButtonArg(b) == "cancel"))
            {
                buttons.Add(new { text = "Cancel Prompt", arg = "cancel" });
            }

            return new
            {
                source = properties.Source,
                selectCard = true,
                selectRing = true,
                selectOrder = properties.Ordered,
                menuTitle = properties.ActivePromptTitle ?? DefaultActivePromptTitle(),
                buttons = buttons,
                promptTitle = ((EffectSource)properties.Source)?.Name
            };
        }

        protected virtual string DefaultActivePromptTitle()
        {
            return "Choose a ring";
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

        public virtual bool OnRingClicked(Player player, Ring ring)
        {
            if (player != choosingPlayer)
            {
                return false;
            }

            if (!properties.RingCondition(ring, context))
            {
                return true;
            }

            if (properties.OnSelect(player, ring))
            {
                Complete();
            }
            
            return true;
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "cancel")
            {
                properties.OnCancel(player);
                Complete();
                return true;
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
            choosingPlayer.ClearSelectableRings();
            base.Complete();
        }
    }
}
