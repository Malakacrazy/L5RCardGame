using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Mirror;
using MessagePack;

namespace UnityCardGame.Server
{
    // =============================================================================
    // CORE INTERFACES
    // =============================================================================

    public interface IEventStore
    {
        Task AppendEventAsync(GameEvent gameEvent);
        Task<List<GameEvent>> GetEventsAsync(string gameId, DateTime? fromTimestamp = null);
        Task<GameState> RebuildStateAsync(string gameId);
    }

    public interface IRedisCache
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<GameState> GetGameStateAsync(string gameId);
        Task UpdateGameStateAsync(string gameId, GameStateChange stateChange);
        Task SetHealthDataAsync(string serverId, ServerHealthData healthData);
        Task RemoveAsync(string key);
    }

    public interface IScriptEngine
    {
        Task<ScriptResult> ExecuteCardScriptAsync(string cardId, GameContext context);
        Task<bool> ValidateScriptAsync(string scriptContent);
        Task PrecompileScriptAsync(string cardId, string scriptContent);
    }

    public interface ITokenService
    {
        Task<bool> ValidateGameTokenAsync(string token);
        ClaimsPrincipal GetTokenClaims(string token);
        string GenerateGameToken(string userId, string gameId, TimeSpan? expiration = null);
    }

    public interface IGameStateCoordinator
    {
        Task OnGameCompleteAsync(string gameId, GameResult result);
        Task OnPlayerJoinedAsync(string gameId, string playerId);
        Task OnPlayerLeftAsync(string gameId, string playerId);
        Task SynchronizeStateAsync(string gameId, GameState state);
    }

    // =============================================================================
    // NETWORK MESSAGES
    // =============================================================================

    [MessagePackObject]
    public struct AuthenticationMessage : NetworkMessage
    {
        [Key(0)] public string Token { get; set; }
        [Key(1)] public string ClientVersion { get; set; }
    }

    [MessagePackObject]
    public struct AuthenticationResponseMessage : NetworkMessage
    {
        [Key(0)] public bool Success { get; set; }
        [Key(1)] public string Reason { get; set; }
        [Key(2)] public string PlayerId { get; set; }
        [Key(3)] public string SessionId { get; set; }
    }

    [MessagePackObject]
    public struct JoinGameMessage : NetworkMessage
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public string PlayerDeck { get; set; }
        [Key(2)] public bool IsSpectator { get; set; }
    }

    [MessagePackObject]
    public struct JoinGameResponseMessage : NetworkMessage
    {
        [Key(0)] public bool Success { get; set; }
        [Key(1)] public string Reason { get; set; }
        [Key(2)] public string GameId { get; set; }
        [Key(3)] public GameState GameState { get; set; }
        [Key(4)] public List<string> PlayerIds { get; set; }
    }

    [MessagePackObject]
    public struct LeaveGameMessage : NetworkMessage
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public bool ForceLeave { get; set; }
    }

    [MessagePackObject]
    public struct LeaveGameResponseMessage : NetworkMessage
    {
        [Key(0)] public bool Success { get; set; }
        [Key(1)] public string Reason { get; set; }
    }

    [MessagePackObject]
    public struct GameActionMessage : NetworkMessage
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public GameAction Action { get; set; }
        [Key(2)] public DateTime ClientTimestamp { get; set; }
        [Key(3)] public int ActionSequence { get; set; }
    }

    [MessagePackObject]
    public struct GameStateUpdateMessage : NetworkMessage
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public GameStateChange StateChange { get; set; }
        [Key(2)] public DateTime Timestamp { get; set; }
        [Key(3)] public int SequenceNumber { get; set; }
    }

    [MessagePackObject]
    public struct GameEndMessage : NetworkMessage
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public GameResult Result { get; set; }
        [Key(2)] public DateTime Timestamp { get; set; }
        [Key(3)] public GameStatistics Statistics { get; set; }
    }

    // =============================================================================
    // GAME DATA STRUCTURES
    // =============================================================================

    [MessagePackObject]
    public class GameAction
    {
        [Key(0)] public string ActionType { get; set; }
        [Key(1)] public string PlayerId { get; set; }
        [Key(2)] public string CardId { get; set; }
        [Key(3)] public Dictionary<string, object> Parameters { get; set; }
        [Key(4)] public bool RequiresScriptExecution { get; set; }
        [Key(5)] public int TargetPosition { get; set; }
        [Key(6)] public string TargetId { get; set; }
        [Key(7)] public DateTime Timestamp { get; set; }

        public GameAction()
        {
            Parameters = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
    }

    [MessagePackObject]
    public class GameEvent
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public string PlayerId { get; set; }
        [Key(2)] public GameAction Action { get; set; }
        [Key(3)] public DateTime Timestamp { get; set; }
        [Key(4)] public ScriptResult ScriptResult { get; set; }
        [Key(5)] public GameStateChange StateChange { get; set; }
        [Key(6)] public int SequenceNumber { get; set; }
    }

    [MessagePackObject]
    public class GameState
    {
        [Key(0)] public string GameId { get; set; }
        [Key(1)] public List<Player> Players { get; set; }
        [Key(2)] public string CurrentPlayerId { get; set; }
        [Key(3)] public int TurnNumber { get; set; }
        [Key(4)] public float TurnTimeRemaining { get; set; }
        [Key(5)] public GamePhase Phase { get; set; }
        [Key(6)] public Board Board { get; set; }
        [Key(7)] public DateTime LastUpdate { get; set; }
        [Key(8)] public Dictionary<string, object> GameVariables { get; set; }

        public GameState()
        {
            Players = new List<Player>();
            GameVariables = new Dictionary<string, object>();
            LastUpdate = DateTime.UtcNow;
        }

        public Player GetPlayer(string playerId)
        {
            return Players.Find(p => p.PlayerId == playerId);
        }
    }

    [MessagePackObject]
    public class GameStateChange
    {
        [Key(0)] public List<StateModification> Modifications { get; set; }
        [Key(1)] public bool IsGameEnded { get; set; }
        [Key(2)] public GameResult GameResult { get; set; }
        [Key(3)] public List<Animation> Animations { get; set; }
        [Key(4)] public Dictionary<string, object> ChangedVariables { get; set; }
        [Key(5)] public DateTime Timestamp { get; set; }

        public GameStateChange()
        {
            Modifications = new List<StateModification>();
            Animations = new List<Animation>();
            ChangedVariables = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
    }

    [MessagePackObject]
    public class Player
    {
        [Key(0)] public string PlayerId { get; set; }
        [Key(1)] public string DisplayName { get; set; }
        [Key(2)] public int Health { get; set; }
        [Key(3)] public int MaxHealth { get; set; }
        [Key(4)] public int CurrentMana { get; set; }
        [Key(5)] public int MaxMana { get; set; }
        [Key(6)] public List<Card> Hand { get; set; }
        [Key(7)] public List<Card> Board { get; set; }
        [Key(8)] public List<Card> Deck { get; set; }
        [Key(9)] public List<Card> Graveyard { get; set; }
        [Key(10)] public PlayerStatus Status { get; set; }
        [Key(11)] public Dictionary<string, int> Resources { get; set; }

        public Player()
        {
            Hand = new List<Card>();
            Board = new List<Card>();
            Deck = new List<Card>();
            Graveyard = new List<Card>();
            Resources = new Dictionary<string, int>();
        }
    }

    [MessagePackObject]
    public class Card
    {
        [Key(0)] public string CardId { get; set; }
        [Key(1)] public string Name { get; set; }
        [Key(2)] public string Type { get; set; }
        [Key(3)] public int ManaCost { get; set; }
        [Key(4)] public int Attack { get; set; }
        [Key(5)] public int Health { get; set; }
        [Key(6)] public int MaxHealth { get; set; }
        [Key(7)] public List<string> Abilities { get; set; }
        [Key(8)] public Dictionary<string, object> Properties { get; set; }
        [Key(9)] public string ScriptId { get; set; }
        [Key(10)] public bool CanAttack { get; set; }
        [Key(11)] public bool HasSummoned { get; set; }

        public Card()
        {
            Abilities = new List<string>();
            Properties = new Dictionary<string, object>();
        }
    }

    [MessagePackObject]
    public class Board
    {
        [Key(0)] public List<List<Card>> PlayerBoards { get; set; }
        [Key(1)] public Dictionary<string, object> GlobalEffects { get; set; }
        [Key(2)] public int MaxBoardSize { get; set; }

        public Board()
        {
            PlayerBoards = new List<List<Card>>();
            GlobalEffects = new Dictionary<string, object>();
            MaxBoardSize = 7;
        }
    }

    [MessagePackObject]
    public class GameResult
    {
        [Key(0)] public string WinnerId { get; set; }
        [Key(1)] public GameEndReason Reason { get; set; }
        [Key(2)] public Dictionary<string, PlayerStats> PlayerStats { get; set; }
        [Key(3)] public TimeSpan GameDuration { get; set; }
        [Key(4)] public DateTime EndTime { get; set; }

        public GameResult()
        {
            PlayerStats = new Dictionary<string, PlayerStats>();
            EndTime = DateTime.UtcNow;
        }
    }

    [MessagePackObject]
    public class PlayerStats
    {
        [Key(0)] public int DamageDealt { get; set; }
        [Key(1)] public int CardsPlayed { get; set; }
        [Key(2)] public int MinionsPlayed { get; set; }
        [Key(3)] public int SpellsCast { get; set; }
        [Key(4)] public int ManaSpent { get; set; }
        [Key(5)] public TimeSpan TotalTurnTime { get; set; }
    }

    [MessagePackObject]
    public class GameStatistics
    {
        [Key(0)] public int TotalTurns { get; set; }
        [Key(1)] public TimeSpan GameDuration { get; set; }
        [Key(2)] public Dictionary<string, int> ActionsPerformed { get; set; }
        [Key(3)] public int TotalDamageDealt { get; set; }

        public GameStatistics()
        {
            ActionsPerformed = new Dictionary<string, int>();
        }
    }

    // =============================================================================
    // SCRIPT ENGINE COMPONENTS
    // =============================================================================

    public class ScriptResult
    {
        public bool Success { get; set; }
        public object ReturnValue { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Dictionary<string, object> ModifiedVariables { get; set; }

        public ScriptResult()
        {
            ModifiedVariables = new Dictionary<string, object>();
        }
    }

    public class GameContext
    {
        public GameState State { get; set; }
        public Player CurrentPlayer { get; set; }
        public Player OpponentPlayer { get; set; }
        public Card SourceCard { get; set; }
        public List<Card> ValidTargets { get; set; }
        public Dictionary<string, object> GameVariables { get; set; }

        public GameContext()
        {
            ValidTargets = new List<Card>();
            GameVariables = new Dictionary<string, object>();
        }
    }

    // =============================================================================
    // GAME INSTANCE CLASS
    // =============================================================================

    public class GameInstance
    {
        public string GameId { get; private set; }
        public DateTime LastActivity { get; private set; }
        
        private readonly IEventStore _eventStore;
        private readonly IRedisCache _stateCache;
        private readonly IScriptEngine _pythonEngine;
        private readonly ILogger _logger;
        
        private GameState _currentState;
        private readonly Dictionary<string, PlayerConnection> _players;
        private readonly object _stateLock = new object();

        public GameInstance(string gameId, IEventStore eventStore, IRedisCache stateCache, 
            IScriptEngine pythonEngine, ILogger logger)
        {
            GameId = gameId;
            _eventStore = eventStore;
            _stateCache = stateCache;
            _pythonEngine = pythonEngine;
            _logger = logger;
            _players = new Dictionary<string, PlayerConnection>();
            LastActivity = DateTime.UtcNow;
        }

        public async Task InitializeAsync()
        {
            // Try to load existing state from cache
            _currentState = await _stateCache.GetGameStateAsync(GameId);
            
            if (_currentState == null)
            {
                // Create new game state
                _currentState = new GameState
                {
                    GameId = GameId,
                    Phase = GamePhase.WaitingForPlayers,
                    TurnNumber = 0,
                    TurnTimeRemaining = 30f
                };
                
                await _stateCache.UpdateGameStateAsync(GameId, new GameStateChange());
            }
        }

        public async Task<bool> AddPlayerAsync(string playerId, NetworkConnectionToClient connection)
        {
            lock (_stateLock)
            {
                if (_players.Count >= 2 || _players.ContainsKey(playerId))
                    return false;

                _players[playerId] = new PlayerConnection
                {
                    PlayerId = playerId,
                    Connection = connection,
                    CurrentGameId = GameId,
                    IsAuthenticated = true
                };

                // Add player to game state
                if (_currentState.GetPlayer(playerId) == null)
                {
                    _currentState.Players.Add(new Player
                    {
                        PlayerId = playerId,
                        Health = 30,
                        MaxHealth = 30,
                        CurrentMana = 1,
                        MaxMana = 1,
                        Status = PlayerStatus.Active
                    });
                }

                LastActivity = DateTime.UtcNow;
            }

            // Start game if we have enough players
            if (_players.Count == 2 && _currentState.Phase == GamePhase.WaitingForPlayers)
            {
                await StartGameAsync();
            }

            return true;
        }

        public async Task RemovePlayerAsync(string playerId)
        {
            lock (_stateLock)
            {
                _players.Remove(playerId);
                
                var player = _currentState.GetPlayer(playerId);
                if (player != null)
                {
                    player.Status = PlayerStatus.Disconnected;
                }

                LastActivity = DateTime.UtcNow;
            }

            // Handle game abandonment
            if (_players.Count == 0)
            {
                await CleanupAsync();
            }
        }

        public async Task<GameState> GetGameStateAsync()
        {
            return _currentState;
        }

        public async Task<GameContext> GetGameContextAsync()
        {
            var currentPlayer = _currentState.GetPlayer(_currentState.CurrentPlayerId);
            var opponent = _currentState.Players.FirstOrDefault(p => p.PlayerId != _currentState.CurrentPlayerId);

            return new GameContext
            {
                State = _currentState,
                CurrentPlayer = currentPlayer,
                OpponentPlayer = opponent,
                GameVariables = _currentState.GameVariables
            };
        }

        public async Task<Card> GetCardAsync(string cardId)
        {
            // Find card in any player's collection
            foreach (var player in _currentState.Players)
            {
                var card = player.Hand.FirstOrDefault(c => c.CardId == cardId) ??
                          player.Board.FirstOrDefault(c => c.CardId == cardId) ??
                          player.Deck.FirstOrDefault(c => c.CardId == cardId) ??
                          player.Graveyard.FirstOrDefault(c => c.CardId == cardId);
                
                if (card != null)
                    return card;
            }

            return null;
        }

        public async Task<GameStateChange> ApplyActionAsync(GameAction action, ScriptResult scriptResult)
        {
            var stateChange = new GameStateChange();
            
            lock (_stateLock)
            {
                LastActivity = DateTime.UtcNow;
                
                // Apply action based on type
                switch (action.ActionType)
                {
                    case "PlayCard":
                        ApplyPlayCardAction(action, scriptResult, stateChange);
                        break;
                    case "EndTurn":
                        ApplyEndTurnAction(action, stateChange);
                        break;
                    case "UseAbility":
                        ApplyAbilityAction(action, scriptResult, stateChange);
                        break;
                }

                // Check for game end conditions
                CheckGameEndConditions(stateChange);
            }

            return stateChange;
        }

        private void ApplyPlayCardAction(GameAction action, ScriptResult scriptResult, GameStateChange stateChange)
        {
            var player = _currentState.GetPlayer(action.PlayerId);
            var card = player.Hand.FirstOrDefault(c => c.CardId == action.CardId);
            
            if (card != null)
            {
                // Remove from hand
                player.Hand.Remove(card);
                player.CurrentMana -= card.ManaCost;

                // Add to board if it's a minion
                if (card.Type == "Minion")
                {
                    card.HasSummoned = true;
                    card.CanAttack = false; // Summoning sickness
                    player.Board.Add(card);
                }
                else
                {
                    // Spell goes to graveyard
                    player.Graveyard.Add(card);
                }

                // Apply script results if any
                if (scriptResult?.Success == true && scriptResult.ModifiedVariables != null)
                {
                    foreach (var kvp in scriptResult.ModifiedVariables)
                    {
                        stateChange.ChangedVariables[kvp.Key] = kvp.Value;
                    }
                }

                stateChange.Modifications.Add(new StateModification
                {
                    Type = "CardPlayed",
                    PlayerId = action.PlayerId,
                    CardId = action.CardId,
                    FromZone = "Hand",
                    ToZone = card.Type == "Minion" ? "Board" : "Graveyard"
                });
            }
        }

        private void ApplyEndTurnAction(GameAction action, GameStateChange stateChange)
        {
            // Switch to next player
            var currentPlayerIndex = _currentState.Players.FindIndex(p => p.PlayerId == _currentState.CurrentPlayerId);
            var nextPlayerIndex = (currentPlayerIndex + 1) % _currentState.Players.Count;
            
            _currentState.CurrentPlayerId = _currentState.Players[nextPlayerIndex].PlayerId;
            _currentState.TurnNumber++;
            _currentState.TurnTimeRemaining = 30f;

            // Start of turn effects
            var nextPlayer = _currentState.Players[nextPlayerIndex];
            nextPlayer.MaxMana = Math.Min(10, nextPlayer.MaxMana + 1);
            nextPlayer.CurrentMana = nextPlayer.MaxMana;

            // Draw a card
            if (nextPlayer.Deck.Count > 0)
            {
                var drawnCard = nextPlayer.Deck[0];
                nextPlayer.Deck.RemoveAt(0);
                nextPlayer.Hand.Add(drawnCard);

                stateChange.Modifications.Add(new StateModification
                {
                    Type = "CardDrawn",
                    PlayerId = nextPlayer.PlayerId,
                    CardId = drawnCard.CardId,
                    FromZone = "Deck",
                    ToZone = "Hand"
                });
            }

            // Remove summoning sickness
            foreach (var minion in nextPlayer.Board)
            {
                minion.CanAttack = true;
            }

            stateChange.Modifications.Add(new StateModification
            {
                Type = "TurnEnded",
                PlayerId = action.PlayerId
            });
        }

        private void ApplyAbilityAction(GameAction action, ScriptResult scriptResult, GameStateChange stateChange)
        {
            // Handle ability activation through script results
            if (scriptResult?.Success == true && scriptResult.ModifiedVariables != null)
            {
                foreach (var kvp in scriptResult.ModifiedVariables)
                {
                    stateChange.ChangedVariables[kvp.Key] = kvp.Value;
                }
            }
        }

        private void CheckGameEndConditions(GameStateChange stateChange)
        {
            foreach (var player in _currentState.Players)
            {
                if (player.Health <= 0)
                {
                    var opponent = _currentState.Players.FirstOrDefault(p => p.PlayerId != player.PlayerId);
                    
                    stateChange.IsGameEnded = true;
                    stateChange.GameResult = new GameResult
                    {
                        WinnerId = opponent?.PlayerId,
                        Reason = GameEndReason.HealthDepleted,
                        GameDuration = DateTime.UtcNow - _currentState.LastUpdate
                    };
                    break;
                }
            }
        }

        public List<PlayerConnection> GetConnectedPlayers()
        {
            return _players.Values.ToList();
        }

        public int GetPlayerCount()
        {
            return _players.Count;
        }

        private async Task StartGameAsync()
        {
            _currentState.Phase = GamePhase.InProgress;
            _currentState.CurrentPlayerId = _currentState.Players[0].PlayerId;
            _currentState.TurnNumber = 1;

            // Initialize player decks, hands, etc.
            foreach (var player in _currentState.Players)
            {
                // Draw initial hand (would normally load from deck data)
                for (int i = 0; i < 3; i++)
                {
                    if (player.Deck.Count > 0)
                    {
                        var card = player.Deck[0];
                        player.Deck.RemoveAt(0);
                        player.Hand.Add(card);
                    }
                }
            }

            await _stateCache.UpdateGameStateAsync(GameId, new GameStateChange());
        }

        public async Task CleanupAsync()
        {
            // Save final state
            if (_currentState != null)
            {
                await _stateCache.UpdateGameStateAsync(GameId, new GameStateChange());
            }

            // Clear connections
            _players.Clear();
        }
    }

    // =============================================================================
    // ENUMS
    // =============================================================================

    public enum GamePhase
    {
        WaitingForPlayers,
        InProgress,
        Ended
    }

    public enum PlayerStatus
    {
        Active,
        Disconnected,
        Spectating
    }

    public enum GameEndReason
    {
        HealthDepleted,
        Concede,
        Timeout,
        Disconnect
    }

    // =============================================================================
    // UTILITY CLASSES
    // =============================================================================

    [MessagePackObject]
    public class StateModification
    {
        [Key(0)] public string Type { get; set; }
        [Key(1)] public string PlayerId { get; set; }
        [Key(2)] public string CardId { get; set; }
        [Key(3)] public string FromZone { get; set; }
        [Key(4)] public string ToZone { get; set; }
        [Key(5)] public Dictionary<string, object> Properties { get; set; }

        public StateModification()
        {
            Properties = new Dictionary<string, object>();
        }
    }

    [MessagePackObject]
    public class Animation
    {
        [Key(0)] public string Type { get; set; }
        [Key(1)] public string TargetId { get; set; }
        [Key(2)] public float Duration { get; set; }
        [Key(3)] public Dictionary<string, object> Parameters { get; set; }

        public Animation()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
