using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Flexibility
{
    public class LocaleSymbolCollection
    {
        private Dictionary<char, string> _charReplacements = new Dictionary<char, string>();

        public void AddSymbol(char symbol, string replacement)
        {
            _charReplacements[symbol] = "<" + replacement + "/>";
        }

        public string ReplaceSymbols(string locale)
        {
            // Alex: I sped this up a bit because this was the bottleneck for locale loading.
            // Rather than looping through each replacement and using .net's Replace function,
            // I added the characters to a Dictionary and only loop through the locale string once.
            // It has better performance in the average case.
            // -- Aaron
            for (int i = 0; i < locale.Length; i++)
            {
                string replacement;
                if (_charReplacements.TryGetValue(locale[i], out replacement))
                {
                    locale = locale.Substring(0, i) + replacement + locale.Substring(i + 1);
                    i += replacement.Length - 1;
                }
            }
            return locale;
        }
    }
}
