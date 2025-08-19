using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace L5RGame
{
    /// <summary>
    /// DeckBuilder provides comprehensive deck building functionality for the L5R card game,
    /// including deck validation, import/export, and mobile-optimized UI management.
    /// </summary>
    public class DeckBuilder : MonoBehaviour
    {
        [Header("UI References")]
        public InputField deckNameInput;
        public Dropdown formatDropdown;
        public Dropdown clanDropdown;
        public Dropdown allianceDropdown;
        public InputField cardSearchInput;
        public InputField numberToAddInput;
        public Button addCardButton;
        public InputField cardListTextArea;
        public Button saveDeckButton;
        public Button importDeckButton;
        public Button exportDeckButton;
        public Button clearDeckButton;

        [Header("Mobile UI")]
        public ScrollRect cardScrollView;
        public GameObject cardItemPrefab;
        public Transform cardListParent;
        public GameObject deckValidationPanel;
        public Text validationErrorText;

        [Header("Deck Settings")]
        public DeckFormat defaultFormat = DeckFormat.Emerald;
        public string defaultClan = "crab";

        // Current deck being edited
        private Deck currentDeck;
        private bool isLoading = false;
        private int numberToAdd = 1;

        // Card database references
        private CardDatabase cardDatabase;
        private List<BaseCard> availableCards = new List<BaseCard>();
        private List<BaseCard> filteredCards = new List<BaseCard>();

        // Validation
        private DeckValidator deckValidator;
        private List<string> validationErrors = new List<string>();

        // Events
        public System.Action<Deck> OnDeckSaved;
        public System.Action<Deck> OnDeckLoaded;
        public System.Action<string> OnValidationChanged;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeDeckBuilder();
        }

        private void Start()
        {
            SetupUI();
            LoadDefaultDeck();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Initialization
        private void InitializeDeckBuilder()
        {
            cardDatabase = FindObjectOfType<CardDatabase>();
            deckValidator = new DeckValidator();
            
            if (cardDatabase != null)
            {
                availableCards = cardDatabase.GetAllCards();
                filteredCards = new List<BaseCard>(availableCards);
            }

            currentDeck = new Deck { name = "New Deck" };
        }

        private void SetupUI()
        {
            // Setup dropdowns
            SetupFormatDropdown();
            SetupClanDropdown();
            SetupAllianceDropdown();

            // Setup input field events
            if (deckNameInput != null)
            {
                deckNameInput.onValueChanged.AddListener(OnDeckNameChanged);
            }

            if (cardSearchInput != null)
            {
                cardSearchInput.onValueChanged.AddListener(OnCardSearchChanged);
            }

            if (numberToAddInput != null)
            {
                numberToAddInput.onValueChanged.AddListener(OnNumberToAddChanged);
                numberToAddInput.text = numberToAdd.ToString();
            }

            if (cardListTextArea != null)
            {
                cardListTextArea.onValueChanged.AddListener(OnCardListTextChanged);
            }

            // Setup button events
            if (addCardButton != null)
                addCardButton.onClick.AddListener(OnAddCardClicked);

            if (saveDeckButton != null)
                saveDeckButton.onClick.AddListener(OnSaveDeckClicked);

            if (importDeckButton != null)
                importDeckButton.onClick.AddListener(OnImportDeckClicked);

            if (exportDeckButton != null)
                exportDeckButton.onClick.AddListener(OnExportDeckClicked);

            if (clearDeckButton != null)
                clearDeckButton.onClick.AddListener(OnClearDeckClicked);
        }

        private void SetupFormatDropdown()
        {
            if (formatDropdown == null) return;

            formatDropdown.ClearOptions();
            var formatOptions = new List<string>
            {
                "Emerald", "Stronghold", "Skirmish", "Enlightenment"
            };

            formatDropdown.AddOptions(formatOptions);
            formatDropdown.onValueChanged.AddListener(OnFormatChanged);
        }

        private void SetupClanDropdown()
        {
            if (clanDropdown == null) return;

            clanDropdown.ClearOptions();
            var clanOptions = new List<string>
            {
                "Crab", "Crane", "Dragon", "Lion", "Phoenix", "Scorpion", "Unicorn"
            };

            clanDropdown.AddOptions(clanOptions);
            clanDropdown.onValueChanged.AddListener(OnClanChanged);
        }

        private void SetupAllianceDropdown()
        {
            if (allianceDropdown == null) return;

            allianceDropdown.ClearOptions();
            var allianceOptions = new List<string>
            {
                "None", "Crab", "Crane", "Dragon", "Lion", "Phoenix", "Scorpion", "Unicorn"
            };

            allianceDropdown.AddOptions(allianceOptions);
            allianceDropdown.onValueChanged.AddListener(OnAllianceChanged);
        }

        private void SubscribeToEvents()
        {
            if (cardDatabase != null)
            {
                cardDatabase.OnCardsLoaded += OnCardsLoaded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (cardDatabase != null)
            {
                cardDatabase.OnCardsLoaded -= OnCardsLoaded;
            }
        }
        #endregion

        #region Deck Management
        private void LoadDefaultDeck()
        {
            currentDeck = new Deck
            {
                name = "New Deck",
                format = defaultFormat,
                clan = defaultClan,
                alliance = "",
                strongholdCards = new List<DeckCard>(),
                roleCards = new List<DeckCard>(),
                provinceCards = new List<DeckCard>(),
                dynastyCards = new List<DeckCard>(),
                conflictCards = new List<DeckCard>()
            };

            UpdateUI();
        }

        public void LoadDeck(Deck deck)
        {
            if (deck == null)
            {
                LoadDefaultDeck();
                return;
            }

            currentDeck = CopyDeck(deck);
            UpdateUI();
            UpdateCardListText();
            ValidateDeck();

            OnDeckLoaded?.Invoke(currentDeck);
        }

        public void SaveDeck()
        {
            if (string.IsNullOrEmpty(currentDeck.name))
            {
                currentDeck.name = "Unnamed Deck";
            }

            ValidateDeck();

            // Save to persistent storage
            var deckJson = JsonConvert.SerializeObject(currentDeck, Formatting.Indented);
            var filePath = $"{Application.persistentDataPath}/Decks/{currentDeck.name}.json";
            
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                System.IO.File.WriteAllText(filePath, deckJson);
                
                Debug.Log($"Deck saved: {currentDeck.name}");
                OnDeckSaved?.Invoke(currentDeck);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save deck: {ex.Message}");
            }
        }

        private Deck CopyDeck(Deck source)
        {
            if (source == null) return new Deck { name = "New Deck" };

            return new Deck
            {
                id = source.id,
                name = source.name,
                format = source.format,
                clan = source.clan,
                alliance = source.alliance,
                strongholdCards = new List<DeckCard>(source.strongholdCards),
                roleCards = new List<DeckCard>(source.roleCards),
                provinceCards = new List<DeckCard>(source.provinceCards),
                dynastyCards = new List<DeckCard>(source.dynastyCards),
                conflictCards = new List<DeckCard>(source.conflictCards)
            };
        }
        #endregion

        #region Card Management
        public void AddCard(BaseCard card, int count = 1)
        {
            if (card == null || count <= 0) return;

            var deckCard = new DeckCard { card = card, count = count };
            var targetList = GetCardList(card);

            var existingCard = targetList.FirstOrDefault(dc => dc.card.id == card.id);
            if (existingCard != null)
            {
                existingCard.count += count;
            }
            else
            {
                targetList.Add(deckCard);
            }

            UpdateCardListText();
            ValidateDeck();
        }

        public void RemoveCard(BaseCard card, int count = 1)
        {
            if (card == null) return;

            var targetList = GetCardList(card);
            var existingCard = targetList.FirstOrDefault(dc => dc.card.id == card.id);

            if (existingCard != null)
            {
                existingCard.count -= count;
                if (existingCard.count <= 0)
                {
                    targetList.Remove(existingCard);
                }
            }

            UpdateCardListText();
            ValidateDeck();
        }

        public void SetCardCount(BaseCard card, int count)
        {
            if (card == null) return;

            var targetList = GetCardList(card);
            var existingCard = targetList.FirstOrDefault(dc => dc.card.id == card.id);

            if (count <= 0)
            {
                if (existingCard != null)
                {
                    targetList.Remove(existingCard);
                }
            }
            else
            {
                if (existingCard != null)
                {
                    existingCard.count = count;
                }
                else
                {
                    targetList.Add(new DeckCard { card = card, count = count });
                }
            }

            UpdateCardListText();
            ValidateDeck();
        }

        private List<DeckCard> GetCardList(BaseCard card)
        {
            switch (card.type)
            {
                case CardTypes.Province:
                    return currentDeck.provinceCards;
                case CardTypes.Stronghold:
                    return currentDeck.strongholdCards;
                case CardTypes.Role:
                    return currentDeck.roleCards;
                default:
                    if (card is DrawCard drawCard)
                    {
                        return drawCard.isConflict ? currentDeck.conflictCards : currentDeck.dynastyCards;
                    }
                    return currentDeck.conflictCards;
            }
        }
        #endregion

        #region Card Search and Filtering
        private void FilterCards(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                filteredCards = new List<BaseCard>(availableCards);
            }
            else
            {
                var lowerSearch = searchText.ToLower();
                filteredCards = availableCards.Where(card =>
                    card.name.ToLower().Contains(lowerSearch) ||
                    card.GetTraits().Any(trait => trait.ToLower().Contains(lowerSearch)) ||
                    card.text.ToLower().Contains(lowerSearch)
                ).ToList();
            }

            UpdateCardSearchResults();
        }

        private void UpdateCardSearchResults()
        {
            // Clear existing results
            foreach (Transform child in cardListParent)
            {
                Destroy(child.gameObject);
            }

            // Add filtered cards to UI
            int maxResults = Mathf.Min(filteredCards.Count, 50); // Limit for performance
            for (int i = 0; i < maxResults; i++)
            {
                var card = filteredCards[i];
                var cardItem = Instantiate(cardItemPrefab, cardListParent);
                var cardItemComponent = cardItem.GetComponent<DeckBuilderCardItem>();
                
                if (cardItemComponent != null)
                {
                    cardItemComponent.Setup(card, this);
                }
            }
        }
        #endregion

        #region Text-based Deck Import/Export
        private void ParseCardListText(string cardListText)
        {
            if (string.IsNullOrEmpty(cardListText)) return;

            // Clear current deck
            currentDeck.strongholdCards.Clear();
            currentDeck.roleCards.Clear();
            currentDeck.provinceCards.Clear();
            currentDeck.dynastyCards.Clear();
            currentDeck.conflictCards.Clear();

            var lines = cardListText.Split('\n');
            foreach (var line in lines)
            {
                ParseCardLine(line.Trim());
            }

            ValidateDeck();
        }

        private void ParseCardLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;

            // Match pattern: "3 Card Name (Pack Name)" or "3x Card Name (Pack Name)"
            var match = Regex.Match(line, @"^(\d+)x?\s+(.+?)(?:\s+\((.+?)\))?$");
            if (!match.Success) return;

            if (!int.TryParse(match.Groups[1].Value, out int count)) return;

            var cardName = match.Groups[2].Value.Trim();
            var packName = match.Groups[3].Value.Trim();

            var card = FindCardByName(cardName, packName);
            if (card != null)
            {
                AddCard(card, count);
            }
        }

        private BaseCard FindCardByName(string cardName, string packName = "")
        {
            var candidates = availableCards.Where(card => 
                card.name.Equals(cardName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];

            // If multiple cards with same name, try to match by pack
            if (!string.IsNullOrEmpty(packName))
            {
                var packMatch = candidates.FirstOrDefault(card => 
                    card.packName.Equals(packName, StringComparison.OrdinalIgnoreCase));
                if (packMatch != null) return packMatch;
            }

            // Return first match if no pack specified or pack not found
            return candidates[0];
        }

        private void UpdateCardListText()
        {
            var sb = new StringBuilder();

            // Add cards in order: Stronghold, Role, Provinces, Dynasty, Conflict
            AddCardsToText(sb, currentDeck.strongholdCards);
            AddCardsToText(sb, currentDeck.roleCards);
            AddCardsToText(sb, currentDeck.provinceCards);
            AddCardsToText(sb, currentDeck.dynastyCards);
            AddCardsToText(sb, currentDeck.conflictCards);

            if (cardListTextArea != null)
            {
                cardListTextArea.text = sb.ToString();
            }
        }

        private void AddCardsToText(StringBuilder sb, List<DeckCard> cards)
        {
            foreach (var deckCard in cards.OrderBy(dc => dc.card.name))
            {
                sb.AppendLine(GetCardListEntry(deckCard.count, deckCard.card));
            }
        }

        private string GetCardListEntry(int count, BaseCard card)
        {
            var packName = !string.IsNullOrEmpty(card.packName) ? $" ({card.packName})" : "";
            return $"{count} {card.name}{packName}";
        }
        #endregion

        #region URL Import (Emerald DB / Five Rings DB)
        public void ImportDeckFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (url.Contains("fiveringsdb.com"))
            {
                ImportFromFiveRingsDB(url);
            }
            else if (url.Contains("emeralddb.org"))
            {
                ImportFromEmeraldDB(url);
            }
            else
            {
                Debug.LogWarning($"Unsupported deck URL: {url}");
            }
        }

        private void ImportFromEmeraldDB(string url)
        {
            // Implementation would use Unity's networking to fetch deck data
            // For now, this is a placeholder that would be implemented with UnityWebRequest
            Debug.Log($"Would import from Emerald DB: {url}");
        }

        private void ImportFromFiveRingsDB(string url)
        {
            // Implementation would use Unity's networking to fetch deck data
            // For now, this is a placeholder that would be implemented with UnityWebRequest
            Debug.Log($"Would import from Five Rings DB: {url}");
        }
        #endregion

        #region Deck Validation
        private void ValidateDeck()
        {
            validationErrors.Clear();

            if (deckValidator != null)
            {
                validationErrors = deckValidator.ValidateDeck(currentDeck);
            }

            UpdateValidationUI();
            OnValidationChanged?.Invoke(string.Join("\n", validationErrors));
        }

        private void UpdateValidationUI()
        {
            bool isValid = validationErrors.Count == 0;

            if (deckValidationPanel != null)
            {
                deckValidationPanel.SetActive(!isValid);
            }

            if (validationErrorText != null && !isValid)
            {
                validationErrorText.text = string.Join("\n", validationErrors);
            }

            if (saveDeckButton != null)
            {
                saveDeckButton.interactable = isValid;
            }
        }
        #endregion

        #region UI Event Handlers
        private void OnDeckNameChanged(string newName)
        {
            currentDeck.name = newName;
        }

        private void OnFormatChanged(int formatIndex)
        {
            currentDeck.format = (DeckFormat)formatIndex;
            ValidateDeck();
        }

        private void OnClanChanged(int clanIndex)
        {
            var clans = new[] { "crab", "crane", "dragon", "lion", "phoenix", "scorpion", "unicorn" };
            if (clanIndex >= 0 && clanIndex < clans.Length)
            {
                currentDeck.clan = clans[clanIndex];
                ValidateDeck();
            }
        }

        private void OnAllianceChanged(int allianceIndex)
        {
            var alliances = new[] { "", "crab", "crane", "dragon", "lion", "phoenix", "scorpion", "unicorn" };
            if (allianceIndex >= 0 && allianceIndex < alliances.Length)
            {
                currentDeck.alliance = alliances[allianceIndex];
                ValidateDeck();
            }
        }

        private void OnCardSearchChanged(string searchText)
        {
            FilterCards(searchText);
        }

        private void OnNumberToAddChanged(string numberText)
        {
            if (int.TryParse(numberText, out int number) && number > 0)
            {
                numberToAdd = number;
            }
        }

        private void OnCardListTextChanged(string cardListText)
        {
            ParseCardListText(cardListText);
        }

        private void OnAddCardClicked()
        {
            // This would be triggered by selecting a card from search results
            // Implementation depends on how card selection UI is set up
        }

        private void OnSaveDeckClicked()
        {
            SaveDeck();
        }

        private void OnImportDeckClicked()
        {
            // Show import dialog or open URL input
            ShowImportDialog();
        }

        private void OnExportDeckClicked()
        {
            ExportDeck();
        }

        private void OnClearDeckClicked()
        {
            LoadDefaultDeck();
        }

        private void OnCardsLoaded(List<BaseCard> cards)
        {
            availableCards = cards;
            filteredCards = new List<BaseCard>(availableCards);
            UpdateCardSearchResults();
        }
        #endregion

        #region Export/Import Dialogs
        private void ShowImportDialog()
        {
            // This would show a mobile-friendly dialog for URL input
            // For now, this is a placeholder
            Debug.Log("Show import dialog");
        }

        private void ExportDeck()
        {
            var cardListText = cardListTextArea?.text ?? "";
            if (string.IsNullOrEmpty(cardListText)) return;

            // Copy to clipboard or share via mobile sharing
            GUIUtility.systemCopyBuffer = cardListText;
            Debug.Log("Deck exported to clipboard");
        }
        #endregion

        #region Public API
        public Deck GetCurrentDeck()
        {
            return CopyDeck(currentDeck);
        }

        public bool IsDeckValid()
        {
            return validationErrors.Count == 0;
        }

        public List<string> GetValidationErrors()
        {
            return new List<string>(validationErrors);
        }

        public void ClearDeck()
        {
            LoadDefaultDeck();
        }

        public int GetCardCount(BaseCard card)
        {
            var targetList = GetCardList(card);
            var deckCard = targetList.FirstOrDefault(dc => dc.card.id == card.id);
            return deckCard?.count ?? 0;
        }

        public int GetTotalCardCount()
        {
            return currentDeck.dynastyCards.Sum(dc => dc.count) +
                   currentDeck.conflictCards.Sum(dc => dc.count);
        }

        public int GetInfluenceCost()
        {
            // Calculate total influence cost of out-of-clan cards
            var clanCards = availableCards.Where(card => card.GetFaction() == currentDeck.clan);
            var totalInfluence = 0;

            foreach (var deckCard in currentDeck.dynastyCards.Concat(currentDeck.conflictCards))
            {
                if (deckCard.card.GetFaction() != currentDeck.clan)
                {
                    totalInfluence += deckCard.card.GetInfluenceCost() * deckCard.count;
                }
            }

            return totalInfluence;
        }
        #endregion

        #region Utility
        private void UpdateUI()
        {
            if (deckNameInput != null)
                deckNameInput.text = currentDeck.name;

            if (formatDropdown != null)
                formatDropdown.value = (int)currentDeck.format;

            if (clanDropdown != null)
            {
                var clans = new[] { "crab", "crane", "dragon", "lion", "phoenix", "scorpion", "unicorn" };
                var clanIndex = Array.IndexOf(clans, currentDeck.clan);
                if (clanIndex >= 0) clanDropdown.value = clanIndex;
            }

            if (allianceDropdown != null)
            {
                var alliances = new[] { "", "crab", "crane", "dragon", "lion", "phoenix", "scorpion", "unicorn" };
                var allianceIndex = Array.IndexOf(alliances, currentDeck.alliance);
                if (allianceIndex >= 0) allianceDropdown.value = allianceIndex;
            }
        }
        #endregion
    }

    #region Supporting Classes
    [System.Serializable]
    public class Deck
    {
        public string id;
        public string name;
        public DeckFormat format;
        public string clan;
        public string alliance;
        public List<DeckCard> strongholdCards = new List<DeckCard>();
        public List<DeckCard> roleCards = new List<DeckCard>();
        public List<DeckCard> provinceCards = new List<DeckCard>();
        public List<DeckCard> dynastyCards = new List<DeckCard>();
        public List<DeckCard> conflictCards = new List<DeckCard>();
    }

    [System.Serializable]
    public class DeckCard
    {
        public BaseCard card;
        public int count;
    }

    [System.Serializable]
    public enum DeckFormat
    {
        Emerald = 0,
        Stronghold = 1,
        Skirmish = 2,
        Enlightenment = 3
    }

    public class DeckValidator
    {
        public List<string> ValidateDeck(Deck deck)
        {
            var errors = new List<string>();

            // Basic validation
            if (string.IsNullOrEmpty(deck.name))
                errors.Add("Deck must have a name");

            if (string.IsNullOrEmpty(deck.clan))
                errors.Add("Deck must have a clan");

            // Stronghold validation
            if (deck.strongholdCards.Count != 1)
                errors.Add("Deck must have exactly 1 stronghold");

            // Role validation
            if (deck.roleCards.Count != 1)
                errors.Add("Deck must have exactly 1 role");

            // Province validation
            if (deck.provinceCards.Count != 5)
                errors.Add("Deck must have exactly 5 provinces");

            // Dynasty deck validation
            var dynastyCount = deck.dynastyCards.Sum(dc => dc.count);
            if (dynastyCount < 40 || dynastyCount > 45)
                errors.Add($"Dynasty deck must have 40-45 cards (current: {dynastyCount})");

            // Conflict deck validation
            var conflictCount = deck.conflictCards.Sum(dc => dc.count);
            if (conflictCount < 40 || conflictCount > 45)
                errors.Add($"Conflict deck must have 40-45 cards (current: {conflictCount})");

            // Card limit validation
            foreach (var deckCard in deck.dynastyCards.Concat(deck.conflictCards))
            {
                if (deckCard.count > 3)
                    errors.Add($"{deckCard.card.name} has more than 3 copies ({deckCard.count})");
            }

            return errors;
        }
    }

    // Component for individual card items in the deck builder UI
    public class DeckBuilderCardItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public Text cardNameText;
        public Text cardCostText;
        public Text cardTypeText;
        public Button addButton;
        public Button removeButton;
        public Text countText;

        private BaseCard card;
        private DeckBuilder deckBuilder;

        public void Setup(BaseCard card, DeckBuilder deckBuilder)
        {
            this.card = card;
            this.deckBuilder = deckBuilder;

            UpdateDisplay();

            if (addButton != null)
                addButton.onClick.AddListener(() => deckBuilder.AddCard(card, 1));

            if (removeButton != null)
                removeButton.onClick.AddListener(() => deckBuilder.RemoveCard(card, 1));
        }

        private void UpdateDisplay()
        {
            if (cardNameText != null)
                cardNameText.text = card.name;

            if (cardCostText != null && card is DrawCard drawCard)
                cardCostText.text = drawCard.GetCost().ToString();

            if (cardTypeText != null)
                cardTypeText.text = card.type;

            if (countText != null)
            {
                int count = deckBuilder.GetCardCount(card);
                countText.text = count > 0 ? count.ToString() : "";
            }
        }
    }
    #endregion
}