using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class MulliganDynastyPrompt : AllPlayerPrompt
    {
        protected Dictionary<string, List<BaseCard>> selectedCards;
        protected Dictionary<string, List<BaseCard>> selectableCards;

        public MulliganDynastyPrompt(Game game) : base(game)
        {
            selectedCards = new Dictionary<string, List<BaseCard>>();
            selectableCards = new Dictionary<string, List<BaseCard>>();
            
            foreach (var player in game.GetPlayers())
            {
                selectedCards[player.Name] = new List<BaseCard>();
            }
        }

        public override bool CompletionCondition(Player player)
        {
            return player.TakenDynastyMulligan;
        }

        public override bool Continue()
        {
            if (!IsComplete())
            {
                HighlightSelectableCards();
            }

            return base.Continue();
        }

        protected virtual void HighlightSelectableCards()
        {
            foreach (var player in Game.GetPlayers())
            {
                if (!selectableCards.ContainsKey(player.Name))
                {
                    var locations = new[] { Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree, Locations.ProvinceFour };
                    selectableCards[player.Name] = locations
                        .Select(location => player.GetDynastyCardInProvince(location))
                        .Where(card => card != null)
                        .ToList();
                }
                player.SetSelectableCards(selectableCards[player.Name]);
            }
        }

        public override object ActivePrompt(Player player)
        {
            return new
            {
                selectCard = true,
                selectRing = true,
                menuTitle = "Select dynasty cards to mulligan",
                buttons = new[] { new { text = "Done", arg = "done" } },
                promptTitle = "Dynasty Mulligan"
            };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player == null || !ActiveCondition(player) || card == null)
            {
                return false;
            }
            if (!CardCondition(card))
            {
                return false;
            }

            if (!selectedCards[player.Name].Contains(card))
            {
                selectedCards[player.Name].Add(card);
            }
            else
            {
                selectedCards[player.Name] = selectedCards[player.Name].Where(c => c != card).ToList();
            }
            player.SetSelectedCards(selectedCards[player.Name]);
            return true;
        }

        private bool CardCondition(BaseCard card)
        {
            return card.IsDynasty && card.IsInProvince();
        }

        public override object WaitingPrompt()
        {
            return new
            {
                menuTitle = "Waiting for opponent to mulligan dynasty cards"
            };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "done")
            {
                if (selectedCards[player.Name].Count > 0)
                {
                    foreach (var card in selectedCards[player.Name])
                    {
                        if (player.DynastyDeck.Size() > 0)
                        {
                            player.MoveCard(player.DynastyDeck.First(), card.Location);
                        }
                    }
                    foreach (var card in selectedCards[player.Name])
                    {
                        var location = card.Location;
                        player.MoveCard(card, "dynasty deck bottom");
                        player.ReplaceDynastyCard(location);
                    }
                    player.ShuffleDynastyDeck();
                    Game.AddMessage("{0} has mulliganed {1} cards from the dynasty deck", player, selectedCards[player.Name].Count);
                }
                else
                {
                    Game.AddMessage("{0} has kept all dynasty cards", player);
                }
                player.ClearSelectedCards();
                player.ClearSelectableCards();
                player.TakenDynastyMulligan = true;
                return true;
            }
            return false;
        }
    }
}
