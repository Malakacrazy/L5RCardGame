using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class MulliganConflictPrompt : MulliganDynastyPrompt
    {
        public MulliganConflictPrompt(Game game) : base(game)
        {
        }

        public override bool CompletionCondition(Player player)
        {
            return player.TakenConflictMulligan;
        }

        public override object ActivePrompt(Player player)
        {
            return new
            {
                selectCard = true,
                selectRing = true,
                menuTitle = "Select conflict cards to mulligan",
                buttons = new[] { new { text = "Done", arg = "done" } },
                promptTitle = "Conflict Mulligan"
            };
        }

        protected override void HighlightSelectableCards()
        {
            foreach (var player in Game.GetPlayers())
            {
                if (!selectableCards.ContainsKey(player.Name))
                {
                    selectableCards[player.Name] = player.Hand.ToList();
                }
                player.SetSelectableCards(selectableCards[player.Name]);
            }
        }

        protected override bool CardCondition(BaseCard card)
        {
            return card.Location == Locations.Hand;
        }

        public override object WaitingPrompt()
        {
            return new { menuTitle = "Waiting for opponent to mulligan conflict cards" };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "done")
            {
                if (selectedCards[player.Name].Count > 0)
                {
                    foreach (var card in selectedCards[player.Name])
                    {
                        player.MoveCard(card, "conflict deck bottom");
                    }
                    player.DrawCardsToHand(selectedCards[player.Name].Count);
                    player.ShuffleConflictDeck();
                    Game.AddMessage("{0} has mulliganed {1} cards from the conflict deck", player, selectedCards[player.Name].Count);
                }
                else
                {
                    Game.AddMessage("{0} has kept all conflict cards", player);
                }

                var locations = new[] { Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree, Locations.ProvinceFour };
                foreach (var location in locations)
                {
                    var card = player.GetDynastyCardInProvince(location);
                    if (card != null)
                    {
                        card.Facedown = true;
                    }
                }

                player.ClearSelectedCards();
                player.ClearSelectableCards();
                player.TakenConflictMulligan = true;
                return true;
            }
            return false;
        }
    }
}
