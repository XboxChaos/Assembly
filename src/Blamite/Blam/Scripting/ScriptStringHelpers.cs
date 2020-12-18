using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Blamite.Blam.Scripting
{
    public static class ScriptStringHelpers
    {
        private static Dictionary<string, string> escapeMapping = new Dictionary<string, string>()
        {
            {"\"", @"\"""},
            {"\n", @"\n"},
            {"\r", @"\r"},
            {"\t", @"\t"},
        };

        private static Regex escapeRegex = new Regex(string.Join("|", escapeMapping.Keys.ToArray()));

        public static string Escape(string s)
        {
            return escapeRegex.Replace(s, EscapeMatchEval);
        }

        public static string Unescape(string s)
        {
            return s.Replace("\\\"", "\"")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private static string EscapeMatchEval(Match m)
        {
            if (escapeMapping.ContainsKey(m.Value))
            {
                return escapeMapping[m.Value];
            }
            return escapeMapping[Regex.Escape(m.Value)];
        }
    }
}
