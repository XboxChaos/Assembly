using System.Diagnostics;

namespace Blamite.Blam.Scripting.Analysis
{
	/// <summary>
	///     BlamScript constant types.
	/// </summary>
	public enum ConstantType
	{
		/// <summary>
		///     No value
		/// </summary>
		None,

		/// <summary>
		///     Floating-point value
		/// </summary>
		Float,

		/// <summary>
		///     String value
		/// </summary>
		String,

		/// <summary>
		///     Boolean value
		/// </summary>
		Boolean
	}

	/// <summary>
	///     A script node representing a constant.
	/// </summary>
	[DebuggerDisplay("Constant {Type} {FloatValue} {StringValue} {BooleanValue}")]
	public class ConstantNode : IScriptNode
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ConstantNode" /> class.
		///     Its type will be set to <see cref="ConstantType.None" />.
		/// </summary>
		public ConstantNode()
		{
			Type = ConstantType.None;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ConstantNode" /> class.
		///     Its type will be set to <see cref="ConstantType.Float" />.
		/// </summary>
		/// <param name="floatValue">The floating-point value.</param>
		public ConstantNode(float floatValue)
		{
			Type = ConstantType.Float;
			FloatValue = floatValue;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ConstantNode" /> class.
		///     Its type will be set to <see cref="ConstantType.String" />.
		/// </summary>
		/// <param name="stringValue">The string value.</param>
		public ConstantNode(string stringValue)
		{
			Type = ConstantType.String;
			StringValue = stringValue;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ConstantNode" /> class.
		///     Its type will be set to <see cref="ConstantType.Boolean" />.
		/// </summary>
		/// <param name="booleanValue">The boolean value.</param>
		public ConstantNode(bool booleanValue)
		{
			Type = ConstantType.Boolean;
			BooleanValue = booleanValue;
		}

		/// <summary>
		///     Gets the type of the constant.
		/// </summary>
		/// <value>The type of the constant.</value>
		public ConstantType Type { get; private set; }

		/// <summary>
		///     Gets the float value of the constant.
		/// </summary>
		/// <value>The float value of the constant.</value>
		public float FloatValue { get; private set; }

		/// <summary>
		///     Gets the string value of the constant.
		/// </summary>
		/// <value>The string value of the constant.</value>
		public string StringValue { get; private set; }

		/// <summary>
		///     Gets the boolean value of the constant.
		/// </summary>
		/// <value>The boolean value of the constant.</value>
		public bool BooleanValue { get; private set; }

		public void Accept(IScriptNodeVisitor visitor)
		{
			visitor.VisitConstant(this);
		}
	}
}