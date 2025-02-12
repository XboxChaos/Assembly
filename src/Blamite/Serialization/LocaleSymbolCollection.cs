﻿using System;
using System.Collections.Generic;

namespace Blamite.Serialization
{
	public class LocaleSymbolCollection
	{
		private readonly Dictionary<char, string> _charReplacements = new Dictionary<char, string>();
		private readonly Dictionary<string, char> _stringReplacements = new Dictionary<string, char>();

		/// <summary>
		///     Adds a symbol to the symbol collection.
		/// </summary>
		/// <param name="symbol">The unicode character to associate with a tag.</param>
		/// <param name="replacement">The name of the tag to replace the character with.</param>
		public void AddSymbol(char symbol, string replacement)
		{
			if (_stringReplacements.ContainsKey(replacement))
				throw new InvalidOperationException("Locale replacement \"" + replacement + "\" has multiple definitions");

			string ampersand = "&" + replacement;

			_charReplacements[symbol] = ampersand;
			_stringReplacements[ampersand] = symbol;
		}

		/// <summary>
		///     Adds a collection of symbols to this symbol collection.
		/// </summary>
		/// <param name="symbols">The symbols to add.</param>
		public void AddSymbols(LocaleSymbolCollection symbols)
		{
			foreach (var symbol in symbols._charReplacements)
				_charReplacements[symbol.Key] = symbol.Value;

			foreach (var str in symbols._stringReplacements)
				_stringReplacements[str.Key] = str.Value;
		}

		/// <summary>
		///     Replaces symbol characters in a locale with their corresponding XML tags.
		/// </summary>
		/// <param name="locale">The locale to replace characters in.</param>
		/// <returns>The locale with symbol characters replaced with XML tags.</returns>
		public string ReplaceSymbols(string locale)
		{
			if (_charReplacements.Count == 0)
				return locale;

			foreach (var c in _charReplacements.Keys)
			{
				if (locale.Contains(c.ToString()))
					locale = locale.Replace(c.ToString(), _charReplacements[c]);
			}
			return locale;
		}

		/// <summary>
		///     Replaces XML tags in a locale with their corresponding symbol characters.
		/// </summary>
		/// <param name="locale">The locale to replace tags in.</param>
		/// <returns>The locale with XML tags replaced with symbol characters.</returns>
		public string ReplaceTags(string locale)
		{
			if (_stringReplacements.Count == 0)
				return locale;

			foreach (string s in _stringReplacements.Keys)
			{
                if (locale.Contains(s))
                {
					locale = locale.Replace(s, _stringReplacements[s].ToString());
                }
            }
			return locale;
		}

		public string GetSymbolName(char symbol)
		{
			if (!_charReplacements.ContainsKey(symbol))
				return null;

			return _charReplacements[symbol];
		}
	}
}