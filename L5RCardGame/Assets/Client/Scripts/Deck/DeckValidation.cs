using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;

namespace L5RGame
{
    /// <summary>
    /// DeckValidator provides hybrid deck validation for L5R card game:
    /// - Client-side validation for immediate UI feedback while deck building
    /// - Server-side validation for authoritative verification in competitive play
    /// Uses the local Unity project database for client validation and game server for authoritative validation.
    /// </summary>
    [System.Serializable]
    public class DeckValidationResult
    {
        public bool valid;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public string validationSource = "Local Database";
        public Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        public int totalInfluenceCost = 0;
        public int availableInfluence = 0;
        public bool isAuthoritative = false; // True if validated by server
        public float validationTimestamp;
    }

    [System.Serializable]
    public class ServerValidationRequest
    {
        public string deckId;
        public Dictionary<string, int> cardCounts;
        public string clan;
        public string alliance;
        public GameMode gameMode;
        public List<string> availablePacks;
        public bool isCompetitive;
    }

    [System.Serializable]
    public class ServerValidationResponse
    {
        public bool valid;
        public List<string> errors;
        public List<string> warnings;
        public bool isAuthoritative;
        public string validationId;
        public float serverTimestamp;
    }

    [System.Serializable]
    public class DeckValidationOptions
    {
        public bool includeExtendedStatus = true;
        public List<string> availablePacks = new List<string>();
        public GameMode gameMode = GameMode.Emerald;
        public bool strictValidation = true;
        public bool allowProxyCards = false;
        public bool requireServerValidation = false; // Force server validation
        public bool isCompetitiveMode = false; // Automatic server validation for competitive
        public float serverValidationTimeout = 15f;
    }

    [System.Serializable]
    public enum GameMode
    {
        Emerald,
        Stronghold,
        Skirmish,
        Enlightenment
    }

    public class DeckValidator : MonoBehaviour
    {
        [Header("Database References")]
        public CardDatabase cardDatabase;
        public PackDatabase packDatabase;
        public FormatDatabase formatDatabase;

        [Header("Server Validation")]
        public string gameServerUrl = "https://your-game-server.com/api";
        public bool enableServerValidation = true;
        public float serverValidationTimeout = 15f;
        public int maxRetries = 3;

        [Header("Local Validation Rules")]
        public int minDynastyCards = 40;
        public int maxDynastyCards = 45;
        public int minConflictCards = 40;
        public int maxConflictCards = 45;
        public int requiredProvinces = 5;
        public int maxCopiesPerCard = 3;

        [Header("Format-Specific Rules")]
        public bool enableInfluenceValidation = true;
        public bool enablePackValidation = true;
        public bool enableBanListValidation = true;
        public bool enableErratumValidation = true;

        // Cache for validation results
        private Dictionary<string, CachedValidationResult> clientValidationCache = new Dictionary<string, CachedValidationResult>();
        private Dictionary<string, CachedValidationResult> serverValidationCache = new Dictionary<string, CachedValidationResult>();
        private const float CLIENT_CACHE_DURATION = 60f; // 1 minute for client validation
        private const float SERVER_CACHE_DURATION = 300f; // 5 minutes for server validation

        // Events
        public System.Action<DeckValidationResult> OnClientValidationComplete;
        public System.Action<DeckValidationResult> OnServerValidationComplete;
        public System.Action<DeckValidationResult> OnFinalValidationComplete;
        public System.Action<string> OnValidationError;

        private void Awake()
        {
            // Auto-find database references if not assigned
            if (cardDatabase == null)
                cardDatabase = FindObjectOfType<CardDatabase>();
            
            if (packDatabase == null)
                packDatabase = FindObjectOfType<PackDatabase>();
                
            if (formatDatabase == null)
                formatDatabase = FindObjectOfType<FormatDatabase>();
        }

        #region Public API - Hybrid Validation
        /// <summary>
        /// Performs hybrid validation: immediate client-side validation + optional server validation
        /// </summary>
        public void ValidateDeck(Deck deck, DeckValidationOptions options = null)
        {
            StartCoroutine(ValidateDeckHybrid(deck, options));
        }

