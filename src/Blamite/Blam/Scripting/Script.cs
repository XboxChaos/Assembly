using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
    /// <summary>
    /// A script.
    /// </summary>
    public class Script
    {
        public Script()
        {
            Parameters = new List<ScriptParameter>();
        }

        internal Script(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs, ScriptExpressionTable expressions, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, stringIDs, expressions, buildInfo);
        }

        /// <summary>
        /// Gets or sets the script's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets a list of parameters that the script accepts.
        /// </summary>
        public IList<ScriptParameter> Parameters { get; private set; }

        /// <summary>
        /// Gets or sets the execution type opcode (static, startup, dormant, etc.) of the script.
        /// </summary>
        public short ExecutionType { get; set; }

        /// <summary>
        /// Gets or sets the script's return type.
        /// </summary>
        public short ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the first expression to execute in the script.
        /// </summary>
        public ScriptExpression RootExpression { get; set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs, ScriptExpressionTable expressions, BuildInformation buildInfo)
        {
            Name = values.HasInteger("name index") ? stringIDs.GetString(new StringID(values.GetInteger("name index"))) : values.GetString("name");
            ExecutionType = (short)values.GetInteger("execution type");
            ReturnType = (short)values.GetInteger("return type");
            DatumIndex rootExpr = new DatumIndex(values.GetInteger("first expression index"));
            if (rootExpr.IsValid)
                RootExpression = expressions.FindExpression(rootExpr);
            if (Name == null)
                Name = "script_" + rootExpr.Value.ToString("X8");

            Parameters = LoadParameters(reader, values, metaArea, buildInfo);
        }

        private IList<ScriptParameter> LoadParameters(IReader reader, StructureValueCollection values, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            int count = (int)values.GetInteger("number of parameters");
            uint address = values.GetInteger("address of parameter list");
            var layout = buildInfo.GetLayout("script parameter entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);
            return entries.Select(e => new ScriptParameter(e)).ToList();
        }
    }
}
