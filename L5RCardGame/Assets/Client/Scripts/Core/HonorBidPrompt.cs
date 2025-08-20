using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class HonorBidPrompt : AllPlayerPrompt
    {
        private string menuTitle;
        private Action<HonorBidPrompt> costHandler;
        private Dictionary<string, List<string>> prohibitedBids;
        private object duel;
        private Dictionary<string, int> bid;

        public HonorBidPrompt(Game game, string menuTitle = null, Action<HonorBidPrompt> costHandler = null, 
                             Dictionary<string, List<string>> prohibitedBids = null, object duel = null) : base(game)
        {
            this.menuTitle = menuTitle ?? "Choose a bid";
            this.costHandler = costHandler;
            this.prohibitedBids = prohibitedBids ?? new Dictionary<string, List<string>>();
            this.duel = duel;
            bid = new Dictionary<string, int>();
        }

        public override bool ActiveCondition(Player player)
        {
            return !bid.ContainsKey(player.Uuid) || bid[player.Uuid] == 0;
        }

        public override bool CompletionCondition(Player player)
        {
            return bid.ContainsKey(player.Uuid) && bid[player.Uuid] > 0;
        }

        public override bool Continue()
        {
            bool completed = base.Continue();

            if (completed)
            {
                Game.RaiseEvent(EventNames.OnHonorDialsRevealed, new { duel = this.duel }, () =>
                {
                    foreach (var player in Game.GetPlayers())
                    {
                        player.HonorBidModifier = 0;
                        Game.Actions.SetHonorDial(new { value = bid[player.Uuid] }).Resolve(player, Game.GetFrameworkContext());
                    }
                });

                if (costHandler != null)
                {
                    Game.QueueSimpleStep(() => costHandler(this));
                }
                else
                {
                    Game.QueueSimpleStep(() => TransferHonorAfterBid());
                }
            }

            return completed;
        }

        public void TransferHonorAfterBid(AbilityContext context = null)
        {
            if (context == null)
            {
                context = Game.GetFrameworkContext();
            }

            var firstPlayer = Game.GetFirstPlayer();
            if (firstPlayer.Opponent == null)
            {
                return;
            }

            int difference = firstPlayer.HonorBid - firstPlayer.Opponent.HonorBid;
            if (difference > 0)
            {
                Game.AddMessage("{0} gives {1} {2} honor", firstPlayer, firstPlayer.Opponent, difference);
                GameActions.TakeHonor(new { amount = difference, afterBid = true }).Resolve(firstPlayer, context);
            }
            else if (difference < 0)
            {
                Game.AddMessage("{0} gives {1} {2} honor", firstPlayer.Opponent, firstPlayer, -difference);
                GameActions.TakeHonor(new { amount = -difference, afterBid = true }).Resolve(firstPlayer.Opponent, context);
            }
        }

        public override object ActivePrompt(Player player)
        {
            var playerProhibitedBids = prohibitedBids.ContainsKey(player.Uuid) ? prohibitedBids[player.Uuid] : new List<string>();
            var allBids = new[] { "1", "2", "3", "4", "5" };
            var buttons = allBids.Where(num => !playerProhibitedBids.Contains(num))
                                 .Select(num => new { text = num, arg = num })
                                 .ToArray();

            return new
            {
                promptTitle = "Honor Bid",
                menuTitle = this.menuTitle,
                buttons = buttons
            };
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent to choose a bid." };
        }

        public override bool MenuCommand(Player player, string bidValue)
        {
            Game.AddMessage("{0} has chosen a bid.", player);
            bid[player.Uuid] = int.Parse(bidValue);
            return true;
        }
    }
}
