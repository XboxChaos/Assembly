using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Flexibility;

namespace Blamite.Blam.Scripting
{
    /// <summary>
    /// A reference to a scenario object in a script file.
    /// </summary>
    public class ScriptObjectReference
    {
        public ScriptObjectReference()
        {
        }

        internal ScriptObjectReference(StructureValueCollection values, StringIDSource stringIDs)
        {
            Load(values, stringIDs);
        }

        /// <summary>
        /// Gets or sets the name of the object variable.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ID of the object's tag class. Can be -1.
        /// </summary>
        public short Class { get; set; }

        /// <summary>
        /// Gets or sets the index of the object in the class's object palette. Can be -1.
        /// </summary>
        public short PlacementIndex { get; set; }

        private void Load(StructureValueCollection values, StringIDSource stringIDs)
        {
            Name = values.HasInteger("name index") ? stringIDs.GetString(new StringID(values.GetInteger("name index"))) : values.GetString("name");
            Class = (short)values.GetInteger("type");
            PlacementIndex = (short)values.GetInteger("placement index");
        }
    }
}
