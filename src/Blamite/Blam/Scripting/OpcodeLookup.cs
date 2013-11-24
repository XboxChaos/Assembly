using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	public class OpcodeLookup
	{
		private readonly Dictionary<string, List<ScriptFunctionInfo>> _functionLookupByName =
			new Dictionary<string, List<ScriptFunctionInfo>>();

		private readonly Dictionary<ushort, ScriptFunctionInfo> _functionLookupByOpcode =
			new Dictionary<ushort, ScriptFunctionInfo>();

		private readonly Dictionary<ushort, string> _scriptTypeNameLookup = new Dictionary<ushort, string>();
		private readonly Dictionary<string, ushort> _scriptTypeOpcodeLookup = new Dictionary<string, ushort>();
		private readonly Dictionary<string, ScriptValueType> _typeLookupByName = new Dictionary<string, ScriptValueType>();
		private readonly Dictionary<ushort, ScriptValueType> _typeLookupByOpcode = new Dictionary<ushort, ScriptValueType>();

		public void RegisterScriptType(string name, ushort opcode)
		{
			_scriptTypeNameLookup[opcode] = name;
			_scriptTypeOpcodeLookup[name] = opcode;
		}

		public void RegisterValueType(ScriptValueType type)
		{
			_typeLookupByName[type.Name] = type;
			_typeLookupByOpcode[type.Opcode] = type;
		}

		public void RegisterFunction(ScriptFunctionInfo func)
		{
			_functionLookupByOpcode[func.Opcode] = func;

			List<ScriptFunctionInfo> functions;
			if (!_functionLookupByName.TryGetValue(func.Name, out functions))
			{
				functions = new List<ScriptFunctionInfo>();
				_functionLookupByName[func.Name] = functions;
			}
			functions.Add(func);
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