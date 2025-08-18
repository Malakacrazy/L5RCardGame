using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a conflict between two players in Legend of the Five Rings.
    /// Handles all aspects of conflict resolution including skill calculation, 
    /// participant management, and winner determination.
    /// </summary>
    public class Conflict : GameObject
    {
        [Header("Conflict Participants")]
        public Player attackingPlayer;
        public Player defendingPlayer;
        public bool isSinglePlayer = false;

        [Header("Conflict Declaration")]
        public Ring declaredRing;
        public Ring ring;
        public string declaredType;
        public string forcedDeclaredType;
        public bool declarationComplete = false;
        public bool defendersChosen = false;

        [Header("Conflict State")]
        public BaseCard conflictProvince;
        public bool conflictPassed = false;
        public bool conflictTypeSwitched = false;
        public bool conflictUnopposed = false;
        public bool winnerGoesStraightToNextConflict = false;
        public bool winnerDetermined = false;

        [Header("Participants")]
        public List<BaseCard> attackers = new List<BaseCard>();
        public List<BaseCard> defenders = new List<BaseCard>();
        public int attackerSkill = 0;
        public int defenderSkill = 0;

        [Header("Conflict Resolution")]
        public Player winner;
        public Player loser;
        public int winnerSkill = 0;
        public int loserSkill = 0;
        public int skillDifference = 0;

        [Header("Cards Played")]
        public List<object> attackerCardsPlayed = new List<object>();
        public List<object> defenderCardsPlayed = new List<object>();

        /// <summary>
        /// Initialize the conflict with attacking and defending players
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="attacker">Attacking player</param>
        /// <param name="defender">Defending player (null for single player)</param>
        /// <param name="conflictRing">Ring being contested (null until declared)</param>
        /// <param name="province">Province being attacked (null until declared)</param>
        /// <param name="forcedType">Forced conflict type if any</param>
        public void Initialize(Game gameInstance, Player attacker, Player defender = null, 
                             Ring conflictRing = null, BaseCard province = null, string forcedType = null)
        {
            base.Initialize(gameInstance, "Conflict");
            
            attackingPlayer = attacker;
            isSinglePlayer = (defender == null);
            defendingPlayer = defender ?? CreateSinglePlayerDefender();
            forcedDeclaredType = forcedType;
            declaredRing = ring = conflictRing;
            conflictProvince = province;
            
            // Initialize collections
            attackers = new List<BaseCard>();
            defenders = new List<BaseCard>();
            attackerCardsPlayed = new List<object>();
            defenderCardsPlayed = new List<object>();
            
            Debug.Log($"⚔️ Conflict initialized: {attackingPlayer.name} vs {defendingPlayer.name}");
        }

        /// <summary>
        /// Current conflict type (military or political) from the contested ring
        /// </summary>
        public string ConflictType => ring?.conflictType ?? "";

        /// <summary>
        /// Current ring element being contested
        /// </summary>
        public string Element => ring?.element ?? "";

        /// <summary>
        /// All elements associated with this conflict
        /// </summary>
        public List<string> Elements => ring?.GetElements() ?? new List<string>();

        /// <summary>
        /// Number of elements to resolve (usually 1 + modifiers)
        /// </summary>
        public int ElementsToResolve => SumEffects(EffectNames.ModifyConflictElementsToResolve) + 1;

        /// <summary>
        /// Maximum number of defenders allowed (from effects)
        /// </summary>
        public int MaxAllowedDefenders
        {
            get
            {
                var effects = GetEffects(EffectNames.RestrictNumberOfDefenders);
                return effects.Count == 0 ? -1 : effects.Cast<int>().Min();
            }
        }

        /// <summary>
        /// Create dummy player for single player mode
        /// </summary>
        /// <returns>Dummy defending player</returns>
        private Player CreateSinglePlayerDefender()
        {
            var dummyGO = new GameObject("DummyPlayer");
            dummyGO.transform.SetParent(game.transform);
            var dummy = dummyGO.AddComponent<Player>();
            
            var dummyUser = new UserInfo
            {
                username = "Dummy Player",
                emailHash = "",
                lobbyId = ""
            };
            
            dummy.Initialize("dummy", dummyUser, false, game, new ClockSettings());
            dummy.Initialize(); // Initialize decks and game state
            
            return dummy;
        }

        /// <summary>
        /// Gets a summary of the current conflict state
        /// </summary>
        /// <returns>Conflict summary for UI display</returns>
        public ConflictSummary GetSummary()
        {
            var forcedUnopposedEffects = GetEffects(EffectNames.ForceConflictUnopposed);
            bool forcedUnopposed = forcedUnopposedEffects.Count > 0;
            
            return new ConflictSummary
            {
                attackingPlayerId = attackingPlayer.id,
                defendingPlayerId = defendingPlayer.id,
                attackerSkill = attackerSkill,
                defenderSkill = defenderSkill,
                type = ConflictType,
                elements = Elements,
                attackerWins = attackers.Count > 0 && attackerSkill >= defenderSkill,
                breaking = conflictProvince != null && 
                          (conflictProvince.GetStrength() - (attackerSkill - defenderSkill) <= 0),
                unopposed = !(defenders.Count > 0 && !forcedUnopposed),
                declarationComplete = declarationComplete,
                defendersChosen = defendersChosen,
                conflictRing = ring?.element ?? "",
                province = conflictProvince?.name ?? "",
                winnerDetermined = winnerDetermined,
                winner = winner?.name ?? "",
                skillDifference = skillDifference
            };
        }

        /// <summary>
        /// Mark the conflict declaration as complete
        /// </summary>
        /// <param name="complete">Whether declaration is complete</param>
        public void SetDeclarationComplete(bool complete)
        {
            declarationComplete = complete;
            if (complete)
            {
                Debug.Log($"⚔️ Conflict declaration complete: {ConflictType} conflict at {Element} ring");
            }
        }

        /// <summary>
        /// Mark that defenders have been chosen
        /// </summary>
        /// <param name="chosen">Whether defenders are chosen</param>
        public void SetDefendersChosen(bool chosen)
        {
            defendersChosen = chosen;
            if (chosen)
            {
                Debug.Log($"🛡️ Defenders chosen: {defenders.Count} defenders");
            }
        }

        /// <summary>
        /// Reset all cards and provinces for conflict participation
        /// </summary>
        public void ResetCards()
        {
            attackingPlayer.ResetForConflict();
            defendingPlayer.ResetForConflict();
            
            if (conflictProvince != null)
            {
                conflictProvince.inConflict = false;
            }
            
            // Reset all participants
            foreach (var attacker in attackers)
            {
                attacker.inConflict = false;
            }
            
            foreach (var defender in defenders)
            {
                defender.inConflict = false;
            }
        }

        /// <summary>
        /// Add multiple attackers to the conflict
        /// </summary>
        /// <param name="newAttackers">List of attacking characters</param>
        public void AddAttackers(List<BaseCard> newAttackers)
        {
            var validAttackers = newAttackers.Where(card => !IsAttacking(card)).ToList();
            if (validAttackers.Count > 0)
            {
                attackers.AddRange(validAttackers);
                MarkAsParticipating(validAttackers);
                
                Debug.Log($"⚔️ Added {validAttackers.Count} attackers to conflict");
            }
        }

        /// <summary>
        /// Add a single attacker to the conflict
        /// </summary>
        /// <param name="attacker">Attacking character</param>
        public void AddAttacker(BaseCard attacker)
        {
            if (!attackers.Contains(attacker))
            {
                attackers.Add(attacker);
                MarkAsParticipating(new List<BaseCard> { attacker });
                
                Debug.Log($"⚔️ {attacker.name} joins as attacker");
            }
        }

        /// <summary>
        /// Add multiple defenders to the conflict
        /// </summary>
        /// <param name="newDefenders">List of defending characters</param>
        public void AddDefenders(List<BaseCard> newDefenders)
        {
            var validDefenders = newDefenders.Where(card => !IsDefending(card)).ToList();
            if (validDefenders.Count > 0)
            {
                defenders.AddRange(validDefenders);
                MarkAsParticipating(validDefenders);
                
                Debug.Log($"🛡️ Added {validDefenders.Count} defenders to conflict");
            }
        }

        /// <summary>
        /// Add a single defender to the conflict
        /// </summary>
        /// <param name="defender">Defending character</param>
        public void AddDefender(BaseCard defender)
        {
            if (!defenders.Contains(defender))
            {
                defenders.Add(defender);
                MarkAsParticipating(new List<BaseCard> { defender });
                
                Debug.Log($"🛡️ {defender.name} joins as defender");
            }
        }

        /// <summary>
        /// Check if this conflict has a specific element
        /// </summary>
        /// <param name="element">Element to check for</param>
        /// <returns>True if conflict has this element</returns>
        public bool HasElement(string element)
        {
            return Elements.Contains(element);
        }

        /// <summary>
        /// Switch the conflict type (military ↔ political)
        /// </summary>
        public void SwitchType()
        {
            ring.FlipConflictType();
            conflictTypeSwitched = true;
            
            Debug.Log($"⚔️ Conflict type switched to {ConflictType}");
        }

        /// <summary>
        /// Switch to a different ring element
        /// </summary>
        /// <param name="element">New element to contest</param>
        public void SwitchElement(string element)
        {
            var newRing = game.rings.GetValueOrDefault(element);
            if (newRing == null)
            {
                Debug.LogError($"SwitchElement called for non-existent element: {element}");
                return;
            }

            // Take fate from new ring if allowed
            if (attackingPlayer.AllowGameAction("takeFateFromRings") && newRing.fate > 0)
            {
                game.AddMessage("{0} takes {1} fate from {2}", attackingPlayer, newRing.fate, newRing);
                attackingPlayer.ModifyFate(newRing.fate);
                newRing.RemoveFate();
            }

            // Flip ring type to match conflict if needed
            if (newRing.conflictType != ConflictType)
            {
                newRing.FlipConflictType();
            }

            // Reset old ring and contest new one
            ring.ResetRing();
            newRing.SetContested();
            ring = newRing;
            
            Debug.Log($"⚔️ Conflict switched to {element} ring");
        }

        /// <summary>
        /// Check for and remove characters that can no longer participate
        /// </summary>
        public void CheckForIllegalParticipants()
        {
            var illegalAttackers = attackers.Where(card => 
                !card.CanParticipateAsAttacker(ConflictType)).ToList();
            var illegalDefenders = defenders.Where(card => 
                !card.CanParticipateAsDefender(ConflictType)).ToList();
            
            var allIllegal = illegalAttackers.Concat(illegalDefenders).ToList();
            
            if (allIllegal.Count > 0)
            {
                string verb = allIllegal.Count > 1 ? "are" : "is";
                game.AddMessage("{0} cannot participate in the conflict any more and {1} sent home bowed", 
                               allIllegal, verb);
                
                var context = game.GetFrameworkContext();
                game.ApplyGameAction(context, new Dictionary<string, object>
                {
                    {"sendHome", allIllegal},
                    {"bow", allIllegal}
                });
            }
        }

        /// <summary>
        /// Remove a character from the conflict
        /// </summary>
        /// <param name="card">Character to remove</param>
        public void RemoveFromConflict(BaseCard card)
        {
            if (attackers.Remove(card) || defenders.Remove(card))
            {
                card.inConflict = false;
                Debug.Log($"🏃 {card.name} removed from conflict");
            }
        }

        /// <summary>
        /// Mark characters as participating in the conflict
        /// </summary>
        /// <param name="cards">Characters to mark as participating</param>
        private void MarkAsParticipating(List<BaseCard> cards)
        {
            foreach (var card in cards)
            {
                card.inConflict = true;
            }
        }

        /// <summary>
        /// Check if a character is attacking
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if the character is attacking</returns>
        public bool IsAttacking(BaseCard card)
        {
            return attackers.Contains(card);
        }

        /// <summary>
        /// Check if a character is defending
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if the character is defending</returns>
        public bool IsDefending(BaseCard card)
        {
            return defenders.Contains(card);
        }

        /// <summary>
        /// Check if a character is participating in any way
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if the character is participating</returns>
        public bool IsParticipating(BaseCard card)
        {
            return IsAttacking(card) || IsDefending(card);
        }

        /// <summary>
        /// Check if any participants match a condition
        /// </summary>
        /// <param name="predicate">Condition to check</param>
        /// <returns>True if any participant matches</returns>
        public bool AnyParticipants(System.Func<BaseCard, bool> predicate)
        {
            return attackers.Concat(defenders).Any(predicate);
        }

        /// <summary>
        /// Get all participants that match a condition
        /// </summary>
        /// <param name="predicate">Condition to match (defaults to all)</param>
        /// <returns>List of matching participants</returns>
        public List<BaseCard> GetParticipants(System.Func<BaseCard, bool> predicate = null)
        {
            predicate = predicate ?? (card => true);
            return attackers.Concat(defenders).Where(predicate).ToList();
        }

        /// <summary>
        /// Count participants that match a condition
        /// </summary>
        /// <param name="predicate">Condition to count</param>
        /// <returns>Number of matching participants</returns>
        public int GetNumberOfParticipants(System.Func<BaseCard, bool> predicate = null)
        {
            predicate = predicate ?? (card => true);
            return attackers.Concat(defenders).Count(predicate);
        }

        /// <summary>
        /// Count participants for a specific player
        /// </summary>
        /// <param name="player">Player to count for (or "attacker"/"defender")</param>
        /// <param name="predicate">Additional condition</param>
        /// <returns>Number of participants</returns>
        public int GetNumberOfParticipantsFor(object player, System.Func<BaseCard, bool> predicate = null)
        {
            Player targetPlayer = player switch
            {
                "attacker" => attackingPlayer,
                "defender" => defendingPlayer,
                Player p => p,
                _ => null
            };

            if (targetPlayer == null) return 0;

            var characters = GetCharacters(targetPlayer);
            int baseCount = predicate != null ? characters.Count(predicate) : characters.Count;
            int additionalCount = targetPlayer.SumEffects(EffectNames.AdditionalCharactersInConflict);
            
            return baseCount + additionalCount;
        }

        /// <summary>
        /// Check if a player has more participants than their opponent
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <param name="predicate">Additional condition</param>
        /// <returns>True if player has more participants</returns>
        public bool HasMoreParticipants(Player player, System.Func<BaseCard, bool> predicate = null)
        {
            if (player?.opponent == null) 
                return GetNumberOfParticipantsFor(player, predicate) > 0;
                
            return GetNumberOfParticipantsFor(player, predicate) > 
                   GetNumberOfParticipantsFor(player.opponent, predicate);
        }

        /// <summary>
        /// Record a card being played during the conflict
        /// </summary>
        /// <param name="player">Player who played the card</param>
        /// <param name="card">Card that was played</param>
        public void AddCardPlayed(Player player, BaseCard card)
        {
            var snapshot = card.CreateSnapshot();
            
            if (player == attackingPlayer)
            {
                attackerCardsPlayed.Add(snapshot);
            }
            else
            {
                defenderCardsPlayed.Add(snapshot);
            }
            
            Debug.Log($"🃏 {player.name} played {card.name} during conflict");
        }

        /// <summary>
        /// Get cards played by a player during this conflict
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <param name="predicate">Filter condition</param>
        /// <returns>List of played card snapshots</returns>
        public List<object> GetCardsPlayed(Player player, System.Func<object, bool> predicate = null)
        {
            predicate = predicate ?? (card => true);
            
            var cardsPlayed = player == attackingPlayer ? attackerCardsPlayed : defenderCardsPlayed;
            return cardsPlayed.Where(predicate).ToList();
        }

        /// <summary>
        /// Count cards played by a player during this conflict
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <param name="predicate">Filter condition</param>
        /// <returns>Number of cards played</returns>
        public int GetNumberOfCardsPlayed(Player player, System.Func<object, bool> predicate = null)
        {
            if (player == null) return 0;
            
            int baseCount = GetCardsPlayed(player, predicate).Count;
            int additionalCount = player.SumEffects(EffectNames.AdditionalCardPlayed);
            
            return baseCount + additionalCount;
        }

        /// <summary>
        /// Calculate current skill totals for both sides
        /// </summary>
        /// <param name="prevStateChanged">Whether state changed in previous check</param>
        /// <returns>True if game state changed</returns>
        public bool CalculateSkill(bool prevStateChanged = false)
        {
            bool stateChanged = game.effectEngine.CheckEffects(prevStateChanged);

            if (winnerDetermined) return stateChanged;

            // Find characters that contribute to conflict from provinces/stronghold
            var contributingLocations = new[]
            {
                Locations.PlayArea,
                Locations.ProvinceOne, Locations.ProvinceTwo, 
                Locations.ProvinceThree, Locations.ProvinceFour,
                Locations.StrongholdProvince
            };

            var additionalContributingCards = game.FindAnyCardsInAnyList(card =>
                card.type == CardTypes.Character &&
                contributingLocations.Contains(card.location) &&
                card.AnyEffect(EffectNames.ContributeToConflict));

            // Calculate attacker skill
            if (attackingPlayer.AnyEffect(EffectNames.SetConflictTotalSkill))
            {
                attackerSkill = (int)attackingPlayer.MostRecentEffect(EffectNames.SetConflictTotalSkill);
            }
            else
            {
                var additionalAttackers = additionalContributingCards
                    .Where(card => card.GetEffects(EffectNames.ContributeToConflict)
                        .Any(value => value.Equals(attackingPlayer))).ToList();
                
                attackerSkill = CalculateSkillFor(attackers.Concat(additionalAttackers).ToList()) + 
                               attackingPlayer.SkillModifier;
                
                // Imperial favor bonus
                if (attackingPlayer.imperialFavor == ConflictType && attackers.Count > 0)
                {
                    attackerSkill++;
                }
            }

            // Calculate defender skill
            if (defendingPlayer.AnyEffect(EffectNames.SetConflictTotalSkill))
            {
                defenderSkill = (int)defendingPlayer.MostRecentEffect(EffectNames.SetConflictTotalSkill);
            }
            else
            {
                var additionalDefenders = additionalContributingCards
                    .Where(card => card.GetEffects(EffectNames.ContributeToConflict)
                        .Any(value => value.Equals(defendingPlayer))).ToList();
                
                defenderSkill = CalculateSkillFor(defenders.Concat(additionalDefenders).ToList()) + 
                               defendingPlayer.SkillModifier;
                
                // Imperial favor bonus
                if (defendingPlayer.imperialFavor == ConflictType && defenders.Count > 0)
                {
                    defenderSkill++;
                }
            }

            return stateChanged;
        }

        /// <summary>
        /// Calculate skill contribution for a list of characters
        /// </summary>
        /// <param name="cards">Characters to calculate skill for</param>
        /// <returns>Total skill contribution</returns>
        private int CalculateSkillFor(List<BaseCard> cards)
        {
            var skillFunction = MostRecentEffect(EffectNames.ChangeConflictSkillFunction) as System.Func<BaseCard, int> ??
                               (card => card.GetContributionToConflict(ConflictType));
            
            var cannotContributeFunctions = GetEffects(EffectNames.CannotContribute)
                .Cast<System.Func<BaseCard, bool>>().ToList();

            return cards.Sum(card =>
            {
                // Check if card cannot contribute
                bool cannotContribute = card.bowed;
                if (!cannotContribute)
                {
                    cannotContribute = cannotContributeFunctions.Any(func => func(card));
                }

                return cannotContribute ? 0 : skillFunction(card);
            });
        }

        /// <summary>
        /// Determine the winner of the conflict
        /// </summary>
        public void DetermineWinner()
        {
            CalculateSkill();
            winnerDetermined = true;

            if (attackerSkill == 0 && defenderSkill == 0)
            {
                // No one wins if both have 0 skill
                loser = null;
                winner = null;
                loserSkill = winnerSkill = 0;
                skillDifference = 0;
                
                Debug.Log("⚔️ Conflict ends with no winner (0 vs 0 skill)");
                return;
            }

            if (attackerSkill >= defenderSkill)
            {
                loser = defendingPlayer;
                loserSkill = defenderSkill;
                winner = attackingPlayer;
                winnerSkill = attackerSkill;
            }
            else
            {
                loser = attackingPlayer;
                loserSkill = attackerSkill;
                winner = defendingPlayer;
                winnerSkill = defenderSkill;
            }

            skillDifference = winnerSkill - loserSkill;
            
            Debug.Log($"🏆 Conflict winner: {winner.name} ({winnerSkill} vs {loserSkill})");
        }

        /// <summary>
        /// Check if the attacking player won the conflict
        /// </summary>
        /// <returns>True if attacker won</returns>
        public bool IsAttackerTheWinner()
        {
            return winner == attackingPlayer;
        }

        /// <summary>
        /// Get characters participating for a specific player
        /// </summary>
        /// <param name="player">Player to get characters for</param>
        /// <returns>List of participating characters</returns>
        public List<BaseCard> GetCharacters(Player player)
        {
            if (player == null) return new List<BaseCard>();
            return player == attackingPlayer ? attackers : defenders;
        }

        /// <summary>
        /// Pass the conflict (attacker chooses not to declare)
        /// </summary>
        /// <param name="message">Message to display</param>
        public void PassConflict(string message = "{0} has chosen to pass their conflict opportunity")
        {
            game.AddMessage(message, attackingPlayer);
            conflictPassed = true;
            
            if (ring != null)
            {
                ring.ResetRing();
            }
            
            game.RecordConflict(this);
            game.currentConflict = null;
            
            game.RaiseEvent(EventNames.OnConflictPass, new Dictionary<string, object>
            {
                {"conflict", this}
            });
            
            ResetCards();
            
            Debug.Log($"🏃 {attackingPlayer.name} passed their conflict");
        }

        /// <summary>
        /// IronPython integration for conflict events
        /// </summary>
        /// <param name="eventType">Type of conflict event</param>
        /// <param name="parameters">Event parameters</param>
        public void ExecuteConflictScript(string eventType, params object[] parameters)
        {
            if (game.enablePythonScripting)
            {
                var allParams = new List<object> { this }.Concat(parameters).ToArray();
                game.ExecuteCardScript("conflict_engine", eventType, allParams);
            }
        }

        /// <summary>
        /// Conflict event handlers for IronPython
        /// </summary>
        public void OnConflictDeclared()
        {
            ExecuteConflictScript("on_conflict_declared", attackingPlayer, defendingPlayer, ring);
        }

        public void OnAttackersChosen()
        {
            ExecuteConflictScript("on_attackers_chosen", attackers);
        }

        public void OnDefendersChosen()
        {
            ExecuteConflictScript("on_defenders_chosen", defenders);
        }

        public void OnConflictResolved()
        {
            ExecuteConflictScript("on_conflict_resolved", winner, loser, skillDifference);
        }

        /// <summary>
        /// Cleanup when conflict is destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            ResetCards();
            attackers.Clear();
            defenders.Clear();
            attackerCardsPlayed.Clear();
            defenderCardsPlayed.Clear();
            
            base.OnDestroy();
            
            Debug.Log("⚔️ Conflict destroyed");
        }
    }

    /// <summary>
    /// Summary data for conflict state
    /// </summary>
    [System.Serializable]
    public class ConflictSummary
    {
        public string attackingPlayerId;
        public string defendingPlayerId;
        public int attackerSkill;
        public int defenderSkill;
        public string type;
        public List<string> elements;
        public bool attackerWins;
        public bool breaking;
        public bool unopposed;
        public bool declarationComplete;
        public bool defendersChosen;
        public string conflictRing;
        public string province;
        public bool winnerDetermined;
        public string winner;
        public int skillDifference;
    }

    /// <summary>
    /// Conflict-specific effect names
    /// </summary>
    public static partial class EffectNames
    {
        public const string RestrictNumberOfDefenders = "restrictNumberOfDefenders";
        public const string ForceConflictUnopposed = "forceConflictUnopposed";
        public const string ModifyConflictElementsToResolve = "modifyConflictElementsToResolve";
        public const string AdditionalCharactersInConflict = "additionalCharactersInConflict";
        public const string AdditionalCardPlayed = "additionalCardPlayed";
        public const string ContributeToConflict = "contributeToConflict";
        public const string SetConflictTotalSkill = "setConflictTotalSkill";
        public const string ChangeConflictSkillFunction = "changeConflictSkillFunction";
        public const string CannotContribute = "cannotContribute";
    }

    /// <summary>
    /// Conflict-specific event names
    /// </summary>
    public static partial class EventNames
    {
        public const string OnConflictPass = "onConflictPass";
        public const string OnConflictDeclared = "onConflictDeclared";
        public const string OnAttackersChosen = "onAttackersChosen";
        public const string OnDefendersChosen = "onDefendersChosen";
        public const string OnConflictResolved = "onConflictResolved";
    }

    /// <summary>
    /// Extension methods for conflict management
    /// </summary>
    public static class ConflictExtensions
    {
        /// <summary>
        /// Check if game is currently during any conflict
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>True if in conflict</returns>
        public static bool IsDuringConflict(this Game game)
        {
            return game.currentConflict != null;
        }

        /// <summary>
        /// Check if game is during a specific type of conflict
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="conflictType">Type to check for</param>
        /// <returns>True if in that type of conflict</returns>
        public static bool IsDuringConflict(this Game game, string conflictType)
        {
            return game.currentConflict != null && game.currentConflict.ConflictType == conflictType;
        }

        /// <summary>
        /// Check if game is during a conflict with specific elements
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="elements">Elements to check for</param>
        /// <returns>True if conflict has any of these elements</returns>
        public static bool IsDuringConflict(this Game game, params string[] elements)
        {
            if (game.currentConflict == null) return false;
            if (elements == null || elements.Length == 0) return true;
            
            return elements.Any(element => game.currentConflict.HasElement(element));
        }

        /// <summary>
        /// Get all characters participating in current conflict
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>List of participating characters</returns>
        public static List<BaseCard> GetConflictParticipants(this Game game)
        {
            return game.currentConflict?.GetParticipants() ?? new List<BaseCard>();
        }

        /// <summary>
        /// Get characters participating for a specific player
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="player">Player to get participants for</param>
        /// <returns>List of that player's participants</returns>
        public static List<BaseCard> GetConflictParticipants(this Game game, Player player)
        {
            return game.currentConflict?.GetCharacters(player) ?? new List<BaseCard>();
        }

        /// <summary>
        /// Check if a character is participating in current conflict
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if participating</returns>
        public static bool IsParticipatingInConflict(this BaseCard card)
        {
            return card.game.currentConflict?.IsParticipating(card) ?? false;
        }

        /// <summary>
        /// Check if a character is attacking in current conflict
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if attacking</returns>
        public static bool IsAttackingInConflict(this BaseCard card)
        {
            return card.game.currentConflict?.IsAttacking(card) ?? false;
        }

        /// <summary>
        /// Check if a character is defending in current conflict
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if defending</returns>
        public static bool IsDefendingInConflict(this BaseCard card)
        {
            return card.game.currentConflict?.IsDefending(card) ?? false;
        }
    }
}