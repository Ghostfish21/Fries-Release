using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Fries.EvtsysSrcgen {
    public class AssemblyNameUtils {
        public static string toValidClassName(string assemblyName) {
            if (string.IsNullOrWhiteSpace(assemblyName)) {
                AttributeUsageCollector.log("Empty or null string as name detected! Returning 'NULL' instead.");
                return "NULL";
            }

            var sb = new StringBuilder(assemblyName.Length);
            foreach (char c in assemblyName) {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            }
            
            if (sb.Length == 0) return "GeneratedClass";

            if (char.IsDigit(sb[0])) sb.Insert(0, '_');

            string result = sb.ToString();
            if (isCSharpKeyword(result)) result = "_" + result;
            
            return result;
        }

        private static readonly HashSet<string> cSharpKeywords = new HashSet<string> {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        private static bool isCSharpKeyword(string identifier) {
            return cSharpKeywords.Contains(identifier);
        }
    }
}