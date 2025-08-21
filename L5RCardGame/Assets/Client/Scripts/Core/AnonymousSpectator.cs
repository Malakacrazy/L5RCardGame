using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents an anonymous spectator viewing the game
    /// </summary>
    public class AnonymousSpectator
    {
        public string Name { get; private set; }
        public string EmailHash { get; private set; }
        public List<object> Buttons { get; private set; }
        public string MenuTitle { get; private set; }

        public AnonymousSpectator()
        {
            Name = "Anonymous";
            EmailHash = "";
            Buttons = new List<object>();
            MenuTitle = "Spectator mode";
        }

        /// <summary>
        /// Get card selection state for spectator
        /// </summary>
        public object GetCardSelectionState()
        {
            return new { };
        }

        /// <summary>
        /// Get ring selection state for spectator
        /// </summary>
        public object GetRingSelectionState()
        {
            return new { };
        }

        /// <summary>
        /// Get spectator's current prompt state
        /// </summary>
        public object GetPromptState()
        {
            return new
            {
                menuTitle = MenuTitle,
                buttons = Buttons,
                selectCard = false,
                selectRing = false,
                promptTitle = "Spectating"
            };
        }

        /// <summary>
        /// Update spectator's menu title
        /// </summary>
        public void SetMenuTitle(string title)
        {
            MenuTitle = title ?? "Spectator mode";
        }

        /// <summary>
        /// Add a button to spectator interface
        /// </summary>
        public void AddButton(object button)
        {
            Buttons.Add(button);
        }

        /// <summary>
        /// Clear all buttons
        /// </summary>
        public void ClearButtons()
        {
            Buttons.Clear();
        }

        /// <summary>
        /// Check if spectator can perform an action
        /// </summary>
        public bool CanPerformAction(string action)
        {
            // Spectators generally cannot perform game actions
            return false;
        }

        /// <summary>
        /// Get spectator information for display
        /// </summary>
        public object GetSpectatorInfo()
        {
            return new
            {
                name = Name,
                emailHash = EmailHash,
                isSpectator = true,
                canInteract = false
            };
        }
    }
}
