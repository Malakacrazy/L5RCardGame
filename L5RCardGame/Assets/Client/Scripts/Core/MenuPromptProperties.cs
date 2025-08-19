using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties class for configuring MenuPrompt behavior.
    /// This matches the JavaScript MenuPrompt properties structure used in tests.
    /// </summary>
    [System.Serializable]
    public class MenuPromptProperties
    {
        #region Fields

        [Header("Context")]
        [SerializeField] private object context;
        [SerializeField] private object source;

        [Header("Prompt Configuration")]
        [SerializeField] private string promptTitle;
        [SerializeField] private string waitingPromptTitle;

        [Header("Active Prompt")]
        [SerializeField] private ActivePromptData activePrompt;

        #endregion

        #region Properties

        /// <summary>
        /// Context object passed to handler methods as the third parameter
        /// </summary>
        public object Context
        {
            get => context;
            set => context = value;
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
        /// Active prompt configuration data containing buttons and other UI elements
        /// </summary>
        public ActivePromptData ActivePrompt
        {
            get => activePrompt ??= new ActivePromptData();
            set => activePrompt = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MenuPromptProperties()
        {
            activePrompt = new ActivePromptData();
        }

        /// <summary>
        /// Constructor with context
        /// </summary>
        /// <param name="contextObject">Context object for handler methods</param>
        public MenuPromptProperties(object contextObject) : this()
        {
            context = contextObject;
        }

        /// <summary>
        /// Constructor with context and source
        /// </summary>
        /// <param name="contextObject">Context object for handler methods</param>
        /// <param name="sourceObject">Source object (usually a card)</param>
        public MenuPromptProperties(object contextObject, object sourceObject) : this(contextObject)
        {
            source = sourceObject;
        }

        #endregion

        #region Fluent API Methods

        /// <summary>
        /// Set the context object
        /// </summary>
        /// <param name="contextObject">Context object</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties SetContext(object contextObject)
        {
            context = contextObject;
            return this;
        }

        /// <summary>
        /// Set the source object
        /// </summary>
        /// <param name="sourceObject">Source object</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties SetSource(object sourceObject)
        {
            source = sourceObject;
            return this;
        }

        /// <summary>
        /// Set the prompt title
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties SetPromptTitle(string title)
        {
            promptTitle = title;
            return this;
        }

        /// <summary>
        /// Set the waiting prompt title
        /// </summary>
        /// <param name="title">Waiting prompt title</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties SetWaitingPromptTitle(string title)
        {
            waitingPromptTitle = title;
            return this;
        }

        /// <summary>
        /// Add a button to the active prompt
        /// </summary>
        /// <param name="text">Button text</param>
        /// <param name="method">Method name to call</param>
        /// <param name="command">Command type (default: "menuButton")</param>
        /// <param name="arg">Optional argument</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties AddButton(string text, string method, string command = "menuButton", object arg = null)
        {
            var button = new MenuButton
            {
                Text = text,
                Method = method,
                Command = command
            };

            if (arg != null)
            {
                button.Arg = arg;
            }

            ActivePrompt.Buttons.Add(button);
            return this;
        }

        /// <summary>
        /// Add a pre-configured button
        /// </summary>
        /// <param name="button">Button to add</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties AddButton(MenuButton button)
        {
            if (button != null)
            {
                ActivePrompt.Buttons.Add(button);
            }
            return this;
        }

        /// <summary>
        /// Clear all buttons
        /// </summary>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties ClearButtons()
        {
            ActivePrompt.Buttons.Clear();
            return this;
        }

        /// <summary>
        /// Set controls for the active prompt
        /// </summary>
        /// <param name="controls">Controls object</param>
        /// <returns>This instance for chaining</returns>
        public MenuPromptProperties SetControls(object controls)
        {
            ActivePrompt.Controls = controls;
            return this;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if a button with the specified method exists
        /// </summary>
        /// <param name="method">Method name to check</param>
        /// <returns>True if button exists</returns>
        public bool HasButtonWithMethod(string method)
        {
            return ActivePrompt.Buttons.Any(button => button.Method == method);
        }

        /// <summary>
        /// Get button by method name
        /// </summary>
        /// <param name="method">Method name</param>
        /// <returns>First button with matching method, or null</returns>
        public MenuButton GetButtonByMethod(string method)
        {
            return ActivePrompt.Buttons.FirstOrDefault(button => button.Method == method);
        }

        /// <summary>
        /// Get all buttons with the specified method
        /// </summary>
        /// <param name="method">Method name</param>
        /// <returns>List of matching buttons</returns>
        public List<MenuButton> GetButtonsByMethod(string method)
        {
            return ActivePrompt.Buttons.Where(button => button.Method == method).ToList();
        }

        /// <summary>
        /// Validate the properties
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            // At minimum, we need buttons to be valid
            return ActivePrompt.Buttons.Count > 0 && ActivePrompt.Buttons.All(b => b.IsValid());
        }

        #endregion

        #region Debug Support

        /// <summary>
        /// Get debug information
        /// </summary>
        /// <returns>Debug string</returns>
        public string GetDebugInfo()
        {
            var info = "MenuPromptProperties:\n";
            info += $"  Context: {context?.GetType().Name ?? "null"}\n";
            info += $"  Source: {GetSourceName()}\n";
            info += $"  Prompt Title: {promptTitle ?? "null"}\n";
            info += $"  Waiting Title: {waitingPromptTitle ?? "null"}\n";
            info += $"  Buttons: {ActivePrompt.Buttons.Count}\n";

            for (int i = 0; i < ActivePrompt.Buttons.Count; i++)
            {
                var button = ActivePrompt.Buttons[i];
                info += $"    {i + 1}. {button.Text} -> {button.Method} ({button.Command})\n";
            }

            return info;
        }

        private string GetSourceName()
        {
            if (source == null) return "null";
            
            if (source is BaseCard card) return card.name;
            
            var nameProperty = source.GetType().GetProperty("name") ?? 
                              source.GetType().GetProperty("Name");
            if (nameProperty != null)
            {
                var name = nameProperty.GetValue(source)?.ToString();
                if (!string.IsNullOrEmpty(name)) return name;
            }

            return source.GetType().Name;
        }

        #endregion
    }

    /// <summary>
    /// Represents the active prompt data structure
    /// </summary>
    [System.Serializable]
    public class ActivePromptData
    {
        [SerializeField] private List<MenuButton> buttons;
        [SerializeField] private object controls;
        [SerializeField] private string promptTitle;
        [SerializeField] private Dictionary<string, object> additionalData;

        /// <summary>
        /// List of buttons in the prompt
        /// </summary>
        public List<MenuButton> Buttons
        {
            get => buttons ??= new List<MenuButton>();
            set => buttons = value ?? new List<MenuButton>();
        }

        /// <summary>
        /// Controls object for the prompt
        /// </summary>
        public object Controls
        {
            get => controls;
            set => controls = value;
        }

        /// <summary>
        /// Prompt title override
        /// </summary>
        public string PromptTitle
        {
            get => promptTitle;
            set => promptTitle = value;
        }

        /// <summary>
        /// Additional data for the prompt
        /// </summary>
        public Dictionary<string, object> AdditionalData
        {
            get => additionalData ??= new Dictionary<string, object>();
            set => additionalData = value ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActivePromptData()
        {
            buttons = new List<MenuButton>();
            additionalData = new Dictionary<string, object>();
        }

        /// <summary>
        /// Set additional data
        /// </summary>
        /// <param name="key">Data key</param>
        /// <param name="value">Data value</param>
        public void SetData(string key, object value)
        {
            AdditionalData[key] = value;
        }

        /// <summary>
        /// Get additional data
        /// </summary>
        /// <param name="key">Data key</param>
        /// <returns>Data value or null</returns>
        public object GetData(string key)
        {
            return AdditionalData.GetValueOrDefault(key);
        }

        /// <summary>
        /// Convert to dictionary for serialization/UI
        /// </summary>
        /// <returns>Dictionary representation</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>(AdditionalData);
            
            if (buttons != null && buttons.Count > 0)
            {
                dict["buttons"] = buttons.Select(b => b.ToDictionary()).ToList();
            }
            
            if (controls != null)
            {
                dict["controls"] = controls;
            }
            
            if (!string.IsNullOrEmpty(promptTitle))
            {
                dict["promptTitle"] = promptTitle;
            }

            return dict;
        }
    }

    /// <summary>
    /// Represents a menu button in the prompt
    /// </summary>
    [System.Serializable]
    public class MenuButton
    {
        [SerializeField] private string command = "menuButton";
        [SerializeField] private string text;
        [SerializeField] private string method;
        [SerializeField] private object arg;
        [SerializeField] private bool disabled = false;
        [SerializeField] private Dictionary<string, object> additionalProperties;

        /// <summary>
        /// Command type for the button (usually "menuButton")
        /// </summary>
        public string Command
        {
            get => command;
            set => command = value;
        }

        /// <summary>
        /// Display text for the button
        /// </summary>
        public string Text
        {
            get => text;
            set => text = value;
        }

        /// <summary>
        /// Method name to call when button is clicked
        /// </summary>
        public string Method
        {
            get => method;
            set => method = value;
        }

        /// <summary>
        /// Argument to pass to the method
        /// </summary>
        public object Arg
        {
            get => arg;
            set => arg = value;
        }

        /// <summary>
        /// Whether the button is disabled
        /// </summary>
        public bool Disabled
        {
            get => disabled;
            set => disabled = value;
        }

        /// <summary>
        /// Additional properties for the button
        /// </summary>
        public Dictionary<string, object> AdditionalProperties
        {
            get => additionalProperties ??= new Dictionary<string, object>();
            set => additionalProperties = value ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MenuButton()
        {
            command = "menuButton";
            additionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor with basic properties
        /// </summary>
        /// <param name="buttonText">Button text</param>
        /// <param name="methodName">Method name</param>
        /// <param name="commandType">Command type</param>
        public MenuButton(string buttonText, string methodName, string commandType = "menuButton") : this()
        {
            text = buttonText;
            method = methodName;
            command = commandType;
        }

        /// <summary>
        /// Constructor with argument
        /// </summary>
        /// <param name="buttonText">Button text</param>
        /// <param name="methodName">Method name</param>
        /// <param name="argument">Argument to pass</param>
        /// <param name="commandType">Command type</param>
        public MenuButton(string buttonText, string methodName, object argument, string commandType = "menuButton") : this(buttonText, methodName, commandType)
        {
            arg = argument;
        }

        /// <summary>
        /// Set additional property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <returns>This instance for chaining</returns>
        public MenuButton SetProperty(string key, object value)
        {
            AdditionalProperties[key] = value;
            return this;
        }

        /// <summary>
        /// Get additional property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Property value or null</returns>
        public object GetProperty(string key)
        {
            return AdditionalProperties.GetValueOrDefault(key);
        }

        /// <summary>
        /// Check if the button is valid
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(method);
        }

        /// <summary>
        /// Convert to dictionary for serialization/UI
        /// </summary>
        /// <returns>Dictionary representation</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>(AdditionalProperties)
            {
                ["command"] = command,
                ["text"] = text,
                ["method"] = method
            };

            if (arg != null)
            {
                dict["arg"] = arg;
            }

            if (disabled)
            {
                dict["disabled"] = disabled;
            }

            return dict;
        }

        /// <summary>
        /// Create from dictionary
        /// </summary>
        /// <param name="dict">Dictionary data</param>
        /// <returns>Menu button instance</returns>
        public static MenuButton FromDictionary(Dictionary<string, object> dict)
        {
            var button = new MenuButton();
            
            if (dict.ContainsKey("command"))
                button.command = dict["command"]?.ToString() ?? "menuButton";
            
            if (dict.ContainsKey("text"))
                button.text = dict["text"]?.ToString();
            
            if (dict.ContainsKey("method"))
                button.method = dict["method"]?.ToString();
            
            if (dict.ContainsKey("arg"))
                button.arg = dict["arg"];
            
            if (dict.ContainsKey("disabled"))
                button.disabled = dict["disabled"] is bool boolVal && boolVal;

            // Copy any additional properties
            foreach (var kvp in dict)
            {
                if (!new[] { "command", "text", "method", "arg", "disabled" }.Contains(kvp.Key))
                {
                    button.AdditionalProperties[kvp.Key] = kvp.Value;
                }
            }

            return button;
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        /// <returns>Debug string</returns>
        public override string ToString()
        {
            var str = $"'{text}' -> {method}";
            if (arg != null) str += $"({arg})";
            if (disabled) str += " [DISABLED]";
            return str;
        }
    }

    /// <summary>
    /// Extension methods for MenuPromptProperties
    /// </summary>
    public static class MenuPromptPropertiesExtensions
    {
        /// <summary>
        /// Create a simple confirmation prompt
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="confirmMethod">Method to call on confirm</param>
        /// <param name="cancelMethod">Method to call on cancel</param>
        /// <param name="context">Context object</param>
        /// <returns>Configured properties</returns>
        public static MenuPromptProperties CreateConfirmation(string title, string confirmMethod, string cancelMethod, object context = null)
        {
            return new MenuPromptProperties(context)
                .SetPromptTitle(title)
                .AddButton("Confirm", confirmMethod)
                .AddButton("Cancel", cancelMethod);
        }

        /// <summary>
        /// Create a simple choice prompt
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="choices">Dictionary of choice text -> method name</param>
        /// <param name="context">Context object</param>
        /// <returns>Configured properties</returns>
        public static MenuPromptProperties CreateChoice(string title, Dictionary<string, string> choices, object context = null)
        {
            var properties = new MenuPromptProperties(context).SetPromptTitle(title);
            
            foreach (var choice in choices)
            {
                properties.AddButton(choice.Key, choice.Value);
            }
            
            return properties;
        }

        /// <summary>
        /// Create properties matching the test structure
        /// </summary>
        /// <param name="context">Context object for methods</param>
        /// <param name="contextData">Context data passed to methods</param>
        /// <param name="buttonText">Button text</param>
        /// <param name="buttonMethod">Button method</param>
        /// <returns>Test-compatible properties</returns>
        public static MenuPromptProperties CreateTestProperties(object context, object contextData, string buttonText, string buttonMethod)
        {
            return new MenuPromptProperties(contextData)
                .AddButton(buttonText, buttonMethod);
        }
    }
}
