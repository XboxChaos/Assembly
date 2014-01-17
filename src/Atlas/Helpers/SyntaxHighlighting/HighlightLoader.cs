using System.Collections.Generic;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Atlas.Helpers.SyntaxHighlighting
{
	public static class HighlightLoader
	{
		private static readonly Dictionary<string, IHighlightingDefinition> DefinitionCache =
			new Dictionary<string, IHighlightingDefinition>();

		/// <summary>
		///     Loads a highlight definition embedded into the assembly.
		/// </summary>
		/// <param name="filename">The name of the definition file to load, e.g. "XMLOrange.xshd".</param>
		/// <returns>The loaded IHighlightingDefinition.</returns>
		public static IHighlightingDefinition LoadEmbeddedDefinition(string filename)
		{
			// Load from the cache if the definition has already been parsed
			IHighlightingDefinition result;
			if (DefinitionCache.TryGetValue(filename, out result))
				return result;

			// Embedded resources are prefixed with their namespace name
			var ns = typeof(HighlightLoader).Namespace;
			var resourcePath = ns + '.' + filename;

			// Read it from the assembly this class is embedded in.
			var assembly = typeof (HighlightLoader).Assembly;
			using (var s = assembly.GetManifestResourceStream(resourcePath))
			{
				if (s != null)
					using (var reader = new XmlTextReader(s))
						result = HighlightingLoader.Load(reader, HighlightingManager.Instance);
			}

			// Cache the result and return it
			DefinitionCache[filename] = result;
			return result;
		}
	}
}