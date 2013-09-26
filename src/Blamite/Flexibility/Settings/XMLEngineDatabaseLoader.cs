using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Flexibility.Settings
{
    /// <summary>
    /// Loads engine databases from XML data.
    /// </summary>
    public static class XMLEngineDatabaseLoader
    {
        /// <summary>
        /// Loads an engine database from an XML file.
        /// </summary>
        /// <param name="path">The path to the XML file to parse.</param>
        /// <returns>The built engine database.</returns>
        public static EngineDatabase LoadDatabase(string path)
        {
            var document = XDocument.Load(path);
            return LoadDatabase(document);
        }

        /// <summary>
        /// Loads an engine database from an XML document.
        /// </summary>
        /// <param name="document">The XML document to process.</param>
        /// <returns>The built engine database.</returns>
        public static EngineDatabase LoadDatabase(XDocument document)
        {
            var enginesElem = document.Element("engines");
            return LoadDatabase(enginesElem);
        }

        /// <summary>
        /// Loads an engine database from an XML container.
        /// </summary>
        /// <param name="container">The container to read engine elements from.</param>
        /// <returns>The built engine database.</returns>
        public static EngineDatabase LoadDatabase(XContainer container)
        {
            var loader = CreateSettingsGroupLoader();
            var result = new EngineDatabase();
            foreach (var elem in container.Elements("engine"))
            {
                var name = XMLUtil.GetStringAttribute(elem, "name");
                var version = XMLUtil.GetStringAttribute(elem, "version");
                var inherits = XMLUtil.GetStringAttribute(elem, "inherits", null);
                var settings = loader.LoadSettingsGroup(elem);
                if (!string.IsNullOrWhiteSpace(inherits))
                {
                    // Clone the base engine's settings and then import the new settings on top of it
                    var baseSettings = result.FindEngineByName(inherits).Settings.DeepClone();
                    baseSettings.Import(settings);
                    settings = baseSettings;
                }
                var desc = new EngineDescription(name, version, settings);
                result.RegisterEngine(desc);
            }
            return result;
        }

        private static XMLSettingsGroupLoader CreateSettingsGroupLoader()
        {
            var loader = new XMLSettingsGroupLoader();
            loader.RegisterComplexSettingLoader("layouts", new XMLLayoutLoader());
            loader.RegisterComplexSettingLoader("localeSymbols", new XMLLocaleSymbolLoader());
            loader.RegisterComplexSettingLoader("stringIds", new XMLStringIDSetLoader());
            loader.RegisterComplexSettingLoader("scripting", new XMLOpcodeLookupLoader());
            loader.RegisterComplexSettingLoader("vertexLayouts", new XMLVertexLayoutLoader());
            return loader;
        }
    }
}