        /// <summary>
        /// Client-side only validation for immediate feedback
        /// </summary>
        public DeckValidationResult ValidateDeckClient(Deck deck, DeckValidationOptions options = null)
        {
            options = options ?? new DeckValidationOptions();
            
            // Check client cache first
            string deckHash = GetDeckHash(deck);
            if (clientValidationCache.TryGetValue(deckHash, out CachedValidationResult cached))
            {
                if (Time.time - cached.timestamp < CLIENT_CACHE_DURATION)
                {
                    return cached.result;
                }
                else
                {
                    clientValidationCache.Remove(deckHash);
                }
            }

            var result = new DeckValidationResult();
            result.validationTimestamp = Time.time;
            result.isAuthoritative = false;

            // Perform all local validation checks using project database
            ValidateBasicDeckStructure(deck, result);
            ValidateCardCounts(deck, result);
            ValidateCardLimits(deck, result);
            ValidateFormatRules(deck, options.gameMode, result);
            ValidateClanAndInfluence(deck, result);
            ValidateUniqueCards(deck, result);
            ValidateRestrictedCards(deck, result);
            ValidatePackAvailability(deck, options, result);
            ValidateBanList(deck, options, result);
            ValidateErratum(deck, options, result);

            result.valid = result.errors.Count == 0;
            result.validationSource = "Client Database";

            // Cache the result
            clientValidationCache[deckHash] = new CachedValidationResult
            {
                result = result,
                timestamp = Time.time
            };

            return result;
        }

        /// <summary>
        /// Server-side validation for authoritative verification
        /// </summary>
        public void ValidateDeckServer(Deck deck, DeckValidationOptions options = null)
        {
            StartCoroutine(ValidateDeckServerCoroutine(deck, options));
        }

        public bool IsDeckValid(Deck deck, DeckValidationOptions options = null)
        {
            var result = ValidateDeckClient(deck, options);
            return result.valid;
        }

        public List<string> GetValidationErrors(Deck deck, DeckValidationOptions options = null)
        {
            var result = ValidateDeckClient(deck, options);
            return result.errors;
        }

        public List<string> GetValidationWarnings(Deck deck, DeckValidationOptions options = null)
        {
            var result = ValidateDeckClient(deck, options);
            return result.warnings;
        }
        #endregion

        #region Hybrid Validation Implementation
        private IEnumerator ValidateDeckHybrid(Deck deck, DeckValidationOptions options)
        {
            options = options ?? new DeckValidationOptions();

            // Step 1: Always do client validation first for immediate feedback
            var clientResult = ValidateDeckClient(deck, options);
            OnClientValidationComplete?.Invoke(clientResult);

            // Step 2: Determine if server validation is needed
            bool needsServerValidation = ShouldValidateOnServer(options, clientResult);

            if (needsServerValidation && enableServerValidation)
            {
                // Check server cache first
                string deckHash = GetDeckHash(deck);
                if (serverValidationCache.TryGetValue(deckHash, out CachedValidationResult serverCached))
                {
                    if (Time.time - serverCached.timestamp < SERVER_CACHE_DURATION)
                    {
                        OnServerValidationComplete?.Invoke(serverCached.result);
                        OnFinalValidationComplete?.Invoke(serverCached.result);
                        yield break;
                    }
                    else
                    {
                        serverValidationCache.Remove(deckHash);
                    }
                }

                // Perform server validation
                yield return StartCoroutine(ValidateOnServer(deck, options, clientResult));
            }
            else
            {
                // Use client result as final result
                OnFinalValidationComplete?.Invoke(clientResult);
            }
        }

        private bool ShouldValidateOnServer(DeckValidationOptions options, DeckValidationResult clientResult)
        {
            // Always validate on server for competitive mode
            if (options.isCompetitiveMode) return true;

            // Forced server validation
            if (options.requireServerValidation) return true;

            // Only validate on server if client validation passes (avoid unnecessary server load)
            return clientResult.valid;
        }

        private IEnumerator ValidateOnServer(Deck deck, DeckValidationOptions options, DeckValidationResult clientResult)
        {
            var request = CreateServerValidationRequest(deck, options);
            yield return StartCoroutine(SendServerValidationRequest(request, deck, clientResult));
        }

        private IEnumerator ValidateDeckServerCoroutine(Deck deck, DeckValidationOptions options)
        {
            options = options ?? new DeckValidationOptions();
            var clientResult = ValidateDeckClient(deck, options);
            yield return StartCoroutine(ValidateOnServer(deck, options, clientResult));
        }
        #endregion

