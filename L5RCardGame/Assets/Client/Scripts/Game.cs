using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class Game : MonoBehaviour
    {
        [Header("Game Settings")]
        public string gameId = "game_001";
        public string gameName = "L5R Card Game";
        public bool allowSpectators = true;

        [Header("Game State")]
        public bool gameStarted = false;
        public int roundNumber = 0;
        public string currentPhase = "Setup";

        // Players in the game
        private Dictionary<string, Player> players = new Dictionary<string, Player>();

        void Start()
        {
            Debug.Log("L5R Card Game Started!");
            Debug.Log($"Game ID: {gameId}");
            Debug.Log($"Game Name: {gameName}");

            InitializeGame();
        }

        void InitializeGame()
        {
            Debug.Log("Initializing game systems...");

            // TODO: Initialize card data
            // TODO: Initialize network connection
            // TODO: Set up game board

            Debug.Log("Game initialization complete!");
        }

        public void StartGame()
        {
            if (!gameStarted)
            {
                gameStarted = true;
                roundNumber = 1;
                currentPhase = "Dynasty";
                Debug.Log("Game has started! Round 1, Dynasty Phase");
            }
        }

        public void AddPlayer(string playerName)
        {
            if (!players.ContainsKey(playerName))
            {
                // We'll create the Player class later
                Debug.Log($"Player {playerName} joined the game!");
            }
        }
    }
}