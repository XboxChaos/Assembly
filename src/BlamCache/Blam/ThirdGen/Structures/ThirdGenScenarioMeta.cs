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
        public ThirdGenScenarioMeta(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, IStringIDSource stringIDs, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, stringIDs, buildInfo);
        }

        public SegmentPointer ScriptExpressionsLocation { get; set; }
        public SegmentPointer ScriptGlobalsLocation { get; set; }
        public SegmentPointer ScriptObjectsLocation { get; set; }
        public SegmentPointer ScriptsLocation { get; set; }

        public ExpressionTable ScriptExpressions { get; private set; }
        public List<IGlobal> ScriptGlobals { get; private set; }
        public List<IGlobalObject> ScriptObjects { get; private set; }
        public List<IScript> Scripts { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, IStringIDSource stringIDs, BuildInformation buildInfo)
        {
            StringTableReader stringReader = new StringTableReader();
            ScriptExpressions = LoadScriptExpressions(values, reader, metaArea, stringReader, buildInfo.GetLayout("script expression entry"));
            ScriptObjects = LoadScriptObjects(values, reader, metaArea, stringIDs, buildInfo.GetLayout("script object entry"));
            ScriptGlobals = LoadScriptGlobals(values, reader, metaArea, ScriptExpressions, buildInfo.GetLayout("script global entry"));
            Scripts = LoadScripts(values, reader, metaArea, stringIDs, ScriptExpressions, buildInfo.GetLayout("script entry"), buildInfo);

            CachedStringTable strings = LoadStrings(values, reader, stringReader, metaArea);
            foreach (IExpression expr in ScriptExpressions)
            {
                // FIXME: hax
                if (expr != null)
                    ((ThirdGenExpression)expr).ResolveStrings(strings);
            }
        }

        private ExpressionTable LoadScriptExpressions(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringTableReader stringReader, StructureLayout entryLayout)
        {
            int exprCount = (int)values.GetInteger("number of script expressions");
            if (exprCount == 0)
                return new ExpressionTable();

            ScriptExpressionsLocation = SegmentPointer.FromPointer(values.GetInteger("script expression table address"), metaArea);

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

        private List<IGlobalObject> LoadScriptObjects(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, IStringIDSource stringIDs, StructureLayout entryLayout)
        {
            int objectsCount = (int)values.GetInteger("number of script objects");
            if (objectsCount == 0)
                return new List<IGlobalObject>();

            ScriptObjectsLocation = SegmentPointer.FromPointer(values.GetInteger("script object table address"), metaArea);

            List<IGlobalObject> result = new List<IGlobalObject>();
            reader.SeekTo(ScriptObjectsLocation.AsOffset());
            for (int i = 0; i < objectsCount; i++)
            {
                StructureValueCollection objValues = StructureReader.ReadStructure(reader, entryLayout);
                result.Add(new ThirdGenGlobalObject(objValues, stringIDs));
            }

            return result;
        }

        private List<IGlobal> LoadScriptGlobals(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, ExpressionTable expressions, StructureLayout entryLayout)
        {
            int globalsCount = (int)values.GetInteger("number of script globals");
            if (globalsCount == 0)
                return new List<IGlobal>();

            ScriptGlobalsLocation = SegmentPointer.FromPointer(values.GetInteger("script global table address"), metaArea);

            List<IGlobal> result = new List<IGlobal>();
            reader.SeekTo(ScriptGlobalsLocation.AsOffset());
            for (int i = 0; i < globalsCount; i++)
            {
                StructureValueCollection globalValues = StructureReader.ReadStructure(reader, entryLayout);
                result.Add(new ThirdGenGlobal(globalValues, expressions));
            }
            return result;
        }

        private List<IScript> LoadScripts(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, IStringIDSource stringIDs, ExpressionTable expressions, StructureLayout entryLayout, BuildInformation buildInfo)
        {
            int scriptCount = (int)values.GetInteger("number of scripts");
            if (scriptCount == 0)
                return new List<IScript>();

            ScriptsLocation = SegmentPointer.FromPointer(values.GetInteger("script table address"), metaArea);

            // Read all of the script entries first, then go back and create the objects
            // ThirdGenScript reads parameters from its constructor - this may or may not need cleaning up to make this more obvious
            reader.SeekTo(ScriptsLocation.AsOffset());
            List<StructureValueCollection> scriptData = new List<StructureValueCollection>();
            for (int i = 0; i < scriptCount; i++)
                scriptData.Add(StructureReader.ReadStructure(reader, entryLayout));

            List<IScript> result = new List<IScript>();
            foreach (StructureValueCollection scriptValues in scriptData)
                result.Add(new ThirdGenScript(reader, scriptValues, metaArea, stringIDs, expressions, buildInfo));
            return result;
        }

        private CachedStringTable LoadStrings(StructureValueCollection values, IReader reader, StringTableReader stringReader, FileSegmentGroup metaArea)
        {
            int stringsSize = (int)values.GetInteger("script string table size");
            if (stringsSize == 0)
                return new CachedStringTable();

            SegmentPointer stringsLocation = SegmentPointer.FromPointer(values.GetInteger("script string table address"), metaArea);

            CachedStringTable result = new CachedStringTable();
            stringReader.ReadRequestedStrings(reader, stringsLocation, result);
            return result;
        }
    }
}
