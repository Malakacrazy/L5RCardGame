using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class FirstPlayerChoicePromptProperties
    {
        public string ActivePromptTitle { get; set; } = "Choose who goes first";
        public string WaitingPromptTitle { get; set; } = "Waiting for opponent to choose first player";
        public Action<Player, Player> OnPlayerChosen { get; set; }
        public bool AllowSelfChoice { get; set; } = true;
        public bool RandomIfNoChoice { get; set; } = true;
    }

    /// <summary>
    /// Prompt that allows a player to choose who goes first in the game.
    /// Typically used during game setup.
    /// </summary>
    public class FirstPlayerChoicePrompt : UiPrompt
    {
        private Player choosingPlayer;
        private FirstPlayerChoicePromptProperties properties;
        private bool choiceMade;

        public FirstPlayerChoicePrompt(Game game, Player choosingPlayer, FirstPlayerChoicePromptProperties properties = null) : base(game)
        {
            this.choosingPlayer = choosingPlayer;
            this.properties = properties ?? new FirstPlayerChoicePromptProperties();
            choiceMade = false;
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer && !choiceMade;
        }

        public override bool IsComplete()
        {
            return choiceMade;
        }

        public override object ActivePrompt(Player player)
        {
            var buttons = new List<object>();
            
            if (properties.AllowSelfChoice)
            {
                buttons.Add(new { text = "I go first", arg = "self" });
            }
            
            if (choosingPlayer.Opponent != null)
            {
                buttons.Add(new { text = $"{choosingPlayer.Opponent.Name} goes first", arg = "opponent" });
            }
            
            if (properties.RandomIfNoChoice)
            {
                buttons.Add(new { text = "Random", arg = "random" });
            }

            return new
            {
                promptTitle = "First Player Choice",
                menuTitle = properties.ActivePromptTitle,
                buttons = buttons
            };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = properties.WaitingPromptTitle };
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (player != choosingPlayer || choiceMade)
            {
                return false;
            }

            Player firstPlayer = null;
            
            switch (arg)
            {
                case "self":
                    firstPlayer = choosingPlayer;
                    Game.AddMessage("{0} chooses to go first", choosingPlayer);
                    break;
                    
                case "opponent":
                    firstPlayer = choosingPlayer.Opponent;
                    Game.AddMessage("{0} chooses {1} to go first", choosingPlayer, choosingPlayer.Opponent);
                    break;
                    
                case "random":
                    var players = Game.GetPlayers();
                    firstPlayer = players[UnityEngine.Random.Range(0, players.Count)];
                    Game.AddMessage("{0} chooses random - {1} will go first", choosingPlayer, firstPlayer);
                    break;
                    
                default:
                    return false;
            }

            if (firstPlayer != null)
            {
                Game.SetFirstPlayer(firstPlayer);
                properties.OnPlayerChosen?.Invoke(choosingPlayer, firstPlayer);
                choiceMade = true;
                Complete();
                return true;
            }

            return false;
        }
    }
}
