using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace L5RGame
{
    /// <summary>
    /// SetupPhase manages the initial game setup for L5R, including:
    /// - Random first player determination
    /// - First player choice (stay first or give to opponent)
    /// - Stronghold and role card placement
    /// - Province setup and arrangement
    /// - Dynasty card placement in provinces
    /// - Starting hand drawing
    /// - Mulligan opportunities for both dynasty and conflict cards
    /// - Initial honor and game state setup
    /// 
    /// Phase Structure:
    /// 1. Setup begins (determine first player)
    /// 2. Choose first player (winner decides)
    /// 3. Attach strongholds and roles
    /// 4. Setup provinces (choose arrangement)
    /// 5. Fill provinces with dynasty cards
    /// 6. Dynasty mulligan
    /// 7. Draw starting hands
    /// 8. Conflict mulligan
    /// 9. Start game (set honor, ready state)
    /// </summary>
    public class SetupPhase : GamePhase
    {
        [Header("Setup Phase Settings")]
        public int startingHandSize = 4;
        public float setupStepDelay = 1f;
        public bool enableMulligans = true;
        public bool enableProvinceChoice = true;
        public float firstPlayerChoiceTimeout = 30f;

        [Header("Mulligan Settings")]
        public bool allowDynastyMulligan = true;
        public bool allowConflictMulligan = true;
        public float mulliganTimeout = 45f;

        // Current state
        private Player temporaryFirstPlayer;
        private bool firstPlayerChosen = false;
        private bool strongholdsAttached = false;
        private bool provincesSetup = false;
        private bool dynastyMulliganComplete = false;
        private bool startingHandsDrawn = false;
        private bool conflictMulliganComplete = false;
        private bool gameStarted = false;

        // Setup prompts
        private FirstPlayerChoicePrompt firstPlayerPrompt;
        private SetupProvincesPrompt setupProvincesPrompt;
        private MulliganDynastyPrompt mulliganDynastyPrompt;
        private MulliganConflictPrompt mulliganConflictPrompt;

        // Events
        public System.Action<Player> OnFirstPlayerDetermined;
        public System.Action<Player> OnFirstPlayerChosen;
        public System.Action OnStrongholdsAttached;
        public System.Action OnProvincesSetup;
        public System.Action OnDynastyMulliganComplete;
        public System.Action OnStartingHandsDrawn;
        public System.Action OnConflictMulliganComplete;
        public System.Action OnGameSetupComplete;

        public SetupPhase(Game game) : base(game, GamePhases.Setup)
        {
            InitializeSetupPhase();
        }

        #region Phase Initialization
        private void InitializeSetupPhase()
        {
            // Initialize the setup phase steps
            steps = new List<IGameStep>
            {
                new SimpleStep(game, SetupBegin),
                new SimpleStep(game, ChooseFirstPlayer),
                new SimpleStep(game, AttachStronghold),
                new SimpleStep(game, SetupProvinces),
                new SimpleStep(game, FillProvinces),
                new SimpleStep(game, DynastyMulligan),
                new SimpleStep(game, DrawStartingHands),
                new SimpleStep(game, ConflictMulligan),
                new SimpleStep(game, StartGame)
            };
        }
        #endregion

        #region Phase Execution
        public override void StartPhase()
        {
            base.StartPhase();
            
            game.AddMessage("=== Game Setup Begins ===");
            ExecutePythonScript("on_setup_phase_start");
            
            // Reset setup state
            temporaryFirstPlayer = null;
            firstPlayerChosen = false;
            strongholdsAttached = false;
            provincesSetup = false;
            dynastyMulliganComplete = false;
            startingHandsDrawn = false;
            conflictMulliganComplete = false;
            gameStarted = false;

            // Set current phase for game state
            game.currentPhase = GamePhases.Setup;

            SetupBegin();
        }

        private void SetupBegin()
        {
            StartCoroutine(SetupBeginCoroutine());
        }

        private IEnumerator SetupBeginCoroutine()
        {
            game.AddMessage("Determining first player...");
            ExecutePythonScript("on_setup_begin");

            yield return new WaitForSeconds(setupStepDelay);

            // Randomly determine first player
            var allPlayers = game.GetPlayers().ToList();
            var shuffledPlayers = allPlayers.OrderBy(p => UnityEngine.Random.value).ToList();
            temporaryFirstPlayer = shuffledPlayers.First();
            
            temporaryFirstPlayer.firstPlayer = true;
            game.SetFirstPlayer(temporaryFirstPlayer);

            game.AddMessage("{0} wins the initial flip!", temporaryFirstPlayer.name);
            OnFirstPlayerDetermined?.Invoke(temporaryFirstPlayer);
            ExecutePythonScript("on_first_player_determined", temporaryFirstPlayer);

            yield return new WaitForSeconds(setupStepDelay);
            game.QueueStep(new SimpleStep(game, ChooseFirstPlayer));
        }
        #endregion

        #region First Player Choice
        private void ChooseFirstPlayer()
        {
            if (firstPlayerChosen) return;

            var currentFirstPlayer = game.GetFirstPlayer();
            if (currentFirstPlayer.opponent != null)
            {
                StartCoroutine(ChooseFirstPlayerCoroutine(currentFirstPlayer));
            }
            else
            {
                // Single player or no opponent - skip choice
                game.AddMessage("{0} will be the first player.", currentFirstPlayer.name);
                firstPlayerChosen = true;
                OnFirstPlayerChosen?.Invoke(currentFirstPlayer);
                game.QueueStep(new SimpleStep(game, AttachStronghold));
            }
        }

        private IEnumerator ChooseFirstPlayerCoroutine(Player firstPlayer)
        {
            game.AddMessage("{0} won the flip and must choose who goes first.", firstPlayer.name);
            
            firstPlayerPrompt = new FirstPlayerChoicePrompt(game, firstPlayer);
            firstPlayerPrompt.OnChoiceMade += OnFirstPlayerChoiceMade;
            
            var choices = new List<string> { "First Player", "Second Player" };
            firstPlayerPrompt.PromptForChoice(
                "You won the flip. Do you want to be:",
                choices,
                firstPlayerChoiceTimeout
            );

            ExecutePythonScript("on_first_player_choice_prompt", firstPlayer);

            // Wait for choice or timeout
            while (!firstPlayerChosen)
            {
                yield return new WaitForSeconds(0.1f);
            }

            game.QueueStep(new SimpleStep(game, AttachStronghold));
        }

        private void OnFirstPlayerChoiceMade(Player choosingPlayer, int choiceIndex)
        {
            if (choiceIndex == 0) // Stay first player
            {
                game.AddMessage("{0} chooses to be the first player.", choosingPlayer.name);
                // No change needed
            }
            else // Give first player to opponent
            {
                game.SetFirstPlayer(choosingPlayer.opponent);
                game.AddMessage("{0} gives first player to {1}.", choosingPlayer.name, choosingPlayer.opponent.name);
            }

            firstPlayerChosen = true;
            OnFirstPlayerChosen?.Invoke(game.GetFirstPlayer());
            ExecutePythonScript("on_first_player_chosen", game.GetFirstPlayer(), choosingPlayer, choiceIndex);

            if (firstPlayerPrompt != null)
            {
                firstPlayerPrompt.Cleanup();
                firstPlayerPrompt = null;
            }
        }
        #endregion

        #region Stronghold Attachment
        private void AttachStronghold()
        {
            if (strongholdsAttached) return;

            StartCoroutine(AttachStrongholdCoroutine());
        }

        private IEnumerator AttachStrongholdCoroutine()
        {
            game.AddMessage("=== Placing Strongholds and Roles ===");
            ExecutePythonScript("on_stronghold_attachment_start");

            foreach (var player in game.GetPlayers())
            {
                // Move stronghold to stronghold province
                if (player.stronghold != null)
                {
                    player.MoveCard(player.stronghold, Locations.StrongholdProvince);
                    game.AddMessage("{0} places {1} as their stronghold.", player.name, player.stronghold.name);
                    ExecutePythonScript("on_stronghold_placed", player, player.stronghold);
                }

                // Move role to role area
                if (player.role != null)
                {
                    player.MoveCard(player.role, Locations.Role);
                    game.AddMessage("{0} places {1} as their role.", player.name, player.role.name);
                    ExecutePythonScript("on_role_placed", player, player.role);
                }

                yield return new WaitForSeconds(setupStepDelay);
            }

            strongholdsAttached = true;
            OnStrongholdsAttached?.Invoke();
            ExecutePythonScript("on_stronghold_attachment_complete");

            game.QueueStep(new SimpleStep(game, SetupProvinces));
        }
        #endregion

        #region Province Setup
        private void SetupProvinces()
        {
            if (provincesSetup) return;

            if (enableProvinceChoice)
            {
                StartCoroutine(SetupProvincesCoroutine());
            }
            else
            {
                // Auto-setup provinces in default order
                AutoSetupProvinces();
                game.QueueStep(new SimpleStep(game, FillProvinces));
            }
        }

        private IEnumerator SetupProvincesCoroutine()
        {
            game.AddMessage("=== Setting Up Provinces ===");
            ExecutePythonScript("on_province_setup_start");

            // For each player, allow them to arrange their provinces
            foreach (var player in game.GetPlayers())
            {
                setupProvincesPrompt = new SetupProvincesPrompt(game, player);
                setupProvincesPrompt.OnSetupComplete += OnPlayerProvinceSetupComplete;
                
                yield return StartCoroutine(setupProvincesPrompt.ExecuteSetup());
                
                setupProvincesPrompt.Cleanup();
                setupProvincesPrompt = null;
            }

            provincesSetup = true;
            OnProvincesSetup?.Invoke();
            ExecutePythonScript("on_province_setup_complete");

            game.QueueStep(new SimpleStep(game, FillProvinces));
        }

        private void AutoSetupProvinces()
        {
            foreach (var player in game.GetPlayers())
            {
                // Automatically place provinces in default order
                var provinces = player.GetAllProvinces();
                for (int i = 0; i < provinces.Count && i < 4; i++)
                {
                    var provinceLocation = GetProvinceLocationByIndex(i);
                    player.MoveCard(provinces[i], provinceLocation);
                }
                
                ExecutePythonScript("on_provinces_auto_setup", player, provinces);
            }

            provincesSetup = true;
            OnProvincesSetup?.Invoke();
        }

        private string GetProvinceLocationByIndex(int index)
        {
            return index switch
            {
                0 => Locations.ProvinceOne,
                1 => Locations.ProvinceTwo,
                2 => Locations.ProvinceThree,
                3 => Locations.ProvinceFour,
                _ => Locations.ProvinceOne
            };
        }

        private void OnPlayerProvinceSetupComplete(Player player)
        {
            game.AddMessage("{0} has arranged their provinces.", player.name);
            ExecutePythonScript("on_player_province_setup_complete", player);
        }
        #endregion

        #region Fill Provinces
        private void FillProvinces()
        {
            StartCoroutine(FillProvincesCoroutine());
        }

        private IEnumerator FillProvincesCoroutine()
        {
            game.AddMessage("=== Filling Provinces with Dynasty Cards ===");
            ExecutePythonScript("on_fill_provinces_start");

            var provinceLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo,
                Locations.ProvinceThree, Locations.ProvinceFour
            };

            foreach (var player in game.GetPlayers())
            {
                var cardsPlaced = new List<BaseCard>();

                foreach (var province in provinceLocations)
                {
                    var card = player.dynastyDeck.GetTopCard();
                    if (card != null)
                    {
                        player.MoveCard(card, province);
                        card.facedown = false; // Revealed during setup
                        cardsPlaced.Add(card);
                        
                        ExecutePythonScript("on_dynasty_card_placed_in_province", player, card, province);
                    }
                }

                if (cardsPlaced.Count > 0)
                {
                    game.AddMessage("{0} places dynasty cards: {1}", 
                                  player.name, string.Join(", ", cardsPlaced.Select(c => c.name)));
                }

                yield return new WaitForSeconds(setupStepDelay);
            }

            // Apply any location-based persistent effects
            foreach (var card in game.GetAllCards())
            {
                card.ApplyAnyLocationPersistentEffects();
            }

            ExecutePythonScript("on_fill_provinces_complete");
            game.QueueStep(new SimpleStep(game, DynastyMulligan));
        }
        #endregion

        #region Dynasty Mulligan
        private void DynastyMulligan()
        {
            if (!allowDynastyMulligan)
            {
                dynastyMulliganComplete = true;
                game.QueueStep(new SimpleStep(game, DrawStartingHands));
                return;
            }

            StartCoroutine(DynastyMulliganCoroutine());
        }

        private IEnumerator DynastyMulliganCoroutine()
        {
            game.AddMessage("=== Dynasty Mulligan Phase ===");
            ExecutePythonScript("on_dynasty_mulligan_start");

            foreach (var player in game.GetPlayers())
            {
                mulliganDynastyPrompt = new MulliganDynastyPrompt(game, player);
                mulliganDynastyPrompt.OnMulliganComplete += OnPlayerDynastyMulliganComplete;
                
                yield return StartCoroutine(mulliganDynastyPrompt.ExecuteMulligan(mulliganTimeout));
                
                mulliganDynastyPrompt.Cleanup();
                mulliganDynastyPrompt = null;
            }

            dynastyMulliganComplete = true;
            OnDynastyMulliganComplete?.Invoke();
            ExecutePythonScript("on_dynasty_mulligan_complete");

            game.QueueStep(new SimpleStep(game, DrawStartingHands));
        }

        private void OnPlayerDynastyMulliganComplete(Player player, List<BaseCard> mulliganedCards)
        {
            if (mulliganedCards.Count > 0)
            {
                game.AddMessage("{0} mulligans {1} dynasty card(s).", player.name, mulliganedCards.Count);
            }
            else
            {
                game.AddMessage("{0} keeps their dynasty cards.", player.name);
            }
            
            ExecutePythonScript("on_player_dynasty_mulligan_complete", player, mulliganedCards);
        }
        #endregion

        #region Starting Hands
        private void DrawStartingHands()
        {
            if (startingHandsDrawn) return;

            StartCoroutine(DrawStartingHandsCoroutine());
        }

        private IEnumerator DrawStartingHandsCoroutine()
        {
            game.AddMessage("=== Drawing Starting Hands ===");
            ExecutePythonScript("on_draw_starting_hands_start");

            foreach (var player in game.GetPlayers())
            {
                player.DrawCardsToHand(startingHandSize);
                game.AddMessage("{0} draws {1} cards for their starting hand.", player.name, startingHandSize);
                ExecutePythonScript("on_player_drew_starting_hand", player, startingHandSize);
                
                yield return new WaitForSeconds(setupStepDelay);
            }

            startingHandsDrawn = true;
            OnStartingHandsDrawn?.Invoke();
            ExecutePythonScript("on_draw_starting_hands_complete");

            game.QueueStep(new SimpleStep(game, ConflictMulligan));
        }
        #endregion

        #region Conflict Mulligan
        private void ConflictMulligan()
        {
            if (!allowConflictMulligan)
            {
                conflictMulliganComplete = true;
                game.QueueStep(new SimpleStep(game, StartGame));
                return;
            }

            StartCoroutine(ConflictMulliganCoroutine());
        }

        private IEnumerator ConflictMulliganCoroutine()
        {
            game.AddMessage("=== Conflict Mulligan Phase ===");
            ExecutePythonScript("on_conflict_mulligan_start");

            foreach (var player in game.GetPlayers())
            {
                mulliganConflictPrompt = new MulliganConflictPrompt(game, player);
                mulliganConflictPrompt.OnMulliganComplete += OnPlayerConflictMulliganComplete;
                
                yield return StartCoroutine(mulliganConflictPrompt.ExecuteMulligan(mulliganTimeout));
                
                mulliganConflictPrompt.Cleanup();
                mulliganConflictPrompt = null;
            }

            conflictMulliganComplete = true;
            OnConflictMulliganComplete?.Invoke();
            ExecutePythonScript("on_conflict_mulligan_complete");

            game.QueueStep(new SimpleStep(game, StartGame));
        }

        private void OnPlayerConflictMulliganComplete(Player player, List<BaseCard> mulliganedCards)
        {
            if (mulliganedCards.Count > 0)
            {
                game.AddMessage("{0} mulligans {1} conflict card(s).", player.name, mulliganedCards.Count);
            }
            else
            {
                game.AddMessage("{0} keeps their conflict hand.", player.name);
            }
            
            ExecutePythonScript("on_player_conflict_mulligan_complete", player, mulliganedCards);
        }
        #endregion

        #region Game Start
        private void StartGame()
        {
            if (gameStarted) return;

            StartCoroutine(StartGameCoroutine());
        }

        private IEnumerator StartGameCoroutine()
        {
            game.AddMessage("=== Finalizing Game Setup ===");
            ExecutePythonScript("on_start_game_begin");

            foreach (var player in game.GetPlayers())
            {
                // Set starting honor from stronghold
                if (player.stronghold != null && player.stronghold is StrongholdCard stronghold)
                {
                    player.honor = stronghold.GetStartingHonor();
                    game.AddMessage("{0} starts with {1} honor.", player.name, player.honor);
                }

                // Set starting fate
                if (player.stronghold != null && player.stronghold is StrongholdCard strongholdForFate)
                {
                    player.fate = strongholdForFate.GetFate();
                    game.AddMessage("{0} starts with {1} fate.", player.name, player.fate);
                }

                // Mark player as ready
                player.readyToStart = true;
                ExecutePythonScript("on_player_ready_to_start", player);
                
                yield return new WaitForSeconds(setupStepDelay);
            }

            gameStarted = true;
            OnGameSetupComplete?.Invoke();
            ExecutePythonScript("on_game_setup_complete");

            game.AddMessage("=== Game Setup Complete - Game Begins! ===");
            
            // End setup phase and begin dynasty phase
            EndPhase();
        }
        #endregion

        #region Phase Management
        public override void EndPhase()
        {
            ExecutePythonScript("on_setup_phase_end");

            // Cleanup any remaining prompts
            CleanupPrompts();

            base.EndPhase();
        }

        private void CleanupPrompts()
        {
            if (firstPlayerPrompt != null)
            {
                firstPlayerPrompt.Cleanup();
                firstPlayerPrompt = null;
            }

            if (setupProvincesPrompt != null)
            {
                setupProvincesPrompt.Cleanup();
                setupProvincesPrompt = null;
            }

            if (mulliganDynastyPrompt != null)
            {
                mulliganDynastyPrompt.Cleanup();
                mulliganDynastyPrompt = null;
            }

            if (mulliganConflictPrompt != null)
            {
                mulliganConflictPrompt.Cleanup();
                mulliganConflictPrompt = null;
            }
        }
        #endregion

        #region Public Interface
        public bool IsFirstPlayerChosen() => firstPlayerChosen;
        public bool AreStrongholdsAttached() => strongholdsAttached;
        public bool AreProvincesSetup() => provincesSetup;
        public bool IsDynastyMulliganComplete() => dynastyMulliganComplete;
        public bool AreStartingHandsDrawn() => startingHandsDrawn;
        public bool IsConflictMulliganComplete() => conflictMulliganComplete;
        public bool IsGameStarted() => gameStarted;

        public void ForceCompleteCurrentStep()
        {
            // Force complete current step for testing or quick setup
            if (!firstPlayerChosen && firstPlayerPrompt != null)
            {
                OnFirstPlayerChoiceMade(temporaryFirstPlayer, 0);
            }
        }
        #endregion

        #region IronPython Integration
        protected override void ExecutePythonScript(string methodName, params object[] parameters)
        {
            try
            {
                game.ExecutePhaseScript("setup_phase.py", methodName, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python script for setup phase: {ex.Message}");
            }
        }

        // Setup-specific Python events
        public void OnDeckShuffled(Player player, string deckType)
        {
            ExecutePythonScript("on_deck_shuffled", player, deckType);
        }

        public void OnCardRevealed(Player player, BaseCard card, string location)
        {
            ExecutePythonScript("on_card_revealed", player, card, location);
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogSetupPhaseStatus()
        {
            Debug.Log($"Setup Phase Status:\n" +
                     $"First Player Chosen: {firstPlayerChosen}\n" +
                     $"Strongholds Attached: {strongholdsAttached}\n" +
                     $"Provinces Setup: {provincesSetup}\n" +
                     $"Dynasty Mulligan Complete: {dynastyMulliganComplete}\n" +
                     $"Starting Hands Drawn: {startingHandsDrawn}\n" +
                     $"Conflict Mulligan Complete: {conflictMulliganComplete}\n" +
                     $"Game Started: {gameStarted}");
        }
        #endregion
    }

    #region Supporting Prompt Classes
    public class FirstPlayerChoicePrompt
    {
        private Game game;
        private Player choosingPlayer;
        public System.Action<Player, int> OnChoiceMade;

        public FirstPlayerChoicePrompt(Game game, Player player)
        {
            this.game = game;
            this.choosingPlayer = player;
        }

        public void PromptForChoice(string promptText, List<string> choices, float timeout)
        {
            game.PromptForChoice(choosingPlayer, new ChoicePrompt
            {
                promptText = promptText,
                options = choices,
                timeoutSeconds = timeout,
                onChoiceMade = (choiceIndex) => OnChoiceMade?.Invoke(choosingPlayer, choiceIndex),
                onTimeout = () => OnChoiceMade?.Invoke(choosingPlayer, 0) // Default to first player
            });
        }

        public void Cleanup()
        {
            OnChoiceMade = null;
        }
    }

    public class SetupProvincesPrompt
    {
        private Game game;
        private Player player;
        public System.Action<Player> OnSetupComplete;

        public SetupProvincesPrompt(Game game, Player player)
        {
            this.game = game;
            this.player = player;
        }

        public IEnumerator ExecuteSetup()
        {
            // For now, auto-arrange provinces
            // In full implementation, this would show UI for province arrangement
            yield return new WaitForSeconds(1f);
            OnSetupComplete?.Invoke(player);
        }

        public void Cleanup()
        {
            OnSetupComplete = null;
        }
    }

    public class MulliganDynastyPrompt
    {
        private Game game;
        private Player player;
        public System.Action<Player, List<BaseCard>> OnMulliganComplete;

        public MulliganDynastyPrompt(Game game, Player player)
        {
            this.game = game;
            this.player = player;
        }

        public IEnumerator ExecuteMulligan(float timeout)
        {
            // For now, auto-skip mulligan
            // In full implementation, this would show UI for selecting cards to mulligan
            yield return new WaitForSeconds(1f);
            OnMulliganComplete?.Invoke(player, new List<BaseCard>());
        }

        public void Cleanup()
        {
            OnMulliganComplete = null;
        }
    }

    public class MulliganConflictPrompt
    {
        private Game game;
        private Player player;
        public System.Action<Player, List<BaseCard>> OnMulliganComplete;

        public MulliganConflictPrompt(Game game, Player player)
        {
            this.game = game;
            this.player = player;
        }

        public IEnumerator ExecuteMulligan(float timeout)
        {
            // For now, auto-skip mulligan
            // In full implementation, this would show UI for selecting cards to mulligan
            yield return new WaitForSeconds(1f);
            OnMulliganComplete?.Invoke(player, new List<BaseCard>());
        }

        public void Cleanup()
        {
            OnMulliganComplete = null;
        }
    }
    #endregion
}