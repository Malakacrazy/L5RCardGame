using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using System.Reflection;
using UnityEngine;
using MessagePack;
using Mirror;

namespace L5RGame.Serialization
{
    /// <summary>
    /// Comprehensive serialization system for the L5R card game.
    /// Handles JSON, binary, and network serialization for game state, cards, and user data.
    /// </summary>
    public static class SerializationManager
    {
        #region Configuration

        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            IncludeFields = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters =
            {
                new JsonStringEnumConverter(),
                new Vector3Converter(),
                new ColorConverter(),
                new DateTimeConverter(),
                new BaseCardConverter(),
                new PlayerConverter(),
                new GameStateConverter(),
                new DeckConverter()
            }
        };

        private static readonly JsonSerializerOptions NetworkJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new CompactCardConverter(),
                new CompactPlayerConverter(),
                new NetworkGameStateConverter()
            }
        };

        #endregion

        #region Public API

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="useNetworkFormat">Use compact network format</param>
        /// <returns>JSON string</returns>
        public static string ToJson<T>(T obj, bool useNetworkFormat = false)
        {
            try
            {
                var options = useNetworkFormat ? NetworkJsonOptions : DefaultJsonOptions;
                return JsonSerializer.Serialize(obj, options);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to serialize {typeof(T).Name} to JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="json">JSON string</param>
        /// <param name="useNetworkFormat">Use compact network format</param>
        /// <returns>Deserialized object</returns>
        public static T FromJson<T>(string json, bool useNetworkFormat = false)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return default(T);

                var options = useNetworkFormat ? NetworkJsonOptions : DefaultJsonOptions;
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize JSON to {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Serialize object to binary format using MessagePack
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Binary data</returns>
        public static byte[] ToBinary<T>(T obj)
        {
            try
            {
                return MessagePackSerializer.Serialize(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to serialize {typeof(T).Name} to binary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialize binary data to object using MessagePack
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="data">Binary data</param>
        /// <returns>Deserialized object</returns>
        public static T FromBinary<T>(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    return default(T);

                return MessagePackSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize binary to {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Save object to file as JSON
        /// </summary>
        /// <typeparam name="T">Type to save</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="filePath">File path</param>
        /// <param name="useNetworkFormat">Use compact format</param>
        /// <returns>True if successful</returns>
        public static bool SaveToFile<T>(T obj, string filePath, bool useNetworkFormat = false)
        {
            try
            {
                var json = ToJson(obj, useNetworkFormat);
                if (json == null) return false;

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save {typeof(T).Name} to file {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load object from file as JSON
        /// </summary>
        /// <typeparam name="T">Type to load</typeparam>
        /// <param name="filePath">File path</param>
        /// <param name="useNetworkFormat">Use compact format</param>
        /// <returns>Loaded object or default</returns>
        public static T LoadFromFile<T>(string filePath, bool useNetworkFormat = false)
        {
            try
            {
                if (!File.Exists(filePath))
                    return default(T);

                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return FromJson<T>(json, useNetworkFormat);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {typeof(T).Name} from file {filePath}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Create a deep copy of an object using serialization
        /// </summary>
        /// <typeparam name="T">Type to clone</typeparam>
        /// <param name="obj">Object to clone</param>
        /// <returns>Deep copy of object</returns>
        public static T DeepClone<T>(T obj)
        {
            if (obj == null) return default(T);

            try
            {
                var json = ToJson(obj);
                return FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deep clone {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Compress JSON string using gzip
        /// </summary>
        /// <param name="json">JSON string to compress</param>
        /// <returns>Compressed bytes</returns>
        public static byte[] CompressJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                return CompressionUtils.Compress(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to compress JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decompress gzip bytes to JSON string
        /// </summary>
        /// <param name="compressedData">Compressed data</param>
        /// <returns>JSON string</returns>
        public static string DecompressJson(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return null;

            try
            {
                var bytes = CompressionUtils.Decompress(compressedData);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to decompress JSON: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Network Serialization

        /// <summary>
        /// Serialize game action for network transmission
        /// </summary>
        /// <param name="action">Game action</param>
        /// <returns>Serialized bytes</returns>
        public static byte[] SerializeGameAction(GameAction action)
        {
            using var writer = new NetworkWriter();
            
            writer.WriteByte((byte)action.ActionType);
            writer.WriteString(action.PlayerId ?? "");
            writer.WriteString(action.CardId ?? "");
            writer.WriteInt(action.Parameters?.Count ?? 0);
            
            if (action.Parameters != null)
            {
                foreach (var param in action.Parameters)
                {
                    writer.WriteString(param.Key);
                    writer.WriteString(param.Value?.ToString() ?? "");
                }
            }
            
            writer.WriteDouble(action.Timestamp.ToBinary());
            
            return writer.ToArray();
        }

        /// <summary>
        /// Deserialize game action from network data
        /// </summary>
        /// <param name="data">Network data</param>
        /// <returns>Game action</returns>
        public static GameAction DeserializeGameAction(byte[] data)
        {
            using var reader = new NetworkReader(data);
            
            var action = new GameAction
            {
                ActionType = (ActionType)reader.ReadByte(),
                PlayerId = reader.ReadString(),
                CardId = reader.ReadString()
            };
            
            var paramCount = reader.ReadInt();
            if (paramCount > 0)
            {
                action.Parameters = new Dictionary<string, object>();
                for (int i = 0; i < paramCount; i++)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadString();
                    action.Parameters[key] = value;
                }
            }
            
            action.Timestamp = DateTime.FromBinary(reader.ReadLong());
            
            return action;
        }

        /// <summary>
        /// Create delta between two game states for efficient network sync
        /// </summary>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">Current state</param>
        /// <returns>State delta</returns>
        public static GameStateDelta CreateStateDelta(GameState oldState, GameState newState)
        {
            var delta = new GameStateDelta
            {
                GameId = newState.GameId,
                Timestamp = DateTime.UtcNow
            };

            // Compare and create deltas
            if (oldState.TurnNumber != newState.TurnNumber)
                delta.TurnNumber = newState.TurnNumber;

            if (oldState.CurrentPlayerId != newState.CurrentPlayerId)
                delta.CurrentPlayerId = newState.CurrentPlayerId;

            if (oldState.Phase != newState.Phase)
                delta.Phase = newState.Phase;

            // Player deltas
            delta.PlayerDeltas = new List<PlayerDelta>();
            foreach (var newPlayer in newState.Players)
            {
                var oldPlayer = oldState.Players.FirstOrDefault(p => p.PlayerId == newPlayer.PlayerId);
                if (oldPlayer == null)
                {
                    // New player
                    delta.PlayerDeltas.Add(new PlayerDelta
                    {
                        PlayerId = newPlayer.PlayerId,
                        IsNew = true,
                        Player = newPlayer
                    });
                }
                else
                {
                    // Check for changes
                    var playerDelta = CreatePlayerDelta(oldPlayer, newPlayer);
                    if (playerDelta != null)
                        delta.PlayerDeltas.Add(playerDelta);
                }
            }

            return delta;
        }

        #endregion

        #region Specialized Serializers

        /// <summary>
        /// Serialize deck for storage or transmission
        /// </summary>
        /// <param name="deck">Deck to serialize</param>
        /// <param name="includeCards">Include full card data</param>
        /// <returns>Serialized deck data</returns>
        public static DeckSerializationData SerializeDeck(Deck deck, bool includeCards = false)
        {
            var data = new DeckSerializationData
            {
                Id = deck.id,
                Name = deck.name,
                Faction = deck.faction?.name,
                Format = deck.format,
                CreatedAt = deck.createdAt,
                UpdatedAt = DateTime.UtcNow
            };

            if (includeCards)
            {
                data.ConflictCards = deck.conflictCards?.Select(SerializeCardEntry).ToList();
                data.DynastyCards = deck.dynastyCards?.Select(SerializeCardEntry).ToList();
                data.ProvinceCards = deck.provinceCards?.Select(SerializeCardEntry).ToList();
                data.StrongholdCards = deck.strongholdCards?.Select(SerializeCardEntry).ToList();
                data.RoleCards = deck.roleCards?.Select(SerializeCardEntry).ToList();
            }
            else
            {
                // Just card IDs and counts
                data.ConflictCardIds = deck.conflictCards?.Select(c => new CardReference { Id = c.id, Count = c.count }).ToList();
                data.DynastyCardIds = deck.dynastyCards?.Select(c => new CardReference { Id = c.id, Count = c.count }).ToList();
                data.ProvinceCardIds = deck.provinceCards?.Select(c => new CardReference { Id = c.id, Count = c.count }).ToList();
                data.StrongholdCardIds = deck.strongholdCards?.Select(c => new CardReference { Id = c.id, Count = c.count }).ToList();
                data.RoleCardIds = deck.roleCards?.Select(c => new CardReference { Id = c.id, Count = c.count }).ToList();
            }

            return data;
        }

        /// <summary>
        /// Serialize player state for save games
        /// </summary>
        /// <param name="player">Player to serialize</param>
        /// <param name="includePrivateData">Include hand and deck data</param>
        /// <returns>Serialized player data</returns>
        public static PlayerSerializationData SerializePlayer(Player player, bool includePrivateData = false)
        {
            var data = new PlayerSerializationData
            {
                Id = player.id,
                Name = player.name,
                Faction = player.faction?.name,
                Honor = player.honor,
                Fate = player.fate,
                ReadyToStart = player.readyToStart,
                FirstPlayer = player.firstPlayer,
                PassedDynasty = player.passedDynasty,
                LimitedPlayed = player.limitedPlayed,
                MaxLimited = player.maxLimited,
                ImperialFavor = player.imperialFavor,
                ShowBid = player.showBid,
                ConflictOpportunities = player.conflictOpportunities
            };

            // Always include public card areas
            data.CardsInPlay = player.cardsInPlay?.Select(c => c.uuid).ToList();
            data.ConflictDiscardPile = player.conflictDiscardPile?.Select(c => c.uuid).ToList();
            data.DynastyDiscardPile = player.dynastyDiscardPile?.Select(c => c.uuid).ToList();
            data.RemovedFromGame = player.removedFromGame?.Select(c => c.uuid).ToList();

            // Province cards (face-up)
            data.ProvinceOne = player.provinceOne?.Select(c => c.uuid).ToList();
            data.ProvinceTwo = player.provinceTwo?.Select(c => c.uuid).ToList();
            data.ProvinceThree = player.provinceThree?.Select(c => c.uuid).ToList();
            data.ProvinceFour = player.provinceFour?.Select(c => c.uuid).ToList();
            data.StrongholdProvince = player.strongholdProvince?.Select(c => c.uuid).ToList();

            if (includePrivateData)
            {
                data.Hand = player.hand?.Select(c => c.uuid).ToList();
                data.ConflictDeck = player.conflictDeck?.Select(c => c.uuid).ToList();
                data.DynastyDeck = player.dynastyDeck?.Select(c => c.uuid).ToList();
                data.ProvinceDeck = player.provinceDeck?.Select(c => c.uuid).ToList();
            }

            return data;
        }

        /// <summary>
        /// Serialize complete game state
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="includePrivateData">Include hidden information</param>
        /// <returns>Serialized game state</returns>
        public static GameSerializationData SerializeGame(Game game, bool includePrivateData = false)
        {
            var data = new GameSerializationData
            {
                Id = game.id,
                Name = game.name,
                Started = game.started,
                Finished = game.finished,
                Winner = game.winner?.name,
                CurrentPhase = game.currentPhase,
                CurrentPlayer = game.currentPlayer?.name,
                TurnNumber = game.turnNumber,
                FirstPlayerDetermined = game.firstPlayerDetermined,
                ManualMode = game.manualMode,
                MuteSpectators = game.muteSpectators,
                AllowSpectators = game.allowSpectators,
                CreatedAt = game.createdAt,
                StartedAt = game.startedAt,
                FinishedAt = game.finishedAt
            };

            // Serialize players
            data.Players = game.GetPlayers().Select(p => SerializePlayer(p, includePrivateData)).ToList();

            // Serialize rings
            data.Rings = game.rings?.ToDictionary(
                kvp => kvp.Key,
                kvp => SerializeRing(kvp.Value)
            );

            // Current conflict
            if (game.currentConflict != null)
            {
                data.CurrentConflict = SerializeConflict(game.currentConflict);
            }

            // Game events (limited to recent)
            if (includePrivateData)
            {
                data.RecentEvents = game.gameEvents?.TakeLast(100).Select(SerializeGameEvent).ToList();
            }

            return data;
        }

        #endregion

        #region Helper Methods

        private static CardSerializationData SerializeCardEntry(DeckCardEntry entry)
        {
            return new CardSerializationData
            {
                Id = entry.id,
                Count = entry.count,
                Name = entry.name,
                Type = entry.type,
                Faction = entry.faction,
                Cost = entry.cost
            };
        }

        private static PlayerDelta CreatePlayerDelta(Player oldPlayer, Player newPlayer)
        {
            var delta = new PlayerDelta { PlayerId = newPlayer.PlayerId };
            bool hasChanges = false;

            if (oldPlayer.Honor != newPlayer.Honor)
            {
                delta.Honor = newPlayer.Honor;
                hasChanges = true;
            }

            if (oldPlayer.Fate != newPlayer.Fate)
            {
                delta.Fate = newPlayer.Fate;
                hasChanges = true;
            }

            if (oldPlayer.hand.Count != newPlayer.hand.Count)
            {
                delta.HandSize = newPlayer.hand.Count;
                hasChanges = true;
            }

            // Add more field comparisons as needed

            return hasChanges ? delta : null;
        }

        private static RingSerializationData SerializeRing(Ring ring)
        {
            return new RingSerializationData
            {
                Element = ring.element,
                ConflictType = ring.conflictType,
                Claimed = ring.claimed,
                ClaimedBy = ring.claimedBy,
                Contested = ring.contested,
                Fate = ring.fate
            };
        }

        private static ConflictSerializationData SerializeConflict(Conflict conflict)
        {
            return new ConflictSerializationData
            {
                ConflictType = conflict.conflictType,
                Ring = conflict.ring?.element,
                AttackingPlayer = conflict.attackingPlayer?.name,
                DefendingPlayer = conflict.defendingPlayer?.name,
                Attackers = conflict.attackers?.Select(c => c.uuid).ToList(),
                Defenders = conflict.defenders?.Select(c => c.uuid).ToList(),
                AttackerSkill = conflict.attackerSkill,
                DefenderSkill = conflict.defenderSkill
            };
        }

        private static GameEventSerializationData SerializeGameEvent(object gameEvent)
        {
            // Simplified event serialization
            return new GameEventSerializationData
            {
                Type = gameEvent.GetType().Name,
                Timestamp = DateTime.UtcNow,
                Data = ToJson(gameEvent, true) // Use compact format
            };
        }

        #endregion
    }

    #region Compression Utilities

    public static class CompressionUtils
    {
        public static byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }

    #endregion

    #region Custom JSON Converters

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader, options);
            return new Vector3(
                obj.GetValueOrDefault("x", 0f),
                obj.GetValueOrDefault("y", 0f),
                obj.GetValueOrDefault("z", 0f)
            );
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }

    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var colorString = reader.GetString();
            return ColorUtility.TryParseHtmlString(colorString, out Color color) ? color : Color.white;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"#{ColorUtility.ToHtmlStringRGBA(value)}");
        }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O")); // ISO 8601 format
        }
    }

    public class BaseCardConverter : JsonConverter<BaseCard>
    {
        public override BaseCard Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Implementation would depend on your card loading system
            var cardData = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
            // Return reconstructed card based on ID lookup
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, BaseCard value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("uuid", value.uuid);
            writer.WriteString("id", value.id);
            writer.WriteString("name", value.name);
            writer.WriteString("type", value.type);
            writer.WriteNumber("cost", value.cost);
            writer.WriteBoolean("facedown", value.facedown);
            writer.WriteString("location", value.location);
            writer.WriteEndObject();
        }
    }

    public class CompactCardConverter : JsonConverter<BaseCard>
    {
        public override BaseCard Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Compact deserialization - just ID
            var cardId = reader.GetString();
            // Would lookup card by ID
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, BaseCard value, JsonSerializerOptions options)
        {
            // Just write the UUID for network efficiency
            writer.WriteStringValue(value.uuid);
        }
    }

    public class PlayerConverter : JsonConverter<Player>
    {
        public override Player Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Player deserialization logic
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, Player value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.id);
            writer.WriteString("name", value.name);
            writer.WriteNumber("honor", value.honor);
            writer.WriteNumber("fate", value.fate);
            writer.WriteEndObject();
        }
    }

    public class CompactPlayerConverter : JsonConverter<Player>
    {
        public override Player Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, Player value, JsonSerializerOptions options)
        {
            // Minimal player data for network
            writer.WriteStartObject();
            writer.WriteString("id", value.id);
            writer.WriteNumber("h", value.honor);  // Shortened property names
            writer.WriteNumber("f", value.fate);
            writer.WriteEndObject();
        }
    }

    public class GameStateConverter : JsonConverter<GameState>
    {
        public override GameState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, GameState value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("gameId", value.GameId);
            writer.WriteNumber("turnNumber", value.TurnNumber);
            writer.WriteString("currentPlayerId", value.CurrentPlayerId);
            writer.WriteString("phase", value.Phase.ToString());
            writer.WriteEndObject();
        }
    }

    public class NetworkGameStateConverter : JsonConverter<GameState>
    {
        public override GameState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, GameState value, JsonSerializerOptions options)
        {
            // Ultra-compact network format
            writer.WriteStartObject();
            writer.WriteString("g", value.GameId);      // g = gameId
            writer.WriteNumber("t", value.TurnNumber);  // t = turn
            writer.WriteString("p", value.CurrentPlayerId); // p = player
            writer.WriteString("h", value.Phase.ToString()); // h = phase
            writer.WriteEndObject();
        }
    }

    public class DeckConverter : JsonConverter<Deck>
    {
        public override Deck Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null; // Placeholder
        }

        public override void Write(Utf8JsonWriter writer, Deck value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.id);
            writer.WriteString("name", value.name);
            writer.WriteString("faction", value.faction?.name);
            writer.WriteEndObject();
        }
    }

    #endregion

    #region Serialization Data Structures

    [Serializable]
    public class DeckSerializationData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Faction { get; set; }
        public string Format { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Full card data
        public List<CardSerializationData> ConflictCards { get; set; }
        public List<CardSerializationData> DynastyCards { get; set; }
        public List<CardSerializationData> ProvinceCards { get; set; }
        public List<CardSerializationData> StrongholdCards { get; set; }
        public List<CardSerializationData> RoleCards { get; set; }
        
        // Just references
        public List<CardReference> ConflictCardIds { get; set; }
        public List<CardReference> DynastyCardIds { get; set; }
        public List<CardReference> ProvinceCardIds { get; set; }
        public List<CardReference> StrongholdCardIds { get; set; }
        public List<CardReference> RoleCardIds { get; set; }
    }

    [Serializable]
    public class CardSerializationData
    {
        public string Id { get; set; }
        public int Count { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Faction { get; set; }
        public int Cost { get; set; }
    }

    [Serializable]
    public class CardReference
    {
        public string Id { get; set; }
        public int Count { get; set; }
    }

    [Serializable]
    public class PlayerSerializationData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Faction { get; set; }
        public int Honor { get; set; }
        public int Fate { get; set; }
        public bool ReadyToStart { get; set; }
        public bool FirstPlayer { get; set; }
        public bool PassedDynasty { get; set; }
        public int LimitedPlayed { get; set; }
        public int MaxLimited { get; set; }
        public string ImperialFavor { get; set; }
        public int ShowBid { get; set; }
        public ConflictOpportunities ConflictOpportunities { get; set; }
        
        // Card locations (UUIDs)
        public List<string> CardsInPlay { get; set; }
        public List<string> ConflictDiscardPile { get; set; }
        public List<string> DynastyDiscardPile { get; set; }
        public List<string> RemovedFromGame { get; set; }
        public List<string> ProvinceOne { get; set; }
        public List<string> ProvinceTwo { get; set; }
        public List<string> ProvinceThree { get; set; }
        public List<string> ProvinceFour { get; set; }
        public List<string> StrongholdProvince { get; set; }
        
        // Private data (only in save games)
        public List<string> Hand { get; set; }
        public List<string> ConflictDeck { get; set; }
        public List<string> DynastyDeck { get; set; }
        public List<string> ProvinceDeck { get; set; }
    }

    [Serializable]
    public class GameSerializationData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Started { get; set; }
        public bool Finished { get; set; }
        public string Winner { get; set; }
        public string CurrentPhase { get; set; }
        public string CurrentPlayer { get; set; }
        public int TurnNumber { get; set; }
        public bool FirstPlayerDetermined { get; set; }
        public bool ManualMode { get; set; }
        public bool MuteSpectators { get; set; }
        public bool AllowSpectators { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        
        public List<PlayerSerializationData> Players { get; set; }
        public Dictionary<string, RingSerializationData> Rings { get; set; }
        public ConflictSerializationData CurrentConflict { get; set; }
        public List<GameEventSerializationData> RecentEvents { get; set; }
    }

    [Serializable]
    public class RingSerializationData
    {
        public string Element { get; set; }
        public string ConflictType { get; set; }
        public bool Claimed { get; set; }
        public string ClaimedBy { get; set; }
        public bool Contested { get; set; }
        public int Fate { get; set; }
    }

    [Serializable]
    public class ConflictSerializationData
    {
        public string ConflictType { get; set; }
        public string Ring { get; set; }
        public string AttackingPlayer { get; set; }
        public string DefendingPlayer { get; set; }
        public List<string> Attackers { get; set; }
        public List<string> Defenders { get; set; }
        public int AttackerSkill { get; set; }
        public int DefenderSkill { get; set; }
    }

    [Serializable]
    public class GameEventSerializationData
    {
        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
    }

    [Serializable]
    public class GameStateDelta
    {
        public string GameId { get; set; }
        public DateTime Timestamp { get; set; }
        public int? TurnNumber { get; set; }
        public string CurrentPlayerId { get; set; }
        public GamePhase? Phase { get; set; }
        public List<PlayerDelta> PlayerDeltas { get; set; }
    }

    [Serializable]
    public class PlayerDelta
    {
        public string PlayerId { get; set; }
        public bool IsNew { get; set; }
        public Player Player { get; set; } // For new players
        public int? Honor { get; set; }
        public int? Fate { get; set; }
        public int? HandSize { get; set; }
        // Add more delta fields as needed
    }

    #endregion

    #region Action Types

    public enum ActionType : byte
    {
        PlayCard = 0,
        EndTurn = 1,
        UseAbility = 2,
        DeclareAttackers = 3,
        DeclareDefenders = 4,
        SelectChoice = 5,
        Cancel = 6,
        Pass = 7,
        Mulligan = 8,
        SelectRing = 9,
        BidHonor = 10
    }

    #endregion

    #region Specialized Serialization Services

    /// <summary>
    /// Service for managing game save files
    /// </summary>
    public static class SaveGameManager
    {
        private static readonly string SaveDirectory = Path.Combine(Application.persistentDataPath, "SaveGames");

        static SaveGameManager()
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

        /// <summary>
        /// Save game state to file
        /// </summary>
        /// <param name="game">Game to save</param>
        /// <param name="saveName">Name for the save file</param>
        /// <returns>True if successful</returns>
        public static bool SaveGame(Game game, string saveName)
        {
            try
            {
                var saveData = SerializationManager.SerializeGame(game, true);
                saveData.SaveName = saveName;
                saveData.SavedAt = DateTime.UtcNow;

                var filePath = Path.Combine(SaveDirectory, $"{saveName}.l5rsave");
                return SerializationManager.SaveToFile(saveData, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load game state from file
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <returns>Loaded game data or null</returns>
        public static GameSerializationData LoadGame(string saveName)
        {
            try
            {
                var filePath = Path.Combine(SaveDirectory, $"{saveName}.l5rsave");
                return SerializationManager.LoadFromFile<GameSerializationData>(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get list of available save games
        /// </summary>
        /// <returns>List of save game info</returns>
        public static List<SaveGameInfo> GetSaveGames()
        {
            var saveGames = new List<SaveGameInfo>();
            
            try
            {
                var saveFiles = Directory.GetFiles(SaveDirectory, "*.l5rsave");
                
                foreach (var file in saveFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new FileInfo(file);
                    
                    saveGames.Add(new SaveGameInfo
                    {
                        SaveName = fileName,
                        FilePath = file,
                        FileSize = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get save games: {ex.Message}");
            }
            
            return saveGames.OrderByDescending(s => s.LastModified).ToList();
        }

        /// <summary>
        /// Delete a save game
        /// </summary>
        /// <param name="saveName">Name of save to delete</param>
        /// <returns>True if successful</returns>
        public static bool DeleteSaveGame(string saveName)
        {
            try
            {
                var filePath = Path.Combine(SaveDirectory, $"{saveName}.l5rsave");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete save game: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Service for managing deck import/export
    /// </summary>
    public static class DeckManager
    {
        private static readonly string DeckDirectory = Path.Combine(Application.persistentDataPath, "Decks");

        static DeckManager()
        {
            if (!Directory.Exists(DeckDirectory))
                Directory.CreateDirectory(DeckDirectory);
        }

        /// <summary>
        /// Export deck to file
        /// </summary>
        /// <param name="deck">Deck to export</param>
        /// <param name="fileName">File name</param>
        /// <param name="format">Export format</param>
        /// <returns>True if successful</returns>
        public static bool ExportDeck(Deck deck, string fileName, DeckExportFormat format)
        {
            try
            {
                var filePath = Path.Combine(DeckDirectory, $"{fileName}.{GetFileExtension(format)}");
                
                switch (format)
                {
                    case DeckExportFormat.Json:
                        var deckData = SerializationManager.SerializeDeck(deck, true);
                        return SerializationManager.SaveToFile(deckData, filePath);
                        
                    case DeckExportFormat.JigokuText:
                        var jigokuText = ConvertToJigokuFormat(deck);
                        File.WriteAllText(filePath, jigokuText);
                        return true;
                        
                    case DeckExportFormat.CompactBinary:
                        var binaryData = SerializationManager.ToBinary(SerializationManager.SerializeDeck(deck, false));
                        File.WriteAllBytes(filePath, binaryData);
                        return true;
                        
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export deck: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import deck from file
        /// </summary>
        /// <param name="filePath">Path to deck file</param>
        /// <returns>Imported deck data or null</returns>
        public static DeckSerializationData ImportDeck(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                switch (extension)
                {
                    case ".json":
                        return SerializationManager.LoadFromFile<DeckSerializationData>(filePath);
                        
                    case ".txt":
                        var textContent = File.ReadAllText(filePath);
                        return ParseJigokuFormat(textContent);
                        
                    case ".l5rdeck":
                        var binaryData = File.ReadAllBytes(filePath);
                        return SerializationManager.FromBinary<DeckSerializationData>(binaryData);
                        
                    default:
                        Debug.LogError($"Unsupported deck file format: {extension}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to import deck: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert deck to Jigoku text format
        /// </summary>
        /// <param name="deck">Deck to convert</param>
        /// <returns>Jigoku format text</returns>
        private static string ConvertToJigokuFormat(Deck deck)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"# {deck.name}");
            sb.AppendLine($"# Faction: {deck.faction?.name}");
            sb.AppendLine($"# Format: {deck.format}");
            sb.AppendLine();
            
            // Stronghold
            if (deck.strongholdCards?.Any() == true)
            {
                sb.AppendLine("## Stronghold");
                foreach (var card in deck.strongholdCards)
                {
                    sb.AppendLine($"{card.count}x {card.name}");
                }
                sb.AppendLine();
            }
            
            // Role
            if (deck.roleCards?.Any() == true)
            {
                sb.AppendLine("## Role");
                foreach (var card in deck.roleCards)
                {
                    sb.AppendLine($"{card.count}x {card.name}");
                }
                sb.AppendLine();
            }
            
            // Province Deck
            if (deck.provinceCards?.Any() == true)
            {
                sb.AppendLine("## Province Deck");
                foreach (var card in deck.provinceCards)
                {
                    sb.AppendLine($"{card.count}x {card.name}");
                }
                sb.AppendLine();
            }
            
            // Dynasty Deck
            if (deck.dynastyCards?.Any() == true)
            {
                sb.AppendLine("## Dynasty Deck");
                foreach (var card in deck.dynastyCards.OrderBy(c => c.cost).ThenBy(c => c.name))
                {
                    sb.AppendLine($"{card.count}x {card.name}");
                }
                sb.AppendLine();
            }
            
            // Conflict Deck
            if (deck.conflictCards?.Any() == true)
            {
                sb.AppendLine("## Conflict Deck");
                foreach (var card in deck.conflictCards.OrderBy(c => c.cost).ThenBy(c => c.name))
                {
                    sb.AppendLine($"{card.count}x {card.name}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Parse Jigoku text format to deck data
        /// </summary>
        /// <param name="text">Jigoku format text</param>
        /// <returns>Parsed deck data</returns>
        private static DeckSerializationData ParseJigokuFormat(string text)
        {
            var deckData = new DeckSerializationData
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConflictCards = new List<CardSerializationData>(),
                DynastyCards = new List<CardSerializationData>(),
                ProvinceCards = new List<CardSerializationData>(),
                StrongholdCards = new List<CardSerializationData>(),
                RoleCards = new List<CardSerializationData>()
            };
            
            var lines = text.Split('\n');
            var currentSection = "";
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("#"))
                {
                    if (trimmed.StartsWith("# ") && string.IsNullOrEmpty(deckData.Name))
                    {
                        deckData.Name = trimmed.Substring(2).Trim();
                    }
                    continue;
                }
                
                if (trimmed.StartsWith("##"))
                {
                    currentSection = trimmed.Substring(2).Trim().ToLower();
                    continue;
                }
                
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                
                // Parse card line: "3x Card Name"
                var parts = trimmed.Split('x', 2);
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int count))
                {
                    var cardName = parts[1].Trim();
                    var cardData = new CardSerializationData
                    {
                        Name = cardName,
                        Count = count,
                        Id = GenerateCardId(cardName) // Would need card database lookup
                    };
                    
                    switch (currentSection)
                    {
                        case "stronghold":
                            deckData.StrongholdCards.Add(cardData);
                            break;
                        case "role":
                            deckData.RoleCards.Add(cardData);
                            break;
                        case "province deck":
                            deckData.ProvinceCards.Add(cardData);
                            break;
                        case "dynasty deck":
                            deckData.DynastyCards.Add(cardData);
                            break;
                        case "conflict deck":
                            deckData.ConflictCards.Add(cardData);
                            break;
                    }
                }
            }
            
            return deckData;
        }

        private static string GenerateCardId(string cardName)
        {
            // This would need to look up the card in your card database
            // For now, return a placeholder
            return cardName.ToLower().Replace(" ", "-");
        }

        private static string GetFileExtension(DeckExportFormat format)
        {
            return format switch
            {
                DeckExportFormat.Json => "json",
                DeckExportFormat.JigokuText => "txt",
                DeckExportFormat.CompactBinary => "l5rdeck",
                _ => "txt"
            };
        }
    }

    /// <summary>
    /// Service for managing user preferences
    /// </summary>
    public static class PreferencesManager
    {
        private static readonly string PreferencesFile = Path.Combine(Application.persistentDataPath, "preferences.json");

        /// <summary>
        /// Save user preferences
        /// </summary>
        /// <param name="preferences">Preferences to save</param>
        /// <returns>True if successful</returns>
        public static bool SavePreferences(UserPreferences preferences)
        {
            return SerializationManager.SaveToFile(preferences, PreferencesFile);
        }

        /// <summary>
        /// Load user preferences
        /// </summary>
        /// <returns>Loaded preferences or default</returns>
        public static UserPreferences LoadPreferences()
        {
            var preferences = SerializationManager.LoadFromFile<UserPreferences>(PreferencesFile);
            return preferences ?? new UserPreferences();
        }
    }

    #endregion

    #region Supporting Data Structures

    [Serializable]
    public class SaveGameInfo
    {
        public string SaveName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string GameName { get; set; }
        public string GameId { get; set; }
        public DateTime SavedAt { get; set; }
    }

    [Serializable]
    public class UserPreferences
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 1.0f;
        public bool EnableAnimations { get; set; } = true;
        public bool AutoPassPriority { get; set; } = false;
        public bool ShowCardPreview { get; set; } = true;
        public string DefaultDeckFormat { get; set; } = "Standard";
        public Dictionary<string, bool> PromptedActionWindows { get; set; } = new();
        public Dictionary<string, bool> TimerSettings { get; set; } = new();
        public Dictionary<string, bool> OptionSettings { get; set; } = new();
        public string PreferredLanguage { get; set; } = "en";
        public bool EnableKeyboardShortcuts { get; set; } = true;
        public float UIScale { get; set; } = 1.0f;
    }

    public enum DeckExportFormat
    {
        Json,
        JigokuText,
        CompactBinary
    }

    #endregion

    #region Network Serialization Extensions

    /// <summary>
    /// Extensions for Mirror networking serialization
    /// </summary>
    public static class NetworkSerializationExtensions
    {
        /// <summary>
        /// Write game action to network writer
        /// </summary>
        public static void WriteGameAction(this NetworkWriter writer, GameAction action)
        {
            var data = SerializationManager.SerializeGameAction(action);
            writer.WriteBytes(data, 0, data.Length);
        }

        /// <summary>
        /// Read game action from network reader
        /// </summary>
        public static GameAction ReadGameAction(this NetworkReader reader)
        {
            var length = reader.ReadInt();
            var data = reader.ReadBytes(length);
            return SerializationManager.DeserializeGameAction(data);
        }

        /// <summary>
        /// Write compressed JSON to network
        /// </summary>
        public static void WriteCompressedJson<T>(this NetworkWriter writer, T obj)
        {
            var json = SerializationManager.ToJson(obj, true);
            var compressed = SerializationManager.CompressJson(json);
            writer.WriteBytes(compressed, 0, compressed.Length);
        }

        /// <summary>
        /// Read compressed JSON from network
        /// </summary>
        public static T ReadCompressedJson<T>(this NetworkReader reader)
        {
            var length = reader.ReadInt();
            var compressed = reader.ReadBytes(length);
            var json = SerializationManager.DecompressJson(compressed);
            return SerializationManager.FromJson<T>(json, true);
        }
    }

    #endregion

    #region Validation and Integrity

    /// <summary>
    /// Utilities for data validation and integrity checking
    /// </summary>
    public static class DataIntegrity
    {
        /// <summary>
        /// Validate game state integrity
        /// </summary>
        /// <param name="gameState">Game state to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateGameState(GameState gameState)
        {
            var result = new ValidationResult();
            
            if (gameState == null)
            {
                result.AddError("Game state is null");
                return result;
            }
            
            // Validate basic properties
            if (string.IsNullOrEmpty(gameState.GameId))
                result.AddError("Game ID is required");
            
            if (gameState.TurnNumber < 0)
                result.AddError("Turn number cannot be negative");
            
            // Validate players
            if (gameState.Players == null || gameState.Players.Count == 0)
                result.AddError("Game must have players");
            
            foreach (var player in gameState.Players ?? new List<Player>())
            {
                ValidatePlayer(player, result);
            }
            
            return result;
        }

        /// <summary>
        /// Validate player state
        /// </summary>
        /// <param name="player">Player to validate</param>
        /// <param name="result">Validation result to add errors to</param>
        private static void ValidatePlayer(Player player, ValidationResult result)
        {
            if (player == null)
            {
                result.AddError("Player is null");
                return;
            }
            
            if (string.IsNullOrEmpty(player.PlayerId))
                result.AddError($"Player {player.name} has no ID");
            
            if (player.Health < 0)
                result.AddError($"Player {player.name} has negative health");
            
            if (player.MaxHealth <= 0)
                result.AddError($"Player {player.name} has invalid max health");
        }

        /// <summary>
        /// Calculate checksum for data integrity
        /// </summary>
        /// <param name="data">Data to checksum</param>
        /// <returns>Checksum string</returns>
        public static string CalculateChecksum(byte[] data)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify data integrity using checksum
        /// </summary>
        /// <param name="data">Data to verify</param>
        /// <param name="expectedChecksum">Expected checksum</param>
        /// <returns>True if data is intact</returns>
        public static bool VerifyIntegrity(byte[] data, string expectedChecksum)
        {
            var actualChecksum = CalculateChecksum(data);
            return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        
        public bool IsValid => Errors.Count == 0;
        public bool HasWarnings => Warnings.Count > 0;
        
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            
            if (Errors.Count > 0)
            {
                sb.AppendLine("Errors:");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }
            
            if (Warnings.Count > 0)
            {
                sb.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }
            
            return sb.ToString();
        }
    }

    #endregion
}
