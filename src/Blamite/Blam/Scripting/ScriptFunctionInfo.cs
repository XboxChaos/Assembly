namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     Contains information about a built-in scripting function.
	/// </summary>
	public class ScriptFunctionInfo
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ScriptFunctionInfo" /> class.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		/// <param name="opcode">The opcode of the function.</param>
		/// <param name="returnType">The return type of the function.</param>
		/// <param name="flags">The flags for the function.</param>
		/// <param name="parameterTypes">The parameter types for the function.</param>
		public ScriptFunctionInfo(string name, ushort opcode, string returnType, uint flags, string[] parameterTypes)
		{
			Name = name;
			ReturnType = returnType;
			Opcode = opcode;
			Flags = flags;
			ParameterTypes = parameterTypes;
		}

		/// <summary>
		///     Gets the name of the function.
		/// </summary>
		/// <value>The name of the function.</value>
		public string Name { get; private set; }

		/// <summary>
		///     Gets the return type of the function.
		/// </summary>
		/// <value>The return type of the function.</value>
		public string ReturnType { get; private set; }

		/// <summary>
		///     Gets the opcode of the function.
		/// </summary>
		/// <value>The opcode of the function.</value>
		public ushort Opcode { get; private set; }

		/// <summary>
		///     Gets the flags for the function.
		/// </summary>
		/// <value>The flags for the function.</value>
		public uint Flags { get; private set; }

		/// <summary>
		///     Gets the parameter types for the function.
		/// </summary>
		/// <value>The parameter types for the function.</value>
		public string[] ParameterTypes { get; private set; }
	}
}