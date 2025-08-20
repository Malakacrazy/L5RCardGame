using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SetupProvincesPrompt : AllPlayerPrompt
    {
        private Dictionary<string, ProvinceCard> strongholdProvince;
        private Dictionary<string, bool> clickedDone;
        private Dictionary<string, List<BaseCard>> selectedCards;
        private Dictionary<string, List<BaseCard>> selectableCards;

        public SetupProvincesPrompt(Game game) : base(game)
        {
            strongholdProvince = new Dictionary<string, ProvinceCard>();
            clickedDone = new Dictionary<string, bool>();
            selectedCards = new Dictionary<string, List<BaseCard>>();
            selectableCards = new Dictionary<string, List<BaseCard>>();

            foreach (var player in game.GetPlayers())
            {
                selectedCards[player.Uuid] = new List<BaseCard>();
                selectableCards[player.Uuid] = player.ProvinceDeck.ToList();
            }
        }

        public override bool CompletionCondition(Player player)
        {
            return clickedDone.ContainsKey(player.Uuid) && clickedDone[player.Uuid];
        }

        public override bool Continue()
        {
            if (!IsComplete())
            {
                HighlightSelectableCards();
            }

            return base.Continue();
        }

        private void HighlightSelectableCards()
        {
            foreach (var player in Game.GetPlayers())
            {
                player.SetSelectableCards(selectableCards[player.Uuid]);
            }
        }

        public override object ActivePrompt(Player player)
        {
            string menuTitle = "Choose province order, or press Done to place them at random";
            if (!strongholdProvince.ContainsKey(player.Uuid) || strongholdProvince[player.Uuid] == null)
            {
                menuTitle = "Select stronghold province";
            }

            var buttons = new List<object>();
            if (strongholdProvince.ContainsKey(player.Uuid) && strongholdProvince[player.Uuid] != null)
            {
                buttons.Add(new { text = "Done", arg = "done" });
                buttons.Add(new { text = "Change stronghold province", arg = "change" });
            }

            return new
            {
                selectCard = true,
                selectRing = true,
                selectOrder = strongholdProvince.ContainsKey(player.Uuid) && strongholdProvince[player.Uuid] != null,
                menuTitle = menuTitle,
                buttons = buttons,
                promptTitle = "Place Provinces"
            };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player == null || !ActiveCondition(player) || card == null)
            {
                return false;
            }
            else if (!selectableCards[player.Uuid].Contains(card))
            {
                return false;
            }
            else if (!strongholdProvince.ContainsKey(player.Uuid) || strongholdProvince[player.Uuid] == null)
            {
                var provinceCard = card as ProvinceCard;
                if (provinceCard == null || provinceCard.CannotBeStrongholdProvince())
                {
                    return false;
                }
                strongholdProvince[player.Uuid] = provinceCard;
                provinceCard.InConflict = true;
                selectableCards[player.Uuid] = selectableCards[player.Uuid].Where(c => c != card).ToList();
                return true;
            }

            if (!selectedCards[player.Uuid].Contains(card))
            {
                selectedCards[player.Uuid].Add(card);
            }
            else
            {
                selectedCards[player.Uuid] = selectedCards[player.Uuid].Where(c => c != card).ToList();
            }
            player.SetSelectedCards(selectedCards[player.Uuid]);
            return true;
        }

        public override object WaitingPrompt()
        {
            return new
            {
                menuTitle = "Waiting for opponent to finish selecting provinces"
            };
        }

        public override bool MenuCommand(Player player, string arg)
        {
            if (arg == "change" || !strongholdProvince.ContainsKey(player.Uuid) || strongholdProvince[player.Uuid] == null)
            {
                if (strongholdProvince.ContainsKey(player.Uuid) && strongholdProvince[player.Uuid] != null)
                {
                    strongholdProvince[player.Uuid].InConflict = false;
                    strongholdProvince[player.Uuid] = null;
                }
                selectableCards[player.Uuid] = player.ProvinceDeck.ToList();
                selectedCards[player.Uuid] = new List<BaseCard>();
                return true;
            }
            else if (arg != "done")
            {
                return false;
            }

            strongholdProvince[player.Uuid].InConflict = false;
            if (!strongholdProvince[player.Uuid].StartsGameFaceup())
            {
                strongholdProvince[player.Uuid].Facedown = true;
            }
            clickedDone[player.Uuid] = true;
            Game.AddMessage("{0} has placed their provinces", player);
            player.MoveCard(strongholdProvince[player.Uuid], Locations.StrongholdProvince);

            var provinces = selectedCards[player.Uuid].Concat(selectableCards[player.Uuid].OrderBy(x => Random.value)).ToList();
            for (int i = 1; i < 5; i++)
            {
                var provinceCard = provinces[i - 1] as ProvinceCard;
                if (provinceCard != null && !provinceCard.StartsGameFaceup())
                {
                    provinceCard.Facedown = true;
                }
                player.MoveCard(provinces[i - 1], $"province {i}");
            }
            player.HideProvinceDeck = true;

            return true;
        }
    }
}