        #region Server Communication
        private ServerValidationRequest CreateServerValidationRequest(Deck deck, DeckValidationOptions options)
        {
            var cardCounts = new Dictionary<string, int>();
            
            // Collect all cards with counts
            foreach (var deckCard in GetAllDeckCards(deck))
            {
                cardCounts[deckCard.card.id] = deckCard.count;
            }

            return new ServerValidationRequest
            {
                deckId = deck.id ?? Guid.NewGuid().ToString(),
                cardCounts = cardCounts,
                clan = deck.clan,
                alliance = deck.alliance,
                gameMode = options.gameMode,
                availablePacks = options.availablePacks,
                isCompetitive = options.isCompetitiveMode
            };
        }

        private IEnumerator SendServerValidationRequest(ServerValidationRequest request, Deck deck, DeckValidationResult clientResult)
        {
            string jsonData = JsonConvert.SerializeObject(request);
            string url = $"{gameServerUrl}/deck/validate";

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using (var webRequest = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.SetRequestHeader("User-Agent", $"L5RCardGame/{Application.version}");
                    webRequest.timeout = (int)serverValidationTimeout;

                    // Add authentication if available
                    var authToken = GetAuthenticationToken();
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        webRequest.SetRequestHeader("Authorization", $"Bearer {authToken}");
                    }

                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var response = JsonConvert.DeserializeObject<ServerValidationResponse>(webRequest.downloadHandler.text);
                            var serverResult = ProcessServerValidationResponse(response, clientResult);
                            
                            // Cache server result
                            string deckHash = GetDeckHash(deck);
                            serverValidationCache[deckHash] = new CachedValidationResult
                            {
                                result = serverResult,
                                timestamp = Time.time
                            };

                            OnServerValidationComplete?.Invoke(serverResult);
                            OnFinalValidationComplete?.Invoke(serverResult);
                            yield break;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to parse server validation response: {ex.Message}");
                            if (attempt == maxRetries - 1)
                            {
                                HandleServerValidationFailure("Failed to parse server response", clientResult);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Server validation attempt {attempt + 1} failed: {webRequest.error}");
                        if (attempt == maxRetries - 1)
                        {
                            HandleServerValidationFailure($"Server validation failed: {webRequest.error}", clientResult);
                        }
                        else
                        {
                            // Wait before retry
                            yield return new WaitForSeconds(Mathf.Pow(2, attempt)); // Exponential backoff
                        }
                    }
                }
            }
        }

        private DeckValidationResult ProcessServerValidationResponse(ServerValidationResponse response, DeckValidationResult clientResult)
        {
            var serverResult = new DeckValidationResult
            {
                valid = response.valid,
                errors = response.errors ?? new List<string>(),
                warnings = response.warnings ?? new List<string>(),
                validationSource = "Game Server",
                isAuthoritative = response.isAuthoritative,
                validationTimestamp = response.serverTimestamp,
                cardCounts = clientResult.cardCounts, // Keep client calculated counts
                totalInfluenceCost = clientResult.totalInfluenceCost,
                availableInfluence = clientResult.availableInfluence
            };

            // Merge any additional warnings from client
            foreach (var warning in clientResult.warnings)
            {
                if (!serverResult.warnings.Contains(warning))
                {
                    serverResult.warnings.Add($"[Client] {warning}");
                }
            }

            return serverResult;
        }

        private void HandleServerValidationFailure(string error, DeckValidationResult clientResult)
        {
            // Fall back to client result but add warning
            clientResult.warnings.Add($"Server validation unavailable: {error}");
            clientResult.validationSource = "Client Database (Server Unavailable)";
            
            OnValidationError?.Invoke(error);
            OnServerValidationComplete?.Invoke(clientResult);
            OnFinalValidationComplete?.Invoke(clientResult);
        }

        private string GetAuthenticationToken()
        {
            // Get authentication token from your auth system
            // This could be from PlayerPrefs, a separate auth manager, etc.
            return PlayerPrefs.GetString("AuthToken", "");
        }
        #endregion

        #region Validation Status Checking
        public bool HasValidServerValidation(Deck deck)
        {
            string deckHash = GetDeckHash(deck);
            if (serverValidationCache.TryGetValue(deckHash, out CachedValidationResult cached))
            {
                return (Time.time - cached.timestamp < SERVER_CACHE_DURATION) && cached.result.isAuthoritative;
            }
            return false;
        }

        public bool IsServerValidationRequired(DeckValidationOptions options)
        {
            return options.isCompetitiveMode || options.requireServerValidation;
        }

        public DeckValidationResult GetLastValidationResult(Deck deck, bool preferServer = true)
        {
            string deckHash = GetDeckHash(deck);
            
            if (preferServer && serverValidationCache.TryGetValue(deckHash, out CachedValidationResult serverCached))
            {
                if (Time.time - serverCached.timestamp < SERVER_CACHE_DURATION)
                {
                    return serverCached.result;
                }
            }

            if (clientValidationCache.TryGetValue(deckHash, out CachedValidationResult clientCached))
            {
                if (Time.time - clientCached.timestamp < CLIENT_CACHE_DURATION)
                {
                    return clientCached.result;
                }
            }

            return null;
        }
        #endregion

        #region Local Database Validation Methods
        private void ValidateBasicDeckStructure(Deck deck, DeckValidationResult result)
        {
            if (string.IsNullOrEmpty(deck.name))
                result.errors.Add("Deck must have a name");

            if (string.IsNullOrEmpty(deck.clan))
                result.errors.Add("Deck must have a clan");

            // Validate clan exists in database
            if (cardDatabase != null && !cardDatabase.IsValidClan(deck.clan))
                result.errors.Add($"Invalid clan: {deck.clan}");

            if (deck.strongholdCards.Count != 1)
                result.errors.Add($"Deck must have exactly 1 stronghold (current: {deck.strongholdCards.Count})");

            if (deck.roleCards.Count != 1)
                result.errors.Add($"Deck must have exactly 1 role (current: {deck.roleCards.Count})");

            if (deck.provinceCards.Count != requiredProvinces)
                result.errors.Add($"Deck must have exactly {requiredProvinces} provinces (current: {deck.provinceCards.Count})");

            // Validate that stronghold and role are compatible with clan
            ValidateStrongholdClanCompatibility(deck, result);
            ValidateRoleClanCompatibility(deck, result);
        }

        private void ValidateCardCounts(Deck deck, DeckValidationResult result)
        {
            // Dynasty deck size
            int dynastyCount = deck.dynastyCards.Sum(dc => dc.count);
            if (dynastyCount < minDynastyCards || dynastyCount > maxDynastyCards)
            {
                result.errors.Add($"Dynasty deck must have {minDynastyCards}-{maxDynastyCards} cards (current: {dynastyCount})");
            }

            // Conflict deck size
            int conflictCount = deck.conflictCards.Sum(dc => dc.count);
            if (conflictCount < minConflictCards || conflictCount > maxConflictCards)
            {
                result.errors.Add($"Conflict deck must have {minConflictCards}-{maxConflictCards} cards (current: {conflictCount})");
            }

            // Store card counts for reference
            result.cardCounts["dynasty"] = dynastyCount;
            result.cardCounts["conflict"] = conflictCount;
            result.cardCounts["provinces"] = deck.provinceCards.Count;
        }

        private void ValidateCardLimits(Deck deck, DeckValidationResult result)
        {
            var allPlayableCards = deck.dynastyCards.Concat(deck.conflictCards).ToList();
            
            foreach (var deckCard in allPlayableCards)
            {
                if (deckCard.count > maxCopiesPerCard)
                {
                    result.errors.Add($"{deckCard.card.name} has more than {maxCopiesPerCard} copies ({deckCard.count})");
                }

                // Check if card is valid in database
                if (cardDatabase != null && !cardDatabase.IsValidCard(deckCard.card.id))
                {
                    result.errors.Add($"Card not found in database: {deckCard.card.name}");
                }

                // Check for restricted cards (max 1 copy)
                if (deckCard.card.HasKeyword("restricted") && deckCard.count > 1)
                {
                    result.errors.Add($"{deckCard.card.name} is restricted and can only have 1 copy ({deckCard.count})");
                }

                // Check for limited cards
                if (deckCard.card.HasKeyword("limited"))
                {
                    int totalLimitedCards = allPlayableCards.Where(dc => dc.card.HasKeyword("limited")).Sum(dc => dc.count);
                    if (totalLimitedCards > 1)
                    {
                        result.warnings.Add($"Deck has {totalLimitedCards} limited cards (only 1 can be played per conflict)");
                    }
                }
            }

            // Validate province uniqueness
            var provinceNames = deck.provinceCards.Select(pc => pc.card.name).ToList();
            var duplicateProvinces = provinceNames.GroupBy(name => name).Where(g => g.Count() > 1);
            foreach (var duplicate in duplicateProvinces)
            {
                result.errors.Add($"Province {duplicate.Key} appears {duplicate.Count()} times (provinces must be unique)");
            }
        }

        private void ValidateFormatRules(Deck deck, GameMode gameMode, DeckValidationResult result)
        {
            if (formatDatabase == null) return;

            var formatRules = formatDatabase.GetFormatRules(gameMode);
            if (formatRules == null) return;

            // Validate format-specific rules
            foreach (var rule in formatRules.restrictions)
            {
                ValidateFormatRestriction(deck, rule, result);
            }

            // Check banned cards for this format
            foreach (var deckCard in GetAllDeckCards(deck))
            {
                if (formatRules.bannedCards.Contains(deckCard.card.id))
                {
                    result.errors.Add($"{deckCard.card.name} is banned in {gameMode} format");
                }
            }

            // Check restricted cards for this format
            foreach (var deckCard in GetAllDeckCards(deck))
            {
                if (formatRules.restrictedCards.Contains(deckCard.card.id) && deckCard.count > 1)
                {
                    result.errors.Add($"{deckCard.card.name} is restricted to 1 copy in {gameMode} format");
                }
            }
        }

        private void ValidateClanAndInfluence(Deck deck, DeckValidationResult result)
        {
            if (!enableInfluenceValidation) return;

            // Get role influence
            int availableInfluence = 0;
            if (deck.roleCards.Count > 0 && deck.roleCards[0].card is RoleCard roleCard)
            {
                availableInfluence = roleCard.GetInfluence();
            }

            result.availableInfluence = availableInfluence;

            // Calculate influence cost
            int totalInfluenceCost = 0;
            var outOfClanCards = GetAllDeckCards(deck)
                .Where(dc => !IsCardFromClan(dc.card, deck.clan) && !IsCardFromAlliance(dc.card, deck.alliance))
                .ToList();

            foreach (var deckCard in outOfClanCards)
            {
                if (deckCard.card is DrawCard drawCard)
                {
                    int influenceCost = drawCard.GetInfluenceCost();
                    totalInfluenceCost += influenceCost * deckCard.count;
                }
            }

            result.totalInfluenceCost = totalInfluenceCost;

            if (totalInfluenceCost > availableInfluence)
            {
                result.errors.Add($"Influence cost ({totalInfluenceCost}) exceeds available influence ({availableInfluence})");
            }

            // Validate alliance restrictions
            if (!string.IsNullOrEmpty(deck.alliance))
            {
                ValidateAllianceRestrictions(deck, result);
            }
        }

        private void ValidateUniqueCards(Deck deck, DeckValidationResult result)
        {
            var allCards = GetAllDeckCards(deck);
            var uniqueCards = allCards.Where(dc => dc.card.IsUnique()).ToList();

            foreach (var uniqueCard in uniqueCards)
            {
                if (uniqueCard.count > 1)
                {
                    result.errors.Add($"{uniqueCard.card.name} is unique and can only have 1 copy ({uniqueCard.count})");
                }
            }

            // Check for conflicting unique names across different card types
            var uniqueNames = uniqueCards.Select(dc => dc.card.printedName).ToList();
            var duplicateNames = uniqueNames.GroupBy(name => name).Where(g => g.Count() > 1);
            foreach (var duplicate in duplicateNames)
            {
                result.errors.Add($"Multiple unique cards with name '{duplicate.Key}' cannot be in the same deck");
            }
        }

        private void ValidateRestrictedCards(Deck deck, DeckValidationResult result)
        {
            var allCards = GetAllDeckCards(deck);
            var restrictedCards = allCards.Where(dc => dc.card.HasKeyword("restricted")).ToList();

            int totalRestrictedCards = restrictedCards.Sum(dc => dc.count);
            if (totalRestrictedCards > 1)
            {
                result.errors.Add($"Deck can only contain 1 restricted card total (current: {totalRestrictedCards})");
            }
        }

        private void ValidatePackAvailability(Deck deck, DeckValidationOptions options, DeckValidationResult result)
        {
            if (!enablePackValidation || packDatabase == null) return;

            var availablePacks = options.availablePacks;
            if (availablePacks.Count == 0)
            {
                availablePacks = packDatabase.GetAllAvailablePacks();
            }

            foreach (var deckCard in GetAllDeckCards(deck))
            {
                if (!string.IsNullOrEmpty(deckCard.card.packName) && !availablePacks.Contains(deckCard.card.packName))
                {
                    result.errors.Add($"{deckCard.card.name} is from pack '{deckCard.card.packName}' which is not available");
                }
            }
        }

        private void ValidateBanList(Deck deck, DeckValidationOptions options, DeckValidationResult result)
        {
            if (!enableBanListValidation || cardDatabase == null) return;

            var bannedCards = cardDatabase.GetBannedCards(options.gameMode);
            foreach (var deckCard in GetAllDeckCards(deck))
            {
                if (bannedCards.Contains(deckCard.card.id))
                {
                    result.errors.Add($"{deckCard.card.name} is banned");
                }
            }
        }

        private void ValidateErratum(Deck deck, DeckValidationOptions options, DeckValidationResult result)
        {
            if (!enableErratumValidation || cardDatabase == null) return;

            foreach (var deckCard in GetAllDeckCards(deck))
            {
                var errata = cardDatabase.GetCardErratum(deckCard.card.id);
                if (errata.Count > 0)
                {
                    result.warnings.Add($"{deckCard.card.name} has errata: {string.Join(", ", errata)}");
                }
            }
        }
        #endregion

        #region Helper Methods
        private void ValidateStrongholdClanCompatibility(Deck deck, DeckValidationResult result)
        {
            if (deck.strongholdCards.Count > 0)
            {
                var stronghold = deck.strongholdCards[0].card;
                if (!IsCardFromClan(stronghold, deck.clan))
                {
                    result.errors.Add($"Stronghold {stronghold.name} is not compatible with clan {deck.clan}");
                }
            }
        }

        private void ValidateRoleClanCompatibility(Deck deck, DeckValidationResult result)
        {
            if (deck.roleCards.Count > 0)
            {
                var role = deck.roleCards[0].card;
                if (role is RoleCard roleCard && !roleCard.IsClanAllowed(deck.clan))
                {
                    result.errors.Add($"Role {role.name} is not available to clan {deck.clan}");
                }
            }
        }

        private void ValidateAllianceRestrictions(Deck deck, DeckValidationResult result)
        {
            // Validate that alliance cards don't exceed certain limits
            var allianceCards = GetAllDeckCards(deck)
                .Where(dc => IsCardFromClan(dc.card, deck.alliance))
                .ToList();

            int totalAllianceCards = allianceCards.Sum(dc => dc.count);
            const int maxAllianceCards = 10; // Example limit

            if (totalAllianceCards > maxAllianceCards)
            {
                result.errors.Add($"Too many alliance cards ({totalAllianceCards}/{maxAllianceCards})");
            }
        }

        private void ValidateFormatRestriction(Deck deck, FormatRestriction restriction, DeckValidationResult result)
        {
            switch (restriction.type)
            {
                case "max_influence":
                    if (result.totalInfluenceCost > restriction.value)
                    {
                        result.errors.Add($"Format restricts influence to {restriction.value} (current: {result.totalInfluenceCost})");
                    }
                    break;

                case "max_deck_size":
                    int totalCards = result.cardCounts.Values.Sum();
                    if (totalCards > restriction.value)
                    {
                        result.errors.Add($"Format restricts deck size to {restriction.value} (current: {totalCards})");
                    }
                    break;

                case "banned_traits":
                    foreach (var deckCard in GetAllDeckCards(deck))
                    {
                        if (deckCard.card.GetTraits().Any(trait => restriction.values.Contains(trait)))
                        {
                            result.errors.Add($"{deckCard.card.name} has banned trait in this format");
                        }
                    }
                    break;
            }
        }

        private bool IsCardFromClan(BaseCard card, string clan)
        {
            return string.Equals(card.GetFaction(), clan, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCardFromAlliance(BaseCard card, string alliance)
        {
            if (string.IsNullOrEmpty(alliance)) return false;
            return string.Equals(card.GetFaction(), alliance, StringComparison.OrdinalIgnoreCase);
        }

        private List<DeckCard> GetAllDeckCards(Deck deck)
        {
            return deck.strongholdCards
                .Concat(deck.roleCards)
                .Concat(deck.provinceCards)
                .Concat(deck.dynastyCards)
                .Concat(deck.conflictCards)
                .ToList();
        }

        private string GetDeckHash(Deck deck)
        {
            var deckData = new
            {
                name = deck.name,
                clan = deck.clan,
                alliance = deck.alliance,
                format = deck.format,
                cards = GetAllDeckCards(deck).Select(dc => new { id = dc.card.id, count = dc.count })
            };

            string json = JsonConvert.SerializeObject(deckData);
            return json.GetHashCode().ToString();
        }
        #endregion

        #region Cache Management
        private class CachedValidationResult
        {
            public DeckValidationResult result;
            public float timestamp;
        }

        public void ClearValidationCache()
        {
            clientValidationCache.Clear();
            serverValidationCache.Clear();
        }

        public void ClearClientCache()
        {
            clientValidationCache.Clear();
        }

        public void ClearServerCache()
        {
            serverValidationCache.Clear();
        }

        public void ClearExpiredCache()
        {
            // Clear expired client cache
            var expiredClientKeys = clientValidationCache
                .Where(kvp => Time.time - kvp.Value.timestamp > CLIENT_CACHE_DURATION)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredClientKeys)
            {
                clientValidationCache.Remove(key);
            }

            // Clear expired server cache
            var expiredServerKeys = serverValidationCache
                .Where(kvp => Time.time - kvp.Value.timestamp > SERVER_CACHE_DURATION)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredServerKeys)
            {
                serverValidationCache.Remove(key);
            }
        }

        // Periodically clean cache
        private void Update()
        {
            if (Time.frameCount % 3600 == 0) // Every minute at 60fps
            {
                ClearExpiredCache();
            }
        }
        #endregion

        #region Debug and Utility
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogValidationResult(DeckValidationResult result)
        {
            string authStatus = result.isAuthoritative ? "AUTHORITATIVE" : "Non-Authoritative";
            
            Debug.Log($"Deck Validation Result ({authStatus}):\n" +
                     $"Valid: {result.valid}\n" +
                     $"Errors: {result.errors.Count}\n" +
                     $"Warnings: {result.warnings.Count}\n" +
                     $"Influence: {result.totalInfluenceCost}/{result.availableInfluence}\n" +
                     $"Source: {result.validationSource}\n" +
                     $"Timestamp: {result.validationTimestamp}");

            if (result.errors.Count > 0)
            {
                Debug.LogWarning("Validation Errors:\n" + string.Join("\n", result.errors));
            }

            if (result.warnings.Count > 0)
            {
                Debug.Log("Validation Warnings:\n" + string.Join("\n", result.warnings));
            }
        }

        public void LogCacheStatus()
        {
            Debug.Log($"Validation Cache Status:\n" +
                     $"Client Cache: {clientValidationCache.Count} entries\n" +
                     $"Server Cache: {serverValidationCache.Count} entries\n" +
                     $"Server Validation Enabled: {enableServerValidation}");
        }
        #endregion
    }

    #region Supporting Database Classes
    [System.Serializable]
    public class FormatRules
    {
        public List<string> bannedCards = new List<string>();
        public List<string> restrictedCards = new List<string>();
        public List<FormatRestriction> restrictions = new List<FormatRestriction>();
    }

    [System.Serializable]
    public class FormatRestriction
    {
        public string type;
        public int value;
        public List<string> values = new List<string>();
    }

    // These would be implemented as separate database components
    public abstract class CardDatabase : MonoBehaviour
    {
        public abstract bool IsValidCard(string cardId);
        public abstract bool IsValidClan(string clan);
        public abstract List<BaseCard> GetAllCards();
        public abstract List<string> GetBannedCards(GameMode gameMode);
        public abstract List<string> GetCardErratum(string cardId);
    }

    public abstract class PackDatabase : MonoBehaviour
    {
        public abstract List<string> GetAllAvailablePacks();
        public abstract bool IsPackAvailable(string packName);
    }

    public abstract class FormatDatabase : MonoBehaviour
    {
        public abstract FormatRules GetFormatRules(GameMode gameMode);
    }
    #endregion
}