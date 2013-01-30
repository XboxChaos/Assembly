/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Flexibility
{
    /// <summary>
    /// Processes build info XML files, providing a method of retrieving the layouts defined for a build.
    /// </summary>
    public class BuildInfoLoader
    {
        private XContainer _builds;
        private string _basePath;

        /// <summary>
        /// Constructs a new BuildLayoutLoader, loading an XML document listing the supported builds.
        /// </summary>
        /// <param name="buildInfo">The XML document listing the supported builds.</param>
        /// <param name="layoutDirPath">The path to the directory where build layout files are stored. Must end with a slash.</param>
        public BuildInfoLoader(XDocument buildInfo, string layoutDirPath)
        {
            _builds = buildInfo.Element("builds");
            if (_builds == null)
                throw new ArgumentException("Invalid build info document");
            _basePath = layoutDirPath;
        }

        /// <summary>
        /// Loads all of the structure layouts defined for a specified build.
        /// </summary>
        /// <param name="buildName">The build version to load structure layouts for.</param>
        /// <returns>The build's information.</returns>
        public BuildInformation LoadBuild(string buildName)
        {
            // Build tags have the format:
            // <build name="(name of this build of the engine)"
            //        version="(build version string)" 
            //        localeKey="(key used to decrypt the locales)" // Optional
            //        requiresTaglist="bool indication if the build requires a taglist)" // Optional
            //        headerSize="(size of the header, in hex)"
            //        filename="(filename containing layouts)" />
            //
            // Just find the first build tag whose version matches and load its file.
            XElement buildElement = _builds.Elements("build").FirstOrDefault(e => e.Attribute("version") != null && e.Attribute("version").Value == buildName);
            if (buildElement == null)
                return null;
            XAttribute gameNameAttrib = buildElement.Attribute("name");
            XAttribute localeKeyAttrib = buildElement.Attribute("localeKey");
            XAttribute stringidKeyAttrib = buildElement.Attribute("stringidKey");
            XAttribute stringidModifiersAttrib = buildElement.Attribute("stringidModifiers"); // NOTE: Deprecated - use stringidDefinitions instead!
            XAttribute stringidDefinitionsAttrib = buildElement.Attribute("stringidDefinitions");
            XAttribute filenameKeyAttrib = buildElement.Attribute("filenameKey");
            XAttribute headerSizeAttrib = buildElement.Attribute("headerSize");
            XAttribute loadStringsAttrib = buildElement.Attribute("loadStrings");
            XAttribute shortNameAttrib = buildElement.Attribute("shortName");
            XAttribute filenameAttrib = buildElement.Attribute("filename");
            XAttribute localeSymbolsAttrib = buildElement.Attribute("localeSymbols");
            XAttribute pluginFolderAttrib = buildElement.Attribute("pluginFolder");
            XAttribute scriptDefinitionsAttrib = buildElement.Attribute("scriptDefinitions");
            XAttribute localeAlignmentAttrib = buildElement.Attribute("localeAlignment");
            if (gameNameAttrib == null || filenameAttrib == null || headerSizeAttrib == null || shortNameAttrib == null || pluginFolderAttrib == null)
                return null;

            bool loadStrings = true;
            string localeKey = null;
            string stringidKey = null;
            string filenameKey = null;
            string localeSymbols = null;
            string scriptOpcodes = null;
            int localeAlignment = 0x1000;
            int headerSize = ParseNumber(headerSizeAttrib.Value);

            if (filenameKeyAttrib != null)
                filenameKey = filenameKeyAttrib.Value;
            if (localeSymbolsAttrib != null)
                localeSymbols = localeSymbolsAttrib.Value;
            if (stringidKeyAttrib != null)
                stringidKey = stringidKeyAttrib.Value;
            if (localeKeyAttrib != null)
                localeKey = localeKeyAttrib.Value;
            if (loadStringsAttrib != null)
                loadStrings = Convert.ToBoolean(loadStringsAttrib.Value);
            if (scriptDefinitionsAttrib != null)
                scriptOpcodes = _basePath + @"Scripting\" + scriptDefinitionsAttrib.Value;
            if (localeAlignmentAttrib != null)
                localeAlignment = ParseNumber(localeAlignmentAttrib.Value);

            // StringID Modifers, this is a bitch
            IStringIDResolver stringIdResolver = null;
            if (stringidModifiersAttrib != null)
            {
                StringIDModifierResolver modifierResolver = new StringIDModifierResolver();
                stringIdResolver = modifierResolver;

                string[] sets = stringidModifiersAttrib.Value.Split('|');
                foreach (string set in sets)
                {
                    string[] parts = set.Split(',');
                    /*
                     Format:
                     * Identifier
                     * Modifier
                     * MathSymbol (+/-)
                     * Direction (>/<)
                     */

                    int identifier = int.Parse(parts[0].Replace("0x", ""), NumberStyles.AllowHexSpecifier);
                    int modifier = int.Parse(parts[1].Replace("0x", ""), NumberStyles.AllowHexSpecifier);
                    bool isAddition = parts[2] == "+";
                    bool isGreaterThan = parts[3] == ">";

                    modifierResolver.AddModifier(identifier, modifier, isGreaterThan, isAddition);
                }
            }
            else if (stringidDefinitionsAttrib != null)
            {
                StringIDSetResolver setResolver = new StringIDSetResolver();
                stringIdResolver = setResolver;

                XDocument stringIdDocument = XDocument.Load(_basePath + @"StringIDs\" + stringidDefinitionsAttrib.Value);
                StringIDSetLoader.LoadAllStringIDSets(stringIdDocument, setResolver);
            }
            else
            {
                // Use a blank modifier
                stringIdResolver = new StringIDModifierResolver();
            }

            BuildInformation info = new BuildInformation(gameNameAttrib.Value, localeKey, stringidKey, stringIdResolver, filenameKey, headerSize, loadStrings, filenameAttrib.Value, shortNameAttrib.Value, pluginFolderAttrib.Value, scriptOpcodes, localeAlignment);
            XDocument layoutDocument = XDocument.Load(_basePath + filenameAttrib.Value);
            LoadAllLayouts(layoutDocument, info);

            if (localeSymbols != null)
            {
                XDocument localeSymbolDocument = XDocument.Load(_basePath + @"LocaleSymbols\" + localeSymbols);
                LoadAllLocaleSymbols(localeSymbolDocument, info);
            }

            return info;
        }

        /// <summary>
        /// Loads all of the structure layouts defined in an XML document.
        /// </summary>
        /// <param name="layoutDocument">The XML document containing the structure layouts.</param>
        /// <param name="info">The BuildInformation object to add the layout to.</param>
        /// <exception cref="ArgumentException">Thrown if the XML document contains invalid layout information.</exception>
        private static void LoadAllLayouts(XDocument layoutDocument, BuildInformation info)
        {
            // Make sure there is a root <layouts> tag
            XContainer layoutContainer = layoutDocument.Element("layouts");
            if (layoutContainer == null)
                throw new ArgumentException("Invalid layout document");

            // Layout tags have the format:
            // <layout for="(layout's purpose)">(structure fields)</layout>
            foreach (XElement layout in layoutContainer.Elements("layout"))
            {
                XAttribute forAttrib = layout.Attribute("for");
                if (forAttrib == null)
                    throw new ArgumentException("Layout tags must have a \"for\" attribute");
                info.AddLayout(forAttrib.Value, XMLLayoutLoader.LoadLayout(layout));
            }
        }

        private static void LoadAllLocaleSymbols(XDocument localeSymbolDocument, BuildInformation info)
        {
            // Make sure there is a root <symbols> tag
            XContainer localeSymbolContainer = localeSymbolDocument.Element("symbols");
            if (localeSymbolContainer == null)
                throw new ArgumentException("Invalid symbols document");

            // Symbol tags have the format:
            // <symbol code="0x(the byte array)" display="(Friendly Name)" />
            foreach (XElement symbol in localeSymbolContainer.Elements("symbol"))
            {
                XAttribute codeAttrib = symbol.Attribute("code");
                XAttribute displayAttrib = symbol.Attribute("display");
                if (codeAttrib == null)
                    throw new ArgumentException("Symbol tags must have a \"code\" attribute");
                if (displayAttrib == null)
                    throw new ArgumentException("Symbol tags must have a \"display\" attribute");

                // Convert code to int
                codeAttrib.Value = codeAttrib.Value.Replace("0x","");
                byte[] code = FunctionHelpers.HexStringToBytes(codeAttrib.Value);
                string codeString = Encoding.UTF8.GetString(code);
                char codeChar = codeString[0];

                info.LocaleSymbols.AddSymbol(codeChar, displayAttrib.Value);
            }
        }

        private static int ParseNumber(string str)
        {
            if (str.StartsWith("0x"))
                return int.Parse(str.Substring(2), NumberStyles.HexNumber);
            return int.Parse(str);
        }
    }
}
