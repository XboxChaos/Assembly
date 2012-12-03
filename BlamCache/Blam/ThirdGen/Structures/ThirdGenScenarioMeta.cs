using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Blam.ThirdGen.Scripting;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenScenarioMeta : IScenario
    {
        public ThirdGenScenarioMeta(StructureValueCollection values, IReader reader, MetaAddressConverter addrConverter, IStringIDSource stringIDs, BuildInformation buildInfo)
        {
            Load(values, reader, addrConverter, stringIDs, buildInfo);
        }

        public Pointer ScriptExpressionsLocation { get; set; }
        public Pointer ScriptGlobalsLocation { get; set; }
        public Pointer ScriptObjectsLocation { get; set; }
        public Pointer ScriptsLocation { get; set; }

        public ExpressionTable ScriptExpressions { get; private set; }
        public List<IGlobal> ScriptGlobals { get; private set; }
        public List<IGlobalObject> ScriptObjects { get; private set; }
        public List<IScript> Scripts { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, MetaAddressConverter addrConverter, IStringIDSource stringIDs, BuildInformation buildInfo)
        {
            StringTableReader stringReader = new StringTableReader();
            ScriptExpressions = LoadScriptExpressions(values, reader, addrConverter, stringReader, buildInfo.GetLayout("script expression entry"));
            ScriptObjects = LoadScriptObjects(values, reader, addrConverter, stringIDs, buildInfo.GetLayout("script object entry"));
            ScriptGlobals = LoadScriptGlobals(values, reader, addrConverter, ScriptExpressions, buildInfo.GetLayout("script global entry"));
            Scripts = LoadScripts(values, reader, addrConverter, stringIDs, ScriptExpressions, buildInfo.GetLayout("script entry"), buildInfo);
            
            CachedStringTable strings = LoadStrings(values, reader, stringReader, addrConverter);
            foreach (IExpression expr in ScriptExpressions)
            {
                // FIXME: hax
                if (expr != null)
                    ((ThirdGenExpression)expr).ResolveStrings(strings);
            }
        }

        private ExpressionTable LoadScriptExpressions(StructureValueCollection values, IReader reader, MetaAddressConverter addrConverter, StringTableReader stringReader, StructureLayout entryLayout)
        {
            int exprCount = (int)values.GetNumber("number of script expressions");
            ScriptExpressionsLocation = new Pointer(values.GetNumber("script expression table address"), addrConverter);

            ExpressionTable result = new ExpressionTable();
            reader.SeekTo(ScriptExpressionsLocation.AsOffset());
            for (int i = 0; i < exprCount; i++)
            {
                StructureValueCollection exprValues = StructureReader.ReadStructure(reader, entryLayout);
                result.AddExpression(new ThirdGenExpression(exprValues, (ushort)i, stringReader));
            }

            foreach (IExpression expr in result)
            {
                // FIXME: hax
                if (expr != null)
                    ((ThirdGenExpression)expr).ResolveReferences(result);
            }
            return result;
        }

        private List<IGlobalObject> LoadScriptObjects(StructureValueCollection values, IReader reader, MetaAddressConverter addrConverter, IStringIDSource stringIDs, StructureLayout entryLayout)
        {
            int objectsCount = (int)values.GetNumber("number of script objects");
            ScriptObjectsLocation = new Pointer(values.GetNumber("script object table address"), addrConverter);

            List<IGlobalObject> result = new List<IGlobalObject>();
            reader.SeekTo(ScriptObjectsLocation.AsOffset());
            for (int i = 0; i < objectsCount; i++)
            {
                StructureValueCollection objValues = StructureReader.ReadStructure(reader, entryLayout);
                result.Add(new ThirdGenGlobalObject(objValues, stringIDs));
            }
            return result;
        }

        private List<IGlobal> LoadScriptGlobals(StructureValueCollection values, IReader reader, MetaAddressConverter addrConverter, ExpressionTable expressions, StructureLayout entryLayout)
        {
            int globalsCount = (int)values.GetNumber("number of script globals");
            ScriptGlobalsLocation = new Pointer(values.GetNumber("script global table address"), addrConverter);

            List<IGlobal> result = new List<IGlobal>();
            reader.SeekTo(ScriptGlobalsLocation.AsOffset());
            for (int i = 0; i < globalsCount; i++)
            {
                StructureValueCollection globalValues = StructureReader.ReadStructure(reader, entryLayout);
                result.Add(new ThirdGenGlobal(globalValues, expressions));
            }
            return result;
        }

        private List<IScript> LoadScripts(StructureValueCollection values, IReader reader, MetaAddressConverter addrConverter, IStringIDSource stringIDs, ExpressionTable expressions, StructureLayout entryLayout, BuildInformation buildInfo)
        {
            int script = (int)values.GetNumber("number of scripts");
            ScriptsLocation = new Pointer(values.GetNumber("script table address"), addrConverter);

            // Read all of the script entries first, then go back and create the objects
            // ThirdGenScript reads parameters from its constructor - this may or may not need cleaning up to make this more obvious
            reader.SeekTo(ScriptsLocation.AsOffset());
            List<StructureValueCollection> scriptData = new List<StructureValueCollection>();
            for (int i = 0; i < script; i++)
                scriptData.Add(StructureReader.ReadStructure(reader, entryLayout));

            List<IScript> result = new List<IScript>();
            foreach (StructureValueCollection scriptValues in scriptData)
                result.Add(new ThirdGenScript(reader, scriptValues, addrConverter, stringIDs, expressions, buildInfo));
            return result;
        }

        private CachedStringTable LoadStrings(StructureValueCollection values, IReader reader, StringTableReader stringReader, MetaAddressConverter addrConverter)
        {
            int stringsSize = (int)values.GetNumber("script string table size");
            Pointer stringsLocation = new Pointer(values.GetNumber("script string table address"), addrConverter);

            CachedStringTable result = new CachedStringTable();
            stringReader.ReadRequestedStrings(reader, stringsLocation, result);
            return result;
        }
    }
}
