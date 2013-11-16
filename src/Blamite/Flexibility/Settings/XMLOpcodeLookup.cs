using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;
using Blamite.Blam.Scripting;
using Blamite.Util;

namespace Blamite.Flexibility.Settings
{
    /// <summary>
    /// Loads script opcode lookup data from XML files.
    /// </summary>
    public class XMLOpcodeLookupLoader : IComplexSettingLoader
    {
        /// <summary>
        /// Loads setting data from a path.
        /// </summary>
        /// <param name="path">The path to load from.</param>
        /// <returns>
        /// The loaded setting data.
        /// </returns>
        public object LoadSetting(string path)
        {
            var document = XDocument.Load(path);
            var result = new OpcodeLookup();
            var root = document.Element("BlamScript");

            RegisterExecutionTypes(root, result);
            RegisterValueTypes(root, result);
            RegisterFunctions(root, result);

            return result;
        }

        private void RegisterExecutionTypes(XContainer root, OpcodeLookup lookup)
        {
            foreach (var element in root.Element("scriptTypes").Descendants("type"))
            {
                var opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode");
                var name = XMLUtil.GetStringAttribute(element, "name");
                lookup.RegisterScriptType(name, opcode);
            }
        }

        private void RegisterValueTypes(XContainer root, OpcodeLookup lookup)
        {
            foreach (var element in root.Element("valueTypes").Descendants("type"))
            {
                var name = XMLUtil.GetStringAttribute(element, "name");
                var opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode");
                var size = XMLUtil.GetNumericAttribute(element, "size");
                var quoted = XMLUtil.GetBoolAttribute(element, "quoted", false);
                var tag = XMLUtil.GetStringAttribute(element, "tag", null);
                var valueType = new ScriptValueType(name, opcode, size, quoted, tag);
                lookup.RegisterValueType(valueType);
            }
        }

        private void RegisterFunctions(XContainer root, OpcodeLookup lookup)
        {
            foreach (var element in root.Element("functions").Descendants("function"))
            {
                var name = XMLUtil.GetStringAttribute(element, "name");
                if (name == "")
                    continue;

                var opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode");
                var returnType = XMLUtil.GetStringAttribute(element, "returnType", "void");
                var flags = (uint)XMLUtil.GetNumericAttribute(element, "flags", 0);
                var parameterTypes = element.Descendants("arg").Select(e => XMLUtil.GetStringAttribute(e, "type")).ToArray();

                var info = new ScriptFunctionInfo(name, opcode, returnType, flags, parameterTypes);
                lookup.RegisterFunction(info);
            }
        }
    }
}
