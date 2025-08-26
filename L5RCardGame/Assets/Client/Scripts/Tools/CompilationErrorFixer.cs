using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame.Tools
{
    /// <summary>
    /// Tool to automatically fix common compilation errors in the L5R codebase
    /// </summary>
    public static class CompilationErrorFixer
    {
        private static readonly Dictionary<string, string> InterfaceMethodReplacements = new Dictionary<string, string>
        {
            // Fix IGameEvent interface method signatures
            { @"bool IsCancelled\(\)", "bool IsCancelled()" },
            { @"bool IsResolved\(\)", "bool IsResolved()" },
            { @"void Execute\(\)", "void Execute()" },
            { @"IGameEvent GetResolutionEvent\(\)", "IGameEvent GetResolutionEvent()" },
            
            // Fix IGameStep interface method signatures  
            { @"bool IsComplete\(\)", "bool IsComplete { get; }" },
            { @"object GetDebugInfo\(\)", "string GetDebugInfo()" },
            { @"bool OnMenuCommand\(Player player, string arg, string uuid, string method\)", "bool OnMenuCommand(Player player, string command, string arg1, string arg2)" }
        };

        private static readonly Dictionary<string, string> ClassDefinitionFixes = new Dictionary<string, string>
        {
            // Add missing partial modifiers
            { @"public class (AbilityLimit[^{]*){", "public partial class $1{" },
            { @"public class (SimpleStep[^{]*){", "public partial class $1{" },
            { @"public class (BaseStep[^{]*){", "public partial class $1{" },
            { @"public class (DrawCard[^{]*){", "public partial class $1{" },
            { @"public class (ProvinceCard[^{]*){", "public partial class $1{" },
            { @"public class (StrongholdCard[^{]*){", "public partial class $1{" },
            { @"public class (RoleCard[^{]*){", "public partial class $1{" }
        };

        private static readonly Dictionary<string, string> BaseClassFixes = new Dictionary<string, string>
        {
            // Fix base class inheritance conflicts
            { @"PlayerGameAction", "PlayerAction" },
            { @"CardGameAction", "CardGameAction" }, // This one is correct
            { @"RingAction", "GameAction" } // Most ring actions inherit from GameAction
        };

        private static readonly string[] FilesToRemoveDuplicateInterfaces = 
        {
            "TriggeredAbilityWindowTitles.cs"
        };

        /// <summary>
        /// Fix all compilation errors in the Core directory
        /// </summary>
        public static void FixAllErrors(string corePath = null)
        {
            if (string.IsNullOrEmpty(corePath))
            {
                corePath = Path.Combine(Application.dataPath, "Client", "Scripts", "Core");
            }

            if (!Directory.Exists(corePath))
            {
                Debug.LogError($"Core directory not found: {corePath}");
                return;
            }

            Debug.Log($"Starting compilation error fixing in: {corePath}");

            // Get all C# files in the directory and subdirectories
            string[] files = Directory.GetFiles(corePath, "*.cs", SearchOption.AllDirectories);
            
            int filesFixed = 0;
            foreach (string file in files)
            {
                try
                {
                    if (FixFile(file))
                    {
                        filesFixed++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing file {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            Debug.Log($"Fixed {filesFixed} files out of {files.Length} total files.");
        }

        /// <summary>
        /// Fix compilation errors in a single file
        /// </summary>
        public static bool FixFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File not found: {filePath}");
                return false;
            }

            string content = File.ReadAllText(filePath);
            string originalContent = content;
            string fileName = Path.GetFileName(filePath);

            // Apply all fixes
            content = FixInterfaceSignatures(content);
            content = FixClassDefinitions(content);
            content = FixBaseClasses(content);
            content = RemoveDuplicateInterfaces(content, fileName);
            content = FixStaticClassUsage(content);
            content = FixDuplicateDefinitions(content);
            content = AddMissingNamespaces(content);

            // Only write if changes were made
            if (content != originalContent)
            {
                File.WriteAllText(filePath, content, Encoding.UTF8);
                Debug.Log($"Fixed compilation errors in: {fileName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fix interface method signatures
        /// </summary>
        private static string FixInterfaceSignatures(string content)
        {
            foreach (var replacement in InterfaceMethodReplacements)
            {
                content = Regex.Replace(content, replacement.Key, replacement.Value);
            }
            return content;
        }

        /// <summary>
        /// Fix class definitions (add partial modifiers, etc.)
        /// </summary>
        private static string FixClassDefinitions(string content)
        {
            foreach (var fix in ClassDefinitionFixes)
            {
                content = Regex.Replace(content, fix.Key, fix.Value);
            }
            return content;
        }

        /// <summary>
        /// Fix base class inheritance issues
        /// </summary>
        private static string FixBaseClasses(string content)
        {
            foreach (var fix in BaseClassFixes)
            {
                content = content.Replace(fix.Key, fix.Value);
            }
            return content;
        }

        /// <summary>
        /// Remove duplicate interface definitions
        /// </summary>
        private static string RemoveDuplicateInterfaces(string content, string fileName)
        {
            if (Array.Exists(FilesToRemoveDuplicateInterfaces, f => fileName.Contains(f)))
            {
                // Remove duplicate IGameEvent interface definition
                content = Regex.Replace(content, 
                    @"/// <summary>\s*/// Enhanced game event interface with additional properties\s*/// </summary>\s*public interface IGameEvent\s*\{[^}]+\}",
                    "",
                    RegexOptions.Multiline | RegexOptions.Singleline);
            }
            return content;
        }

        /// <summary>
        /// Fix static class usage issues
        /// </summary>
        private static string FixStaticClassUsage(string content)
        {
            // Fix static type usage issues
            var staticTypeFixes = new Dictionary<string, string>
            {
                { @"static type '([^']+)'", "$1" },
                { @"Cannot declare a variable of static type '([^']+)'", "// Fixed: Cannot declare variable of $1" },
                { @"static types cannot be used as type arguments", "// Fixed: Static type usage" },
                { @"static types cannot be used as return types", "// Fixed: Static return type" },
                { @"static types cannot be used as parameters", "// Fixed: Static parameter type" }
            };

            foreach (var fix in staticTypeFixes)
            {
                content = Regex.Replace(content, fix.Key, fix.Value);
            }

            return content;
        }

        /// <summary>
        /// Fix duplicate class/enum/interface definitions
        /// </summary>
        private static string FixDuplicateDefinitions(string content)
        {
            // Common duplicate definition patterns
            var patterns = new[]
            {
                @"(\s*//\s*)?public\s+(partial\s+)?class\s+(\w+)\s*[^{]*{[^}]*}\s*(?=\s*public\s+(partial\s+)?class\s+\3)",
                @"(\s*//\s*)?public\s+enum\s+(\w+)\s*{[^}]*}\s*(?=\s*public\s+enum\s+\2)",
                @"(\s*//\s*)?public\s+interface\s+(\w+)\s*{[^}]*}\s*(?=\s*public\s+interface\s+\2)"
            };

            foreach (var pattern in patterns)
            {
                content = Regex.Replace(content, pattern, "", RegexOptions.Multiline | RegexOptions.Singleline);
            }

            return content;
        }

        /// <summary>
        /// Add missing namespace imports
        /// </summary>
        private static string AddMissingNamespaces(string content)
        {
            var requiredUsings = new HashSet<string>();

            // Check for common missing references
            if (content.Contains("Mirror") || content.Contains("NetworkBehaviour"))
            {
                requiredUsings.Add("// using Mirror; // Mirror networking - not available in Unity standalone");
            }

            if (content.Contains("MessagePack"))
            {
                requiredUsings.Add("// using MessagePack; // MessagePack - not available in Unity");
            }

            if (content.Contains("ILogger") || content.Contains("IConfiguration"))
            {
                requiredUsings.Add("// using Microsoft.Extensions.Logging; // ASP.NET Core - not available in Unity");
                requiredUsings.Add("// using Microsoft.Extensions.Configuration; // ASP.NET Core - not available in Unity");
            }

            if (requiredUsings.Count > 0)
            {
                // Insert after existing using statements
                var usingRegex = new Regex(@"(using\s+[^;]+;\s*)*", RegexOptions.Multiline);
                var match = usingRegex.Match(content);
                
                if (match.Success)
                {
                    var insertPos = match.Index + match.Length;
                    var missingUsings = string.Join("\n", requiredUsings) + "\n";
                    content = content.Insert(insertPos, missingUsings);
                }
            }

            return content;
        }

        /// <summary>
        /// Get a summary of issues found in the codebase
        /// </summary>
        public static void AnalyzeIssues(string corePath = null)
        {
            if (string.IsNullOrEmpty(corePath))
            {
                corePath = Path.Combine(Application.dataPath, "Client", "Scripts", "Core");
            }

            var issueCount = new Dictionary<string, int>();
            string[] files = Directory.GetFiles(corePath, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                
                // Count common issues
                CountIssues(content, issueCount, "Missing partial modifier", @"public class (AbilityLimit|SimpleStep|BaseStep|DrawCard|ProvinceCard|StrongholdCard|RoleCard)");
                CountIssues(content, issueCount, "Duplicate interface definitions", @"public interface IGameEvent");
                CountIssues(content, issueCount, "Wrong base class", @"PlayerGameAction");
                CountIssues(content, issueCount, "Static type usage", @"static type");
                CountIssues(content, issueCount, "Method signature mismatch", @"OnMenuCommand.*string.*string.*string");
                CountIssues(content, issueCount, "Missing Mirror namespace", @"\bMirror\b");
                CountIssues(content, issueCount, "Missing MessagePack namespace", @"\bMessagePack\b");
            }

            Debug.Log("=== Compilation Issue Analysis ===");
            foreach (var issue in issueCount)
            {
                Debug.Log($"{issue.Key}: {issue.Value} occurrences");
            }
        }

        private static void CountIssues(string content, Dictionary<string, int> issueCount, string issueName, string pattern)
        {
            var matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                issueCount[issueName] = issueCount.ContainsKey(issueName) ? 
                    issueCount[issueName] + matches.Count : matches.Count;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor menu item to fix compilation errors
        /// </summary>
        [UnityEditor.MenuItem("L5R Tools/Fix Compilation Errors")]
        public static void FixCompilationErrorsMenuItem()
        {
            FixAllErrors();
            UnityEditor.AssetDatabase.Refresh();
        }

        /// <summary>
        /// Unity Editor menu item to analyze issues
        /// </summary>
        [UnityEditor.MenuItem("L5R Tools/Analyze Compilation Issues")]
        public static void AnalyzeIssuesMenuItem()
        {
            AnalyzeIssues();
        }
#endif
    }
}
