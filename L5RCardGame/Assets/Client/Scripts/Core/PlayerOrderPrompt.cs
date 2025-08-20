using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a UI Prompt that prompts each player individually in first-player
    /// order. Inheritors should call CompletePlayer() when the prompt for the
    /// current player has been completed. Overriding SkipCondition will exclude
    /// any matching players from the prompt.
    /// </summary>
    public class PlayerOrderPrompt : UiPrompt
    {
        protected List<Player> players;

        public Player CurrentPlayer
        {
            get
            {
                LazyFetchPlayers();
                return players.Count > 0 ? players[0] : null;
            }
        }

        public PlayerOrderPrompt(Game game) : base(game)
        {
        }

        protected virtual void LazyFetchPlayers()
        {
            if (players == null)
            {
                players = Game.GetPlayersInFirstPlayerOrder().ToList();
            }
        }

        protected virtual void SkipPlayers()
        {
            LazyFetchPlayers();
            players = players.Where(p => !SkipCondition(p)).ToList();
        }

        protected virtual bool SkipCondition(Player player)
        {
            return false;
        }

        protected virtual void CompletePlayer()
        {
            LazyFetchPlayers();
            if (players.Count > 0)
            {
                players.RemoveAt(0);
            }
        }

        protected virtual void SetPlayers(List<Player> players)
        {
            this.players = players;
        }

        public override bool IsComplete()
        {
            LazyFetchPlayers();
            return players.Count == 0;
        }

        public override bool ActiveCondition(Player player)
        {
            return player == CurrentPlayer;
        }

        public override bool Continue()
        {
            SkipPlayers();
            return base.Continue();
        }
    }
}
