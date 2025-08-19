using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Manages the prompt state for a player, including selectable cards, rings, and UI elements.
    /// This class tracks what the player can interact with and what prompts they are currently seeing.
    /// </summary>
    [System.Serializable]
    public class PlayerPromptState
    {
        #region Fields

        [Header("Player Reference")]
        [SerializeField] private Player player;

        [Header("Selection Modes")]
        [SerializeField] private bool selectCard = false;
        [SerializeField] private bool selectOrder = false;
        [SerializeField] private bool selectRing = false;

        [Header("Prompt Display")]
        [SerializeField] private string menuTitle = "";
        [SerializeField] private string promptTitle = "";

        [Header("UI Elements")]
        [SerializeField] private List<PromptButton> buttons = new List<PromptButton>();
        [SerializeField] private List<object> controls = new List<object>();

        [Header("Selection Lists")]
        [SerializeField] private List<BaseCard> selectableCards = new List<BaseCard>();
        [SerializeField] private List<Ring> selectableRings = new List<Ring>();
        [SerializeField] private List<BaseCard> selectedCards = new List<BaseCard>();

        #endregion

        #region Properties

        /// <summary>
        /// The player this prompt state belongs to
        /// </summary>
        public Player Player
        {
            get => player;
            set => player = value;
        }

        /// <summary>
        /// Whether the player is in card selection mode
        /// </summary>
        public bool SelectCard
        {
            get => selectCard;
            set => selectCard = value;
        }

        /// <summary>
        /// Whether the player is selecting cards in a specific order
        /// </summary>
        public bool SelectOrder
        {
            get => selectOrder;
            set => selectOrder = value;
        }

        /// <summary>
        /// Whether the player is in ring selection mode
        /// </summary>
        public bool SelectRing
        {
            get => selectRing;
            set => selectRing = value;
        }

        /// <summary>
        /// Title displayed in the menu area
        /// </summary>
        public string MenuTitle
        {
            get => menuTitle;
            set => menuTitle = value ?? "";
        }

        /// <summary>
        /// Title displayed in the prompt area
        /// </summary>
        public string PromptTitle
        {
            get => promptTitle;
            set => promptTitle = value ?? "";
        }

        /// <summary>
        /// List of buttons available to the player
        /// </summary>
        public List<PromptButton> Buttons
        {
            get => buttons;
            set => buttons = value ?? new List<PromptButton>();
        }

        /// <summary>
        /// List of controls/UI elements for the prompt
        /// </summary>
        public List<object> Controls
        {
            get => controls;
            set => controls = value ?? new List<object>();
        }

        /// <summary>
        /// Cards that can be selected by the player
        /// </summary>
        public List<BaseCard> SelectableCards
        {
            get => selectableCards;
            set => selectableCards = value ?? new List<BaseCard>();
        }

        /// <summary>
        /// Rings that can be selected by the player
        /// </summary>
        public List<Ring> SelectableRings
        {
            get => selectableRings;
            set => selectableRings = value ?? new List<Ring>();
        }

        /// <summary>
        /// Cards currently selected by the player
        /// </summary>
        public List<BaseCard> SelectedCards
        {
            get => selectedCards;
            set => selectedCards = value ?? new List<BaseCard>();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public PlayerPromptState()
        {
            InitializeCollections();
        }

        /// <summary>
        /// Constructor with player reference
        /// </summary>
        /// <param name="ownerPlayer">The player this prompt state belongs to</param>
        public PlayerPromptState(Player ownerPlayer)
        {
            player = ownerPlayer;
            InitializeCollections();
        }

        #endregion

        #region Selected Cards Management

        /// <summary>
        /// Set the list of selected cards
        /// </summary>
        /// <param name="cards">Cards to set as selected</param>
        public void SetSelectedCards(List<BaseCard> cards)
        {
            selectedCards = cards?.ToList() ?? new List<BaseCard>();
        }

        /// <summary>
        /// Clear all selected cards
        /// </summary>
        public void ClearSelectedCards()
        {
            selectedCards.Clear();
        }

        /// <summary>
        /// Add a card to the selected cards list
        /// </summary>
        /// <param name="card">Card to add</param>
        public void AddSelectedCard(BaseCard card)
        {
            if (card != null && !selectedCards.Contains(card))
            {
                selectedCards.Add(card);
            }
        }

        /// <summary>
        /// Remove a card from the selected cards list
        /// </summary>
        /// <param name="card">Card to remove</param>
        public void RemoveSelectedCard(BaseCard card)
        {
            selectedCards.Remove(card);
        }

        /// <summary>
        /// Check if a card is currently selected
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if card is selected</returns>
        public bool IsCardSelected(BaseCard card)
        {
            return selectedCards.Contains(card);
        }

        #endregion

        #region Selectable Cards Management

        /// <summary>
        /// Set the list of selectable cards
        /// </summary>
        /// <param name="cards">Cards to set as selectable</param>
        public void SetSelectableCards(List<BaseCard> cards)
        {
            selectableCards = cards?.ToList() ?? new List<BaseCard>();
        }

        /// <summary>
        /// Clear all selectable cards
        /// </summary>
        public void ClearSelectableCards()
        {
            selectableCards.Clear();
        }

        /// <summary>
        /// Add a card to the selectable cards list
        /// </summary>
        /// <param name="card">Card to add</param>
        public void AddSelectableCard(BaseCard card)
        {
            if (card != null && !selectableCards.Contains(card))
            {
                selectableCards.Add(card);
            }
        }

        /// <summary>
        /// Remove a card from the selectable cards list
        /// </summary>
        /// <param name="card">Card to remove</param>
        public void RemoveSelectableCard(BaseCard card)
        {
            selectableCards.Remove(card);
        }

        /// <summary>
        /// Check if a card is currently selectable
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if card is selectable</returns>
        public bool IsCardSelectable(BaseCard card)
        {
            return selectableCards.Contains(card);
        }

        #endregion

        #region Selectable Rings Management

        /// <summary>
        /// Set the list of selectable rings
        /// </summary>
        /// <param name="rings">Rings to set as selectable</param>
        public void SetSelectableRings(List<Ring> rings)
        {
            selectableRings = rings?.ToList() ?? new List<Ring>();
        }

        /// <summary>
        /// Clear all selectable rings
        /// </summary>
        public void ClearSelectableRings()
        {
            selectableRings.Clear();
        }

        /// <summary>
        /// Add a ring to the selectable rings list
        /// </summary>
        /// <param name="ring">Ring to add</param>
        public void AddSelectableRing(Ring ring)
        {
            if (ring != null && !selectableRings.Contains(ring))
            {
                selectableRings.Add(ring);
            }
        }

        /// <summary>
        /// Remove a ring from the selectable rings list
        /// </summary>
        /// <param name="ring">Ring to remove</param>
        public void RemoveSelectableRing(Ring ring)
        {
            selectableRings.Remove(ring);
        }

        /// <summary>
        /// Check if a ring is currently selectable
        /// </summary>
        /// <param name="ring">Ring to check</param>
        /// <returns>True if ring is selectable</returns>
        public bool IsRingSelectable(Ring ring)
        {
            return selectableRings.Contains(ring);
        }

        #endregion

        #region Prompt Management

        /// <summary>
        /// Set the prompt from a prompt object (matches JavaScript setPrompt method)
        /// </summary>
        /// <param name="prompt">Prompt object containing display data</param>
        public void SetPrompt(object prompt)
        {
            if (prompt == null) return;

            // Handle dictionary-style prompt
            if (prompt is Dictionary<string, object> promptDict)
            {
                SetPromptFromDictionary(promptDict);
                return;
            }

            // Handle other prompt types through reflection
            var promptType = prompt.GetType();
            
            selectCard = GetBoolProperty(prompt, "selectCard", false);
            selectOrder = GetBoolProperty(prompt, "selectOrder", false);
            selectRing = GetBoolProperty(prompt, "selectRing", false);
            menuTitle = GetStringProperty(prompt, "menuTitle", "");
            promptTitle = GetStringProperty(prompt, "promptTitle", "");

            // Handle buttons
            var buttonsProperty = promptType.GetProperty("buttons");
            if (buttonsProperty != null)
            {
                var buttonsValue = buttonsProperty.GetValue(prompt);
                SetButtonsFromObject(buttonsValue);
            }

            // Handle controls
            var controlsProperty = promptType.GetProperty("controls");
            if (controlsProperty != null)
            {
                var controlsValue = controlsProperty.GetValue(prompt);
                if (controlsValue is IEnumerable<object> controlsList)
                {
                    controls = controlsList.ToList();
                }
                else
                {
                    controls.Clear();
                }
            }
        }

        /// <summary>
        /// Cancel the current prompt and reset state
        /// </summary>
        public void CancelPrompt()
        {
            selectCard = false;
            selectRing = false;
            selectOrder = false;
            menuTitle = "";
            promptTitle = "";
            buttons.Clear();
            controls.Clear();
            // Note: We don't clear selectable/selected items on cancel as they may be needed
        }

        #endregion

        #region Selection State Methods

        /// <summary>
        /// Get the selection state for a card (matches JavaScript getCardSelectionState method)
        /// </summary>
        /// <param name="card">Card to get selection state for</param>
        /// <returns>Selection state information</returns>
        public CardSelectionState GetCardSelectionState(BaseCard card)
        {
            if (card == null)
            {
                return new CardSelectionState();
            }

            bool selectable = selectableCards.Contains(card);
            int selectedIndex = selectedCards.IndexOf(card);
            bool isSelected = card.selected || (selectedIndex != -1);

            var state = new CardSelectionState
            {
                selected = isSelected,
                selectable = selectable,
                unselectable = selectCard && !selectable
            };

            // Add order information if in order selection mode
            if (selectedIndex != -1 && selectOrder)
            {
                state.order = selectedIndex + 1;
            }

            return state;
        }

        /// <summary>
        /// Get the selection state for a ring (matches JavaScript getRingSelectionState method)
        /// </summary>
        /// <param name="ring">Ring to get selection state for</param>
        /// <returns>Selection state information</returns>
        public RingSelectionState GetRingSelectionState(Ring ring)
        {
            if (ring == null)
            {
                return new RingSelectionState { unselectable = true };
            }

            if (selectRing)
            {
                return new RingSelectionState
                {
                    unselectable = !selectableRings.Contains(ring)
                };
            }

            // Default ring selection logic when not in ring selection mode
            bool isUnselectable = ring.game?.currentConflict != null && !ring.contested;
            return new RingSelectionState
            {
                unselectable = isUnselectable
            };
        }

        #endregion

        #region State Export

        /// <summary>
        /// Get the complete state for this prompt (matches JavaScript getState method)
        /// </summary>
        /// <returns>Complete prompt state</returns>
        public PromptState GetState()
        {
            return new PromptState
            {
                selectCard = selectCard,
                selectOrder = selectOrder,
                selectRing = selectRing,
                menuTitle = menuTitle,
                promptTitle = promptTitle,
                buttons = buttons.Select(b => b.ToDictionary()).ToList(),
                controls = new List<object>(controls)
            };
        }

        /// <summary>
        /// Get state as dictionary for serialization
        /// </summary>
        /// <returns>Dictionary representation of state</returns>
        public Dictionary<string, object> GetStateDictionary()
        {
            return new Dictionary<string, object>
            {
                ["selectCard"] = selectCard,
                ["selectOrder"] = selectOrder,
                ["selectRing"] = selectRing,
                ["menuTitle"] = menuTitle,
                ["promptTitle"] = promptTitle,
                ["buttons"] = buttons.Select(b => b.ToDictionary()).ToList(),
                ["controls"] = new List<object>(controls)
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Initialize all collections
        /// </summary>
        private void InitializeCollections()
        {
            buttons = new List<PromptButton>();
            controls = new List<object>();
            selectableCards = new List<BaseCard>();
            selectableRings = new List<Ring>();
            selectedCards = new List<BaseCard>();
        }

        /// <summary>
        /// Set prompt from dictionary object
        /// </summary>
        /// <param name="promptDict">Dictionary containing prompt data</param>
        private void SetPromptFromDictionary(Dictionary<string, object> promptDict)
        {
            selectCard = GetDictBoolValue(promptDict, "selectCard", false);
            selectOrder = GetDictBoolValue(promptDict, "selectOrder", false);
            selectRing = GetDictBoolValue(promptDict, "selectRing", false);
            menuTitle = GetDictStringValue(promptDict, "menuTitle", "");
            promptTitle = GetDictStringValue(promptDict, "promptTitle", "");

            // Handle buttons
            if (promptDict.ContainsKey("buttons"))
            {
                SetButtonsFromObject(promptDict["buttons"]);
            }
            else
            {
                buttons.Clear();
            }

            // Handle controls
            if (promptDict.ContainsKey("controls") && promptDict["controls"] is IEnumerable<object> controlsList)
            {
                controls = controlsList.ToList();
            }
            else
            {
                controls.Clear();
            }
        }

        /// <summary>
        /// Set buttons from various object types
        /// </summary>
        /// <param name="buttonsObj">Buttons object</param>
        private void SetButtonsFromObject(object buttonsObj)
        {
            buttons.Clear();

            if (buttonsObj is IEnumerable<object> buttonsList)
            {
                foreach (var buttonObj in buttonsList)
                {
                    var button = CreateButtonFromObject(buttonObj);
                    if (button != null)
                    {
                        buttons.Add(button);
                    }
                }
            }
        }

        /// <summary>
        /// Create a PromptButton from various object types (matches JavaScript button transformation)
        /// </summary>
        /// <param name="buttonObj">Button object</param>
        /// <returns>Created PromptButton or null</returns>
        private PromptButton CreateButtonFromObject(object buttonObj)
        {
            if (buttonObj == null) return null;

            // Handle dictionary-style button
            if (buttonObj is Dictionary<string, object> buttonDict)
            {
                return CreateButtonFromDictionary(buttonDict);
            }

            // Handle PromptButton directly
            if (buttonObj is PromptButton existingButton)
            {
                return existingButton;
            }

            // Handle other object types through reflection
            var buttonType = buttonObj.GetType();
            var button = new PromptButton();

            // Check if button has a card property (special case from JavaScript)
            var cardProperty = buttonType.GetProperty("card");
            if (cardProperty != null && cardProperty.GetValue(buttonObj) is BaseCard card)
            {
                // Transform button with card (matches JavaScript logic)
                button.text = card.name;
                button.arg = card.uuid;
                button.card = card.GetShortSummary();

                // Copy other properties except 'card'
                foreach (var prop in buttonType.GetProperties())
                {
                    if (prop.Name != "card" && prop.CanRead)
                    {
                        var value = prop.GetValue(buttonObj);
                        SetButtonProperty(button, prop.Name, value);
                    }
                }
            }
            else
            {
                // Copy all properties
                foreach (var prop in buttonType.GetProperties())
                {
                    if (prop.CanRead)
                    {
                        var value = prop.GetValue(buttonObj);
                        SetButtonProperty(button, prop.Name, value);
                    }
                }
            }

            return button;
        }

        /// <summary>
        /// Create button from dictionary
        /// </summary>
        /// <param name="buttonDict">Button dictionary</param>
        /// <returns>Created button</returns>
        private PromptButton CreateButtonFromDictionary(Dictionary<string, object> buttonDict)
        {
            var button = new PromptButton();

            // Handle card transformation (matches JavaScript)
            if (buttonDict.ContainsKey("card") && buttonDict["card"] is BaseCard card)
            {
                button.text = card.name;
                button.arg = card.uuid;
                button.card = card.GetShortSummary();

                // Copy other properties except 'card'
                foreach (var kvp in buttonDict)
                {
                    if (kvp.Key != "card")
                    {
                        SetButtonProperty(button, kvp.Key, kvp.Value);
                    }
                }
            }
            else
            {
                // Copy all properties
                foreach (var kvp in buttonDict)
                {
                    SetButtonProperty(button, kvp.Key, kvp.Value);
                }
            }

            return button;
        }

        /// <summary>
        /// Set property on button by name
        /// </summary>
        /// <param name="button">Button to modify</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Property value</param>
        private void SetButtonProperty(PromptButton button, string propertyName, object value)
        {
            switch (propertyName.ToLower())
            {
                case "text":
                    button.text = value?.ToString() ?? "";
                    break;
                case "arg":
                    button.arg = value?.ToString() ?? "";
                    break;
                case "method":
                    button.method = value?.ToString() ?? "";
                    break;
                case "command":
                    button.command = value?.ToString() ?? "";
                    break;
                case "card":
                    button.card = value;
                    break;
                case "disabled":
                    button.disabled = value is bool boolVal && boolVal;
                    break;
                default:
                    button.SetAdditionalProperty(propertyName, value);
                    break;
            }
        }

        /// <summary>
        /// Get boolean property from object
        /// </summary>
        /// <param name="obj">Object to read from</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Boolean value</returns>
        private bool GetBoolProperty(object obj, string propertyName, bool defaultValue)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property?.CanRead == true)
            {
                var value = property.GetValue(obj);
                if (value is bool boolValue)
                    return boolValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get string property from object
        /// </summary>
        /// <param name="obj">Object to read from</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>String value</returns>
        private string GetStringProperty(object obj, string propertyName, string defaultValue)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property?.CanRead == true)
            {
                var value = property.GetValue(obj);
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get boolean value from dictionary
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Boolean value</returns>
        private bool GetDictBoolValue(Dictionary<string, object> dict, string key, bool defaultValue)
        {
            if (dict.ContainsKey(key) && dict[key] is bool boolValue)
                return boolValue;
            return defaultValue;
        }

        /// <summary>
        /// Get string value from dictionary
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>String value</returns>
        private string GetDictStringValue(Dictionary<string, object> dict, string key, string defaultValue)
        {
            if (dict.ContainsKey(key))
                return dict[key]?.ToString() ?? defaultValue;
            return defaultValue;
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Represents a button in the prompt UI
    /// </summary>
    [System.Serializable]
    public class PromptButton
    {
        public string text = "";
        public string arg = "";
        public string method = "";
        public string command = "";
        public object card;
        public bool disabled = false;
        
        private Dictionary<string, object> additionalProperties = new Dictionary<string, object>();

        /// <summary>
        /// Set additional property on the button
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        public void SetAdditionalProperty(string key, object value)
        {
            additionalProperties[key] = value;
        }

        /// <summary>
        /// Get additional property from the button
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Property value</returns>
        public object GetAdditionalProperty(string key)
        {
            return additionalProperties.GetValueOrDefault(key);
        }

        /// <summary>
        /// Convert to dictionary for serialization
        /// </summary>
        /// <returns>Dictionary representation</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>(additionalProperties);
            
            if (!string.IsNullOrEmpty(text)) dict["text"] = text;
            if (!string.IsNullOrEmpty(arg)) dict["arg"] = arg;
            if (!string.IsNullOrEmpty(method)) dict["method"] = method;
            if (!string.IsNullOrEmpty(command)) dict["command"] = command;
            if (card != null) dict["card"] = card;
            if (disabled) dict["disabled"] = disabled;

            return dict;
        }
    }

    /// <summary>
    /// Card selection state information
    /// </summary>
    [System.Serializable]
    public class CardSelectionState
    {
        public bool selected = false;
        public bool selectable = false;
        public bool unselectable = false;
        public int order = 0; // Used when selectOrder is true

        /// <summary>
        /// Convert to dictionary
        /// </summary>
        /// <returns>Dictionary representation</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["selected"] = selected,
                ["selectable"] = selectable,
                ["unselectable"] = unselectable
            };

            if (order > 0)
            {
                dict["order"] = order;
            }

            return dict;
        }
    }

    /// <summary>
    /// Ring selection state information
    /// </summary>
    [System.Serializable]
    public class RingSelectionState
    {
        public bool unselectable = false;

        /// <summary>
        /// Convert to dictionary
        /// </summary>
        /// <returns>Dictionary representation</returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["unselectable"] = unselectable
            };
        }
    }

    /// <summary>
    /// Complete prompt state
    /// </summary>
    [System.Serializable]
    public class PromptState
    {
        public bool selectCard = false;
        public bool selectOrder = false;
        public bool selectRing = false;
        public string menuTitle = "";
        public string promptTitle = "";
        public List<object> buttons = new List<object>();
        public List<object> controls = new List<object>();
    }

    #endregion
}
