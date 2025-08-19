using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Interface for all game steps that can be executed in the pipeline
    /// </summary>
    public interface IGameStep
    {
        /// <summary>
        /// Execute this step. Return false to pause pipeline execution.
        /// </summary>
        /// <returns>True to continue to next step, false to pause pipeline</returns>
        bool Continue();

        /// <summary>
        /// Check if this step is complete
        /// </summary>
        /// <returns>True if step is complete</returns>
        bool IsComplete();

        /// <summary>
        /// Cancel this step if possible
        /// </summary>
        void CancelStep();

        /// <summary>
        /// Queue a sub-step within this step
        /// </summary>
        /// <param name="step">Step to queue</param>
        void QueueStep(IGameStep step);

        /// <summary>
        /// Handle card being clicked
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="card">Card that was clicked</param>
        /// <returns>True if handled, false otherwise</returns>
        bool OnCardClicked(Player player, BaseCard card);

        /// <summary>
        /// Handle ring being clicked
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="ring">Ring that was clicked</param>
        /// <returns>True if handled, false otherwise</returns>
        bool OnRingClicked(Player player, Ring ring);

        /// <summary>
        /// Handle menu command
        /// </summary>
        /// <param name="player">Player who issued command</param>
        /// <param name="arg">Command argument</param>
        /// <param name="uuid">Object UUID</param>
        /// <param name="method">Method name</param>
        /// <returns>True if handled, false otherwise</returns>
        bool OnMenuCommand(Player player, string arg, string uuid, string method);

        /// <summary>
        /// Get debug information about this step
        /// </summary>
        /// <returns>Debug info object</returns>
        object GetDebugInfo();

        /// <summary>
        /// Get the name of this step for debugging
        /// </summary>
        string StepName { get; }
    }

    /// <summary>
    /// Manages the execution flow of the game through a series of steps.
    /// Handles queuing, player interactions, and step lifecycle management.
    /// </summary>
    public class GamePipeline : MonoBehaviour
    {
        [Header("Pipeline State")]
        [SerializeField] private List<IGameStep> pipeline = new List<IGameStep>();
        [SerializeField] private List<IGameStep> queue = new List<IGameStep>();
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool enableStepTracing = false;

        /// <summary>
        /// Reference to the game instance
        /// </summary>
        public Game game { get; private set; }

        /// <summary>
        /// Current number of steps in the pipeline
        /// </summary>
        public int Length => pipeline.Count;

        /// <summary>
        /// Initialize the pipeline with initial steps
        /// </summary>
        /// <param name="steps">Initial steps to execute</param>
        public void Initialize(IGameStep steps = null)
        {
            game = GetComponent<Game>();
            pipeline.Clear();
            queue.Clear();

            if (steps != null)
            {
                if (steps is IEnumerable<IGameStep> stepList)
                {
                    pipeline.AddRange(stepList);
                }
                else
                {
                    pipeline.Add(steps);
                }
            }

            if (enableDebugLogging)
                Debug.Log($"🔄 GamePipeline initialized with {pipeline.Count} steps");
        }

        /// <summary>
        /// Initialize with a list of steps
        /// </summary>
        /// <param name="steps">List of steps to execute</param>
        public void Initialize(List<IGameStep> steps)
        {
            game = GetComponent<Game>();
            pipeline = steps?.ToList() ?? new List<IGameStep>();
            queue.Clear();

            if (enableDebugLogging)
                Debug.Log($"🔄 GamePipeline initialized with {pipeline.Count} steps");
        }

        /// <summary>
        /// Get the current step, resolving factory functions if needed
        /// </summary>
        /// <returns>Current executable step</returns>
        public IGameStep GetCurrentStep()
        {
            var step = pipeline.FirstOrDefault();
            
            // Handle factory functions (steps that create other steps)
            if (step is System.Func<IGameStep> factory)
            {
                var createdStep = factory();
                pipeline[0] = createdStep;
                
                if (enableStepTracing)
                    Debug.Log($"🔄 Factory step created: {createdStep.StepName}");
                    
                return createdStep;
            }
            
            return step;
        }

        /// <summary>
        /// Queue a new step for execution
        /// </summary>
        /// <param name="step">Step to queue</param>
        public void QueueStep(IGameStep step)
        {
            if (step == null)
            {
                Debug.LogWarning("⚠️ Attempted to queue null step");
                return;
            }

            if (pipeline.Count == 0)
            {
                // No current steps, add to front of pipeline
                pipeline.Insert(0, step);
                
                if (enableStepTracing)
                    Debug.Log($"🔄 Step queued to empty pipeline: {step.StepName}");
            }
            else
            {
                var currentStep = GetCurrentStep();
                
                // Try to queue within the current step first
                if (currentStep != null)
                {
                    try
                    {
                        currentStep.QueueStep(step);
                        
                        if (enableStepTracing)
                            Debug.Log($"🔄 Step queued within {currentStep.StepName}: {step.StepName}");
                        
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"⚠️ Current step couldn't queue sub-step: {e.Message}");
                    }
                }
                
                // Fallback to main queue
                queue.Add(step);
                
                if (enableStepTracing)
                    Debug.Log($"🔄 Step queued to main queue: {step.StepName}");
            }
        }

        /// <summary>
        /// Cancel the current step
        /// </summary>
        public void CancelStep()
        {
            if (pipeline.Count == 0)
            {
                if (enableDebugLogging)
                    Debug.Log("🔄 No step to cancel");
                return;
            }

            var step = GetCurrentStep();
            if (step == null) return;

            try
            {
                if (step.IsComplete())
                {
                    // Step is already complete, just remove it
                    pipeline.RemoveAt(0);
                    
                    if (enableStepTracing)
                        Debug.Log($"🔄 Completed step removed: {step.StepName}");
                }
                else
                {
                    // Try to cancel the step
                    step.CancelStep();
                    
                    // Check if cancellation completed the step
                    if (step.IsComplete())
                    {
                        pipeline.RemoveAt(0);
                        
                        if (enableStepTracing)
                            Debug.Log($"🔄 Step cancelled and removed: {step.StepName}");
                    }
                    else
                    {
                        if (enableStepTracing)
                            Debug.Log($"🔄 Step cancelled but not complete: {step.StepName}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error cancelling step {step.StepName}: {e.Message}");
                // Force remove problematic step
                pipeline.RemoveAt(0);
            }
        }

        /// <summary>
        /// Handle card click events
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="card">Card that was clicked</param>
        /// <returns>True if handled by current step</returns>
        public bool HandleCardClicked(Player player, BaseCard card)
        {
            if (pipeline.Count == 0) return false;

            var step = GetCurrentStep();
            if (step == null) return false;

            try
            {
                bool handled = step.OnCardClicked(player, card);
                
                if (enableStepTracing && handled)
                    Debug.Log($"🔄 Card click handled by {step.StepName}: {card.name}");
                
                return handled;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error handling card click in {step.StepName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle ring click events
        /// </summary>
        /// <param name="player">Player who clicked</param>
        /// <param name="ring">Ring that was clicked</param>
        /// <returns>True if handled by current step</returns>
        public bool HandleRingClicked(Player player, Ring ring)
        {
            if (pipeline.Count == 0) return false;

            var step = GetCurrentStep();
            if (step == null) return false;

            try
            {
                bool handled = step.OnRingClicked(player, ring);
                
                if (enableStepTracing && handled)
                    Debug.Log($"🔄 Ring click handled by {step.StepName}: {ring.element}");
                
                return handled;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error handling ring click in {step.StepName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle menu command events
        /// </summary>
        /// <param name="player">Player who issued command</param>
        /// <param name="arg">Command argument</param>
        /// <param name="uuid">Object UUID</param>
        /// <param name="method">Method name</param>
        /// <returns>True if handled by current step</returns>
        public bool HandleMenuCommand(Player player, string arg, string uuid, string method)
        {
            if (pipeline.Count == 0) return false;

            var step = GetCurrentStep();
            if (step == null) return false;

            try
            {
                bool handled = step.OnMenuCommand(player, arg, uuid, method);
                
                if (enableStepTracing && handled)
                    Debug.Log($"🔄 Menu command handled by {step.StepName}: {method}");
                
                return handled;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error handling menu command in {step.StepName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Continue pipeline execution
        /// </summary>
        /// <returns>True if pipeline completed, false if paused</returns>
        public bool Continue()
        {
            // Merge queue into pipeline
            if (queue.Count > 0)
            {
                pipeline.InsertRange(0, queue);
                queue.Clear();
                
                if (enableStepTracing)
                    Debug.Log($"🔄 Merged {queue.Count} queued steps into pipeline");
            }

            int iterations = 0;
            const int maxIterations = 1000; // Prevent infinite loops

            while (pipeline.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                var currentStep = GetCurrentStep();
                if (currentStep == null)
                {
                    // Remove null step and continue
                    pipeline.RemoveAt(0);
                    continue;
                }

                try
                {
                    if (enableStepTracing)
                        Debug.Log($"🔄 Executing step: {currentStep.StepName}");

                    // Execute the step
                    bool continueExecution = currentStep.Continue();

                    if (!continueExecution)
                    {
                        // Step wants to pause execution
                        if (queue.Count == 0)
                        {
                            if (enableStepTracing)
                                Debug.Log($"🔄 Pipeline paused at: {currentStep.StepName}");
                            return false;
                        }
                        // There are queued steps, so continue with them
                    }
                    else
                    {
                        // Step completed, remove it
                        pipeline.RemoveAt(0);
                        
                        if (enableStepTracing)
                            Debug.Log($"🔄 Step completed: {currentStep.StepName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Error executing step {currentStep.StepName}: {e.Message}\n{e.StackTrace}");
                    
                    // Remove problematic step to prevent infinite loop
                    pipeline.RemoveAt(0);
                }

                // Merge any new queued steps
                if (queue.Count > 0)
                {
                    pipeline.InsertRange(0, queue);
                    queue.Clear();
                }
            }

            if (iterations >= maxIterations)
            {
                Debug.LogError($"❌ Pipeline execution exceeded maximum iterations ({maxIterations}). Possible infinite loop!");
                pipeline.Clear(); // Emergency stop
                return false;
            }

            if (enableDebugLogging)
                Debug.Log($"🔄 Pipeline execution completed after {iterations} iterations");

            return true;
        }

        /// <summary>
        /// Get debug information about the current pipeline state
        /// </summary>
        /// <returns>Debug information object</returns>
        public PipelineDebugInfo GetDebugInfo()
        {
            return new PipelineDebugInfo
            {
                pipelineSteps = pipeline.Select(GetDebugInfoForStep).ToList(),
                queuedSteps = queue.Select(GetDebugInfoForStep).ToList(),
                currentStepName = GetCurrentStep()?.StepName ?? "None",
                totalSteps = pipeline.Count,
                queuedCount = queue.Count
            };
        }

        /// <summary>
        /// Get debug information for a specific step
        /// </summary>
        /// <param name="step">Step to get info for</param>
        /// <returns>Debug info object</returns>
        private object GetDebugInfoForStep(IGameStep step)
        {
            if (step == null) return "null";

            try
            {
                // Check if step has its own pipeline (nested pipeline)
                if (step is INestedPipeline nestedPipeline)
                {
                    return new Dictionary<string, object>
                    {
                        { step.StepName, nestedPipeline.GetNestedDebugInfo() }
                    };
                }

                // Use step's own debug info if available
                var debugInfo = step.GetDebugInfo();
                if (debugInfo != null)
                {
                    return debugInfo;
                }

                // Handle factory functions
                if (step is System.Func<IGameStep> factory)
                {
                    return $"Factory: {factory.Method.Name}";
                }

                // Default to step name
                return step.StepName;
            }
            catch (Exception e)
            {
                return $"Error getting debug info: {e.Message}";
            }
        }

        /// <summary>
        /// Log debug information to console
        /// </summary>
        public void ConsoleDebugInfo()
        {
            if (!enableDebugLogging) return;

            var debugInfo = GetDebugInfo();
            Debug.Log($"🔄 Pipeline Debug Info:\n" +
                     $"Current Step: {debugInfo.currentStepName}\n" +
                     $"Pipeline Steps: {debugInfo.totalSteps}\n" +
                     $"Queued Steps: {debugInfo.queuedCount}");

            // Find the deepest nested pipeline for detailed logging
            var pipeline = this;
            var step = pipeline.pipeline.FirstOrDefault();
            
            while (step is INestedPipeline nestedStep)
            {
                var nested = nestedStep.GetNestedPipeline();
                if (nested?.pipeline.Count > 0)
                {
                    pipeline = nested;
                    step = nested.pipeline.FirstOrDefault();
                }
                else
                {
                    break;
                }
            }

            if (pipeline != this)
            {
                Debug.Log($"🔄 Deepest Pipeline: {pipeline.Length} steps");
            }
        }

        /// <summary>
        /// Clear all steps and reset pipeline
        /// </summary>
        public void Reset()
        {
            pipeline.Clear();
            queue.Clear();
            
            if (enableDebugLogging)
                Debug.Log("🔄 Pipeline reset");
        }

        /// <summary>
        /// Check if pipeline is currently executing
        /// </summary>
        /// <returns>True if there are steps to execute</returns>
        public bool IsActive()
        {
            return pipeline.Count > 0 || queue.Count > 0;
        }

        /// <summary>
        /// Get the current step without resolving factories
        /// </summary>
        /// <returns>Raw current step</returns>
        public IGameStep PeekCurrentStep()
        {
            return pipeline.FirstOrDefault();
        }

        /// <summary>
        /// Force completion of current step (emergency method)
        /// </summary>
        public void ForceCompleteCurrentStep()
        {
            if (pipeline.Count > 0)
            {
                var step = pipeline[0];
                pipeline.RemoveAt(0);
                
                Debug.LogWarning($"⚠️ Force completed step: {step?.StepName ?? "Unknown"}");
            }
        }

        /// <summary>
        /// Unity Update method for debugging
        /// </summary>
        private void Update()
        {
            // Debug hotkeys (only in development builds)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ConsoleDebugInfo();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                enableStepTracing = !enableStepTracing;
                Debug.Log($"🔄 Step tracing: {(enableStepTracing ? "Enabled" : "Disabled")}");
            }
#endif
        }

        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        private void OnDestroy()
        {
            Reset();
            Debug.Log("🔄 GamePipeline destroyed");
        }
    }

    /// <summary>
    /// Interface for steps that contain nested pipelines
    /// </summary>
    public interface INestedPipeline
    {
        /// <summary>
        /// Get the nested pipeline for debug purposes
        /// </summary>
        /// <returns>Nested pipeline instance</returns>
        GamePipeline GetNestedPipeline();

        /// <summary>
        /// Get debug info for the nested pipeline
        /// </summary>
        /// <returns>Debug info object</returns>
        object GetNestedDebugInfo();
    }

    /// <summary>
    /// Debug information structure for pipeline state
    /// </summary>
    [System.Serializable]
    public class PipelineDebugInfo
    {
        public List<object> pipelineSteps;
        public List<object> queuedSteps;
        public string currentStepName;
        public int totalSteps;
        public int queuedCount;
    }

    /// <summary>
    /// Base implementation for simple game steps
    /// </summary>
    public abstract class BaseGameStep : IGameStep
    {
        protected Game game;
        protected bool completed = false;

        public abstract string StepName { get; }

        protected BaseGameStep(Game gameInstance)
        {
            game = gameInstance;
        }

        public abstract bool Continue();

        public virtual bool IsComplete()
        {
            return completed;
        }

        public virtual void CancelStep()
        {
            completed = true;
        }

        public virtual void QueueStep(IGameStep step)
        {
            game.pipeline.QueueStep(step);
        }

        public virtual bool OnCardClicked(Player player, BaseCard card)
        {
            return false; // Not handled by default
        }

        public virtual bool OnRingClicked(Player player, Ring ring)
        {
            return false; // Not handled by default
        }

        public virtual bool OnMenuCommand(Player player, string arg, string uuid, string method)
        {
            return false; // Not handled by default
        }

        public virtual object GetDebugInfo()
        {
            return new
            {
                stepType = StepName,
                completed = completed,
                gamePhase = game.currentPhase
            };
        }
    }

    /// <summary>
    /// Simple step that executes a function
    /// </summary>
    public class SimpleStep : BaseGameStep
    {
        private System.Func<bool> action;
        private string stepName;

        public override string StepName => stepName ?? "SimpleStep";

        public SimpleStep(Game gameInstance, System.Func<bool> stepAction, string name = null) 
            : base(gameInstance)
        {
            action = stepAction;
            stepName = name;
        }

        public override bool Continue()
        {
            if (completed) return true;

            try
            {
                completed = action?.Invoke() ?? true;
                return completed;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error in SimpleStep {StepName}: {e.Message}");
                completed = true;
                return true;
            }
        }

        public override object GetDebugInfo()
        {
            return new
            {
                stepType = StepName,
                completed = completed,
                hasAction = action != null
            };
        }
    }

    /// <summary>
    /// Extension methods for pipeline management
    /// </summary>
    public static class PipelineExtensions
    {
        /// <summary>
        /// Queue a simple function as a step
        /// </summary>
        /// <param name="pipeline">Pipeline to queue on</param>
        /// <param name="action">Function to execute</param>
        /// <param name="stepName">Name for debugging</param>
        public static void QueueSimpleStep(this GamePipeline pipeline, System.Func<bool> action, string stepName = null)
        {
            pipeline.QueueStep(new SimpleStep(pipeline.game, action, stepName));
        }

        /// <summary>
        /// Queue multiple steps at once
        /// </summary>
        /// <param name="pipeline">Pipeline to queue on</param>
        /// <param name="steps">Steps to queue</param>
        public static void QueueSteps(this GamePipeline pipeline, IEnumerable<IGameStep> steps)
        {
            foreach (var step in steps)
            {
                pipeline.QueueStep(step);
            }
        }

        /// <summary>
        /// Queue a step factory function
        /// </summary>
        /// <param name="pipeline">Pipeline to queue on</param>
        /// <param name="factory">Factory function that creates a step</param>
        public static void QueueStepFactory(this GamePipeline pipeline, System.Func<IGameStep> factory)
        {
            pipeline.QueueStep(factory as IGameStep);
        }
    }
}