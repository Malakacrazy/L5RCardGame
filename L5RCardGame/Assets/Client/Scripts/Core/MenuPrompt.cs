using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class MenuPromptProperties
    {
        public object ActivePrompt { get; set; }
        public string WaitingPromptTitle { get; set; }
        public object Source { get; set; }
        public string PromptTitle { get; set; }
    }

    /// <summary>
    /// General purpose menu prompt. By specifying a context object, the buttons in
    /// the active prompt can call the corresponding method on the context object.
    /// Methods on the contact object should return true in order to complete the
    /// prompt.
    /// 
    /// The properties option object may contain the following:
    /// activePrompt       - the full prompt to display for the prompted player.
    /// waitingPromptTitle - the title to display for opponents.
    /// source             - what is at the origin of the user prompt, usually a card;
    ///                      used to provide a default waitingPromptTitle, if missing
    /// </summary>
    public class MenuPrompt : UiPrompt
    {
        protected Player player;
        protected object context;
        protected MenuPromptProperties properties;

        public MenuPrompt(Game game, Player player, object context, MenuPromptProperties properties) : base(game)
        {
            this.player = player;
            this.context = context;
            
            if (properties.Source != null && string.IsNullOrEmpty(properties.WaitingPromptTitle))
            {
                var sourceName = GetSourceName(properties.Source);
                properties.WaitingPromptTitle = $"Waiting for opponent to use {sourceName}";
            }
            
            this.properties = properties;
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
            var promptTitle = properties.PromptTitle ?? GetSourceName(properties.Source);
            
            // Merge the promptTitle with the activePrompt object
            var activePrompt = properties.ActivePrompt;
            if (activePrompt != null)
            {
                var activePromptType = activePrompt.GetType();
                var existingProperties = activePromptType.GetProperties()
                    .ToDictionary(p => p.Name, p => p.GetValue(activePrompt));
                
                existingProperties["promptTitle"] = promptTitle;
                
                return existingProperties.Aggregate(new Dictionary<string, object>(), 
                    (dict, kvp) => { dict[kvp.Key] = kvp.Value; return dict; });
            }
            
            return new { promptTitle = promptTitle };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = properties.WaitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool MenuCommand(Player player, string arg, string method)
        {
            if (context == null)
            {
                return false;
            }

            // Use reflection to find and invoke the method
            var contextType = context.GetType();
            var methodInfo = contextType.GetMethod(method);
            
            if (methodInfo == null)
            {
                return false;
            }

            try
            {
                var parameters = new object[] { player, arg, properties.Source };
                var result = methodInfo.Invoke(context, parameters);
                
                if (result is bool success && success)
                {
                    Complete();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking method {method}: {ex.Message}");
                return false;
            }
        }

        public bool HasMethodButton(string method)
        {
            if (properties.ActivePrompt == null)
                return false;
                
            var activePromptType = properties.ActivePrompt.GetType();
            var buttonsProperty = activePromptType.GetProperty("buttons");
            
            if (buttonsProperty?.GetValue(properties.ActivePrompt) is IEnumerable<object> buttons)
            {
                return buttons.Any(button =>
                {
                    var buttonType = button.GetType();
                    var methodProperty = buttonType.GetProperty("method");
                    return methodProperty?.GetValue(button)?.ToString() == method;
                });
            }
            
            return false;
        }
    }
}
