using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game steps in the L5R pipeline system.
    /// Provides common functionality for handling user interactions and step progression.
    /// </summary>
    [System.Serializable]
    public abstract class BaseStep : IGameStep
    {
        [Header("Base Step Configuration")]
        [SerializeField] protected Game game;
        [SerializeField] protected string stepName;
        [SerializeField] protected bool isComplete = false;
        [SerializeField] protected bool canCancel = true;
        [SerializeField] protected float timeoutDuration = 0f; // 0 = no timeout
        
        // Step state
        protected DateTime stepStartTime;
        protected bool hasStarted = false;
        protected Exception lastError;
        
        // Events
        public event Action<BaseStep> OnStepStarted;
        public event Action<BaseStep> OnStepCompleted;
        public event Action<BaseStep, Exception> OnStepError;
        public event Action<BaseStep> OnStepCancelled;
        public event Action<BaseStep> OnStepTimeout;
        
        #region Properties
        
        /// <summary>
        /// Reference to the game instance
        /// </summary>
        public Game Game => game;
        
        /// <summary>
        /// Name of this step for debugging
        /// </summary>
        public virtual string StepName => !string.IsNullOrEmpty(stepName) ? stepName : GetType().Name;
        
        /// <summary>
        /// Whether this step has completed execution
        /// </summary>
        public virtual bool IsComplete => isComplete;
        
        /// <summary>
        /// Whether this step can be cancelled
        /// </summary>
        public virtual bool CanCancel => canCancel;
        
        /// <summary>
        /// Whether this step has started execution
        /// </summary>
        public bool HasStarted => hasStarted;
        
        /// <summary>
        /// Time when this step started
        /// </summary>
        public DateTime StartTime => stepStartTime;
        
        /// <summary>
        /// How long this step has been running
        /// </summary>
        public TimeSpan ElapsedTime => hasStarted ? DateTime.Now - stepStartTime : TimeSpan.Zero;
        
        /// <summary>
        /// Whether this step has timed out
        /// </summary>
        public bool HasTimedOut => timeoutDuration > 0 && ElapsedTime.TotalSeconds > timeoutDuration;
        
        /// <summary>
        /// Last error that occurred in this step
        /// </summary>
        public Exception LastError => lastError;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Constructor for BaseStep
        /// </summary>
        /// <param name="game">Game instance</param>
        public BaseStep(Game game)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            Initialize();
        }
        
        /// <summary>
        /// Constructor with custom step name
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="stepName">Custom step name</param>
        public BaseStep(Game game, string stepName) : this(game)
        {
            this.stepName = stepName;
        }
        
        /// <summary>
        /// Initialize the step
        /// </summary>
        protected virtual void Initialize()
        {
            isComplete = false;
            hasStarted = false;
            lastError = null;
        }
        
        #endregion
        
        #region Core Step Methods
        
        /// <summary>
        /// Execute this step. Override in derived classes to implement step logic.
        /// </summary>
        /// <returns>True if step completed successfully, false if it needs more time</returns>
        public virtual bool Execute()
        {
            try
            {
                if (!hasStarted)
                {
                    StartStep();
                }
                
                // Check for timeout
                if (HasTimedOut)
                {
                    HandleTimeout();
                    return true; // Complete with timeout
                }
                
                // Execute step logic
                bool completed = Continue();
                
                if (completed)
                {
                    CompleteStep();
                }
                
                return completed;
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return true; // Complete with error
            }
        }
        
        /// <summary>
        /// Continue step execution. Override in derived classes.
        /// </summary>
        /// <returns>True if step is complete, false if it should continue</returns>
        public virtual bool Continue()
        {
            // Default implementation - step completes immediately
            return true;
        }
        
        /// <summary>
        /// Cancel this step if possible
        /// </summary>
        /// <returns>True if step was cancelled successfully</returns>
        public virtual bool Cancel()
        {
            if (!CanCancel || IsComplete)
                return false;
            
            try
            {
                OnStepCancellation();
                isComplete = true;
                
                LogStep("Step cancelled");
                OnStepCancelled?.Invoke(this);
                
                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }
        
        /// <summary>
        /// Reset this step to be executed again
        /// </summary>
        public virtual void Reset()
        {
            isComplete = false;
            hasStarted = false;
            lastError = null;
            
            OnStepReset();
            LogStep("Step reset");
        }
        
        #endregion
        
        #region User Interaction Methods
        
        /// <summary>
        /// Handle card click events. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="card">Card that was clicked</param>
        /// <returns>True if the click was handled</returns>
        public virtual bool OnCardClicked(Player player, BaseCard card)
        {
            // Default implementation - no handling
            return false;
        }
        
        /// <summary>
        /// Handle ring click events. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="ring">Ring that was clicked</param>
        /// <returns>True if the click was handled</returns>
        public virtual bool OnRingClicked(Player player, Ring ring)
        {
            // Default implementation - no handling
            return false;
        }
        
        /// <summary>
        /// Handle menu command events. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who issued the command</param>
        /// <param name="command">Command that was issued</param>
        /// <param name="args">Additional command arguments</param>
        /// <returns>True if the command was handled</returns>
        public virtual bool OnMenuCommand(Player player, string command, object[] args = null)
        {
            // Default implementation - no handling
            return false;
        }
        
        /// <summary>
        /// Handle province click events. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="province">Province that was clicked</param>
        /// <returns>True if the click was handled</returns>
        public virtual bool OnProvinceClicked(Player player, BaseCard province)
        {
            // Default implementation - no handling
            return false;
        }
        
        /// <summary>
        /// Handle generic UI button clicks. Override in derived classes if needed.
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="buttonId">ID of the button clicked</param>
        /// <param name="args">Additional button arguments</param>
        /// <returns>True if the click was handled</returns>
        public virtual bool OnButtonClicked(Player player, string buttonId, object[] args = null)
        {
            // Default implementation - no handling
            return false;
        }
        
        #endregion
        
        #region Step Lifecycle Methods
        
        /// <summary>
        /// Called when the step starts. Override for custom start logic.
        /// </summary>
        protected virtual void OnStepStart()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called when the step completes successfully. Override for custom completion logic.
        /// </summary>
        protected virtual void OnStepComplete()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called when the step is cancelled. Override for custom cancellation logic.
        /// </summary>
        protected virtual void OnStepCancellation()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called when the step is reset. Override for custom reset logic.
        /// </summary>
        protected virtual void OnStepReset()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called when the step times out. Override for custom timeout logic.
        /// </summary>
        protected virtual void OnStepTimedOut()
        {
            // Override in derived classes
        }
        
        #endregion
        
        #region Internal Step Management
        
        /// <summary>
        /// Start the step execution
        /// </summary>
        private void StartStep()
        {
            hasStarted = true;
            stepStartTime = DateTime.Now;
            
            LogStep("Step started");
            
            OnStepStart();
            OnStepStarted?.Invoke(this);
        }
        
        /// <summary>
        /// Complete the step execution
        /// </summary>
        private void CompleteStep()
        {
            isComplete = true;
            
            LogStep($"Step completed (took {ElapsedTime.TotalMilliseconds:F0}ms)");
            
            OnStepComplete();
            OnStepCompleted?.Invoke(this);
        }
        
        /// <summary>
        /// Handle step timeout
        /// </summary>
        private void HandleTimeout()
        {
            isComplete = true;
            
            LogWarning($"Step timed out after {timeoutDuration} seconds");
            
            OnStepTimedOut();
            OnStepTimeout?.Invoke(this);
        }
        
        /// <summary>
        /// Handle step error
        /// </summary>
        private void HandleError(Exception ex)
        {
            lastError = ex;
            isComplete = true;
            
            LogError($"Step error: {ex.Message}");
            
            OnStepError?.Invoke(this, ex);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Set timeout for this step
        /// </summary>
        /// <param name="timeout">Timeout duration in seconds</param>
        public virtual void SetTimeout(float timeout)
        {
            timeoutDuration = Mathf.Max(0f, timeout);
        }
        
        /// <summary>
        /// Check if a player can interact with this step
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if player can interact</returns>
        public virtual bool CanPlayerInteract(Player player)
        {
            if (player == null || IsComplete || !HasStarted)
                return false;
            
            // Default - any player can interact
            return true;
        }
        
        /// <summary>
        /// Get the current active player for this step
        /// </summary>
        /// <returns>Active player, or null if no specific player</returns>
        public virtual Player GetActivePlayer()
        {
            return game?.activePlayer;
        }
        
        /// <summary>
        /// Force complete this step
        /// </summary>
        public virtual void ForceComplete()
        {
            if (!IsComplete)
            {
                isComplete = true;
                LogStep("Step force completed");
                OnStepComplete();
                OnStepCompleted?.Invoke(this);
            }
        }
        
        #endregion
        
        #region Debug and Logging
        
        /// <summary>
        /// Get debug information about this step
        /// </summary>
        /// <returns>Debug information string</returns>
        public virtual string GetDebugInfo()
        {
            var info = $"{StepName}";
            
            if (HasStarted)
            {
                info += $" (Running {ElapsedTime.TotalSeconds:F1}s)";
            }
            
            if (IsComplete)
            {
                info += " [Complete]";
            }
            
            if (HasTimedOut)
            {
                info += " [Timeout]";
            }
            
            if (LastError != null)
            {
                info += " [Error]";
            }
            
            return info;
        }
        
        /// <summary>
        /// Get detailed step information for debugging
        /// </summary>
        /// <returns>Detailed debug information</returns>
        public virtual string GetDetailedDebugInfo()
        {
            var info = GetDebugInfo() + "\n";
            info += $"  Type: {GetType().Name}\n";
            info += $"  Started: {HasStarted}\n";
            info += $"  Complete: {IsComplete}\n";
            info += $"  Can Cancel: {CanCancel}\n";
            
            if (HasStarted)
            {
                info += $"  Start Time: {StartTime:HH:mm:ss.fff}\n";
                info += $"  Elapsed: {ElapsedTime.TotalMilliseconds:F0}ms\n";
            }
            
            if (timeoutDuration > 0)
            {
                info += $"  Timeout: {timeoutDuration}s\n";
                info += $"  Timed Out: {HasTimedOut}\n";
            }
            
            if (LastError != null)
            {
                info += $"  Last Error: {LastError.Message}\n";
            }
            
            return info;
        }
        
        /// <summary>
        /// Log step message
        /// </summary>
        protected virtual void LogStep(string message)
        {
            Debug.Log($"🔄 {StepName}: {message}");
        }
        
        /// <summary>
        /// Log step warning
        /// </summary>
        protected virtual void LogWarning(string message)
        {
            Debug.LogWarning($"⚠️ {StepName}: {message}");
        }
        
        /// <summary>
        /// Log step error
        /// </summary>
        protected virtual void LogError(string message)
        {
            Debug.LogError($"❌ {StepName}: {message}");
        }
        
        #endregion
        
        #region Serialization Support
        
        /// <summary>
        /// Get serializable state of this step
        /// </summary>
        /// <returns>Serializable step state</returns>
        public virtual StepState GetState()
        {
            return new StepState
            {
                stepName = StepName,
                stepType = GetType().Name,
                isComplete = IsComplete,
                hasStarted = HasStarted,
                canCancel = CanCancel,
                elapsedTime = ElapsedTime.TotalSeconds,
                hasError = LastError != null
            };
        }
        
        /// <summary>
        /// Restore step from serializable state
        /// </summary>
        /// <param name="state">State to restore from</param>
        public virtual void RestoreState(StepState state)
        {
            if (state == null) return;
            
            isComplete = state.isComplete;
            hasStarted = state.hasStarted;
            canCancel = state.canCancel;
            
            if (hasStarted)
            {
                stepStartTime = DateTime.Now.AddSeconds(-state.elapsedTime);
            }
        }
        
        #endregion
        
        #region Operator Overloads
        
        public override string ToString()
        {
            return GetDebugInfo();
        }
        
        public override bool Equals(object obj)
        {
            return obj is BaseStep other && 
                   StepName == other.StepName && 
                   GetType() == other.GetType();
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(StepName, GetType());
        }
        
        #endregion
    }
    
    /// <summary>
    /// Serializable state for steps
    /// </summary>
    [System.Serializable]
    public class StepState
    {
        public string stepName;
        public string stepType;
        public bool isComplete;
        public bool hasStarted;
        public bool canCancel;
        public double elapsedTime;
        public bool hasError;
    }
    
    /// <summary>
    /// Interface for game steps
    /// </summary>
    public interface IGameStep
    {
        /// <summary>
        /// Execute the step
        /// </summary>
        /// <returns>True if step completed</returns>
        bool Execute();
        
        /// <summary>
        /// Continue step execution
        /// </summary>
        /// <returns>True if step is complete</returns>
        bool Continue();
        
        /// <summary>
        /// Whether the step is complete
        /// </summary>
        bool IsComplete { get; }
        
        /// <summary>
        /// Whether the step can be cancelled
        /// </summary>
        bool CanCancel { get; }
        
        /// <summary>
        /// Get debug information
        /// </summary>
        /// <returns>Debug info string</returns>
        string GetDebugInfo();
    }
    
    /// <summary>
    /// Simple step implementation for quick one-off steps
    /// </summary>
    public class SimpleStep : BaseStep
    {
        private readonly System.Func<bool> stepFunction;
        private readonly string customName;
        
        /// <summary>
        /// Create a simple step with a function
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="stepFunction">Function to execute</param>
        /// <param name="stepName">Optional custom step name</param>
        public SimpleStep(Game game, System.Func<bool> stepFunction, string stepName = null) 
            : base(game, stepName ?? "SimpleStep")
        {
            this.stepFunction = stepFunction ?? throw new ArgumentNullException(nameof(stepFunction));
            this.customName = stepName;
        }
        
        public override string StepName => customName ?? $"SimpleStep({stepFunction.Method.Name})";
        
        public override bool Continue()
        {
            return stepFunction();
        }
    }
    
    /// <summary>
    /// Extension methods for BaseStep
    /// </summary>
    public static class BaseStepExtensions
    {
        /// <summary>
        /// Execute step with timeout
        /// </summary>
        public static bool ExecuteWithTimeout(this BaseStep step, float timeout)
        {
            step.SetTimeout(timeout);
            return step.Execute();
        }
        
        /// <summary>
        /// Chain multiple steps together
        /// </summary>
        public static BaseStep Then(this BaseStep step, BaseStep nextStep)
        {
            // This would typically be handled by a pipeline system
            // For now, just return the next step
            return nextStep;
        }
        
        /// <summary>
        /// Check if step is still active (started but not complete)
        /// </summary>
        public static bool IsActive(this BaseStep step)
        {
            return step.HasStarted && !step.IsComplete;
        }
        
        /// <summary>
        /// Get step status as string
        /// </summary>
        public static string GetStatus(this BaseStep step)
        {
            if (!step.HasStarted) return "Not Started";
            if (step.IsComplete) return "Complete";
            if (step.HasTimedOut) return "Timed Out";
            if (step.LastError != null) return "Error";
            return "Running";
        }
    }
}
