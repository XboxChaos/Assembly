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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Util;

namespace Blamite.Flexibility
{
    /// <summary>
    /// Processes build info XML files, providing a method of retrieving the layouts defined for a build.
    /// </summary>
    public class BuildInfoLoader
    {
        private XContainer _builds;
        private string _basePath;

        /// <summary>
        /// Constructs a new BuildInfoLoader, loading an XML document listing the supported builds.
        /// </summary>
        /// <param name="buildInfo">The path to the XML document listing the supported builds.</param>
        /// <param name="layoutDirPath">The path to the directory where build layout files are stored.</param>
        public BuildInfoLoader(string buildInfoPath, string layoutDirPath)
            : this(XDocument.Load(buildInfoPath), layoutDirPath)
        {
        }

        /// <summary>
        /// Constructs a new BuildInfoLoader, loading an XML document listing the supported builds.
        /// </summary>
        /// <param name="buildInfo">The XML document listing the supported builds.</param>
        /// <param name="layoutDirPath">The path to the directory where build layout files are stored.</param>
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
        /// <returns>The build's information, or null if it was not found.</returns>
        public BuildInformation LoadBuild(string buildName)
        {
            // Find the first build tag whose version matches and load its file.
            XElement buildElement = _builds.Elements("build").FirstOrDefault(e => XMLUtil.GetStringAttribute(e, "version", "") == buildName);
            if (buildElement == null)
                return null;

            // Read attributes
            string gameName = XMLUtil.GetStringAttribute(buildElement, "name");
            string shortName = XMLUtil.GetStringAttribute(buildElement, "shortName");
            string pluginFolder = XMLUtil.GetStringAttribute(buildElement, "pluginFolder");
            string layoutsPath = XMLUtil.GetStringAttribute(buildElement, "filename");
            bool loadStrings = XMLUtil.GetBoolAttribute(buildElement, "loadStrings", true);
            string localeKey = XMLUtil.GetStringAttribute(buildElement, "localeKey", null);
            string stringidKey = XMLUtil.GetStringAttribute(buildElement, "stringidKey", null);
            string filenameKey = XMLUtil.GetStringAttribute(buildElement, "filenameKey", null);
            string localeSymbolsPath = XMLUtil.GetStringAttribute(buildElement, "localeSymbols", null);
            string scriptDefinitionsPath = XMLUtil.GetStringAttribute(buildElement, "scriptDefinitions", null);
            int segmentAlignment = XMLUtil.GetNumericAttribute(buildElement, "segmentAlignment", 0x1000);
            int headerSize = XMLUtil.GetNumericAttribute(buildElement, "headerSize");
            string stringidDefinitionsPath = XMLUtil.GetStringAttribute(buildElement, "stringidDefinitions", null);
            string vertexLayoutsPath = XMLUtil.GetStringAttribute(buildElement, "vertexLayouts", null);

            // Load structure layouts
            layoutsPath = Path.Combine(_basePath, layoutsPath);
            StructureLayoutCollection layouts = XMLLayoutLoader.LoadLayouts(layoutsPath);

            // StringID info
            IStringIDResolver stringIdResolver = null;
            if (stringidDefinitionsPath != null)
            {
                stringidDefinitionsPath = Path.Combine(_basePath, "StringIDs", stringidDefinitionsPath);
                stringIdResolver = StringIDSetLoader.LoadStringIDSets(stringidDefinitionsPath);
            }

            if (scriptDefinitionsPath != null)
                scriptDefinitionsPath = Path.Combine(_basePath, "Scripting", scriptDefinitionsPath);

            BuildInformation info = new BuildInformation(gameName, localeKey, stringidKey, stringIdResolver, filenameKey, headerSize, loadStrings, layouts, shortName, pluginFolder, scriptDefinitionsPath, segmentAlignment);

            if (localeSymbolsPath != null)
            {
                localeSymbolsPath = Path.Combine(_basePath, "LocaleSymbols", localeSymbolsPath);
                info.LocaleSymbols.AddSymbols(LocaleSymbolLoader.LoadLocaleSymbols(localeSymbolsPath));
            }

            if (vertexLayoutsPath != null)
            {
                vertexLayoutsPath = Path.Combine(_basePath, "Vertices", vertexLayoutsPath);
                info.VertexLayouts.AddLayouts(VertexLayoutLoader.LoadLayouts(vertexLayoutsPath));
            }

            return info;
        }
    }
}
