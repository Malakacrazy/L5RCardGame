using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace L5RGame.UI
{
    /// <summary>
    /// Unity UI component representing a province display area.
    /// Handles province cards, dynasty cards, and stronghold cards with drag-and-drop support.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class Province : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Inspector Fields

        [Header("Province Configuration")]
        [SerializeField] private string source = "";
        [SerializeField] private string title = "";
        [SerializeField] private ProvinceSize size = ProvinceSize.Normal;
        [SerializeField] private ProvinceOrientation orientation = ProvinceOrientation.Vertical;
        
        [Header("Display Options")]
        [SerializeField] private bool hiddenProvinceCard = false;
        [SerializeField] private bool hiddenDynastyCard = false;
        [SerializeField] private bool showDynastyRow = true;
        [SerializeField] private bool isMe = false;
        [SerializeField] private bool isBroken = false;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        [Header("Card Prefabs")]
        [SerializeField] private GameObject cardPrefab;
        
        [Header("Layout Settings")]
        [SerializeField] private float attachmentOffset = 13f;
        [SerializeField] private float cardHeight = 84f;
        [SerializeField] private Vector2 cardSpacing = new Vector2(10f, 5f);

        #endregion

        #region Private Fields

        private List<BaseCard> cards = new List<BaseCard>();
        private BaseCard provinceCard;
        private List<BaseCard> dynastyCards = new List<BaseCard>();
        private BaseCard strongholdCard;
        
        private List<CardComponent> cardComponents = new List<CardComponent>();
        private RectTransform rectTransform;
        private bool isDragHighlighted = false;

        #endregion

        #region Events

        public event Action<BaseCard, string, string> OnDragDrop;
        public event Action<BaseCard> OnCardClick;
        public event Action<BaseCard, string> OnMenuItemClick;
        public event Action<BaseCard> OnMouseOver;
        public event Action<BaseCard> OnMouseOut;

        #endregion

        #region Properties

        /// <summary>
        /// Source location identifier for this province
        /// </summary>
        public string Source
        {
            get => source;
            set
            {
                source = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Title displayed in the province header
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value;
                UpdateHeaderText();
            }
        }

        /// <summary>
        /// Size of the province display
        /// </summary>
        public ProvinceSize Size
        {
            get => size;
            set
            {
                size = value;
                UpdateLayout();
            }
        }

        /// <summary>
        /// Orientation of the province
        /// </summary>
        public ProvinceOrientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;
                UpdateLayout();
            }
        }

        /// <summary>
        /// Cards currently in this province
        /// </summary>
        public List<BaseCard> Cards
        {
            get => new List<BaseCard>(cards);
            set
            {
                cards = value?.ToList() ?? new List<BaseCard>();
                UpdateCardReferences();
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Province card in this province
        /// </summary>
        public BaseCard ProvinceCard
        {
            get => provinceCard;
            set
            {
                provinceCard = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Dynasty cards in this province
        /// </summary>
        public List<BaseCard> DynastyCards
        {
            get => new List<BaseCard>(dynastyCards);
            set
            {
                dynastyCards = value?.ToList() ?? new List<BaseCard>();
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Stronghold card in this province
        /// </summary>
        public BaseCard StrongholdCard
        {
            get => strongholdCard;
            set
            {
                strongholdCard = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Number of cards in this province
        /// </summary>
        public int CardCount => cards.Count;

        /// <summary>
        /// Whether this province belongs to the local player
        /// </summary>
        public bool IsMe
        {
            get => isMe;
            set
            {
                isMe = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Whether this province is broken
        /// </summary>
        public bool IsBroken
        {
            get => isBroken;
            set
            {
                isBroken = value;
                UpdateAppearance();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
                
            if (cardContainer == null)
                cardContainer = transform;
                
            SetupDefaultReferences();
        }

        private void Start()
        {
            UpdateDisplay();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the cards for this province and update display
        /// </summary>
        /// <param name="newCards">New cards to display</param>
        /// <param name="cardCount">Optional override for card count display</param>
        public void SetCards(List<BaseCard> newCards, int? cardCount = null)
        {
            cards = newCards?.ToList() ?? new List<BaseCard>();
            UpdateCardReferences();
            UpdateDisplay();
            
            if (cardCount.HasValue)
            {
                UpdateHeaderText(cardCount.Value);
            }
        }

        /// <summary>
        /// Add a card to this province
        /// </summary>
        /// <param name="card">Card to add</param>
        public void AddCard(BaseCard card)
        {
            if (card != null && !cards.Contains(card))
            {
                cards.Add(card);
                UpdateCardReferences();
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Remove a card from this province
        /// </summary>
        /// <param name="card">Card to remove</param>
        public void RemoveCard(BaseCard card)
        {
            if (cards.Remove(card))
            {
                UpdateCardReferences();
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Clear all cards from this province
        /// </summary>
        public void ClearCards()
        {
            cards.Clear();
            provinceCard = null;
            dynastyCards.Clear();
            strongholdCard = null;
            UpdateDisplay();
        }

        /// <summary>
        /// Set specific card types
        /// </summary>
        /// <param name="province">Province card</param>
        /// <param name="dynasty">Dynasty cards</param>
        /// <param name="stronghold">Stronghold card</param>
        public void SetSpecificCards(BaseCard province = null, List<BaseCard> dynasty = null, BaseCard stronghold = null)
        {
            provinceCard = province;
            dynastyCards = dynasty?.ToList() ?? new List<BaseCard>();
            strongholdCard = stronghold;
            
            // Update cards list
            cards.Clear();
            if (provinceCard != null) cards.Add(provinceCard);
            cards.AddRange(dynastyCards);
            if (strongholdCard != null) cards.Add(strongholdCard);
            
            UpdateDisplay();
        }

        #endregion

        #region Card Reference Management

        private void UpdateCardReferences()
        {
            // Find specific card types from the cards list
            provinceCard = cards.FirstOrDefault(card => card.isProvince);
            dynastyCards = cards.Where(card => card.isDynasty).ToList();
            strongholdCard = cards.FirstOrDefault(card => card.isStronghold);
        }

        #endregion

        #region Display Update Methods

        private void UpdateDisplay()
        {
            UpdateHeaderText();
            UpdateCardDisplay();
            UpdateLayout();
            UpdateAppearance();
        }

        private void UpdateHeaderText(int? cardCountOverride = null)
        {
            if (headerText == null) return;
            
            int displayCount = cardCountOverride ?? CardCount;
            string displayTitle = !string.IsNullOrEmpty(title) ? $"{title} ({displayCount})" : "";
            headerText.text = displayTitle;
        }

        private void UpdateCardDisplay()
        {
            // Clear existing card components
            foreach (var cardComponent in cardComponents)
            {
                if (cardComponent != null)
                    DestroyImmediate(cardComponent.gameObject);
            }
            cardComponents.Clear();

            // Apply face-down states
            ApplyFaceDownStates();

            // Create card components
            CreateCardComponent(provinceCard, "province-attachment");
            
            if (showDynastyRow)
            {
                foreach (var dynastyCard in dynastyCards)
                {
                    CreateCardComponent(dynastyCard, "province-attachment");
                }
            }
            
            CreateCardComponent(strongholdCard, "province-attachment");

            // Update card positions
            LayoutCards();
        }

        private void ApplyFaceDownStates()
        {
            // Apply hidden states based on settings
            if (hiddenProvinceCard && provinceCard != null)
            {
                provinceCard.facedown = true;
            }

            if (hiddenDynastyCard && dynastyCards.Count > 0)
            {
                foreach (var dynastyCard in dynastyCards)
                {
                    dynastyCard.facedown = true;
                }
            }
        }

        private void CreateCardComponent(BaseCard card, string cardClassName)
        {
            if (card == null || cardPrefab == null) return;

            var cardObject = Instantiate(cardPrefab, cardContainer);
            var cardComponent = cardObject.GetComponent<CardComponent>();
            
            if (cardComponent != null)
            {
                cardComponent.SetCard(card);
                cardComponent.SetSource(source);
                cardComponent.SetSize(size);
                cardComponent.SetClassName(cardClassName);
                
                // Set up mouse interaction settings
                bool disableMouseOver = card.facedown && !isMe;
                cardComponent.SetMouseOverEnabled(!disableMouseOver);
                
                // Subscribe to events
                cardComponent.OnClick += HandleCardClick;
                cardComponent.OnMenuItemClick += HandleMenuItemClick;
                cardComponent.OnMouseEnter += HandleCardMouseOver;
                cardComponent.OnMouseExit += HandleCardMouseOut;
                cardComponent.OnDragDrop += HandleCardDragDrop;
                
                cardComponents.Add(cardComponent);
            }
        }

        private void LayoutCards()
        {
            if (provinceCard == null) return;

            var wrapperStyle = GetWrapperStyle(provinceCard);
            
            // Apply wrapper style to the province container
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(wrapperStyle.marginLeft, 0);
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, wrapperStyle.minHeight);
            }

            // Position cards based on orientation
            PositionCards();
        }

        private void PositionCards()
        {
            float currentX = 0f;
            float currentY = 0f;
            
            foreach (var cardComponent in cardComponents)
            {
                if (cardComponent != null)
                {
                    var cardRect = cardComponent.GetComponent<RectTransform>();
                    
                    if (orientation == ProvinceOrientation.Horizontal)
                    {
                        cardRect.anchoredPosition = new Vector2(currentX, currentY);
                        currentX += cardRect.sizeDelta.x + cardSpacing.x;
                    }
                    else
                    {
                        cardRect.anchoredPosition = new Vector2(currentX, -currentY);
                        currentY += cardRect.sizeDelta.y + cardSpacing.y;
                    }
                }
            }
        }

        private void UpdateLayout()
        {
            // Update CSS-like class names based on size and orientation
            string className = $"panel province {GetSizeClassName()}";
            
            if (orientation == ProvinceOrientation.Horizontal)
            {
                className += " horizontal";
            }
            else
            {
                className += " vertical";
            }
            
            // Apply layout changes
            LayoutCards();
        }

        private void UpdateAppearance()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isDragHighlighted ? highlightColor : 
                                       isBroken ? Color.red : normalColor;
            }
        }

        #endregion

        #region Layout Calculations (Matching React Logic)

        private WrapperStyle GetWrapperStyle(BaseCard provinceCard)
        {
            var wrapperStyle = new WrapperStyle();
            float currentAttachmentOffset = attachmentOffset;
            float currentCardHeight = cardHeight;

            // Apply size multipliers (matching React logic)
            switch (size)
            {
                case ProvinceSize.Large:
                    currentAttachmentOffset *= 1.4f;
                    currentCardHeight *= 1.4f;
                    break;
                case ProvinceSize.Small:
                    currentAttachmentOffset *= 0.8f;
                    currentCardHeight *= 0.8f;
                    break;
                case ProvinceSize.XLarge:
                    currentAttachmentOffset *= 2f;
                    currentCardHeight *= 2f;
                    break;
            }

            int attachmentCount = provinceCard.attachments?.Count ?? 0;
            int totalTiers = 0;

            if (provinceCard.attachments != null)
            {
                foreach (var attachment in provinceCard.attachments)
                {
                    if (attachment.bowed)
                    {
                        totalTiers += 1;
                    }
                }
            }

            if (attachmentCount > 0)
            {
                wrapperStyle.marginLeft = 4 + attachmentCount * currentAttachmentOffset;
                wrapperStyle.minHeight = currentCardHeight + totalTiers * currentAttachmentOffset;
            }

            return wrapperStyle;
        }

        private string GetSizeClassName()
        {
            return size switch
            {
                ProvinceSize.Small => "small",
                ProvinceSize.Large => "large",
                ProvinceSize.XLarge => "x-large",
                _ => "normal"
            };
        }

        #endregion

        #region Event Handlers

        private void HandleCardClick(BaseCard card)
        {
            OnCardClick?.Invoke(card);
        }

        private void HandleMenuItemClick(BaseCard card, string menuItem)
        {
            OnMenuItemClick?.Invoke(card, menuItem);
        }

        private void HandleCardMouseOver(BaseCard card)
        {
            OnMouseOver?.Invoke(card);
        }

        private void HandleCardMouseOut(BaseCard card)
        {
            OnMouseOut?.Invoke(card);
        }

        private void HandleCardDragDrop(BaseCard card, string sourceLocation, string targetLocation)
        {
            OnDragDrop?.Invoke(card, sourceLocation, targetLocation);
        }

        #endregion

        #region Drag and Drop Implementation

        public void OnDrop(PointerEventData eventData)
        {
            SetDragHighlight(false);
            
            var dragData = DragDropManager.GetDragData(eventData);
            if (dragData != null && dragData.card != null)
            {
                OnDragDrop?.Invoke(dragData.card, dragData.source, source);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (DragDropManager.IsDragging())
            {
                SetDragHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetDragHighlight(false);
        }

        private void SetDragHighlight(bool highlighted)
        {
            isDragHighlighted = highlighted;
            UpdateAppearance();
        }

        #endregion

        #region Utility Methods

        private void SetupDefaultReferences()
        {
            // Find or create default UI references if not assigned
            if (headerText == null)
            {
                headerText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (cardContainer == null)
            {
                var containerTransform = transform.Find("CardContainer");
                cardContainer = containerTransform != null ? containerTransform : transform;
            }
        }

        #endregion

        #region Debug Support

        [ContextMenu("Debug Province State")]
        private void DebugProvinceState()
        {
            Debug.Log($"Province Debug Info:\n" +
                     $"Source: {source}\n" +
                     $"Title: {title}\n" +
                     $"Card Count: {CardCount}\n" +
                     $"Province Card: {provinceCard?.name ?? "None"}\n" +
                     $"Dynasty Cards: {dynastyCards.Count}\n" +
                     $"Stronghold Card: {strongholdCard?.name ?? "None"}\n" +
                     $"Is Broken: {isBroken}\n" +
                     $"Size: {size}\n" +
                     $"Orientation: {orientation}");
        }

        #endregion
    }

    #region Supporting Types and Classes

    /// <summary>
    /// Province size options
    /// </summary>
    public enum ProvinceSize
    {
        Small,
        Normal,
        Large,
        XLarge
    }

    /// <summary>
    /// Province orientation options
    /// </summary>
    public enum ProvinceOrientation
    {
        Vertical,
        Horizontal,
        Bowed
    }

    /// <summary>
    /// Wrapper style data (matching React getWrapperStyle)
    /// </summary>
    [System.Serializable]
    public struct WrapperStyle
    {
        public float marginLeft;
        public float minHeight;
    }

    /// <summary>
    /// Drag data structure for drag and drop operations
    /// </summary>
    [System.Serializable]
    public class DragData
    {
        public BaseCard card;
        public string source;
        
        public DragData(BaseCard dragCard, string sourceLocation)
        {
            card = dragCard;
            source = sourceLocation;
        }
    }

    /// <summary>
    /// Static utility class for managing drag and drop operations
    /// </summary>
    public static class DragDropManager
    {
        private static DragData currentDragData;
        private static bool isDragging = false;

        public static void StartDrag(BaseCard card, string source)
        {
            currentDragData = new DragData(card, source);
            isDragging = true;
        }

        public static void EndDrag()
        {
            currentDragData = null;
            isDragging = false;
        }

        public static DragData GetDragData(PointerEventData eventData)
        {
            return currentDragData;
        }

        public static bool IsDragging()
        {
            return isDragging;
        }

        public static string SerializeDragData(DragData data)
        {
            if (data?.card == null) return "";
            
            return JsonUtility.ToJson(new
            {
                card = new { uuid = data.card.uuid, name = data.card.name },
                source = data.source
            });
        }

        public static DragData DeserializeDragData(string jsonData)
        {
            try
            {
                // Implementation would depend on your JSON parsing strategy
                // This is a simplified version
                return JsonUtility.FromJson<DragData>(jsonData);
            }
            catch
            {
                return null;
            }
        }
    }

    #endregion
}
