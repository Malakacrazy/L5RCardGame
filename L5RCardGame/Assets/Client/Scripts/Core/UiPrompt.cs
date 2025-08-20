using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class UiPrompt : BaseStep
    {
        protected bool completed;
        protected string uuid;

        public UiPrompt(Game game) : base(game)
        {
            completed = false;
            uuid = System.Guid.NewGuid().ToString();
        }

        public override bool IsComplete()
        {
            return completed;
        }

        public virtual void Complete()
        {
            completed = true;
            Game.ResetClocks();
        }

        protected virtual void SetPrompt()
        {
            foreach (var player in Game.GetPlayers())
            {
                if (ActiveCondition(player))
                {
                    player.SetPrompt(AddDefaultCommandToButtons(ActivePrompt(player)));
                    player.StartClock();
                }
                else
                {
                    player.SetPrompt(WaitingPrompt());
                    player.ResetClock();
                }
            }
        }

        public virtual bool ActiveCondition(Player player)
        {
            return true;
        }

        public virtual object ActivePrompt(Player player)
        {
            return null;
        }

        protected virtual object AddDefaultCommandToButtons(object original)
        {
            if (original == null) return null;

            // Create a copy and add default properties to buttons
            var originalType = original.GetType();
            var properties = originalType.GetProperties().ToDictionary(p => p.Name, p => p.GetValue(original));

            if (properties.ContainsKey("buttons") && properties["buttons"] is IEnumerable<object> buttons)
            {
                var modifiedButtons = buttons.Select(button =>
                {
                    var buttonType = button.GetType();
                    var buttonProps = buttonType.GetProperties().ToDictionary(p => p.Name, p => p.GetValue(button));
                    
                    if (!buttonProps.ContainsKey("command"))
                        buttonProps["command"] = "menuButton";
                    buttonProps["uuid"] = uuid;
                    
                    return buttonProps.Aggregate(new Dictionary<string, object>(), 
                        (dict, kvp) => { dict[kvp.Key] = kvp.Value; return dict; });
                }).ToList();
                
                properties["buttons"] = modifiedButtons;
            }

            if (properties.ContainsKey("controls") && properties["controls"] is IEnumerable<object> controls)
            {
                var modifiedControls = controls.Select(control =>
                {
                    var controlType = control.GetType();
                    var controlProps = controlType.GetProperties().ToDictionary(p => p.Name, p => p.GetValue(control));
                    controlProps["uuid"] = uuid;
                    
                    return controlProps.Aggregate(new Dictionary<string, object>(), 
                        (dict, kvp) => { dict[kvp.Key] = kvp.Value; return dict; });
                }).ToList();
                
                properties["controls"] = modifiedControls;
            }

            return properties.Aggregate(new Dictionary<string, object>(), 
                (dict, kvp) => { dict[kvp.Key] = kvp.Value; return dict; });
        }

        public virtual object WaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent" };
        }

        public override bool Continue()
        {
            var completed = IsComplete();

            if (completed)
            {
                ClearPrompts();
            }
            else
            {
                SetPrompt();
            }

            return completed;
        }

        protected virtual void ClearPrompts()
        {
            foreach (var player in Game.GetPlayers())
            {
                player.CancelPrompt();
            }
        }

        public virtual bool OnMenuCommand(Player player, string arg, string uuid, string method)
        {
            if (!ActiveCondition(player) || uuid != this.uuid)
            {
                return false;
            }

            return MenuCommand(player, arg, method);
        }

        public virtual bool MenuCommand(Player player, string arg, string method)
        {
            return true;
        }

        public virtual bool OnCardClicked(Player player, BaseCard card)
        {
            return false;
        }
    }
}
