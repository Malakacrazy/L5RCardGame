using UnityEngine;

namespace L5RGame
{
    public class Player
    {
        public string playerName;
        public int honor = 10;
        public int fate = 7;

        public Player(string name)
        {
            playerName = name;
        }
    }
}