using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for configuring handler menu prompts in the L5R card game.
    /// Handler menu prompts provide players with contextual menu options that execute specific handlers.
    /// </summary>
    [System.Serializable]
    public class HandlerMenuPromptProperties
    {
        [Header("Prompt Display")]
        [SerializeField] private string activePromptTitle = "";
        [SerializeField] private string waitingPromptTitle = "Waiting for opponent";
        [SerializeField] private string source = "";
        
        [Header("Menu Configuration")]
        [SerializeField] private List<MenuChoice> choices = new List<MenuChoice>();
        [SerializeField] private bool allowCancel = true;
        [SerializeField] private bool autoSingle = false;
        [SerializeField] private int maxSelections = 1;
        
        [Header("Handler Settings")]
        [SerializeField] private string defaultHandler = "";
        [SerializeField] private Dictionary<string, object> handlerContext = new Dictionary<string, object>();
        [SerializeField] private bool executeImmediately = true;
        
        [Header("UI Behavior")]
        [SerializeField] private bool showOnlyToActivePlayer = true;
        [SerializeField] private bool highlightChoices = true;
        [SerializeField] private bool enableKeyboardShortcuts = true;
        
        [Header("Timing")]
        [SerializeField] private float timeoutSeconds = 0f; // 0 = no timeout
        [SerializeField] private string timeoutHandler = "";
        [SerializeField] private bool skipIfNoChoices = true;

        #region Properties

        /// <summary>
        /// Title displayed to the active player making the choice
        /// </summary>
        public string ActivePromptTitle
        {
            get => activePromptTitle;
            set => activePromptTitle = value;
        }

        /// <summary>
        /// Title displayed to players waiting for the active player's choice
        /// </summary>
        public string WaitingPromptTitle
        {
            get => waitingPromptTitle;
            set => waitingPromptTitle = value;
        }

        /// <summary>
        /// Source of the prompt (e.g., card name, ability name)
        /// </summary>
        public string Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>
        /// List of menu choices available to the player
        /// </summary>
        public List<MenuChoice> Choices
        {
            get => choices;
            set => choices = value ?? new List<MenuChoice>();
        }

        /// <summary>
        /// Whether the player can cancel/skip this prompt
        /// </summary>
        public bool AllowCancel
        {
            get => allowCancel;
            set => allowCancel = value;
        }

        /// <summary>
        /// If true and only one choice available, automatically select it
        /// </summary>
        public bool AutoSingle
        {
            get => autoSingle;
            set => autoSingle = value;
        }

        /// <summary>
        /// Maximum number of choices the player can select
        /// </summary>
        public int MaxSelections
        {
            get => maxSelections;
            set => maxSelections = Mathf.Max(1, value);
        }

        /// <summary>
        /// Default handler method name to call if no specific handler provided
        /// </summary>
        public string DefaultHandler
        {
            get => defaultHandler;
            set => defaultHandler = value;
        }

        /// <summary>
        /// Context data passed to handlers
        /// </summary>
        public Dictionary<string, object> HandlerContext
        {
            get => handlerContext;
            set => handlerContext = value ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Whether to execute the handler immediately when choice is made
        /// </summary>
        public bool ExecuteImmediately
        {
            get => executeImmediately;
            set => executeImmediately = value;
        }

        /// <summary>
        /// Whether to show this prompt only to the active player
        /// </summary>
        public bool ShowOnlyToActivePlayer
        {
            get => showOnlyToActivePlayer;
            set => showOnlyToActivePlayer = value;
        }

        /// <summary>
        /// Whether to highlight the choice options in the UI
        /// </summary>
        public bool HighlightChoices
        {
            get => highlightChoices;
            set => highlightChoices = value;
        }

        /// <summary>
        /// Whether keyboard shortcuts are enabled for choices
        /// </summary>
        public bool EnableKeyboardShortcuts
        {
            get => enableKeyboardShortcuts;
            set => enableKeyboardShortcuts = value;
        }

        /// <summary>
        /// Time limit for making the choice (0 = no limit)
        /// </summary>
        public float TimeoutSeconds
        {
            get => timeoutSeconds;
            set => timeoutSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Handler to call if the choice times out
        /// </summary>
        public string TimeoutHandler
        {
            get => timeoutHandler;
            set => timeoutHandler = value;
        }

        /// <summary>
        /// Whether to skip this prompt if no choices are available
        /// </summary>
        public bool SkipIfNoChoices
        {
            get => skipIfNoChoices;
            set => skipIfNoChoices = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public HandlerMenuPromptProperties()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// Constructor with title
        /// </summary>
        /// <param name="title">Prompt title</param>
        public HandlerMenuPromptProperties(string title) : this()
        {
            activePromptTitle = title;
        }

        /// <summary>
        /// Constructor with title and choices
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="menuChoices">Menu choices</param>
        public HandlerMenuPromptProperties(string title, List<MenuChoice> menuChoices) : this(title)
        {
            choices = menuChoices ?? new List<MenuChoice>();
        }

        /// <summary>
        /// Constructor with title and source
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="promptSource">Source of the prompt</param>
        public HandlerMenuPromptProperties(string title, string promptSource) : this(title)
        {
            source = promptSource;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize default values
        /// </summary>
        private void InitializeDefaults()
        {
            choices = new List<MenuChoice>();
            handlerContext = new Dictionary<string, object>();
            allowCancel = true;
            autoSingle = false;
            maxSelections = 1;
            executeImmediately = true;
            showOnlyToActivePlayer = true;
            highlightChoices = true;
            enableKeyboardShortcuts = true;
            timeoutSeconds = 0f;
            skipIfNoChoices = true;
        }

        /// <summary>
        /// Add a menu choice to the prompt
        /// </summary>
        /// <param name="choice">Menu choice to add</param>
        public HandlerMenuPromptProperties AddChoice(MenuChoice choice)
        {
            if (choice != null)
            {
                choices.Add(choice);
            }
            return this;
        }

        /// <summary>
        /// Add a menu choice with text and handler
        /// </summary>
        /// <param name="text">Display text</param>
        /// <param name="handler">Handler method name</param>
        /// <param name="arg">Optional argument</param>
        public HandlerMenuPromptProperties AddChoice(string text, string handler, string arg = "")
        {
            return AddChoice(new MenuChoice(text, handler, arg));
        }

        /// <summary>
        /// Add a menu choice with text, handler, and context object
        /// </summary>
        /// <param name="text">Display text</param>
        /// <param name="handler">Handler method name</param>
        /// <param name="contextObj">Context object</param>
        /// <param name="arg">Optional argument</param>
        public HandlerMenuPromptProperties AddChoice(string text, string handler, object contextObj, string arg = "")
        {
            return AddChoice(new MenuChoice(text, handler, contextObj, arg));
        }

        /// <summary>
        /// Remove all choices
        /// </summary>
        public HandlerMenuPromptProperties ClearChoices()
        {
            choices.Clear();
            return this;
        }

        /// <summary>
        /// Set the context data for handlers
        /// </summary>
        /// <param name="key">Context key</param>
        /// <param name="value">Context value</param>
        public HandlerMenuPromptProperties SetContext(string key, object value)
        {
            handlerContext[key] = value;
            return this;
        }

        /// <summary>
        /// Get context data
        /// </summary>
        /// <param name="key">Context key</param>
        /// <returns>Context value or null if not found</returns>
        public object GetContext(string key)
        {
            return handlerContext.GetValueOrDefault(key);
        }

        /// <summary>
        /// Get typed context data
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="key">Context key</param>
        /// <returns>Typed context value or default</returns>
        public T GetContext<T>(string key)
        {
            var value = GetContext(key);
            if (value is T typedValue)
                return typedValue;
            return default(T);
        }

        /// <summary>
        /// Check if there are any choices available
        /// </summary>
        /// <returns>True if choices are available</returns>
        public bool HasChoices()
        {
            return choices.Count > 0;
        }

        /// <summary>
        /// Check if only one choice is available
        /// </summary>
        /// <returns>True if exactly one choice is available</returns>
        public bool HasSingleChoice()
        {
            return choices.Count == 1;
        }

        /// <summary>
        /// Get choice by index
        /// </summary>
        /// <param name="index">Choice index</param>
        /// <returns>Menu choice or null if index invalid</returns>
        public MenuChoice GetChoice(int index)
        {
            if (index >= 0 && index < choices.Count)
                return choices[index];
            return null;
        }

        /// <summary>
        /// Find choice by handler name
        /// </summary>
        /// <param name="handler">Handler method name</param>
        /// <returns>First matching menu choice or null</returns>
        public MenuChoice FindChoiceByHandler(string handler)
        {
            return choices.Find(c => c.Handler == handler);
        }

        /// <summary>
        /// Find choices by handler name
        /// </summary>
        /// <param name="handler">Handler method name</param>
        /// <returns>List of matching menu choices</returns>
        public List<MenuChoice> FindChoicesByHandler(string handler)
        {
            return choices.FindAll(c => c.Handler == handler);
        }

        /// <summary>
        /// Validate the prompt properties
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            // Must have a title
            if (string.IsNullOrEmpty(activePromptTitle))
                return false;

            // If no choices but skip is disabled, invalid
            if (!HasChoices() && !skipIfNoChoices)
                return false;

            // Validate choices
            foreach (var choice in choices)
            {
                if (!choice.IsValid())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Create a copy of these properties
        /// </summary>
        /// <returns>Copied properties</returns>
        public HandlerMenuPromptProperties Clone()
        {
            var clone = new HandlerMenuPromptProperties
            {
                activePromptTitle = this.activePromptTitle,
                waitingPromptTitle = this.waitingPromptTitle,
                source = this.source,
                allowCancel = this.allowCancel,
                autoSingle = this.autoSingle,
                maxSelections = this.maxSelections,
                defaultHandler = this.defaultHandler,
                executeImmediately = this.executeImmediately,
                showOnlyToActivePlayer = this.showOnlyToActivePlayer,
                highlightChoices = this.highlightChoices,
                enableKeyboardShortcuts = this.enableKeyboardShortcuts,
                timeoutSeconds = this.timeoutSeconds,
                timeoutHandler = this.timeoutHandler,
                skipIfNoChoices = this.skipIfNoChoices
            };

            // Deep copy choices
            clone.choices = new List<MenuChoice>();
            foreach (var choice in this.choices)
            {
                clone.choices.Add(choice.Clone());
            }

            // Deep copy context
            clone.handlerContext = new Dictionary<string, object>(this.handlerContext);

            return clone;
        }

        #endregion

        #region Debug

        /// <summary>
        /// Get debug information about this prompt
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            var info = $"HandlerMenuPrompt: '{activePromptTitle}'\n";
            info += $"  Source: {source}\n";
            info += $"  Choices: {choices.Count}\n";
            info += $"  Allow Cancel: {allowCancel}\n";
            info += $"  Auto Single: {autoSingle}\n";
            info += $"  Max Selections: {maxSelections}\n";
            
            if (timeoutSeconds > 0)
                info += $"  Timeout: {timeoutSeconds}s\n";

            for (int i = 0; i < choices.Count; i++)
            {
                info += $"    {i + 1}. {choices[i].GetDebugInfo()}\n";
            }

            return info;
        }

        #endregion
    }

    /// <summary>
    /// Represents a single menu choice in a handler menu prompt
    /// </summary>
    [System.Serializable]
    public class MenuChoice
    {
        [SerializeField] private string text = "";
        [SerializeField] private string handler = "";
        [SerializeField] private string arg = "";
        [SerializeField] private object contextObject;
        [SerializeField] private bool enabled = true;
        [SerializeField] private string tooltip = "";
        [SerializeField] private KeyCode shortcutKey = KeyCode.None;

        /// <summary>
        /// Display text for the choice
        /// </summary>
        public string Text
        {
            get => text;
            set => text = value;
        }

        /// <summary>
        /// Handler method name to call when this choice is selected
        /// </summary>
        public string Handler
        {
            get => handler;
            set => handler = value;
        }

        /// <summary>
        /// Argument to pass to the handler
        /// </summary>
        public string Arg
        {
            get => arg;
            set => arg = value;
        }

        /// <summary>
        /// Context object associated with this choice
        /// </summary>
        public object ContextObject
        {
            get => contextObject;
            set => contextObject = value;
        }

        /// <summary>
        /// Whether this choice is currently enabled/selectable
        /// </summary>
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>
        /// Tooltip text for the choice
        /// </summary>
        public string Tooltip
        {
            get => tooltip;
            set => tooltip = value;
        }

        /// <summary>
        /// Keyboard shortcut for this choice
        /// </summary>
        public KeyCode ShortcutKey
        {
            get => shortcutKey;
            set => shortcutKey = value;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MenuChoice()
        {
        }

        /// <summary>
        /// Constructor with text and handler
        /// </summary>
        /// <param name="choiceText">Display text</param>
        /// <param name="handlerMethod">Handler method name</param>
        /// <param name="argument">Optional argument</param>
        public MenuChoice(string choiceText, string handlerMethod, string argument = "")
        {
            text = choiceText;
            handler = handlerMethod;
            arg = argument;
            enabled = true;
        }

        /// <summary>
        /// Constructor with text, handler, and context object
        /// </summary>
        /// <param name="choiceText">Display text</param>
        /// <param name="handlerMethod">Handler method name</param>
        /// <param name="context">Context object</param>
        /// <param name="argument">Optional argument</param>
        public MenuChoice(string choiceText, string handlerMethod, object context, string argument = "")
        {
            text = choiceText;
            handler = handlerMethod;
            contextObject = context;
            arg = argument;
            enabled = true;
        }

        /// <summary>
        /// Check if this choice is valid
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(handler);
        }

        /// <summary>
        /// Create a copy of this choice
        /// </summary>
        /// <returns>Cloned choice</returns>
        public MenuChoice Clone()
        {
            return new MenuChoice
            {
                text = this.text,
                handler = this.handler,
                arg = this.arg,
                contextObject = this.contextObject,
                enabled = this.enabled,
                tooltip = this.tooltip,
                shortcutKey = this.shortcutKey
            };
        }

        /// <summary>
        /// Get debug information about this choice
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            var info = $"'{text}' -> {handler}";
            if (!string.IsNullOrEmpty(arg))
                info += $"({arg})";
            if (!enabled)
                info += " [DISABLED]";
            return info;
        }

        /// <summary>
        /// Convert to string for debugging
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetDebugInfo();
        }
    }

    /// <summary>
    /// Extension methods for HandlerMenuPromptProperties
    /// </summary>
    public static class HandlerMenuPromptExtensions
    {
        /// <summary>
        /// Create a simple choice prompt
        /// </summary>
        public static HandlerMenuPromptProperties CreateChoice(string title, string choice1Text, string choice1Handler, 
            string choice2Text, string choice2Handler)
        {
            return new HandlerMenuPromptProperties(title)
                .AddChoice(choice1Text, choice1Handler)
                .AddChoice(choice2Text, choice2Handler);
        }

        /// <summary>
        /// Create a yes/no prompt
        /// </summary>
        public static HandlerMenuPromptProperties CreateYesNo(string title, string yesHandler, string noHandler)
        {
            return CreateChoice(title, "Yes", yesHandler, "No", noHandler);
        }

        /// <summary>
        /// Create a confirmation prompt
        /// </summary>
        public static HandlerMenuPromptProperties CreateConfirmation(string title, string confirmHandler)
        {
            return new HandlerMenuPromptProperties(title)
                .AddChoice("Confirm", confirmHandler)
                .AddChoice("Cancel", "cancel");
        }
    }
}
