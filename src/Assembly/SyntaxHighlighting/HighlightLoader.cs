using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Blamite.Blam.Scripting;

namespace Assembly.SyntaxHighlighting
{
	public static class HighlightLoader
	{
		private static readonly Dictionary<string, IHighlightingDefinition> _definitionCache =
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
			if (_definitionCache.TryGetValue(filename, out result))
				return result;

			// Embedded resources are prefixed with their namespace name
			string ns = typeof (HighlightLoader).Namespace;
			string resourcePath = ns + '.' + filename;

			// Read it from the assembly this class is embedded in.
			System.Reflection.Assembly assembly = typeof (HighlightLoader).Assembly;
			using (Stream s = assembly.GetManifestResourceStream(resourcePath))
			{
				if (s != null)
					using (var reader = new XmlTextReader(s))
						result = HighlightingLoader.Load(reader, HighlightingManager.Instance);
			}

			// Cache the result and return it
			_definitionCache[filename] = result;
			return result;
		}

		public static IHighlightingDefinition LoadEmbeddedBlamScriptDefinition(string filename, OpcodeLookup lookup)
		{
			// Embedded resources are prefixed with their namespace name
			string ns = typeof(HighlightLoader).Namespace;
			string resourcePath = ns + '.' + filename;

			// Read it from the assembly this class is embedded in.
			System.Reflection.Assembly assembly = typeof(HighlightLoader).Assembly;
			using (Stream s = assembly.GetManifestResourceStream(resourcePath))
			{
				if (s != null)
                {
					XDocument doc = XDocument.Load(s);
					UpdateXshdWithScriptInfo(doc, lookup);

					// We can't write the updated definition to the resource stream. Therefore we have to create a new stream.
					using (var destinationStream = new MemoryStream())
                    {
						// Save and rewind the stream.
						doc.Save(destinationStream);
						destinationStream.Seek(0, SeekOrigin.Begin);

						using (var reader = new XmlTextReader(destinationStream))
						{
							// Don't cache the definition, since keywords vary between games.
							return HighlightingLoader.Load(reader, HighlightingManager.Instance);
						}
					}
				}
				else
                {
					throw new FileNotFoundException($"An embedded highlighting definition file with the name {filename} could not be found.");
                }
			}
		}

		private static void UpdateXshdWithScriptInfo(XDocument doc, OpcodeLookup opcodes)
        {
			XNamespace df = doc.Root.Name.Namespace;
			var keyWords = doc.Root.Descendants(df + "Keywords");
			XElement scriptTypeElement = keyWords.Where(x => x.Attribute("color")?.Value == "ScriptType")?.First();
			XElement valueTypeElement = keyWords.Where(x => x.Attribute("color")?.Value == "ValueType")?.First();
			XElement functionElement = keyWords.Where(x => x.Attribute("color")?.Value == "Function")?.First();

			foreach (string scriptType in opcodes.GetAllScriptTypeNames())
            {
				XElement element = StringToWordElement(scriptType, df);
				scriptTypeElement.Add(element);
            }

            foreach (string type in opcodes.GetAllValueTypeNames())
			{
				XElement element = StringToWordElement(type, df);
				valueTypeElement.Add(element);
			}

			foreach (string function in opcodes.GetAllUniqueFunctionNames())
			{
				XElement element = StringToWordElement(function, df);
				functionElement.Add(element);
			}
		}

		private static XElement StringToWordElement(string word, XNamespace ns)
        {
			XElement element = new XElement(ns + "Word");
			element.Value = word;
			return element;
		}
	}
}