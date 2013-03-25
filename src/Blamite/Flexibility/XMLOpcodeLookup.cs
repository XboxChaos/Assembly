using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;
using Blamite.Blam.Scripting;
using Blamite.Util;

namespace Blamite.Flexibility
{
    public class XMLOpcodeLookup : IOpcodeLookup
    {
        private Dictionary<ushort, string> _scriptTypeNameLookup;
        private Dictionary<string, ushort> _scriptTypeOpcodeLookup;
        private Dictionary<ushort, ScriptValueType> _typeLookupByOpcode;
        private Dictionary<string, ScriptValueType> _typeLookupByName;
        private Dictionary<ushort, ScriptFunctionInfo> _functionLookupByOpcode = new Dictionary<ushort, ScriptFunctionInfo>();
        private Dictionary<string, List<ScriptFunctionInfo>> _functionLookupByName = new Dictionary<string, List<ScriptFunctionInfo>>();

        public XMLOpcodeLookup(XDocument document)
        {
            XContainer root = document.Element("BlamScript");

            // Script execution types
            var scriptTypes = from element in root.Element("scriptTypes").Descendants("type")
                              select new
                              {
                                  Opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode"),
                                  Name = XMLUtil.GetStringAttribute(element, "name")
                              };
            _scriptTypeNameLookup = scriptTypes.ToDictionary(t => t.Opcode, t => t.Name);
            _scriptTypeOpcodeLookup = scriptTypes.ToDictionary(t => t.Name, t => t.Opcode);

            // Value types
	        var valueTypes = new List<ScriptValueType>();
			foreach (var element in root.Element("valueTypes").Descendants("type"))
			{
				var name = XMLUtil.GetStringAttribute(element, "name");
				var opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode");
				var size = XMLUtil.GetNumericAttribute(element, "size");
				var quoted = XMLUtil.GetBoolAttribute(element, "quoted", false);
                var tag = XMLUtil.GetStringAttribute(element, "tag", null);

				valueTypes.Add(new ScriptValueType(name, opcode, size, quoted, tag));
			}
            _typeLookupByOpcode = valueTypes.ToDictionary(t => t.Opcode);
            _typeLookupByName = valueTypes.ToDictionary(t => t.Name);

            // Functions
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
                List<ScriptFunctionInfo> functions;
                if (!_functionLookupByName.TryGetValue(name, out functions))
                {
                    functions = new List<ScriptFunctionInfo>();
                    _functionLookupByName[name] = functions;
                }
                functions.Add(info);
                _functionLookupByOpcode[opcode] = info;
            }
        }

        public string GetScriptTypeName(ushort opcode)
        {
            string result;
            if (_scriptTypeNameLookup.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public ushort GetScriptTypeOpcode(string name)
        {
            ushort result;
            if (_scriptTypeOpcodeLookup.TryGetValue(name, out result))
                return result;
            return 0xFFFF;
        }

        public ScriptValueType GetTypeInfo(ushort opcode)
        {
            ScriptValueType result;
            if (_typeLookupByOpcode.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public ScriptValueType GetTypeInfo(string name)
        {
            ScriptValueType result;
            if (_typeLookupByName.TryGetValue(name, out result))
                return result;
            return null;
        }

        public ScriptFunctionInfo GetFunctionInfo(ushort opcode)
        {
            ScriptFunctionInfo result;
            if (_functionLookupByOpcode.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public List<ScriptFunctionInfo> GetFunctionInfo(string name)
        {
            List<ScriptFunctionInfo> result;
            if (_functionLookupByName.TryGetValue(name, out result))
                return result;
            return null;
        }
    }
}
