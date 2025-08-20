using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GameWonPrompt : AllPlayerPrompt
    {
        private Player winner;
        private Dictionary<string, bool> clickedButton;

        public GameWonPrompt(Game game, Player winner) : base(game)
        {
            this.winner = winner;
            clickedButton = new Dictionary<string, bool>();
        }

        public override bool CompletionCondition(Player player)
        {
            return clickedButton.ContainsKey(player.Name) && clickedButton[player.Name];
        }

        public override object ActivePrompt(Player player)
        {
            return new
            {
                promptTitle = "Game Won",
                menuTitle = winner.Name + " has won the game!",
                buttons = new[] { new { text = "Continue Playing" } }
            };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent to choose to continue" };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            Game.AddMessage("{0} wants to continue", player);
            clickedButton[player.Name] = true;
            return true;
        }
    }
}
