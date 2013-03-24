using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Analysis
{
    /// <summary>
    /// A script node representing a global object definition.
    /// </summary>
    [DebuggerDisplay("Object {TagClass} {Name} {PaletteIndex}")]
    public class ObjectDefinitionNode : IScriptNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDefinitionNode"/> class.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <param name="tagClass">The tag class of the object.</param>
        /// <param name="paletteIndex">The index of the object in its class's palette.</param>
        public ObjectDefinitionNode(string name, string tagClass, int paletteIndex)
        {
            Name = name;
            TagClass = tagClass;
            PaletteIndex = paletteIndex;
        }

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        /// <value>The name of the object.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the tag class of the object.
        /// </summary>
        /// <value>The tag class of the object.</value>
        public string TagClass { get; private set; }

        /// <summary>
        /// Gets the index of the object in its class's palette.
        /// </summary>
        /// <value>The index of the object in its class's palette.</value>
        public int PaletteIndex { get; private set; }

        public void Accept(IScriptNodeVisitor visitor)
        {
            visitor.VisitObjectDefinition(this);
        }
    }
}
