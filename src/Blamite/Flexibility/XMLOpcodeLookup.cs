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
        private Dictionary<ushort, ScriptValueType> _typeLookup;
        private Dictionary<ushort, string> _functionLookup;
        private Dictionary<short, string> _classLookup;

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

            // Object types
            var objectTypes = from element in root.Element("objectTypes").Descendants("type")
                              select new
                              {
                                  Opcode = (short)XMLUtil.GetNumericAttribute(element, "opcode"),
                                  Name = XMLUtil.GetStringAttribute(element, "name")
                              };
            _classLookup = objectTypes.ToDictionary(t => t.Opcode, t => t.Name);

            // Value types
	        var valueTypes = new List<ScriptValueType>();
			foreach (var element in root.Element("valueTypes").Descendants("type"))
			{
				var name = XMLUtil.GetStringAttribute(element, "name");
				var opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode");
				var size = XMLUtil.GetNumericAttribute(element, "size");
				var quoted = XMLUtil.GetBoolAttribute(element, "quoted", false);

				valueTypes.Add(new ScriptValueType(name, opcode, size, quoted));
			}
            _typeLookup = valueTypes.ToDictionary(t => t.Opcode);

            // Functions
            var functions = from element in root.Element("functions").Descendants("function")
                            select new
                            {
                                Opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode"),
                                Name = XMLUtil.GetStringAttribute(element, "name")
                            };
            _functionLookup = functions.ToDictionary(f => f.Opcode, f => f.Name);
        }

        public string GetScriptTypeName(ushort opcode)
        {
            string result;
            if (_scriptTypeNameLookup.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public ScriptValueType GetTypeInfo(ushort opcode)
        {
            ScriptValueType result;
            if (_typeLookup.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public string GetFunctionName(ushort opcode)
        {
            string result;
            if (_functionLookup.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public string GetTagClassName(short opcode)
        {
            string result;
            if (opcode == -1)
                return "none";
            if (_classLookup.TryGetValue(opcode, out result))
                return result;
            return null;
        }
    }
}
