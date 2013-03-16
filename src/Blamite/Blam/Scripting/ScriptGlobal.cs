using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Flexibility;

namespace Blamite.Blam.Scripting
{
    /// <summary>
    /// A global variable in a script file.
    /// </summary>
    public class ScriptGlobal
    {
        public ScriptGlobal()
        {
        }

        internal ScriptGlobal(StructureValueCollection values, ScriptExpressionTable allExpressions)
        {
            Load(values, allExpressions);
        }

        /// <summary>
        /// Gets or sets the name of the global variable.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the global variable.
        /// </summary>
        public short Type { get; set; }

        /// <summary>
        /// Gets or sets the expression which determines the variable's default value.
        /// </summary>
        public ScriptExpression Expression { get; set; }

        private void Load(StructureValueCollection values, ScriptExpressionTable allExpressions)
        {
            Name = values.GetString("name");
            Type = (short)values.GetInteger("type");

            DatumIndex valueIndex = new DatumIndex(values.GetInteger("expression index"));
            if (valueIndex.IsValid)
                Expression = allExpressions.FindExpression(valueIndex);
        }
    }
}
