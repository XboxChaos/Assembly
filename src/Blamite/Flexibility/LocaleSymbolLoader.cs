using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Flexibility
{
    public static class LocaleSymbolLoader
    {
        /// <summary>
        /// Loads all of the locale symbols defined in an XML document.
        /// </summary>
        /// <param name="layoutDocument">The XML document to load locale symbols from.</param>
        /// <returns>The symbols that were loaded.</returns>
        public static LocaleSymbolCollection LoadLocaleSymbols(XDocument symbolDocument)
        {
            // Make sure there is a root <symbols> tag
            XContainer container = symbolDocument.Element("symbols");
            if (container == null)
                throw new ArgumentException("Invalid symbols document");

            // Symbol tags have the format:
            // <symbol code="0x(the byte array)" display="(Friendly Name)" />
            LocaleSymbolCollection result = new LocaleSymbolCollection();
            foreach (XElement symbol in container.Elements("symbol"))
            {
                string code = XMLUtil.GetStringAttribute(symbol, "code");
                string display = XMLUtil.GetStringAttribute(symbol, "display");

                // Convert code to int
                code = code.Replace("0x", "");
                byte[] codeBytes = FunctionHelpers.HexStringToBytes(code);
                string codeString = Encoding.UTF8.GetString(codeBytes);
                char codeChar = codeString[0];

                result.AddSymbol(codeChar, display);
            }
            return result;
        }

        /// <summary>
        /// Loads all of the locale symbols defined in an XML document.
        /// </summary>
        /// <param name="documentPath">The path to the XML document to load.</param>
        /// <returns>The symbols that were loaded.</returns>
        public static LocaleSymbolCollection LoadLocaleSymbols(string documentPath)
        {
            return LoadLocaleSymbols(XDocument.Load(documentPath));
        }
    }
}
