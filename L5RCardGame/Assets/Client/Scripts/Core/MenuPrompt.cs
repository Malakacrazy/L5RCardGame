using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// General purpose menu prompt that provides contextual buttons to the player.
    /// By specifying a context object, the buttons in the active prompt can call
    /// the corresponding method on the context object. Methods on the context object
    /// should return true in order to complete the prompt.
    /// 
    /// This is the C# equivalent of the JavaScript MenuPrompt class.
    /// </summary>
    public class MenuPrompt : UiPrompt
    {
        #region Fields

        protected Player player;
        protected object context;
        protected MenuPromptProperties properties;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new menu prompt
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="promptPlayer">Player being prompted</param>
        /// <param name="contextObject">Context object containing handler methods</param>
        /// <param name="promptProperties">Prompt configuration properties</param>
        public MenuPrompt(Game game, Player promptPlayer, object contextObject, MenuPromptProperties promptProperties) 
            : base(game)
        {
            player = promptPlayer;
            context = contextObject;
            properties = promptProperties ?? new MenuPromptProperties();

            // Auto-generate waiting prompt title from source if not provided
            if (properties.Source != null && string.IsNullOrEmpty(properties.WaitingPromptTitle))
            {
                var sourceName = GetSourceName(properties.Source);
                properties.WaitingPromptTitle = $"Waiting for opponent to use {sourceName}";
            }
        }

        #endregion

        #region UiPrompt Implementation

        /// <summary>
        /// Determine if the specified player should see the active prompt
        /// </summary>
        /// <param name="checkPlayer">Player to check</param>
        /// <returns>True if player should see active prompt</returns>
        public override bool ActiveCondition(Player checkPlayer)
        {
            return checkPlayer == player;
        }

        /// <summary>
        /// Get the active prompt data for the prompted player
        /// </summary>
        /// <returns>Active prompt configuration</returns>
        public override object ActivePrompt()
        {
            var promptTitle = properties.PromptTitle;
            
            // Use source name as default prompt title if not specified
            if (string.IsNullOrEmpty(promptTitle) && properties.Source != null)
            {
                promptTitle = GetSourceName(properties.Source);
            }

            // Convert to dictionary format expected by UI
            var activePrompt = properties.ActivePrompt.ToDictionary();
            
            // Set or override the prompt title
            if (!string.IsNullOrEmpty(promptTitle))
            {
                activePrompt["promptTitle"] = promptTitle;
            }

            return activePrompt;
        }

        /// <summary>
        /// Get the waiting prompt data for non-active players
        /// </summary>
        /// <returns>Waiting prompt configuration</returns>
        public override object WaitingPrompt()
        {
            var waitingTitle = properties.WaitingPromptTitle ?? "Waiting for opponent";
            
            return new Dictionary<string, object>
            {
                ["menuTitle"] = waitingTitle
            };
        }

        /// <summary>
        /// Handle menu command from player interaction
        /// </summary>
        /// <param name="commandPlayer">Player who issued the command</param>
        /// <param name="arg">Command argument</param>
        /// <param name="method">Method name to call on context object</param>
        /// <returns>True if command was handled</returns>
        public override bool MenuCommand(Player commandPlayer, string arg, string method)
        {
            return MenuCommand(commandPlayer, arg, null, method);
        }

        /// <summary>
        /// Handle menu command with UUID parameter (OnMenuCommand interface method)
        /// This matches the test expectations exactly
        /// </summary>
        /// <param name="commandPlayer">Player who issued the command</param>
        /// <param name="arg">Command argument</param>
        /// <param name="uuid">Object UUID (optional)</param>
        /// <param name="method">Method name to call on context object</param>
        /// <returns>True if command was handled</returns>
        public virtual bool OnMenuCommand(Player commandPlayer, string arg, string uuid, string method)
        {
            // Check if the player is the prompted player (test requirement)
            if (commandPlayer != player)
            {
                return false;
            }

            // Validate that we have a context object
            if (context == null)
            {
                return false;
            }

            // Find the method on the context object
            var methodInfo = FindContextMethod(method);
            if (methodInfo == null)
            {
                return false;
            }

            // Check if the method has a corresponding button (test requirement)
            if (!HasMethodButton(method))
            {
                return false;
            }

            try
            {
                // Prepare method parameters: (player, arg, context)
                var parameters = new object[] { commandPlayer, arg, properties.Context };
                
                // Invoke the method
                var result = methodInfo.Invoke(context, parameters);
                
                // If method returns true, complete the prompt
                if (result is bool boolResult && boolResult)
                {
                    Complete();
                }
                
                // Always return true if we successfully called the method (test requirement)
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"MenuPrompt: Error invoking method '{method}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handle menu command with UUID parameter (legacy interface)
        /// </summary>
        /// <param name="commandPlayer">Player who issued the command</param>
        /// <param name="arg">Command argument</param>
        /// <param name="uuid">Object UUID (optional)</param>
        /// <param name="method">Method name to call on context object</param>
        /// <returns>True if command was handled</returns>
        public virtual bool MenuCommand(Player commandPlayer, string arg, string uuid, string method)
        {
            return OnMenuCommand(commandPlayer, arg, uuid, method);
        }

        /// <summary>
        /// Check if the prompt has a button with the specified method
        /// This matches the test's expectation for checking button existence
        /// </summary>
        /// <param name="method">Method name to check</param>
        /// <returns>True if button with method exists</returns>
        public virtual bool HasMethodButton(string method)
        {
            if (properties?.ActivePrompt?.Buttons == null)
            {
                return false;
            }

            return properties.ActivePrompt.Buttons.Any(button => button.Method == method);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get display name from source object
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Display name</returns>
        protected virtual string GetSourceName(object source)
        {
            if (source == null) return "Unknown";
            
            // Try to get name property
            if (source is BaseCard card)
                return card.name;
            
            var nameProperty = source.GetType().GetProperty("name") ?? 
                              source.GetType().GetProperty("Name");
            if (nameProperty != null)
            {
                var name = nameProperty.GetValue(source)?.ToString();
                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            return source.ToString();
        }

        /// <summary>
        /// Find method on context object with flexible parameter matching
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <returns>MethodInfo if found, null otherwise</returns>
        protected virtual MethodInfo FindContextMethod(string methodName)
        {
            var contextType = context.GetType();
            
            // First try exact match
            var methods = contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == methodName)
                .ToArray();
            
            if (methods.Length == 0)
                return null;
            
            // If multiple methods, prefer the one with the most appropriate signature
            var preferredMethod = methods.FirstOrDefault(m => 
            {
                var parameters = m.GetParameters();
                return parameters.Length >= 1 && parameters.Length <= 3 &&
                       parameters[0].ParameterType == typeof(Player);
            });
            
            return preferredMethod ?? methods[0];
        }

        /// <summary>
        /// Prepare method parameters based on method signature
        /// </summary>
        /// <param name="methodInfo">Method to call</param>
        /// <param name="commandPlayer">Player issuing command</param>
        /// <param name="arg">Command argument</param>
        /// <param name="uuid">Object UUID</param>
        /// <returns>Parameter array</returns>
        protected virtual object[] PrepareMethodParameters(MethodInfo methodInfo, Player commandPlayer, string arg, string uuid)
        {
            var parameters = methodInfo.GetParameters();
            var paramValues = new object[parameters.Length];
            
            // Fill parameters based on their types and positions
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                
                if (i == 0 && param.ParameterType == typeof(Player))
                {
                    paramValues[i] = commandPlayer;
                }
                else if (param.ParameterType == typeof(string))
                {
                    // Use arg for the first string parameter after Player, uuid for the second
                    if (paramValues.Count(p => p is string) == 0)
                        paramValues[i] = arg;
                    else
                        paramValues[i] = uuid;
                }
                else if (param.ParameterType == typeof(object))
                {
                    // Pass context from properties if available
                    paramValues[i] = properties.Context;
                }
                else
                {
                    // Use default value for parameter
                    paramValues[i] = param.HasDefaultValue ? param.DefaultValue : 
                                     param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                }
            }
            
            return paramValues;
        }

        /// <summary>
        /// Check if a button object has the specified method
        /// </summary>
        /// <param name="button">Button object</param>
        /// <param name="method">Method name</param>
        /// <returns>True if button has the method</returns>
        protected virtual bool ButtonHasMethod(object button, string method)
        {
            if (button == null) return false;
            
            // Handle dictionary-style button
            if (button is Dictionary<string, object> buttonDict)
            {
                return buttonDict.ContainsKey("method") && 
                       buttonDict["method"]?.ToString() == method;
            }
            
            // Handle anonymous object or other types
            var buttonType = button.GetType();
            var methodProperty = buttonType.GetProperty("method");
            if (methodProperty != null)
            {
                var buttonMethod = methodProperty.GetValue(button)?.ToString();
                return buttonMethod == method;
            }
            
            return false;
        }

        #endregion

        #region Debug Support

        /// <summary>
        /// Get debug information about this prompt
        /// </summary>
        /// <returns>Debug info string</returns>
        public override string GetDebugInfo()
        {
            var info = $"MenuPrompt for {player?.name ?? "null"}\n";
            info += $"  Context: {context?.GetType().Name ?? "null"}\n";
            info += $"  Prompt Title: {properties.PromptTitle ?? "default"}\n";
            info += $"  Waiting Title: {properties.WaitingPromptTitle ?? "default"}\n";
            info += $"  Source: {GetSourceName(properties.Source)}\n";
            
            if (properties.ActivePrompt?.ContainsKey("buttons") == true)
            {
                var buttons = properties.ActivePrompt["buttons"];
                if (buttons is IEnumerable<object> buttonList)
                {
                    var buttonCount = buttonList.Count();
                    info += $"  Buttons: {buttonCount}\n";
                }
            }
            
            return info;
        }

        #endregion
    }

    /// <summary>
    /// Base class for UI prompts in the L5R game system
    /// This provides the interface that MenuPrompt extends
    /// </summary>
    public abstract class UiPrompt : IGameStep
    {
        #region Fields

        protected Game game;
        protected bool isComplete = false;

        #endregion

        #region Constructor

        protected UiPrompt(Game gameInstance)
        {
            game = gameInstance;
        }

        #endregion

        #region IGameStep Implementation

        public virtual bool Continue()
        {
            return isComplete;
        }

        public virtual bool IsComplete()
        {
            return isComplete;
        }

        public virtual void CancelStep()
        {
            isComplete = true;
        }

        public virtual void QueueStep(IGameStep step)
        {
            game?.QueueStep(step);
        }

        public virtual bool OnCardClicked(Player player, BaseCard card)
        {
            return false;
        }

        public virtual bool OnRingClicked(Player player, Ring ring)
        {
            return false;
        }

        public override bool OnMenuCommand(Player playerParam, string arg, string uuid, string method)
        {
            return ((MenuPrompt)this).OnMenuCommand(playerParam, arg, uuid, method);
        }

        public virtual object GetDebugInfo()
        {
            return GetDebugInfo();
        }

        public virtual string StepName => GetType().Name;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Determine if the specified player should see the active prompt
        /// </summary>
        public abstract bool ActiveCondition(Player player);

        /// <summary>
        /// Get the active prompt data
        /// </summary>
        public abstract object ActivePrompt();

        /// <summary>
        /// Get the waiting prompt data
        /// </summary>
        public abstract object WaitingPrompt();

        /// <summary>
        /// Handle menu command
        /// </summary>
        public abstract bool MenuCommand(Player player, string arg, string method);

        /// <summary>
        /// Get debug information
        /// </summary>
        public abstract string GetDebugInfo();

        #endregion

        #region Protected Methods

        /// <summary>
        /// Mark the prompt as complete
        /// </summary>
        protected virtual void Complete()
        {
            isComplete = true;
        }

        #endregion
    }

    /// <summary>
    /// Properties class for configuring MenuPrompt behavior
    /// </summary>
    [System.Serializable]
    public class MenuPromptProperties
    {
        [Header("Prompt Configuration")]
        [SerializeField] private string promptTitle;
        [SerializeField] private string waitingPromptTitle;
        [SerializeField] private object source;
        [SerializeField] private object context;

        [Header("Active Prompt")]
        [SerializeField] private Dictionary<string, object> activePrompt;

        /// <summary>
        /// Title for the active prompt
        /// </summary>
        public string PromptTitle
        {
            get => promptTitle;
            set => promptTitle = value;
        }

        /// <summary>
        /// Title shown to waiting players
        /// </summary>
        public string WaitingPromptTitle
        {
            get => waitingPromptTitle;
            set => waitingPromptTitle = value;
        }

        /// <summary>
        /// Source object (usually a card) that originated the prompt
        /// </summary>
        public object Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>
        /// Context object passed to handler methods
        /// </summary>
        public object Context
        {
            get => context;
            set => context = value;
        }

        /// <summary>
        /// Active prompt configuration data
        /// </summary>
        public Dictionary<string, object> ActivePrompt
        {
            get => activePrompt ??= new Dictionary<string, object>();
            set => activePrompt = value;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MenuPromptProperties()
        {
            activePrompt = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor with prompt title
        /// </summary>
        public MenuPromptProperties(string title) : this()
        {
            promptTitle = title;
        }

        /// <summary>
        /// Constructor with title and source
        /// </summary>
        public MenuPromptProperties(string title, object sourceObject) : this(title)
        {
            source = sourceObject;
        }

        /// <summary>
        /// Add a button to the active prompt
        /// </summary>
        public MenuPromptProperties AddButton(string text, string method, string arg = null)
        {
            if (!ActivePrompt.ContainsKey("buttons"))
            {
                ActivePrompt["buttons"] = new List<object>();
            }

            var buttons = (List<object>)ActivePrompt["buttons"];
            var button = new Dictionary<string, object>
            {
                ["text"] = text,
                ["method"] = method
            };
            
            if (!string.IsNullOrEmpty(arg))
            {
                button["arg"] = arg;
            }

            buttons.Add(button);
            return this;
        }

        /// <summary>
        /// Set the active prompt controls
        /// </summary>
        public MenuPromptProperties SetControls(object controls)
        {
            ActivePrompt["controls"] = controls;
            return this;
        }

        /// <summary>
        /// Set custom active prompt data
        /// </summary>
        public MenuPromptProperties SetActivePrompt(Dictionary<string, object> prompt)
        {
            activePrompt = prompt ?? new Dictionary<string, object>();
            return this;
        }
    }
}
