using Blamite.Blam.Scripting.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting
{
    public class GlobalInfo : IScriptingConstantInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalInfo" /> class.
        /// </summary>
        /// <param name="name">The name of the global.</param>
        /// <param name="opcode">The opcode of the global.</param>
        /// <param name="returnType">The return type of the global.</param>
        /// <param name="implemented">Whether this global is implemented in this game version or not.</param>
        public GlobalInfo(string name, ushort opcode, string returnType, bool implemented)
        {
            Name = name.ToLowerInvariant();
            ReturnType = returnType;
            Opcode = opcode;
            Implemented = implemented;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalInfo" /> class. Used for map globals.
        /// </summary>
        /// <param name="context">The parser rule context of the global.</param>
        /// <param name="index">The index of the map global.</param>
        public GlobalInfo(BS_ReachParser.GlobalDeclarationContext context, ushort index)
        {
            Name = context.ID().GetTextSanitized();
            ReturnType = context.VALUETYPE().GetTextSanitized();
            Opcode = index;
            Implemented = true;
        }

        /// <summary>
        ///     Gets the name of the global.
        /// </summary>
        /// <value>The name of the function.</value>
        public string Name { get; private set; }

        /// <summary>
        ///     Gets the return type of the global.
        /// </summary>
        /// <value>The return type of the function.</value>
        public string ReturnType { get; private set; }

        /// <summary>
        ///     Gets the opcode of the global.
        /// </summary>
        /// <value>The opcode of the function.</value>
        public ushort Opcode { get; private set; }

        /// <summary>
        ///     Gets the masked opcode of the global.
        /// </summary>
        /// <value>The opcode of the function.</value>
        public ushort MaskedOpcode { get => (ushort)(Opcode | 0x8000); }

        /// <summary>
        ///		Is true if the global exists in this game version and is implemented.
        /// </summary>
        public bool Implemented { get; private set; }
    }
}
