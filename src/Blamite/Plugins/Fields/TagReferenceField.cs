using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	/// <summary>
	/// A reference to another tag in the cache file.
	/// </summary>
	public class TagReferenceField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TagReferenceField" /> class.
		/// </summary>
		/// <param name="includesClass">If set to <c>false</c>, then the tag reference is just a datum index.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public TagReferenceField(bool includesClass, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			IncludesClass = includesClass;
		}

		/// <summary>
		/// Gets a value indicating whether or not the reference includes tag class information.
		/// </summary>
		public bool IncludesClass { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
