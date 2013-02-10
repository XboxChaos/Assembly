using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen.Scripting
{
    public class ThirdGenScript : IScript
    {
        public ThirdGenScript(IReader reader, StructureValueCollection values, MetaAddressConverter addrConverter, IStringIDSource stringIDs, ExpressionTable expressions, BuildInformation buildInfo)
        {
            Load(reader, values, addrConverter, stringIDs, expressions, buildInfo);
        }

        public string Name { get; private set; }
        public IList<IScriptParameter> Parameters { get; private set; }
        public short ExecutionType { get; private set; }
        public short ReturnType { get; private set; }
        public IExpression RootExpression { get; private set; }

        private void Load(IReader reader, StructureValueCollection values, MetaAddressConverter addrConverter, IStringIDSource stringIDs, ExpressionTable expressions, BuildInformation buildInfo)
        {
            Name = stringIDs.GetString(new StringID((int)values.GetNumber("name index")));
            ExecutionType = (short)values.GetNumber("execution type");
            ReturnType = (short)values.GetNumber("return type");
            DatumIndex rootExpr = new DatumIndex(values.GetNumber("first expression index"));
            if (rootExpr.IsValid)
                RootExpression = expressions.FindExpression(rootExpr);
            if (Name == null)
                Name = "script_" + rootExpr.Value.ToString("X8");

            Parameters = LoadParameters(reader, values, addrConverter, buildInfo);
        }

        private IList<IScriptParameter> LoadParameters(IReader reader, StructureValueCollection values, MetaAddressConverter addrConverter, BuildInformation buildInfo)
        {
            int paramCount = (int)values.GetNumber("number of parameters");
            Pointer paramListLocation = new Pointer(values.GetNumber("address of parameter list"), addrConverter);

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
