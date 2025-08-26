using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Interface for game events
    /// </summary>
    public interface IGameEvent
    {
        string Name { get; }
        BaseCard Card { get; }
        Ring Ring { get; }
        string Phase { get; }
        AbilityContext Context { get; }
        bool cancelled { get; set; }
        void Cancel();
        IGameEvent GetResolutionEvent();
        bool IsCancelled();
        bool IsResolved();
        void Execute();
    }
}
