using UnityEngine;

namespace L5RGame
{
    public class EndRoundPrompt : PlayerOrderPrompt
    {
        public EndRoundPrompt(Game game) : base(game)
        {
        }

        public override object ActivePrompt(Player player)
        {
            return new
            {
                menuTitle = "",
                buttons = new[] { new { text = "End Round" } }
            };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent to end the round" };
        }

        public override bool OnMenuCommand(Player player, string arg, string uuid, string method)
        {
            if (player != CurrentPlayer)
            {
                return false;
            }

            CompletePlayer();
            return true;
        }
    }
}
