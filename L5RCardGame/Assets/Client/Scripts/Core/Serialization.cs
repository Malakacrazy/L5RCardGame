using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Serialization utilities for Unity without external dependencies
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Serialize object to JSON using Unity's JsonUtility
        /// </summary>
        public static string ToJson(object obj)
        {
            if (obj == null) return "null";
            return JsonUtility.ToJson(obj);
        }
        
        /// <summary>
        /// Deserialize JSON to object using Unity's JsonUtility
        /// </summary>
        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);
            return JsonUtility.FromJson<T>(json);
        }
        
        /// <summary>
        /// Serialize with pretty printing
        /// </summary>
        public static string ToJsonPretty(object obj)
        {
            if (obj == null) return "null";
            return JsonUtility.ToJson(obj, true);
        }
        
        /// <summary>
        /// Convert dictionary to JSON manually (Unity JsonUtility limitation)
        /// </summary>
        public static string DictionaryToJson<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            var json = "{";
            bool first = true;
            foreach (var kvp in dictionary)
            {
                if (!first) json += ",";
                json += $"\"{kvp.Key}\":\"{kvp.Value}\"";
                first = false;
            }
            json += "}";
            return json;
        }
        
        /// <summary>
        /// Simple list to JSON conversion
        /// </summary>
        public static string ListToJson<T>(List<T> list)
        {
            var json = "[";
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) json += ",";
                json += JsonUtility.ToJson(list[i]);
            }
            json += "]";
            return json;
        }
    }
}
