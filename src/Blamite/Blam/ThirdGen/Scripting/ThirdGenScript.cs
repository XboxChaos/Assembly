using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Scripting
{
    public class ThirdGenScript : IScript
    {
        public ThirdGenScript(IReader reader, StructureValueCollection values, FileSegmentGroup metaArea, StringIDSource stringIDs, ExpressionTable expressions, BuildInformation buildInfo)
        {
            Load(reader, values, metaArea, stringIDs, expressions, buildInfo);
        }

        public string Name { get; private set; }
        public IList<IScriptParameter> Parameters { get; private set; }
        public short ExecutionType { get; private set; }
        public short ReturnType { get; private set; }
        public IExpression RootExpression { get; private set; }

        private void Load(IReader reader, StructureValueCollection values, FileSegmentGroup metaArea, StringIDSource stringIDs, ExpressionTable expressions, BuildInformation buildInfo)
        {
            Name = stringIDs.GetString(new StringID((int)values.GetInteger("name index")));
            ExecutionType = (short)values.GetInteger("execution type");
            ReturnType = (short)values.GetInteger("return type");
            DatumIndex rootExpr = new DatumIndex(values.GetInteger("first expression index"));
            if (rootExpr.IsValid)
                RootExpression = expressions.FindExpression(rootExpr);
            if (Name == null)
                Name = "script_" + rootExpr.Value.ToString("X8");

            Parameters = LoadParameters(reader, values, metaArea, buildInfo);
        }

        private IList<IScriptParameter> LoadParameters(IReader reader, StructureValueCollection values, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int paramCount = (int)values.GetInteger("number of parameters");
            if (paramCount == 0)
                return new List<IScriptParameter>();

            SegmentPointer paramListLocation = SegmentPointer.FromPointer(values.GetInteger("address of parameter list"), metaArea);

            StructureLayout layout = buildInfo.GetLayout("script parameter entry");
            List<IScriptParameter> result = new List<IScriptParameter>();
            
            reader.SeekTo(paramListLocation.AsOffset());
            for (int i = 0; i < paramCount; i++)
            {
                StructureValueCollection paramValues = StructureReader.ReadStructure(reader, layout);
                result.Add(new ThirdGenScriptParameter(paramValues));
            }
            return result;
        }
    }
}
