using System.Collections.Generic;
using System.Linq;

namespace Blamite.Blam.Scripting
{
	public class OpcodeLookup
	{
		private readonly Dictionary<string, List<FunctionInfo>> _functionLookupByName =
			new Dictionary<string, List<FunctionInfo>>();

		private readonly Dictionary<ushort, FunctionInfo> _functionLookupByOpcode =
			new Dictionary<ushort, FunctionInfo>();

        private readonly Dictionary<string, GlobalInfo> _globalLookupByName =
            new Dictionary<string, GlobalInfo>();

        private readonly Dictionary<ushort, GlobalInfo> _globalLookupByOpcode =
            new Dictionary<ushort, GlobalInfo>();

        private readonly Dictionary<ushort, string> _scriptTypeNameLookup = new Dictionary<ushort, string>();
		private readonly Dictionary<string, ushort> _scriptTypeOpcodeLookup = new Dictionary<string, ushort>();
		private readonly Dictionary<string, ScriptValueType> _typeLookupByName = new Dictionary<string, ScriptValueType>();
		private readonly Dictionary<ushort, ScriptValueType> _typeLookupByOpcode = new Dictionary<ushort, ScriptValueType>();

        private readonly Dictionary<string, CastInfo> _typeCastLookup = new Dictionary<string, CastInfo>();

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

		public void RegisterFunction(FunctionInfo func)
		{
			_functionLookupByOpcode[func.Opcode] = func;

			List<FunctionInfo> functions;
			if (!_functionLookupByName.TryGetValue(func.Name, out functions))
			{
				functions = new List<FunctionInfo>();
				_functionLookupByName[func.Name] = functions;
			}
			functions.Add(func);
		}

        public void RegisterGlobal(GlobalInfo glo)
        {
            _globalLookupByOpcode[glo.Opcode] = glo;
            _globalLookupByName[glo.Name] = glo;
        }

        public void RegisterTypeCast(string to, CastInfo info)
        {
            _typeCastLookup[to] = info;
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

		public FunctionInfo GetFunctionInfo(ushort opcode)
		{
			FunctionInfo result;
			if (_functionLookupByOpcode.TryGetValue(opcode, out result))
				return result;
			return null;
		}

		public List<FunctionInfo> GetFunctionInfo(string name)
		{
			List<FunctionInfo> result;
			if (_functionLookupByName.TryGetValue(name, out result))
				return result;
			return null;
		}

        public GlobalInfo GetGlobalInfo(ushort opcode)
        {
            GlobalInfo result;
            if (_globalLookupByOpcode.TryGetValue(opcode, out result))
                return result;
            return null;
        }

        public GlobalInfo GetGlobalInfo(string name)
        {
            GlobalInfo result;
            if (_globalLookupByName.TryGetValue(name, out result))
                return result;
            return null;
        }

        public CastInfo GetTypeCast(string type)
        {
            CastInfo result;
            if (_typeCastLookup.TryGetValue(type, out result))
                return result;
            return null;
        }

		public IEnumerable<FunctionInfo> GetAllImplementedFunctions()
		{
			return _functionLookupByOpcode.Where(f => f.Value.Implemented).Select(f => f.Value);
		}

		public IEnumerable<GlobalInfo> GetAllImplementedGlobals()
		{
			return _globalLookupByOpcode.Where(f => f.Value.Implemented).Select(f => f.Value);
		}

		public IEnumerable<string> GetAllUniqueFunctionNames()
        {
			return _functionLookupByName.Keys;
        }

		public IEnumerable<string> GetAllUniqueGlobalNames()
		{
			return _globalLookupByName.Keys;
		}

		public IEnumerable<string> GetAllScriptTypeNames()
        {
			return _scriptTypeNameLookup.Values;
        }

		public IEnumerable<string> GetAllValueTypeNames()
		{
			return _typeLookupByName.Keys;
		}
	}
}