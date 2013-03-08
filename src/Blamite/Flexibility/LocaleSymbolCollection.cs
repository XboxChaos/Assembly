using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Flexibility
{
    public class LocaleSymbolCollection
    {
        private Dictionary<char, string> _charReplacements = new Dictionary<char, string>();

        public void AddSymbol(char symbol, string replacement)
        {
            _charReplacements[symbol] = "<" + replacement + "/>";
        }

        public void AddSymbols(LocaleSymbolCollection symbols)
        {
            foreach (var symbol in symbols._charReplacements)
                _charReplacements[symbol.Key] = symbol.Value;
        }

        public string ReplaceSymbols(string locale)
        {
            // Loop through each character and see if it's in the replacements dictionary
            for (int i = 0; i < locale.Length; i++)
            {
                string replacement;
                if (_charReplacements.TryGetValue(locale[i], out replacement))
                {
                    // Replace the character with its replacement tag
                    locale = locale.Substring(0, i) + replacement + locale.Substring(i + 1);
                    i += replacement.Length - 1;
                }
            }
            return locale;
        }
    }
}
