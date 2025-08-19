using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Properties for configuring SelectRingPrompt behavior.
    /// This class contains all the configuration options for ring selection prompts,
    /// including ring conditions, callbacks, and UI settings.
    /// </summary>
    [System.Serializable]
    public class SelectRingPromptProperties
    {
        #region Fields

        [Header("Prompt Configuration")]
        [SerializeField] private string activePromptTitle;
        [SerializeField] private string waitingPromptTitle;
        [SerializeField] private EffectSource source;
        [SerializeField] private AbilityContext context;

        [Header("Selection Options")]
        [SerializeField] private bool optional = false;
        [SerializeField] private bool ordered = false;
        [SerializeField] private int maxSelections = 1;
        [SerializeField] private int minSelections = 1;

        [Header("UI Elements")]
        [SerializeField] private List<PromptButton> additionalButtons = new List<PromptButton>();
        [SerializeField] private List<object> controls = new List<object>();

        // Callback delegates
        private Func<Ring, AbilityContext, bool> ringCondition;
        private Func<Player, Ring, bool> onSelect;
        private Func<Player, string, bool> onMenuCommand;
        private Func<Player, bool> onCancel;

        #endregion

        #region Properties

        /// <summary>
        /// Title displayed to the active player making the selection
        /// </summary>
        public string ActivePromptTitle
        {
            get => activePromptTitle;
            set => activePromptTitle = value;
        }

        /// <summary>
        /// Title displayed to players waiting for the active player's selection
        /// </summary>
        public string WaitingPromptTitle
        {
            get => waitingPromptTitle;
            set => waitingPromptTitle = value;
        }

        /// <summary>
        /// Source of the prompt (usually a card or ability)
        /// </summary>
        public EffectSource Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>
        /// Ability context associated with this prompt
        /// </summary>
        public AbilityContext Context
        {
            get => context;
            set => context = value;
        }

        /// <summary>
        /// Whether ring selection is optional (allows Done button)
        /// </summary>
        public bool Optional
        {
            get => optional;
            set => optional = value;
        }

        /// <summary>
        /// Whether rings should be selected in a specific order
        /// </summary>
        public bool Ordered
        {
            get => ordered;
            set => ordered = value;
        }

        /// <summary>
        /// Maximum number of rings that can be selected
        /// </summary>
        public int MaxSelections
        {
            get => maxSelections;
            set => maxSelections = Mathf.Max(1, value);
        }

        /// <summary>
        /// Minimum number of rings that must be selected
        /// </summary>
        public int MinSelections
        {
            get => minSelections;
            set => minSelections = Mathf.Max(0, value);
        }

        /// <summary>
        /// Additional buttons to display in the prompt
        /// </summary>
        public List<PromptButton> AdditionalButtons
        {
            get => additionalButtons;
            set => additionalButtons = value ?? new List<PromptButton>();
        }

        /// <summary>
        /// Controls/UI elements for the prompt
        /// </summary>
        public List<object> Controls
        {
            get => controls;
            set => controls = value ?? new List<object>();
        }

        /// <summary>
        /// Function that determines if a ring is eligible for selection
        /// </summary>
        public Func<Ring, AbilityContext, bool> RingCondition
        {
            get => ringCondition ?? DefaultRingCondition;
            set => ringCondition = value;
        }

        /// <summary>
        /// Callback called when a ring is selected
        /// </summary>
        public Func<Player, Ring, bool> OnSelect
        {
            get => onSelect ?? DefaultOnSelect;
            set => onSelect = value;
        }

        /// <summary>
        /// Callback called when a menu command is executed
        /// </summary>
        public Func<Player, string, bool> OnMenuCommand
        {
            get => onMenuCommand ?? DefaultOnMenuCommand;
            set => onMenuCommand = value;
        }

        /// <summary>
        /// Callback called when the prompt is cancelled
        /// </summary>
        public Func<Player, bool> OnCancel
        {
            get => onCancel ?? DefaultOnCancel;
            set => onCancel = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public SelectRingPromptProperties()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// Constructor with title
        /// </summary>
        /// <param name="title">Active prompt title</param>
        public SelectRingPromptProperties(string title) : this()
        {
            activePromptTitle = title;
        }

        /// <summary>
        /// Constructor with title and source
        /// </summary>
        /// <param name="title">Active prompt title</param>
        /// <param name="promptSource">Source of the prompt</param>
        public SelectRingPromptProperties(string title, EffectSource promptSource) : this(title)
        {
            source = promptSource;
            
            // Auto-generate waiting prompt title if source is provided
            if (promptSource != null && string.IsNullOrEmpty(waitingPromptTitle))
            {
                waitingPromptTitle = $"Waiting for opponent to use {promptSource.name}";
            }
        }

        /// <summary>
        /// Constructor with context
        /// </summary>
        /// <param name="abilityContext">Ability context for the prompt</param>
        public SelectRingPromptProperties(AbilityContext abilityContext) : this()
        {
            context = abilityContext;
            
            if (abilityContext?.source != null)
            {
                source = abilityContext.source;
                waitingPromptTitle = $"Waiting for opponent to use {source.name}";
            }
        }

        /// <summary>
        /// Constructor with title, source, and ring condition
        /// </summary>
        /// <param name="title">Active prompt title</param>
        /// <param name="promptSource">Source of the prompt</param>
        /// <param name="condition">Ring selection condition</param>
        public SelectRingPromptProperties(string title, EffectSource promptSource, Func<Ring, AbilityContext, bool> condition) 
            : this(title, promptSource)
        {
            ringCondition = condition;
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Set the ring selection condition
        /// </summary>
        /// <param name="condition">Function to determine ring eligibility</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetRingCondition(Func<Ring, AbilityContext, bool> condition)
        {
            ringCondition = condition;
            return this;
        }

        /// <summary>
        /// Set the ring selection condition (simple version without context)
        /// </summary>
        /// <param name="condition">Function to determine ring eligibility</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetRingCondition(Func<Ring, bool> condition)
        {
            ringCondition = (ring, context) => condition(ring);
            return this;
        }

        /// <summary>
        /// Set the selection callback
        /// </summary>
        /// <param name="callback">Callback for when a ring is selected</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOnSelect(Func<Player, Ring, bool> callback)
        {
            onSelect = callback;
            return this;
        }

        /// <summary>
        /// Set the selection callback (simple version)
        /// </summary>
        /// <param name="callback">Callback for when a ring is selected</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOnSelect(Action<Player, Ring> callback)
        {
            onSelect = (player, ring) =>
            {
                callback(player, ring);
                return true;
            };
            return this;
        }

        /// <summary>
        /// Set the menu command callback
        /// </summary>
        /// <param name="callback">Callback for menu commands</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOnMenuCommand(Func<Player, string, bool> callback)
        {
            onMenuCommand = callback;
            return this;
        }

        /// <summary>
        /// Set the cancel callback
        /// </summary>
        /// <param name="callback">Callback for when prompt is cancelled</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOnCancel(Func<Player, bool> callback)
        {
            onCancel = callback;
            return this;
        }

        /// <summary>
        /// Set the cancel callback (simple version)
        /// </summary>
        /// <param name="callback">Callback for when prompt is cancelled</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOnCancel(Action<Player> callback)
        {
            onCancel = (player) =>
            {
                callback(player);
                return true;
            };
            return this;
        }

        /// <summary>
        /// Add an additional button to the prompt
        /// </summary>
        /// <param name="text">Button text</param>
        /// <param name="arg">Button argument</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties AddButton(string text, string arg)
        {
            additionalButtons.Add(new PromptButton(text, "", arg));
            return this;
        }

        /// <summary>
        /// Add an additional button to the prompt
        /// </summary>
        /// <param name="button">Button to add</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties AddButton(PromptButton button)
        {
            if (button != null)
            {
                additionalButtons.Add(button);
            }
            return this;
        }

        /// <summary>
        /// Clear all additional buttons
        /// </summary>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties ClearButtons()
        {
            additionalButtons.Clear();
            return this;
        }

        /// <summary>
        /// Set whether the prompt is optional
        /// </summary>
        /// <param name="isOptional">True if optional</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOptional(bool isOptional = true)
        {
            optional = isOptional;
            return this;
        }

        /// <summary>
        /// Set whether rings should be selected in order
        /// </summary>
        /// <param name="isOrdered">True if ordered</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetOrdered(bool isOrdered = true)
        {
            ordered = isOrdered;
            return this;
        }

        /// <summary>
        /// Set the selection limits
        /// </summary>
        /// <param name="min">Minimum selections required</param>
        /// <param name="max">Maximum selections allowed</param>
        /// <returns>This instance for chaining</returns>
        public SelectRingPromptProperties SetSelectionLimits(int min, int max)
        {
            minSelections = Mathf.Max(0, min);
            maxSelections = Mathf.Max(1, max);
            return this;
        }

        #endregion

        #region Default Controls Generation

        /// <summary>
        /// Generate default controls based on context (matches JavaScript logic)
        /// </summary>
        /// <returns>List of default controls</returns>
        public List<object> GetDefaultControls()
        {
            if (context == null)
            {
                return new List<object>();
            }

            var targets = new List<object>();

            // Get targets from context
            if (context.targets?.Count > 0)
            {
                targets = context.targets.Values
                    .Select(target => GetTargetSummaryForControls(target))
                    .Where(summary => summary != null)
                    .ToList();
            }

            // Fallback to event card if no targets
            if (targets.Count == 0 && context.eventData != null)
            {
                var eventCard = GetEventCard(context.eventData);
                if (eventCard != null)
                {
                    var summary = GetTargetSummaryForControls(eventCard);
                    if (summary != null)
                    {
                        targets.Add(summary);
                    }
                }
            }

            // Create targeting control
            var control = new Dictionary<string, object>
            {
                ["type"] = "targeting",
                ["source"] = source?.GetShortSummary(),
                ["targets"] = targets
            };

            return new List<object> { control };
        }

        /// <summary>
        /// Get summary for controls (matches choosingPlayer context)
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Summary object or null</returns>
        private object GetTargetSummaryForControls(object target)
        {
            if (target is BaseCard card)
            {
                return card.GetShortSummaryForControls(context?.player);
            }
            
            if (target is Ring ring)
            {
                return ring.GetShortSummary();
            }

            return target;
        }

        /// <summary>
        /// Extract card from event data
        /// </summary>
        /// <param name="eventData">Event data</param>
        /// <returns>Card from event or null</returns>
        private BaseCard GetEventCard(object eventData)
        {
            if (eventData == null) return null;

            // Try to get card property from event
            var eventType = eventData.GetType();
            var cardProperty = eventType.GetProperty("card") ?? eventType.GetProperty("Card");
            
            return cardProperty?.GetValue(eventData) as BaseCard;
        }

        #endregion

        #region Default Callbacks

        /// <summary>
        /// Default ring condition (allows all rings)
        /// </summary>
        /// <param name="ring">Ring to check</param>
        /// <param name="context">Ability context</param>
        /// <returns>Always true</returns>
        private static bool DefaultRingCondition(Ring ring, AbilityContext context)
        {
            return true;
        }

        /// <summary>
        /// Default selection callback (always succeeds)
        /// </summary>
        /// <param name="player">Player making selection</param>
        /// <param name="ring">Selected ring</param>
        /// <returns>Always true</returns>
        private static bool DefaultOnSelect(Player player, Ring ring)
        {
            return true;
        }

        /// <summary>
        /// Default menu command callback (always succeeds)
        /// </summary>
        /// <param name="player">Player executing command</param>
        /// <param name="arg">Command argument</param>
        /// <returns>Always true</returns>
        private static bool DefaultOnMenuCommand(Player player, string arg)
        {
            return true;
        }

        /// <summary>
        /// Default cancel callback (always succeeds)
        /// </summary>
        /// <param name="player">Player cancelling</param>
        /// <returns>Always true</returns>
        private static bool DefaultOnCancel(Player player)
        {
            return true;
        }

        #endregion

        #region Validation and Utility

        /// <summary>
        /// Validate the prompt properties
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            // Must have a ring condition
            if (RingCondition == null)
                return false;

            // Must have selection callback
            if (OnSelect == null)
                return false;

            // Min selections cannot exceed max selections
            if (minSelections > maxSelections)
                return false;

            return true;
        }

        /// <summary>
        /// Check if a ring is selectable with current conditions
        /// </summary>
        /// <param name="ring">Ring to check</param>
        /// <returns>True if ring can be selected</returns>
        public bool IsRingSelectable(Ring ring)
        {
            if (ring == null) return false;
            return RingCondition(ring, context);
        }

        /// <summary>
        /// Get all buttons including default ones
        /// </summary>
        /// <param name="manualMode">Whether game is in manual mode</param>
        /// <returns>Complete list of buttons</returns>
        public List<PromptButton> GetAllButtons(bool manualMode = false)
        {
            var buttons = new List<PromptButton>(additionalButtons);

            // Add Done button if optional
            if (optional)
            {
                buttons.Add(new PromptButton("Done", "", "done"));
            }

            // Add Cancel button in manual mode
            if (manualMode && !buttons.Any(b => b.arg == "cancel"))
            {
                buttons.Add(new PromptButton("Cancel Prompt", "", "cancel"));
            }

            return buttons;
        }

        /// <summary>
        /// Create a copy of these properties
        /// </summary>
        /// <returns>Cloned properties</returns>
        public SelectRingPromptProperties Clone()
        {
            var clone = new SelectRingPromptProperties
            {
                activePromptTitle = this.activePromptTitle,
                waitingPromptTitle = this.waitingPromptTitle,
                source = this.source,
                context = this.context,
                optional = this.optional,
                ordered = this.ordered,
                maxSelections = this.maxSelections,
                minSelections = this.minSelections,
                ringCondition = this.ringCondition,
                onSelect = this.onSelect,
                onMenuCommand = this.onMenuCommand,
                onCancel = this.onCancel
            };

            // Deep copy buttons and controls
            clone.additionalButtons = additionalButtons.Select(b => new PromptButton(b.text, b.method, b.arg)).ToList();
            clone.controls = new List<object>(controls);

            return clone;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize default values
        /// </summary>
        private void InitializeDefaults()
        {
            additionalButtons = new List<PromptButton>();
            controls = new List<object>();
            optional = false;
            ordered = false;
            maxSelections = 1;
            minSelections = 1;
        }

        #endregion

        #region Debug Support

        /// <summary>
        /// Get debug information about this prompt
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            var info = $"SelectRingPromptProperties:\n";
            info += $"  Active Title: {activePromptTitle ?? "default"}\n";
            info += $"  Waiting Title: {waitingPromptTitle ?? "default"}\n";
            info += $"  Source: {source?.name ?? "null"}\n";
            info += $"  Optional: {optional}\n";
            info += $"  Ordered: {ordered}\n";
            info += $"  Selection Range: {minSelections}-{maxSelections}\n";
            info += $"  Additional Buttons: {additionalButtons.Count}\n";
            info += $"  Has Context: {context != null}\n";
            info += $"  Has Ring Condition: {ringCondition != null}\n";

            return info;
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for SelectRingPromptProperties
    /// </summary>
    public static class SelectRingPromptExtensions
    {
        /// <summary>
        /// Create a simple ring selection prompt
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="condition">Ring selection condition</param>
        /// <param name="onSelected">Selection callback</param>
        /// <returns>Configured properties</returns>
        public static SelectRingPromptProperties CreateSimpleRingPrompt(
            string title, 
            Func<Ring, bool> condition, 
            Action<Player, Ring> onSelected)
        {
            return new SelectRingPromptProperties(title)
                .SetRingCondition(condition)
                .SetOnSelect(onSelected);
        }

        /// <summary>
        /// Create an optional ring selection prompt
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="condition">Ring selection condition</param>
        /// <param name="onSelected">Selection callback</param>
        /// <param name="onSkipped">Skip callback</param>
        /// <returns>Configured properties</returns>
        public static SelectRingPromptProperties CreateOptionalRingPrompt(
            string title,
            Func<Ring, bool> condition,
            Action<Player, Ring> onSelected,
            Action<Player> onSkipped = null)
        {
            var properties = new SelectRingPromptProperties(title)
                .SetRingCondition(condition)
                .SetOnSelect(onSelected)
                .SetOptional(true);

            if (onSkipped != null)
            {
                properties.SetOnCancel(onSkipped);
            }

            return properties;
        }

        /// <summary>
        /// Create a multi-ring selection prompt
        /// </summary>
        /// <param name="title">Prompt title</param>
        /// <param name="minRings">Minimum rings to select</param>
        /// <param name="maxRings">Maximum rings to select</param>
        /// <param name="condition">Ring selection condition</param>
        /// <param name="onCompleted">Completion callback</param>
        /// <returns>Configured properties</returns>
        public static SelectRingPromptProperties CreateMultiRingPrompt(
            string title,
            int minRings,
            int maxRings,
            Func<Ring, bool> condition,
            Action<Player, List<Ring>> onCompleted)
        {
            var selectedRings = new List<Ring>();
            
            return new SelectRingPromptProperties(title)
                .SetRingCondition(condition)
                .SetSelectionLimits(minRings, maxRings)
                .SetOnSelect((player, ring) =>
                {
                    selectedRings.Add(ring);
                    if (selectedRings.Count >= minRings)
                    {
                        onCompleted(player, selectedRings);
                        return true;
                    }
                    return false;
                });
        }
    }
}
